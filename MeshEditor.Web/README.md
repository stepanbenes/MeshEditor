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

1. **Add a platform → Single-page application**, with the redirect URI of each place the app runs: the app's `signin` page at the deployed URL (for example `https://<name>.azurestaticapps.net/signin`) and, for testing against Azure, `http://localhost:5280/signin`.
2. The app asks for the `https://storage.azure.com/.default` scope. The registration already has the Azure Storage `user_impersonation` permission.

Keep the `/signin` path. Entra ignores the port of `localhost` redirect URIs, so `http://localhost:5280/` would be the same as the desktop app's `http://localhost`. Entra then takes either of the two, and the sign-in fails with AADSTS9002326 (web app) or the desktop sign-in breaks.

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

#### Azure Static Web Apps

The Free plan is enough: 100 GB of traffic a month, and it never bills overage. The workflow `.github/workflows/deploy-web.yml` publishes the app on every push to `master`; any branch can be deployed from the Actions tab (**Deploy web app → Run workflow**).

1. In the Azure portal, create a **Static Web App**: plan **Free**, deployment source **Other** (the workflow in this repository deploys it).
2. Copy its deployment token (**Overview → Manage deployment token**) into the repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN` (GitHub: **Settings → Secrets and variables → Actions**).
3. Add `https://<name>.azurestaticapps.net/signin` as a redirect URI of the app registration (see [App registration](#app-registration)) and `https://<name>.azurestaticapps.net` to the storage account's CORS rule (see [CORS](#cors)). Do the same for a custom domain.
4. Run the workflow.

### Who can read what

The app opens only the signed-in user's folder, but it runs in the user's browser with the user's own token, which also works outside the app (browser developer tools, Azure Storage Explorer, `az storage blob list --auth-mode login`). So the storage has to enforce the user folders: each user gets a role assignment limited to their folder, made by `scripts/onboard-user.sh` (see [REMOTE-STORAGE.md](../REMOTE-STORAGE.md#3-users-and-access-rights)). A role such as Storage Blob Data Contributor on the containers, for example through a group, would let its users read, change or delete every user's data. People without a role assignment can't read anything, as long as the containers are private.

When the storage refuses access, the app shows the user's id: the folder id that `scripts/onboard-user.sh --folder-id` needs for guests.

## Notes

- The worker runs the .NET code interpreted, not AOT-compiled. Layer generation is about 3 to 4 times slower than in the desktop app.
- An open layer is held in the browser's memory. Very large layers are limited by the memory of the tab.
- Keep the tab open while an import, upload or layer generation runs. A remote solution is listed only when it is complete.
