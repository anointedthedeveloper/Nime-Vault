using NimeVault.Models;
using NimeVault.Services.AnimePahe;
using NimeVault.Services.Interfaces;

// Usage: dotnet run --project tools/ProviderCheck -- "naruto"
// Walks the AnimePahe flow step by step and prints what each step returned.
var words = args.Where(a => !a.StartsWith("--")).ToArray();
var query = words.Length > 0 ? string.Join(' ', words) : "naruto";
var ct = CancellationToken.None;
var client = new AnimePaheClient();
var details = new AnimePaheDetailsService(client);
var search = new AnimePaheSearchService(client, details);
var kwik = new KwikResolver(client);

async Task Step(string name, Func<Task> body)
{
    Console.WriteLine($"\n=== {name}");
    try { await body(); Console.WriteLine("OK"); }
    catch (Exception ex) { Console.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}"); Environment.ExitCode = 1; }
}

NimeVault.Models.Anime? pick = null;
List<NimeVault.Models.Episode> eps = new();

await Step($"Host resolve", async () => Console.WriteLine(await client.GetBaseUrlAsync(ct)));

await Step($"Search '{query}'", async () =>
{
    var r = await search.SearchAsync(query, ct);
    foreach (var a in r.Take(5)) Console.WriteLine($"  {a.Title} | {a.Year} | {a.TotalEpisodes} eps | {a.Id} | {a.PosterUrl}");
    Console.WriteLine($"  ({r.Count} results)");
    pick = r.FirstOrDefault() ?? throw new Exception("no results");
});
if (pick == null) return;

await Step("Details", async () =>
{
    var d = await details.GetDetailsAsync(pick.Id, ct);
    Console.WriteLine($"  title: {d?.Title}\n  jp: {d?.AlternativeTitle}\n  status: {d?.Status}  year: {d?.Year}  eps: {d?.TotalEpisodes}");
    Console.WriteLine($"  genres: {string.Join(", ", d?.Genres ?? new())}");
    Console.WriteLine($"  poster: {d?.PosterUrl}\n  synopsis: {(d?.Description.Length > 120 ? d!.Description[..120] + "..." : d?.Description)}");
});

await Step("Episodes", async () =>
{
    eps = await details.GetEpisodesAsync(pick.Id, ct);
    foreach (var e in eps.Take(3)) Console.WriteLine($"  E{e.Number} {e.Title} [{e.Duration}] {e.Id}");
    Console.WriteLine($"  ({eps.Count} episodes)");
    if (eps.Count == 0) throw new Exception("no episodes");
});
if (eps.Count == 0) return;

await Step("Home feeds (recent / popular / featured)", async () =>
{
    var recent = await search.GetRecentlyAddedAsync(ct);
    var popular = await search.GetPopularAsync(ct);
    var featured = await search.GetFeaturedAsync(ct);
    foreach (var a in recent) Console.WriteLine($"  recent: {a.Title} | poster {a.PosterUrl}");
    Console.WriteLine($"  popular: {popular.Count}, featured: {featured?.Title} ({featured?.Description.Length} chars)");
    if (recent.Count == 0 || featured == null) throw new Exception("empty home feed");
});

DownloadOption? choice = null;
await Step("Download options (play page)", async () =>
{
    var opts = await kwik.GetOptionsAsync(pick.Id, eps[0].Id, ct);
    foreach (var o in opts) Console.WriteLine($"  {o.Resolution}p dub={o.IsDub} {o.SizeMb}MB | {o.Label} | {o.PaheUrl}");
    choice = KwikResolver.Pick(opts, false) ?? throw new Exception("no options");
});
if (choice == null) return;

await Step("Resolve direct mp4 link (pahe.win -> kwik)", async () =>
{
    var url = await kwik.ResolveDirectUrlAsync(choice, ct);
    Console.WriteLine($"  {url}");
    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    req.Headers.Referrer = new Uri("https://kwik.si/");
    req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 1023);
    using var res = await client.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    Console.WriteLine($"  range probe: HTTP {(int)res.StatusCode}, type {res.Content.Headers.ContentType}, range {res.Content.Headers.ContentRange}");
    res.EnsureSuccessStatusCode();
});

// ---- Download tests (real AnimePaheDownloadService) ----
if (args.Contains("--no-download")) goto done;
var dir = Path.Combine(Path.GetTempPath(), "providercheck-" + Guid.NewGuid().ToString("N")[..6]);
var settings = new StubSettings(dir);
var dl = new AnimePaheDownloadService(client, kwik, settings);

