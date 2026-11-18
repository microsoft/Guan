param([string] $Configuration = "Release")

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    $project = [IO.Path]::Combine($PSScriptRoot, 'Guan', 'Guan.csproj')
    $output = [IO.Path]::Combine($PSScriptRoot, 'bin', $Configuration.ToLowerInvariant(), 'netstandard2.0')
    dotnet publish $project -c $Configuration -o $output
    if ($LASTEXITCODE -ne 0) { throw "Guan publish failed: $LASTEXITCODE" }
}
finally {
    Pop-Location
}
