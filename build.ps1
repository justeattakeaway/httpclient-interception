#! /usr/bin/pwsh
param(
    [Parameter(Mandatory = $false)][string] $Configuration = "Release",
    [Parameter(Mandatory = $false)][string] $VersionSuffix = "",
    [Parameter(Mandatory = $false)][string] $OutputPath = "",
    [Parameter(Mandatory = $false)][switch] $SkipTests
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$solutionPath = Split-Path $MyInvocation.MyCommand.Definition
$sdkFile = Join-Path $solutionPath "global.json"

$libraryProject = Join-Path $solutionPath "src\HttpClientInterception\JustEat.HttpClientInterception.csproj"

$testProjects = @(
    (Join-Path $solutionPath "tests\HttpClientInterception.Tests\JustEat.HttpClientInterception.Tests.csproj"),
    (Join-Path $solutionPath "samples\SampleApp.Tests\SampleApp.Tests.csproj")
)

$dotnetVersion = (Get-Content $sdkFile | Out-String | ConvertFrom-Json).sdk.version

if ($OutputPath -eq "") {
    $OutputPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "artifacts"
}

$installDotNetSdk = $false;

if (($null -eq (Get-Command "dotnet" -ErrorAction SilentlyContinue)) -and ($null -eq (Get-Command "dotnet.exe" -ErrorAction SilentlyContinue))) {
    Write-Host "The .NET Core SDK is not installed."
    $installDotNetSdk = $true
}
else {
    Try {
        $installedDotNetVersion = (dotnet --version 2>&1 | Out-String).Trim()
    }
    Catch {
        $installedDotNetVersion = "?"
    }

    if ($installedDotNetVersion -ne $dotnetVersion) {
        Write-Host "The required version of the .NET Core SDK is not installed. Expected $dotnetVersion."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {

    $env:DOTNET_INSTALL_DIR = Join-Path "$(Convert-Path "$PSScriptRoot")" ".dotnetcli"
    $sdkPath = Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetVersion"

    if (!(Test-Path $sdkPath)) {
        if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
            mkdir $env:DOTNET_INSTALL_DIR | Out-Null
        }
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor "Tls12"

        if (($PSVersionTable.PSVersion.Major -ge 6) -And !$IsWindows) {
            $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.sh"
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.sh" -OutFile $installScript -UseBasicParsing
            chmod +x $installScript
            & $installScript --version "$dotnetVersion" --install-dir "$env:DOTNET_INSTALL_DIR" --no-path
        }
        else {
            $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
            & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
        }
    }
}
else {
    $env:DOTNET_INSTALL_DIR = Split-Path -Path (Get-Command dotnet).Path
}

$dotnet = Join-Path "$env:DOTNET_INSTALL_DIR" "dotnet"

if ($installDotNetSdk -eq $true) {
    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
}

function DotNetPack {
    param([string]$Project)

    if ($VersionSuffix) {
        & $dotnet pack $Project --output (Join-Path $OutputPath "packages") --configuration $Configuration --version-suffix "$VersionSuffix" --include-symbols --include-source
    }
    else {
        & $dotnet pack $Project --output (Join-Path $OutputPath "packages") --configuration $Configuration --include-symbols --include-source
    }
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }
}

function DotNetTest {
    param([string]$Project)

    $nugetPath = Join-Path ($env:USERPROFILE ?? "~") ".nuget\packages"
    $propsFile = Join-Path $solutionPath "Directory.Packages.props"
    $reportGeneratorVersion = (Select-Xml -Path $propsFile -XPath "//PackageVersion[@Include='ReportGenerator']/@Version").Node.'#text'
    $reportGeneratorPath = Join-Path $nugetPath "reportgenerator\$reportGeneratorVersion\tools\netcoreapp3.0\ReportGenerator.dll"

    $coverageOutput = Join-Path $OutputPath "coverage.*.cobertura.xml"
    $reportOutput = Join-Path $OutputPath "coverage"

    & $dotnet test $Project --output $OutputPath

    $dotNetTestExitCode = $LASTEXITCODE

    if (Test-Path $coverageOutput) {
        & $dotnet `
            $reportGeneratorPath `
            `"-reports:$coverageOutput`" `
            `"-targetdir:$reportOutput`" `
            -reporttypes:HTML `
            -verbosity:Warning
    }

    if ($dotNetTestExitCode -ne 0) {
        throw "dotnet test failed with exit code $dotNetTestExitCode"
    }
}


Write-Host "Packaging library..." -ForegroundColor Green
DotNetPack $libraryProject

Write-Host "Running tests..." -ForegroundColor Green
Remove-Item -Path (Join-Path $OutputPath "coverage.*.json") -Force -ErrorAction SilentlyContinue | Out-Null
ForEach ($testProject in $testProjects) {
    DotNetTest $testProject
}
