param(
    [string] $Configuration = "Release",
    [string] $PackageVersion
)

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    $project = [IO.Path]::Combine($PSScriptRoot, 'Guan', 'Guan.csproj')
    $output = [IO.Path]::Combine([string[]]@($PSScriptRoot, 'bin', $Configuration.ToLowerInvariant(), 'Guan', 'Nugets'))
    $arguments = @("pack", $project, "-c", $Configuration, "-o", $output)
    if ($PackageVersion) { $arguments += "-p:PackageVersion=$PackageVersion" }

    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Guan pack failed: $LASTEXITCODE" }
}
finally {
    Pop-Location
}
