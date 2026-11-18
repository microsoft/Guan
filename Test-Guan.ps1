param([string] $Configuration = "Release")

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    dotnet build Guan.sln -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Guan build failed: $LASTEXITCODE" }

    $scripts = @(Get-ChildItem "$PSScriptRoot\GuanTest\Scripts" -Filter *.test -File | Sort-Object Name)
    if ($scripts.Count -eq 0) { throw "No Guan test scripts found." }

    foreach ($script in $scripts) {
        Write-Host "Running $($script.Name)"
        dotnet run --project GuanTest\GuanTest.csproj -c $Configuration --no-build --no-launch-profile -- $script.FullName
        if ($LASTEXITCODE -ne 0) { throw "Guan test $($script.Name) failed: $LASTEXITCODE" }
    }

    dotnet run --project GuanExamples\GuanExamples.csproj -c $Configuration --no-build --no-launch-profile
    if ($LASTEXITCODE -ne 0) { throw "Guan custom predicate examples failed: $LASTEXITCODE" }
}
finally {
    Pop-Location
}
