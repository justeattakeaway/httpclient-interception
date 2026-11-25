#! /usr/bin/pwsh

#Requires -PSEdition Core
#Requires -Version 7

param(
    [Parameter(Mandatory = $false)][switch] $SkipTests
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$solutionPath = $PSScriptRoot
$sdkFile = Join-Path $solutionPath "global.json"

$libraryProject = Join-Path $solutionPath "src" "HttpClientInterception" "JustEat.HttpClientInterception.csproj"

$testProjects = @(
    @{ Path = (Join-Path $solutionPath "tests" "HttpClientInterception.Tests" "JustEat.HttpClientInterception.Tests.csproj"); CoverageThreshold = 95 },
    @{ Path = (Join-Path $solutionPath "samples" "SampleApp.Tests" "SampleApp.Tests.csproj"); CoverageThreshold = 0 }
)

$dotnetVersion = (Get-Content $sdkFile | Out-String | ConvertFrom-Json).sdk.version

# Additional .NET runtimes required to run the tests for older target frameworks
$additionalRuntimes = @("10.0")

function Get-MissingDotNetRuntime {
    param([string]$DotNet)

    $installedRuntimes = & $DotNet --list-runtimes 2>&1 | Out-String

    ForEach ($runtime in $additionalRuntimes) {
        if (-Not ($installedRuntimes -match "Microsoft\.NETCore\.App $([regex]::Escape($runtime))\.")) {
            $runtime
        }
    }
}

function Install-DotNet {
    param([string[]]$ShellArgs, [hashtable]$PowerShellArgs)

    if (($PSVersionTable.PSVersion.Major -ge 6) -And !$IsWindows) {
        $installScript = Join-Path ${env:DOTNET_INSTALL_DIR} "install.sh"
        if (-Not $script:installScriptDownloaded) {
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.sh" -OutFile $installScript -UseBasicParsing
            chmod +x $installScript
            $script:installScriptDownloaded = $true
        }
        & $installScript @ShellArgs --install-dir ${env:DOTNET_INSTALL_DIR} --no-path
    }
    else {
        $installScript = Join-Path ${env:DOTNET_INSTALL_DIR} "install.ps1"
        if (-Not $script:installScriptDownloaded) {
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
            $script:installScriptDownloaded = $true
        }
        & $installScript @PowerShellArgs -InstallDir ${env:DOTNET_INSTALL_DIR} -NoPath
    }
}

function DotNetPack {
    param([string]$Project)

    $additionalArgs = @()

    if ($VersionSuffix) {
        $additionalArgs += "--version-suffix"
        $additionalArgs += $VersionSuffix
    }

    & $dotnet pack $Project --include-symbols --include-source --tl $additionalArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }
}

$coverageDirectory = Join-Path $solutionPath "artifacts" "coverage"

