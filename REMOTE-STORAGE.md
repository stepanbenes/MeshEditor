# Remote solutions: Azure setup

Remote solutions are stored in Azure Blob Storage. Users sign in with their Microsoft account (Microsoft Entra ID). There is no storage key in the application. Each user sees only their own solutions.

## How it works

- The app signs the user in with MSAL. The sign-in opens the system web browser on Windows, Linux and macOS.
  - If no browser can be opened (Linux without a desktop), the app shows a device code to enter in a browser on another device.
  - The command line tool always uses a device code: `fem-format-converter remote-storage --login`.
- The sign-in is remembered. The tokens are cached in the secure storage of the operating system:
  - Windows: a DPAPI-encrypted file
  - macOS: Keychain
  - Linux: the Secret Service keyring; without a keyring, a file in `~/.config/MeshEditor` that only the user can read
- The app calls Blob Storage directly with the user's token. Access is granted by Azure roles.
- Everything a user stores is in a folder named by the user's object id (the object id from the user's home account). The folder exists in each container:
  - `fea-solutions/<object id>/<n>.solution.json`: the solution. It records its owner (object id and e-mail).
  - `fea-layers/<object id>/<layer id>/...`: the layer data.
  - `fea-results/<object id>/...`: the FEM results for imports into a remote solution.
- The app lists and opens only the solutions in the folder of the signed-in user.

## 1. Storage account

1. Create a storage account (StorageV2).
2. Create three private containers: `fea-solutions`, `fea-layers` and `fea-results`. The app does not create them.
3. Recommended: disable *Allow storage account key access*, so the storage can be reached only with Entra ID.

## 2. App registration

In Microsoft Entra ID, open **App registrations → New registration**:

1. Name: `MeshEditor`.
2. Supported account types: *Accounts in this organizational directory only*.
3. Redirect URI: platform **Public client/native (mobile & desktop)**, value `http://localhost`.

Then:

1. **Authentication → Allow public client flows: Yes**. The device code sign-in needs this.
2. **API permissions → Add a permission → Azure Storage → Delegated → user_impersonation**.
3. Optional: **Grant admin consent**, so users are not asked to consent on their first sign-in.

## 3. Users and access rights

1. Users must be in the tenant. Invite people with personal Microsoft accounts (outlook.com and similar) as guests: **Users → Invite external user**.
2. Assign the role **Storage Blob Data Contributor** to the users, or better to a group of them. Scope the role to the storage account or to the three containers.

**What enforces the separation:** with this role, a user can technically read the whole container. The separation into user folders is enforced by the app.

**For separation enforced by the storage itself:** give each user their own role assignment with a condition. The condition limits blob paths to the user's folder: the path starts with `<object id>/`. This is a per-user setup. See [Azure role assignment conditions for Blob Storage](https://learn.microsoft.com/azure/storage/blobs/storage-auth-abac).

## 4. Application configuration

The values are not secret. Put them into the `AzureBlobStorage` section of `config.json`:

```json
"AzureBlobStorage": {
  "AccountUrl": "https://<account>.blob.core.windows.net",
  "TenantId": "<directory (tenant) id>",
  "ClientId": "<application (client) id>"
}
```

Where to put the file:

- **For all users:** the installation `config.json`, next to the executable. It is copied into the user configuration when a newer version starts.
- **For one user:** the user configuration file:
  - Windows: `%APPDATA%\MeshEditor\config.json`
  - Linux: `~/.config/MeshEditor/config.json`

The *Remote solutions* tab and the *Upload to remote* button appear only when all three values are set.

The container names can be changed with `SolutionsBlobContainerName`, `LayersBlobContainerName` and `ResultsBlobContainerName`.
