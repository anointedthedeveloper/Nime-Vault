#!/usr/bin/env python3
"""Fake AnimePahe for offline testing: JSON API, anime/play pages, pahe.win -> kwik -> mp4 chain.
Serves every host (animepahe / pahe.test / kwik.test) from one port; map them to 127.0.0.1 in /etc/hosts."""
import json, re, sys, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 80
FILE_SIZE = 8_000_000
KEY, RADIX, SHIFT = "abcdefghi", 8, 5
ANIME = {"id": 1, "title": "Test Hero Academy", "type": "TV", "episodes": 35, "status": "Finished Airing",
         "season": "Fall", "year": 2020, "score": 8.4, "poster": "http://animepahe.test/posters/a1.jpg", "session": "sess-anime-1"}
OTHER = {"id": 2, "title": "Another Show", "type": "Movie", "episodes": 1, "status": "Finished Airing",
         "season": "Summer", "year": 2018, "score": 7.1, "poster": "http://animepahe.test/posters/a2.jpg", "session": "sess-anime-2"}
EPISODES = [{"id": 1000 + i, "anime_id": 1, "episode": i, "episode2": 0, "edition": "", "title": "",
             "snapshot": f"http://animepahe.test/snap/{i}.jpg", "disc": "", "audio": "jpn", "duration": "00:24:10",
             "session": f"sess-ep-{i}", "filler": 0, "created_at": "2020-10-0%d 12:00:00" % (1 + i % 9)} for i in range(1, 36)]
state = {"file_hits": 0}
lock = threading.Lock()

def byte_at(i): return (i * 31 + 7) & 255
def pattern(start, n): return bytes(byte_at(i) for i in range(start, start + n))

def encode(text):
    out = []
    for ch in text:
        n = ord(ch) + SHIFT; d = ""
        while n: d = KEY[n % RADIX] + d; n //= RADIX
        out.append(d + KEY[RADIX])
    return "".join(out)

def kwik_page(fid):
    form = f'<form action="http://kwik.test/d/{fid}" method="POST"><input type="hidden" name="_token" value="tok-{fid}"></form>'
    return f'<html><body><script>eval(function(h,u,n,t,e,r){{}}("{encode(form)}",12,"{KEY}",{SHIFT},{RADIX},3))</script></body></html>'

def anime_page():
    return f'''<html><head><meta property="og:image" content="{ANIME['poster']}"></head><body>
<div class="anime-poster"><a href="{ANIME['poster']}" class="youtube-preview"><img data-src="{ANIME['poster']}"></a></div>
<div class="title-wrapper"><h1><span>{ANIME['title']}</span></h1><h2 class="japanese">テストヒーロー</h2></div>
<div class="anime-synopsis">A school for heroes.<br>Second line &amp; more.</div>
<div class="anime-info"><p><strong>Type:</strong><a href="/a/TV">TV</a></p><p><strong>Episodes:</strong> 35</p>
<p><strong>Status:</strong> <a href="/a/fin" title="Finished Airing">Finished Airing</a></p><p><strong>Season:</strong> <a href="/season/fall2020" title="Fall 2020">Fall 2020</a></p></div>
<div class="anime-genre"><ul><li><a href="/anime/genre/action" title="Action">Action</a></li><li><a href="/anime/genre/comedy" title="Comedy">Comedy</a></li></ul></div>
</body></html>'''

def play_page(a, e):
    return f'''<html><body><div id="pickDownload">
<a href="http://pahe.test/pk1" class="dropdown-item" target="_blank">SubsPlease · 360p (40MB)</a>
<a href="http://pahe.test/pk2" class="dropdown-item" target="_blank">SubsPlease · 720p (90MB)</a>
<a href="http://pahe.test/pk3" class="dropdown-item" target="_blank">SubsPlease · 1080p (180MB) <span class="badge badge-warning">eng</span></a>
</div></body></html>'''

