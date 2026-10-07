"""
AnimePahe API tester - bypasses Cloudflare using a real browser,
then calls the API endpoints directly from within the same browser context.
"""
import asyncio, json
from playwright.async_api import async_playwright

BASE = "https://animepahe.pw"

STEALTH = """
Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
window.chrome = { runtime: {} };
Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3,4,5] });
Object.defineProperty(navigator, 'languages', { get: () => ['en-US','en'] });
"""

async def api(page, path):
    """Call API via fetch() inside the already-cleared browser page."""
    result = await page.evaluate(f"""
        async () => {{
            const r = await fetch('{BASE}{path}', {{
                headers: {{
                    'Accept': 'application/json, text/javascript, */*',
                    'X-Requested-With': 'XMLHttpRequest',
                    'Referer': '{BASE}/'
                }}
            }});
            return await r.text();
        }}
    """)
    return json.loads(result)

async def main():
    async with async_playwright() as pw:
        browser = await pw.chromium.launch(
            headless=False,
            args=[
                "--no-sandbox",
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
            ],
        )
        ctx = await browser.new_context(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            viewport={"width": 1280, "height": 720},
        )
        await ctx.add_init_script(STEALTH)
        page = await ctx.new_page()

        # ── Step 1: land on homepage to clear CF challenge ──────────────────
        print("[*] Loading homepage...")
        print("[!] If a Cloudflare challenge appears, click 'Verify you are human' manually.")
        print("[!] The script will wait until the real page loads.")
        await page.goto(BASE, wait_until="domcontentloaded", timeout=60_000)

        # Wait until the real site title appears (not CF challenge)
        await page.wait_for_function(
            """() => {
                const t = document.title.toLowerCase();
                return !t.includes('just a moment') && !t.includes('attention') && !t.includes('loading');
            }""",
            timeout=120_000  # 2 min for human to click
        )
        print("[+] CF cleared!")

        # Let cookies fully bake
        try:
            await page.wait_for_load_state("networkidle", timeout=10_000)
        except:
            pass
        await asyncio.sleep(2)

        cookies = await ctx.cookies()
        print(f"[*] Cookies: {[c['name'] for c in cookies]}")
        with open("cookies.json", "w") as f:
            json.dump(cookies, f, indent=2)

        # ── Step 2: Search API ───────────────────────────────────────────────
        print("\n[*] Testing search: 'one piece'")
        try:
            data = await api(page, "/api?m=search&q=one+piece")
            results = data.get("data", [])
            print(f"[+] {len(results)} results")
            for r in results[:5]:
                print(f"    title={r.get('title')!r}  session={r.get('session')!r}  ep={r.get('episodes')}")
            with open("search.json", "w") as f:
                json.dump(data, f, indent=2)
            print("[+] Saved search.json")

            if results:
                session = results[0]["session"]

                # ── Step 3: Episode list ─────────────────────────────────────
                print(f"\n[*] Fetching episode list for session={session!r}")
                ep_data = await api(page, f"/api?m=release&id={session}&sort=episode_asc&page=1")
                eps = ep_data.get("data", [])
                print(f"[+] {len(eps)} episodes on page 1")
                for e in eps[:3]:
                    print(f"    ep={e.get('episode')!r}  session={e.get('session')!r}  snap={e.get('snapshot')!r}")
                with open("episodes.json", "w") as f:
                    json.dump(ep_data, f, indent=2)
                print("[+] Saved episodes.json")

                if eps:
                    ep_session = eps[0]["session"]
                    anime_session = session

                    # ── Step 4: Stream sources ───────────────────────────────
                    print(f"\n[*] Fetching stream sources for ep_session={ep_session!r}")
                    src_data = await api(page, f"/api?m=links&id={anime_session}&session={ep_session}&p=kwik")
                    print(f"[+] Stream data keys: {list(src_data.keys())}")
                    with open("sources.json", "w") as f:
                        json.dump(src_data, f, indent=2)
                    print("[+] Saved sources.json")
                    print(json.dumps(src_data, indent=2)[:1000])

        except Exception as e:
            print(f"[-] Error: {e}")
            body = await page.content()
            print(body[:500])

        await browser.close()

asyncio.run(main())
