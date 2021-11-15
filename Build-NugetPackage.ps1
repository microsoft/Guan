function Install-Nuget {
    # Path to Latest nuget.exe on nuget.org
    $source = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

    # Save file to top level directory in repo
    $destination = "$scriptPath\nuget.exe"

    #Download the file
    if (-Not [System.IO.File]::Exists($destination)) {
        Invoke-WebRequest -Uri $source -OutFile $destination
    }
}

function Build-Nuget {
    param (
        [string]
        $packageId,

        [string]
        $basePath
    )

    [string] $nugetSpecTemplate = [System.IO.File]::ReadAllText([System.IO.Path]::Combine($scriptPath, "Guan.nuspec.template"))

    [string] $nugetSpecPath = "$scriptPath\Guan\bin\release\netstandard2.1\$($packageId).nuspec"

    [System.IO.File]::WriteAllText($nugetSpecPath, $nugetSpecTemplate.Replace("%PACKAGE_ID%", $packageId).Replace("%ROOT_PATH%", $scriptPath))

    .\nuget.exe pack $nugetSpecPath -basepath $basePath -OutputDirectory bin\release\Guan\Nugets -properties NoWarn=NU5100
}

[string] $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

try {

    Push-Location $scriptPath

    Install-Nuget

    Build-Nuget "Microsoft.Logic.Guan" "$scriptPath\Guan\bin\release\netstandard2.1"
}
finally {

    Pop-Location
}