DownloadItem NewItem(string lang) => new()
{
    AnimeId = pick!.Id, EpisodeId = eps[0].Id, AnimeTitle = pick.Title, EpisodeNumber = 1, Language = lang
};
static bool PatternOk(string path)
{
    using var f = File.OpenRead(path);
    var buf = new byte[65536]; long pos = 0; int n;
    while ((n = f.Read(buf, 0, buf.Length)) > 0)
    {
        for (int i = 0; i < n; i++) if (buf[i] != (byte)(((pos + i) * 31 + 7) & 255)) return false;
        pos += n;
    }
    return true;
}

await Step("Download SUB (720p) to disk", async () =>
{
    var item = NewItem("SUB"); int updates = 0;
    dl.DownloadProgressChanged += (_, i) => { if (i.Id == item.Id) updates++; };
    await dl.StartDownloadAsync(item);
    Console.WriteLine($"  status={item.Status} bytes={item.DownloadedBytes}/{item.TotalBytes} progressEvents={updates} path={item.SavePath}");
    if (item.Status != DownloadStatus.Completed) throw new Exception("not completed: " + item.ErrorMessage);
    if (new FileInfo(item.SavePath!).Length != item.TotalBytes) throw new Exception("size mismatch");
    if (!PatternOk(item.SavePath!)) throw new Exception("file content corrupted");
    if (File.Exists(item.SavePath + ".part")) throw new Exception(".part left behind");
});

await Step("Pause then resume mid-download", async () =>
{
    var item = NewItem("SUB"); item.EpisodeNumber = 2; item.EpisodeId = eps[1].Id;
    var task = dl.StartDownloadAsync(item);
    while (item.Progress < 20 && !task.IsCompleted) await Task.Delay(20);
    await dl.PauseDownloadAsync(item.Id);
    await Task.Delay(600);
    var at = item.DownloadedBytes; await Task.Delay(800);
    Console.WriteLine($"  paused at {at} bytes; 800ms later {item.DownloadedBytes}");
    if (item.DownloadedBytes - at > 70000) throw new Exception("kept downloading while paused");
    await dl.ResumeDownloadAsync(item.Id);
    await task;
    Console.WriteLine($"  status={item.Status} bytes={item.DownloadedBytes}/{item.TotalBytes}");
    if (item.Status != DownloadStatus.Completed || !PatternOk(item.SavePath!)) throw new Exception("bad result: " + item.ErrorMessage);
});

await Step("Cancel mid-download removes .part", async () =>
{
    var item = NewItem("SUB"); item.EpisodeNumber = 3; item.EpisodeId = eps[2].Id;
    var task = dl.StartDownloadAsync(item);
    while (item.Progress < 15 && !task.IsCompleted) await Task.Delay(20);
    await dl.CancelDownloadAsync(item.Id);
    await task;
    Console.WriteLine($"  status={item.Status}");
    if (item.Status != DownloadStatus.Cancelled) throw new Exception("not cancelled");
    if (File.Exists(item.SavePath + ".part") || File.Exists(item.SavePath)) throw new Exception("files left behind");
});

await Step("Dropped connection fails, retry resumes from .part (DUB)", async () =>
{
    var item = NewItem("DUB"); item.EpisodeNumber = 4; item.EpisodeId = eps[3].Id;
    await dl.StartDownloadAsync(item);
    var part = item.SavePath + ".part";
    Console.WriteLine($"  1st attempt: status={item.Status} err={item.ErrorMessage} part={(File.Exists(part) ? new FileInfo(part).Length : -1)}");
    if (item.Status != DownloadStatus.Failed) throw new Exception("expected a failure on the flaky file");
    long partial = new FileInfo(part).Length;
    await dl.StartDownloadAsync(item);
    Console.WriteLine($"  2nd attempt: status={item.Status} bytes={item.DownloadedBytes}/{item.TotalBytes} (resumed from {partial})");
    if (item.Status != DownloadStatus.Completed || !PatternOk(item.SavePath!)) throw new Exception("resume failed: " + item.ErrorMessage);
});

Console.WriteLine($"\n  files in {dir}:");
foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)) Console.WriteLine($"    {f} ({new FileInfo(f).Length} bytes)");
try { Directory.Delete(dir, true); } catch { }

done:
Console.WriteLine(Environment.ExitCode == 0 ? "\nALL STEPS PASSED" : "\nSOME STEPS FAILED");

class StubSettings : ISettingsService
{
    public AppSettings Settings { get; }
    public StubSettings(string dir) => Settings = new AppSettings { DownloadLocation = dir };
    public Task SaveAsync() => Task.CompletedTask;
    public Task LoadAsync() => Task.CompletedTask;
}
