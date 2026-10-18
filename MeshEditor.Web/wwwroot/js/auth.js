// Microsoft account sign-in (MSAL.js, redirect flow) and access tokens for Azure Storage.
// The user's folder in the storage is named by the object id of the home account, the same as in the desktop app
// (for guests with a personal Microsoft account it differs from the oid claim of the ID token).
const storageScopes = ['https://storage.azure.com/.default'];
let app = null;
let initPromise = null;

function toUser(account) {
    return account ? { id: account.homeAccountId.split('.')[0], name: account.username || account.name || '' } : null;
}

// Returns the signed-in user or null. Also completes a sign-in that returns from the Microsoft login page.
export function init(clientId, tenantId) {
    if (!initPromise) {
        initPromise = (async () => {
            // The redirect URI is a page of its own (Pages/SignIn.razor): Entra ignores the port of localhost redirect URIs,
            // so http://localhost:5280/ would be taken for the desktop app's http://localhost. The path tells them apart.
            // MSAL takes the response there and returns to the page where the sign-in started.
            const signInPage = new URL('signin', document.baseURI).href;
            app = new msal.PublicClientApplication({
                auth: {
                    clientId,
                    authority: `https://login.microsoftonline.com/${tenantId}`,
                    redirectUri: signInPage,
                    // must be a registered redirect URI as well
                    postLogoutRedirectUri: signInPage
                },
                cache: { cacheLocation: 'localStorage' }
            });
            await app.initialize();
            const result = await app.handleRedirectPromise();
            const account = result?.account ?? app.getActiveAccount() ?? app.getAllAccounts()[0] ?? null;
            if (account) app.setActiveAccount(account);
            return toUser(account);
        })();
    }
    return initPromise;
}

export async function signIn() {
    await app.loginRedirect({ scopes: storageScopes, prompt: 'select_account' });
}

export async function signOut() {
    const account = app.getActiveAccount();
    await app.logoutRedirect({ account });
}

// { token, expiresOn (ms) } for the storage; redirects to the login page when the sign-in has expired.
export async function getToken() {
    const account = app.getActiveAccount();
    if (!account) throw new Error('Not signed in.');
    try {
        const result = await app.acquireTokenSilent({ scopes: storageScopes, account });
        return { token: result.accessToken, expiresOn: result.expiresOn ? result.expiresOn.getTime() : Date.now() + 30 * 60 * 1000 };
    } catch (err) {
        if (err instanceof msal.InteractionRequiredAuthError) {
            await app.acquireTokenRedirect({ scopes: storageScopes, account });
        }
        throw err;
    }
}
