param(
    [string] $Configuration = "Release",
    [string] $PackageVersion
)

$ErrorActionPreference = "Stop"
$arguments = @("pack", "$PSScriptRoot\Guan\Guan.csproj", "-c", $Configuration,
    "-o", "$PSScriptRoot\bin\release\Guan\Nugets")
if ($PackageVersion) { $arguments += "-p:PackageVersion=$PackageVersion" }

dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Guan pack failed: $LASTEXITCODE" }
