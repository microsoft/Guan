$ErrorActionPreference = "Stop"
$testRoot = Join-Path $PSScriptRoot 'bin' 'script-tests'
$caller = Join-Path $testRoot 'caller'
New-Item -ItemType Directory -Force -Path $caller | Out-Null
[IO.File]::WriteAllText((Join-Path $caller 'global.json'),
    '{"sdk":{"version":"99.0.100","rollForward":"disable","allowPrerelease":false}}')
[xml] $project = Get-Content (Join-Path $PSScriptRoot 'Guan' 'Guan.csproj') -Raw
$packageId = $project.Project.PropertyGroup.PackageId
$version = $project.Project.PropertyGroup.Version
if (!$packageId -or !$version) { throw "Guan package identity is missing." }

$dotnet = (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source
$probe = Start-Process $dotnet -ArgumentList '--version' -WorkingDirectory $caller -Wait -PassThru `
    -RedirectStandardOutput (Join-Path $testRoot 'caller-sdk.stdout.log') `
    -RedirectStandardError (Join-Path $testRoot 'caller-sdk.stderr.log')
if ($probe.ExitCode -eq 0) { throw "The conflicting caller SDK pin did not reject SDK selection." }

Push-Location $caller
try {
    $location = (Get-Location).ProviderPath
    foreach ($configuration in @("Release", "Debug")) {
        & (Join-Path $PSScriptRoot 'Build-Guan.ps1') -Configuration $configuration
        if ((Get-Location).ProviderPath -ne $location) { throw "Publishing did not restore the caller's directory." }
        & (Join-Path $PSScriptRoot 'Build-NugetPackage.ps1') -Configuration $configuration
        if ((Get-Location).ProviderPath -ne $location) { throw "Packaging did not restore the caller's directory." }
        $outputRoot = Join-Path $PSScriptRoot 'bin' $configuration.ToLowerInvariant()
        $assembly = Join-Path $outputRoot 'netstandard2.0' 'Guan.dll'
        $package = Join-Path $outputRoot 'Guan' 'Nugets' "$packageId.$version.nupkg"
        if (!(Test-Path -LiteralPath $assembly) -or !(Test-Path -LiteralPath $package)) {
            throw "$configuration artifacts are missing from the configuration-specific output."
        }
        if ($configuration -eq "Release") {
            $releaseAssembly = $assembly
            $releasePackage = $package
            $assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
            $packageHash = (Get-FileHash -LiteralPath $package).Hash
            $assemblyTime = (Get-Item -LiteralPath $assembly).LastWriteTimeUtc
            $packageTime = (Get-Item -LiteralPath $package).LastWriteTimeUtc
        }
        else {
            if ((Get-FileHash -LiteralPath $releaseAssembly).Hash -ne $assemblyHash -or
                (Get-FileHash -LiteralPath $releasePackage).Hash -ne $packageHash -or
                (Get-Item -LiteralPath $releaseAssembly).LastWriteTimeUtc -ne $assemblyTime -or
                (Get-Item -LiteralPath $releasePackage).LastWriteTimeUtc -ne $packageTime) {
                throw "Debug build scripts modified Release artifacts."
            }
        }
        Write-Host "PASS $configuration publish/pack: repository SDK, isolated output, caller directory restored."
    }
}
finally {
    Pop-Location
}
