#!/usr/bin/env bash
# Gives a user access to their own folder in the remote solution storage, and to nothing else.
#
# The user gets a role assignment on the storage account with a condition: blob paths must start with the user's folder
# ("<folder id>/"). The storage enforces it, not the apps. See REMOTE-STORAGE.md, section 3.
#
#   scripts/onboard-user.sh --account feastorage --user jane@contoso.com
#   scripts/onboard-user.sh --account feastorage --user jane@outlook.com --folder-id 00000000-0000-0000-1234-56789abcdef0
#   scripts/onboard-user.sh --account feastorage --user jane@outlook.com --invite https://<name>.azurestaticapps.net
#
# Needs the Azure CLI, signed in (az login) as someone who can create role assignments (and, on the first run, the role) on the
# storage account's subscription, e.g. an Owner.

set -euo pipefail

ROLE_NAME="MeshEditor Storage User"

usage() {
	cat <<'EOF'
Usage: scripts/onboard-user.sh --account <storage account> --user <e-mail> [options]

  --account <name>        storage account name (e.g. feastorage)
  --resource-group <rg>   resource group of the storage account (only needed when the name is ambiguous)
  --user <e-mail>         the user's sign-in name or e-mail
  --folder-id <id>        the user's folder id; required for guests (people from outside the tenant): ask the user to sign in to
                          MeshEditor Online, which shows it when the storage refuses access
  --invite <url>          invite the user as a guest first when they are not in the tenant; <url> is where the invitation leads
                          (e.g. the web app)
  -h, --help              this help
EOF
}

fail() {
	echo "error: $*" >&2
	exit 1
}

account="" resource_group="" user="" folder_id="" invite_url=""
while [ $# -gt 0 ]; do
	case "$1" in
		--account) account="${2:-}"; shift 2 ;;
		--resource-group) resource_group="${2:-}"; shift 2 ;;
		--user) user="${2:-}"; shift 2 ;;
		--folder-id) folder_id="${2:-}"; shift 2 ;;
		--invite) invite_url="${2:-}"; shift 2 ;;
		-h|--help) usage; exit 0 ;;
		*) usage >&2; fail "unknown argument: $1" ;;
	esac
done
[ -n "$account" ] || { usage >&2; fail "--account is required"; }
[ -n "$user" ] || { usage >&2; fail "--user is required"; }
command -v az >/dev/null || fail "the Azure CLI (az) is not installed"

guid_pattern='^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
if [ -n "$folder_id" ] && ! [[ "$folder_id" =~ $guid_pattern ]]; then
	fail "--folder-id must be a GUID, e.g. 00000000-0000-0000-1234-56789abcdef0"
fi
folder_id=$(echo "$folder_id" | tr '[:upper:]' '[:lower:]')

# --- storage account --------------------------------------------------------------------------------------------------

if [ -n "$resource_group" ]; then
	account_id=$(az storage account show --name "$account" --resource-group "$resource_group" --query id --output tsv)
else
	account_id=$(az storage account show --name "$account" --query id --output tsv)
fi
[ -n "$account_id" ] || fail "storage account $account not found"
subscription_scope=$(echo "$account_id" | cut -d/ -f1-3)
echo "Storage account: $account_id"

# --- user -------------------------------------------------------------------------------------------------------------

find_user() {
	# a member by sign-in name, otherwise a guest by e-mail (a guest's sign-in name is mangled: jane_outlook.com#EXT#@...)
	az ad user show --id "$user" --query '{id:id, type:userType, name:displayName}' --output tsv 2>/dev/null && return
	local escaped="${user//\'/\'\'}"
	az ad user list --filter "mail eq '$escaped' or otherMails/any(m:m eq '$escaped')" \
		--query '[0].{id:id, type:userType, name:displayName}' --output tsv 2>/dev/null
}

user_info=$(find_user || true)
if [ -z "$user_info" ]; then
	[ -n "$invite_url" ] || fail "$user is not in the tenant. Invite them with --invite <url of the web app>, or in the Azure portal (Users > Invite external user)."
	echo "Inviting $user as a guest…"
	body=$(printf '{"invitedUserEmailAddress":"%s","inviteRedirectUrl":"%s","sendInvitationMessage":true}' "$user" "$invite_url")
	az rest --method POST --url "https://graph.microsoft.com/v1.0/invitations" --headers "Content-Type=application/json" --body "$body" --output none
	for _ in 1 2 3 4 5 6; do
		user_info=$(find_user || true)
		[ -n "$user_info" ] && break
		sleep 5
	done
	[ -n "$user_info" ] || fail "the invited user doesn't show up in the tenant yet; run the script again in a minute"
