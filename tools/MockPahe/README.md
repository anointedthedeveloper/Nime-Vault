# Offline testing

`server.py` is a fake AnimePahe (API, anime/play pages, pahe.win -> kwik -> mp4 chain, Range support,
and a flaky file that drops the connection once). It lets `tools/ProviderCheck` run end to end without the real site.

```
echo "127.0.0.1 animepahe.test pahe.test kwik.test" | sudo tee -a /etc/hosts   # one-time
sudo python3 tools/MockPahe/server.py 80 &
ANIMEPAHE_BASE_URL=http://animepahe.test NO_PROXY=animepahe.test,pahe.test,kwik.test \
  dotnet run --project tools/ProviderCheck -- hero
```

Against the real site, omit `ANIMEPAHE_BASE_URL` and pass `--no-download` to skip the download tests.
The mock is built from the page structure the scrapers assume, so it proves the plumbing, not that the real site still matches.
