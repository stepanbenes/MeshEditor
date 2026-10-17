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
  - `fea-layers/<object id>/<layer folder>/<layer id>/...`: the layer data. Each solution has its own layer folder (recorded in the solution), so solutions uploaded from the same local solution don't share layers. Solutions uploaded before the layer folders have their layers directly in `fea-layers/<object id>/<layer id>/...`.
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

Users must be in the tenant. People from outside it (personal Microsoft accounts such as outlook.com, or other organizations) are invited as guests.

Each user gets their own role assignment on the storage account, with a condition that limits it to their folder: blob paths must start with `<folder id>/`. The storage enforces it, so a user can't reach other users' folders even with their own token outside the apps. `scripts/onboard-user.sh` (Azure CLI, bash; also runs in Azure Cloud Shell) does it:

```sh
scripts/onboard-user.sh --account <storage account> --user jane@contoso.com
```

- **Members of the tenant:** the folder id is their object id; the script finds it.
- **Guests:** the folder id is the object id of their home account (their own tenant or Microsoft account), which this tenant doesn't know. Ask them to sign in to MeshEditor Online: until they have access, it says that the storage refused access and shows their user id. Then pass it with `--folder-id <id>`.
- **Not in the tenant yet:** `--invite <url of the web app>` invites them as a guest and sends the invitation e-mail.

The script needs someone who can create role assignments on the subscription (e.g. an Owner). On the first run it creates the custom role **MeshEditor Storage User**: read, write and delete blobs, with no container actions. Storage Blob Data Contributor would also let a user delete a whole container through Azure Resource Manager, where the condition doesn't apply.

**Don't also give users a role with blob data access on the account or the containers**, for example Storage Blob Data Contributor through a group. Role assignments add up, and that one would open every folder again. The script warns when the user has other roles on the account. To move from a group to per-user access, run the script for every member, then remove the group's role assignment.

See [Azure role assignment conditions for Blob Storage](https://learn.microsoft.com/azure/storage/blobs/storage-auth-abac).

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