fi
IFS=$'\t' read -r object_id user_type display_name <<<"$user_info"
echo "User: $display_name ($user_type, object id $object_id)"

# The folder is named by the object id of the user's home account (see REMOTE-STORAGE.md). For members it is the object id here;
# for guests it is the id in their own tenant or Microsoft account, which this tenant doesn't know.
if [ "$user_type" = "Guest" ]; then
	if [ -z "$folder_id" ]; then
		cat >&2 <<EOF
error: $user is a guest: their folder id differs from their object id in this tenant and has to be given with --folder-id.
Ask them to sign in to MeshEditor Online: until they have access, it says that the storage refused access and shows their user id.
Then run this script again with --folder-id <that id>.
EOF
		exit 2
	fi
else
	if [ -n "$folder_id" ] && [ "$folder_id" != "$object_id" ]; then
		fail "$user is a member of the tenant: their folder id is their object id $object_id, not $folder_id"
	fi
	folder_id="$object_id"
fi
echo "Folder: $folder_id/"

# --- role -------------------------------------------------------------------------------------------------------------

# Only the blob data actions the apps use. Unlike Storage Blob Data Contributor, no container (management) actions: a user must
# not be able to delete or change a whole container, which holds everyone's folders.
if [ -z "$(az role definition list --name "$ROLE_NAME" --scope "$subscription_scope" --query '[0].id' --output tsv)" ]; then
	echo "Creating the role \"$ROLE_NAME\"…"
	role_file=$(mktemp)
	trap 'rm -f "$role_file"' EXIT
	cat >"$role_file" <<EOF
{
  "Name": "$ROLE_NAME",
  "Description": "Read, write and delete blobs; a condition on each assignment limits it to the user's folder (MeshEditor remote solutions).",
  "Actions": [],
  "DataActions": [
    "Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read",
    "Microsoft.Storage/storageAccounts/blobServices/containers/blobs/write",
    "Microsoft.Storage/storageAccounts/blobServices/containers/blobs/add/action",
    "Microsoft.Storage/storageAccounts/blobServices/containers/blobs/delete"
  ],
  "NotDataActions": [],
  "AssignableScopes": ["$subscription_scope"]
}
EOF
	az role definition create --role-definition "@$role_file" --output none
fi

# --- role assignment --------------------------------------------------------------------------------------------------

existing=$(az role assignment list --assignee "$object_id" --role "$ROLE_NAME" --scope "$account_id" --query '[0].condition' --output tsv)
if [ -n "$existing" ]; then
	echo "$user already has the role on $account (condition: $existing). Nothing to do."
else
	# Reading, writing and deleting a blob: its path must be in the folder. Listing blobs: the prefix must be in the folder.
	condition=$(cat <<EOF
(
 (
  !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read'} AND NOT SubOperationMatches{'Blob.List'})
  AND
  !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/write'})
  AND
  !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/add/action'})
  AND
  !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/delete'})
 )
 OR
 (
  @Resource[Microsoft.Storage/storageAccounts/blobServices/containers/blobs:path] StringStartsWith '$folder_id/'
 )
)
AND
(
 (
  !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read'} AND SubOperationMatches{'Blob.List'})
 )
 OR
 (
  @Request[Microsoft.Storage/storageAccounts/blobServices/containers/blobs:prefix] StringStartsWith '$folder_id/'
 )
)
EOF
)
	echo "Assigning the role…"
	# a new role definition takes a while to reach the role assignment service
	for attempt in 1 2 3 4 5 6; do
		if az role assignment create \
			--assignee-object-id "$object_id" --assignee-principal-type User \
			--role "$ROLE_NAME" --scope "$account_id" \
			--condition "$condition" --condition-version "2.0" \
			--description "MeshEditor: only the folder $folder_id/" \
			--output none; then
			break
		fi
		[ "$attempt" -lt 6 ] || fail "the role assignment could not be created"
		echo "Retrying in 10 s…"
		sleep 10
	done
fi

# --- other access -----------------------------------------------------------------------------------------------------

# Role assignments add up: another role on the account, e.g. through a group, would still open every folder.
others=$(az role assignment list --assignee "$object_id" --all --include-inherited --include-groups \
	--query "[?roleDefinitionName != '$ROLE_NAME' && starts_with('$account_id', scope)].[roleDefinitionName, principalType, principalName, scope]" \
	--output tsv)
if [ -n "$others" ]; then
	echo
	echo "warning: $user has other roles that include the storage account. If one of them gives blob data access (e.g. Storage Blob"
	echo "Data Contributor through a group), they can still reach every folder. Remove those role assignments or the user's group membership:"
	echo "$others" | sed 's/^/  /'
fi

echo
echo "Done: $user can use the folder $folder_id/ of $account. It can take a few minutes until the storage applies it."