class H(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"
    def log_message(self, *a): pass
    def send(self, code, body=b"", ctype="text/html; charset=utf-8", extra=None):
        if isinstance(body, str): body = body.encode()
        self.send_response(code); self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(body)))
        for k, v in (extra or {}).items(): self.send_header(k, v)
        self.end_headers(); self.wfile.write(body)
    def do_GET(self):
        u = urlparse(self.path); q = {k: v[0] for k, v in parse_qs(u.query).items()}; p = u.path
        if p == "/api":
            m = q.get("m")
            if m == "search":
                s = q.get("q", "").lower()
                data = [x for x in (ANIME, OTHER) if s in x["title"].lower()]
                return self.send(200, json.dumps({"total": len(data), "per_page": 8, "current_page": 1, "last_page": 1, "data": data}), "application/json")
            if m == "airing":
                data = [{"id": 1, "anime_id": 1, "anime_title": ANIME["title"], "episode": 35, "snapshot": "http://animepahe.test/snap/1.jpg",
                         "session": "sess-ep-35", "anime_session": "sess-anime-1", "fansub": "SubsPlease"},
                        {"id": 2, "anime_id": 2, "anime_title": OTHER["title"], "episode": 3, "snapshot": "http://animepahe.test/snap/2.jpg",
                         "session": "sess-ep-x", "anime_session": "sess-anime-2", "fansub": "SubsPlease"}]
                return self.send(200, json.dumps({"total": 2, "per_page": 12, "current_page": int(q.get("page", 1)), "last_page": 2, "data": data}), "application/json")
            if m == "release":
                if q.get("id") != "sess-anime-1": return self.send(200, json.dumps({"total": 0, "last_page": 1, "data": []}), "application/json")
                page = int(q.get("page", 1)); per = 30
                return self.send(200, json.dumps({"total": 35, "per_page": per, "current_page": page, "last_page": 2,
                                                  "data": EPISODES[(page - 1) * per: page * per]}), "application/json")
            return self.send(404)
        if p == "/anime/sess-anime-1": return self.send(200, anime_page())
        if p.startswith("/play/"): return self.send(200, play_page(*p.split("/")[2:4]))
        if re.fullmatch(r"/pk\d", p):
            return self.send(200, f'<html><a href="http://kwik.test/f/{p[1:]}" class="redirect">Continue</a></html>')
        if p.startswith("/f/"):
            return self.send(200, kwik_page(p[3:]), extra={"Set-Cookie": "kwik_session=abc123; Path=/"})
        if p.startswith("/files/"):
            if "kwik" not in (self.headers.get("Referer") or ""): return self.send(403, "hotlink")
            with lock: state["file_hits"] += 1; hit = state["file_hits"]
            rng = self.headers.get("Range"); start = 0
            if rng:
                start = int(re.match(r"bytes=(\d+)-", rng).group(1))
                if start >= FILE_SIZE: return self.send(416, extra={"Content-Range": f"bytes */{FILE_SIZE}"})
            length = FILE_SIZE - start
            self.send_response(206 if rng else 200)
            self.send_header("Content-Type", "video/mp4"); self.send_header("Accept-Ranges", "bytes")
            self.send_header("Content-Length", str(length))
            if rng: self.send_header("Content-Range", f"bytes {start}-{FILE_SIZE-1}/{FILE_SIZE}")
            self.end_headers()
            drop_at = int(FILE_SIZE * 0.4) if (p == "/files/flaky.mp4" and not rng) else None
            pos = start
            try:
                while pos < FILE_SIZE:
                    n = min(65536, FILE_SIZE - pos)
                    if drop_at is not None and pos >= drop_at:
                        self.close_connection = True; self.connection.close(); return
                    self.wfile.write(pattern(pos, n)); pos += n
                    import time; time.sleep(0.004)
            except (BrokenPipeError, ConnectionResetError): pass
            return
        if p == "/_state": return self.send(200, json.dumps(state), "application/json")
        self.send(404, "not found")
    def do_POST(self):
        u = urlparse(self.path)
        if u.path.startswith("/d/"):
            n = int(self.headers.get("Content-Length", 0)); body = self.rfile.read(n).decode()
            fid = u.path[3:]
            if "kwik_session=abc123" not in (self.headers.get("Cookie") or ""): return self.send(419, "no session cookie")
            if f"_token=tok-{fid}" not in body: return self.send(419, "bad token")
            if "kwik" not in (self.headers.get("Referer") or ""): return self.send(403, "bad referer")
            name = "flaky.mp4" if fid == "pk3" else "ep.mp4"
            return self.send(302, extra={"Location": f"http://kwik.test/files/{name}"})
        self.send(404)

if __name__ == "__main__":
    ThreadingHTTPServer(("127.0.0.1", PORT), H).serve_forever()
