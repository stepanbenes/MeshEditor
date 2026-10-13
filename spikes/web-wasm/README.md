# Spike: LayerManager in Blazor WebAssembly

Checks that `MeshEditor.LayerManager` runs unchanged in the browser and that a layer can be
downloaded from Blob Storage, filtered in WASM and uploaded back.

- `Shared/Bench.cs`: in-memory `IStorageService` and the benchmark (import, surface, slice, iso-surface, deformation, SVD).
- `Spike`: Blazor WASM app. `/` runs the benchmark on `wwwroot/data/grid.msh|res`; `/azure?layer=<id>&sas=<container SAS URL>`
  downloads the layer `user1/folder/<id>` from the container, runs the surface filter and uploads the new layer.
- `Native`: the same benchmark as a console app (`dotnet run -c Release -- <path without extension>`), and
  `seed <path>` / `list <prefix>` against Azurite (`UseDevelopmentStorage=true`): sets CORS, uploads a master layer, prints its id and a SAS URL.
- `gen.py N T out`: generates a GiD hexahedral block with N³ elements and T time steps.
- `serve.js`: static server with SPA fallback; `run.js`: opens `$URL` in headless Chromium and prints the log.

```
python3 gen.py 40 10 grid40 && cp grid40.msh Spike/wwwroot/data/grid.msh && cp grid40.res Spike/wwwroot/data/grid.res
dotnet publish Spike -c Release -o pub && node serve.js pub/wwwroot &
URL=http://localhost:8765/ PWPATH=$(npm root -g)/playwright node run.js
```
