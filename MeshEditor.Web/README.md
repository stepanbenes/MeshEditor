# MeshEditor Online

A web app for the remote solutions in Azure Blob Storage. It lists the signed-in user's solutions, shows their layers in 3D, generates sub layers, imports FEM results and uploads local solutions.

Everything runs in the browser. The app is a static site: there is no server of its own.

- **MeshEditor.Web**: the Blazor WebAssembly UI. The 3D view is three.js (`wwwroot/js/viewer.js`). Sign-in is MSAL.js.
- **MeshEditor.Web.Worker**: the layer code (`MeshEditor.LayerManager`, `MeshEditor.SolutionManager`) compiled to WebAssembly. It runs in a Web Worker, so long computations don't freeze the page. Building MeshEditor.Web publishes it into `wwwroot/worker`.
- **MeshEditor.Web.DevSetup**: prepares Azurite for local testing.

The worker downloads the records of a layer into memory, runs the same layer generator as the desktop app, and uploads the new records. Solutions made in the web app open in the desktop app, and the other way round.

## Run locally with Azurite

No Azure account and no sign-in: the app uses the Azure Storage emulator and a fixed local user.

Needs the .NET 10 SDK and Node.js.

1. Start Azurite (the blob service is enough):

   ```sh
   npx --package azurite azurite-blob --blobHost 127.0.0.1 --blobPort 10000 --inMemoryPersistence --loose --skipApiVersionCheck
   ```

   Leave out `--inMemoryPersistence` to keep the data between runs. `--skipApiVersionCheck` is needed because the Azure SDK of the app uses a newer storage API version than Azurite knows.

2. Create the containers and allow the app's origin (CORS). Run it again after each restart of an in-memory Azurite:

   ```sh
   dotnet run --project MeshEditor.Web.DevSetup
   ```

   It allows `http://localhost:5280` by default. Pass other origins as arguments.

3. Run the app:

   ```sh
   dotnet run --project MeshEditor.Web
   ```

   Open http://localhost:5280. `DEV STORAGE` next to the user name means the development storage is used. The first build takes a while because it also builds the worker.

The development storage is enabled by `wwwroot/appsettings.Development.json`, which is used when the app runs in the Development environment (`dotnet run`).

The start page lists the solutions. Open one to see its layer tree and the 3D view. A new Azurite has no solutions: import a GiD `.msh` with its `.res` (for example `MeshEditor.Avalonia.Tests/TestData/props.msh` and `props.res`), or upload a local solution folder made by the desktop app.

## Azure

The app uses the same storage account, containers and user folders as the desktop app. Set those up first: see [REMOTE-STORAGE.md](../REMOTE-STORAGE.md).

### App registration

The web app can use the desktop app's registration. In **Authentication**:

1. **Add a platform → Single-page application**, with the redirect URI of each place the app runs: the deployed URL (for example `https://<name>.azurestaticapps.net/`) and, for testing against Azure, `http://localhost:5280/`.
2. The app asks for the `https://storage.azure.com/.default` scope. The registration already has the Azure Storage `user_impersonation` permission.

### CORS

The browser calls Blob Storage directly, so the storage account must allow the app's origin. In the storage account: **Resource sharing (CORS) → Blob service**:

- Allowed origins: the app's URL (and `http://localhost:5280` for testing)
- Allowed methods: `GET, HEAD, PUT, POST, DELETE, OPTIONS`
- Allowed headers: `*`
- Exposed headers: `*`
- Max age: `3600`

### Configuration

Put the values into `wwwroot/appsettings.json`. They are not secret:

```json
"AzureBlobStorage": {
  "AccountUrl": "https://<account>.blob.core.windows.net",
  "TenantId": "<directory (tenant) id>",
  "ClientId": "<application (client) id>"
}
```

To test against Azure locally, also set `DevelopmentStorage.Enabled` to `false` in `wwwroot/appsettings.Development.json`.

### Deploy

```sh
dotnet publish MeshEditor.Web -c Release -o publish
```

Deploy `publish/wwwroot` to any static host. For Azure Static Web Apps, `staticwebapp.config.json` sends the app's routes to `index.html`. The download is about 8 MB compressed. The browser caches it after the first visit.

## Notes

- The worker runs the .NET code interpreted, not AOT-compiled. Layer generation is about 3 to 4 times slower than in the desktop app.
- An open layer is held in the browser's memory. Very large layers are limited by the memory of the tab.
- Keep the tab open while an import, upload or layer generation runs. A remote solution is listed only when it is complete.
