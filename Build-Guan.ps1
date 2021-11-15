$ErrorActionPreference = "Stop"

$Configuration="Release"
[string] $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

try {
    Push-Location $scriptPath

    Remove-Item $scriptPath\Guan\bin\release\netstandard2.1\ -Recurse -Force -EA SilentlyContinue

    dotnet publish $scriptPath\Guan\Guan.csproj -o bin\release\netstandard2.1 -c $Configuration
}
finally {
    Pop-Location
}