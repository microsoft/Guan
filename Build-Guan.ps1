param([string] $Configuration = "Release")

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    $project = Join-Path $PSScriptRoot 'Guan' 'Guan.csproj'
    $output = Join-Path $PSScriptRoot 'bin' $Configuration.ToLowerInvariant() 'netstandard2.0'
    dotnet publish $project -c $Configuration -o $output
    if ($LASTEXITCODE -ne 0) { throw "Guan publish failed: $LASTEXITCODE" }
}
finally {
    Pop-Location
}
