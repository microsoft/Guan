param([string] $Configuration = "Release")

$ErrorActionPreference = "Stop"

dotnet publish "$PSScriptRoot\Guan\Guan.csproj" -c $Configuration -o "$PSScriptRoot\bin\release\netstandard2.0"
if ($LASTEXITCODE -ne 0) { throw "Guan publish failed: $LASTEXITCODE" }
