# Builds out\MeshEditor-<version>-win-x64.zip: self-contained .NET builds of MeshEditor (MeshEditor.Avalonia)
# and of the command line tool fem-format-converter, so no .NET runtime is needed. Needs the .NET 10 SDK.
$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$out = Join-Path $PSScriptRoot 'out'

# the version is the assembly version (SharedAssemblyInfo.cs)
$version = Select-String -Path (Join-Path $repo 'SharedAssemblyInfo.cs') -Pattern '^\[assembly: AssemblyVersion\("(.*)"\)\]' |
	ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1
if (-not $version) { throw 'AssemblyVersion not found in SharedAssemblyInfo.cs' }

if (Test-Path $out) { Remove-Item $out -Recurse -Force }

function Publish([string] $project, [string] $name) {
	dotnet publish (Join-Path $repo $project) -c Release -r win-x64 --self-contained -p:DebugType=None -o (Join-Path $out "publish\$name")
	if ($LASTEXITCODE -ne 0) { throw "dotnet publish $project failed" }
}
Publish 'MeshEditor.Avalonia' 'mesheditor'
Publish 'MeshEditor.FormatConverter.Console' 'fem-format-converter'

# both builds go into one directory: the files they share (the .NET runtime, common assemblies) must be the same
$app = Join-Path $out 'MeshEditor'
Copy-Item (Join-Path $out 'publish\mesheditor') $app -Recurse
$converter = Join-Path $out 'publish\fem-format-converter'
Get-ChildItem $converter -Recurse -File | ForEach-Object {
	$target = Join-Path $app $_.FullName.Substring($converter.Length + 1)
	if ((Test-Path $target) -and (Get-FileHash $target).Hash -ne (Get-FileHash $_.FullName).Hash) {
		throw "fem-format-converter and MeshEditor need different versions of $($_.Name)"
	}
}
Copy-Item (Join-Path $converter '*') $app -Recurse -Force
Copy-Item (Join-Path $repo 'LICENSE.md'), (Join-Path $repo 'NOTICE.md') $app

$zip = Join-Path $out "MeshEditor-$version-win-x64.zip"
Compress-Archive -Path $app -DestinationPath $zip
Write-Host "package created: $zip"
