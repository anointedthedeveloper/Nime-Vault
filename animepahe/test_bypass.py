"""
AnimePahe Cloudflare bypass test using Playwright.
Strategy: launch a real Chromium with stealth args, solve the CF challenge,
then reuse the cf_clearance cookie for direct HTTP requests.
"""

import asyncio
import json
import sys
from playwright.async_api import async_playwright

TARGET = "https://animepahe.ru"
SEARCH_QUERY = "one piece"

STEALTH_ARGS = [
    "--no-sandbox",
    "--disable-blink-features=AutomationControlled",
    "--disable-infobars",
    "--disable-dev-shm-usage",
    "--disable-extensions",
    "--disable-setuid-sandbox",
    "--window-size=1280,720",
]

async def main():
    async with async_playwright() as pw:
        browser = await pw.chromium.launch(
            headless=False,   # visible so CF can see real browser signals
            args=STEALTH_ARGS,
        )

        ctx = await browser.new_context(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            viewport={"width": 1280, "height": 720},
            locale="en-US",
        )

        # Remove the webdriver property so CF doesn't detect automation
        await ctx.add_init_script("""
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3] });
            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
        """)

        page = await ctx.new_page()

        print(f"[*] Navigating to {TARGET} ...")
        await page.goto(TARGET, wait_until="domcontentloaded", timeout=60_000)

        # Wait for CF challenge to clear — it redirects to the real page
        # We wait until the URL is no longer a challenge page and the body has real content
        for i in range(30):
            title = await page.title()
            url   = page.url
            print(f"  [{i}] title={title!r}  url={url}")
            if "just a moment" not in title.lower() and "attention required" not in title.lower():
                print("[+] CF challenge passed!")
                break
            await asyncio.sleep(2)
        else:
            print("[-] CF challenge did NOT clear in time")
            await browser.close()
            return

        # Dump cookies — we need cf_clearance + __ddg* etc.
        cookies = await ctx.cookies()
        cf_cookies = {c["name"]: c["value"] for c in cookies}
        print(f"\n[+] Cookies obtained: {list(cf_cookies.keys())}")

        # Save cookies to file for reuse
        with open("cookies.json", "w") as f:
            json.dump(cookies, f, indent=2)
        print("[+] Saved cookies to cookies.json")

        # Now test the search API directly in the same browser context
        print(f"\n[*] Testing search API for: {SEARCH_QUERY!r}")
        api_url = f"{TARGET}/api?m=search&q={SEARCH_QUERY.replace(' ', '+')}"
        
        response = await page.goto(api_url, wait_until="domcontentloaded", timeout=30_000)
        status = response.status if response else "?"
        body = await page.content()
        
        print(f"[*] Status: {status}")
        
        # Try to parse as JSON
        try:
            # page.content() returns full HTML, get the text body
            text = await page.evaluate("document.body.innerText")
            data = json.loads(text)
            print(f"[+] Search API works! Got {len(data.get('data', []))} results")
            for item in data.get("data", [])[:5]:
                print(f"    - {item.get('title')} | session={item.get('session')} | ep={item.get('episodes')}")
            with open("search_result.json", "w") as f:
                json.dump(data, f, indent=2)
            print("[+] Saved full result to search_result.json")
        except Exception as e:
            print(f"[-] Could not parse JSON: {e}")
            print(f"    Body preview: {body[:500]}")

        await browser.close()

if __name__ == "__main__":
    asyncio.run(main())