function DotNetTest {
    param([string]$Project, [int]$CoverageThreshold)

    $additionalArgs = @()

    if ($CoverageThreshold -gt 0) {
        $additionalArgs += "--coverlet-threshold"
        $additionalArgs += $CoverageThreshold
        $additionalArgs += "--coverlet-threshold-type"
        $additionalArgs += "line,branch,method"
    }

    if (![string]::IsNullOrEmpty(${env:GITHUB_SHA})) {
        $additionalArgs += "--report-github"
    }

    # The coverage filter is passed inline as a quoted literal, as PowerShell on Linux and macOS
    # expands wildcards in arguments passed to native commands from variables.
    & $dotnet test `
        --project $Project `
        --configuration "Release" `
        --results-directory $coverageDirectory `
        --coverlet `
        --coverlet-output-format "cobertura" `
        --coverlet-include "[JustEat.HttpClientInterception]*" `
        --coverlet-file-prefix ([System.IO.Path]::GetFileNameWithoutExtension($Project)) `
        $additionalArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

function GenerateCoverageReport {

    $reportTypes = "HTML"
    $outputMarkdown = ![string]::IsNullOrEmpty(${env:GITHUB_STEP_SUMMARY})

    if ($outputMarkdown) {
        $reportTypes += ";MarkdownSummaryGithub"
    }

    & $dotnet tool restore

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE"
    }

    & $dotnet reportgenerator `
        "-reports:$(Join-Path $coverageDirectory "*.cobertura.*.xml")" `
        "-targetdir:$coverageDirectory" `
        "-reporttypes:$reportTypes" `
        "-title:JustEat.HttpClientInterception" `
        "-verbosity:Warning"

    if ($LASTEXITCODE -ne 0) {
        throw "reportgenerator failed with exit code $LASTEXITCODE"
    }

    if ($outputMarkdown) {
        $summary = Get-Content (Join-Path $coverageDirectory "SummaryGithub.md") | Out-String
        @(
            "<details><summary>:chart_with_upwards_trend: <b>JustEat.HttpClientInterception Code Coverage report</b></summary>",
            "",
            $summary,
            "",
            "</details>"
        ) | Out-File -FilePath ${env:GITHUB_STEP_SUMMARY} -Append -Encoding utf8
    }
}

# The .NET SDK resolves global.json, which selects the SDK version and the test runner,
# and the local tools manifest from the current directory, so run from the repository root.
Push-Location $solutionPath

try {
    $installDotNetSdk = $false;
    $installScriptDownloaded = $false;

    if (($null -eq (Get-Command "dotnet" -ErrorAction SilentlyContinue)) -and ($null -eq (Get-Command "dotnet.exe" -ErrorAction SilentlyContinue))) {
        Write-Output "The .NET SDK is not installed."
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
            Write-Output "The required version of the .NET SDK is not installed. Expected $dotnetVersion."
            $installDotNetSdk = $true
        }
        else {
            $missingRuntimes = @(Get-MissingDotNetRuntime "dotnet")
            if ($missingRuntimes.Count -gt 0) {
                Write-Output "The .NET $($missingRuntimes -join ", ") runtime is not installed."
                $installDotNetSdk = $true
            }
        }
    }

    if ($installDotNetSdk -eq $true) {

        ${env:DOTNET_INSTALL_DIR} = Join-Path $PSScriptRoot ".dotnetcli"
        $sdkPath = Join-Path ${env:DOTNET_INSTALL_DIR} "sdk" $dotnetVersion

        if (!(Test-Path ${env:DOTNET_INSTALL_DIR})) {
            mkdir ${env:DOTNET_INSTALL_DIR} | Out-Null
        }

        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor "Tls12"

        if (!(Test-Path $sdkPath)) {
            Install-DotNet -ShellArgs @("--version", $dotnetVersion) -PowerShellArgs @{ Version = $dotnetVersion }
        }

        # The runtimes are checked separately in case the SDK was installed by a previous build
        ForEach ($runtime in $additionalRuntimes) {
            if (!(Test-Path (Join-Path ${env:DOTNET_INSTALL_DIR} "shared" "Microsoft.NETCore.App" "$runtime.*"))) {
                Install-DotNet -ShellArgs @("--channel", $runtime, "--runtime", "dotnet") -PowerShellArgs @{ Channel = $runtime; Runtime = "dotnet" }
            }
        }
    }
    else {
        ${env:DOTNET_INSTALL_DIR} = Split-Path -Path (Get-Command dotnet).Path
    }

    $dotnet = Join-Path ${env:DOTNET_INSTALL_DIR} "dotnet"

    if ($installDotNetSdk -eq $true) {
        # Ensure the locally installed SDK and runtimes are used, including by the test applications
        ${env:DOTNET_ROOT} = ${env:DOTNET_INSTALL_DIR}
        ${env:PATH} = "${env:DOTNET_INSTALL_DIR}$([System.IO.Path]::PathSeparator)${env:PATH}"
    }

    Write-Output "Packaging library..."
    DotNetPack $libraryProject

    if (-Not $SkipTests)
    {
        Write-Output "Running tests..."
        Remove-Item -Path $coverageDirectory -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
        ForEach ($testProject in $testProjects) {
            DotNetTest $testProject.Path $testProject.CoverageThreshold
        }
        GenerateCoverageReport
    }
}
finally {
    Pop-Location
}
