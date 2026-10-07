using NimeVault.Services.AnimePahe;

// Usage: dotnet run --project tools/ProviderCheck -- "naruto"
// Walks the AnimePahe flow step by step and prints what each step returned.
var query = args.Length > 0 ? string.Join(' ', args) : "naruto";
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
Console.WriteLine(Environment.ExitCode == 0 ? "\nALL STEPS PASSED" : "\nSOME STEPS FAILED");
