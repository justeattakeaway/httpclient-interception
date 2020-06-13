#! /usr/bin/pwsh
<#
.SYNOPSIS
    Attempts to update the version of the .NET SDK used by the repository's global.json file.
.DESCRIPTION
    Attempts to update the version of the .NET SDK specified in a global.json file to the latest release of
    the .NET SDK for a release channel of .NET, as specified by the https://github.com/dotnet/core repository.
.PARAMETER Channel
    Default: 3.1
    The .NET release channel to download the SDK for (2.1, 3.1, 5.0, etc.).
.PARAMETER BranchName
    The optional Git branch name to use.
.PARAMETER CommitMessage
    The optional Git commit message to use.
.PARAMETER GitHubToken
    The optional GitHub token to use to create a pull request for any update.
.PARAMETER GlobalJsonFile
    Default: ./global.json
    The optional path to the global.json file to update.
.PARAMETER UserEmail
    The optional email address to use for the Git commit.
.PARAMETER UserName
    The optional user name to use for the Git commit.
.PARAMETER DryRun
    If set, will not actually make changes to the file system, Git or GitHub.
.PARAMETER Verbose
    Displays additional diagnostics information.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)][string] $Channel = "3.1",
    [Parameter(Mandatory = $false)][string] $BranchName = "",
    [Parameter(Mandatory = $false)][string] $CommitMessage = "",
    [Parameter(Mandatory = $false)][string] $GitHubToken = "",
    [Parameter(Mandatory = $false)][string] $GlobalJsonFile = "./global.json",
    [Parameter(Mandatory = $false)][string] $UserEmail = "",
    [Parameter(Mandatory = $false)][string] $UserName = "",
    [Parameter(Mandatory = $false)][switch] $DryRun
)

function Get-Global-Json-Version([string] $FileName) {

    if (-Not (Test-Path $FileName)) {
        throw "Unable to find '$FileName'"
    }

    try {
        $JsonContent = Get-Json-From-File $FileName | Select-Object -Expand "sdk" -ErrorAction SilentlyContinue
    }
    catch {
        throw "JSON file unreadable: '$FileName'"
    }

    $Version = $null

    if ($JsonContent) {

        Say-Verbose "JSON: $JsonContent"

        try {
            $JsonContent.PSObject.Properties | ForEach-Object {
                $PropertyName = $_.Name
                if ($PropertyName -eq "version") {
                    $Version = $_.Value
                }
            }
        }
        catch {
            throw "Unable to parse the SDK node in '$FileName'"
        }
    }
    else {
        throw "Unable to find the SDK node in '$FileName'"
    }

    if ($null -eq $Version) {
        throw "Unable to find the SDK:version node in '$FileName'"
    }

    return $Version
}

function Get-Latest-SDK-Version([string] $FileName) {

    if (-Not (Test-Path $FileName)) {
        throw "Unable to find '$FileName'"
    }

    try {
        $JsonContent = Get-Json-From-File $FileName | Select-Object -ErrorAction SilentlyContinue
    }
    catch {
        throw "JSON file unreadable: '$FileName'"
    }

    $Version = $null

    if ($JsonContent) {

        Say-Verbose "JSON: $JsonContent"

        try {
            $JsonContent.PSObject.Properties | ForEach-Object {
                $PropertyName = $_.Name
                if ($PropertyName -eq "latest-sdk") {
                    $Version = $_.Value
                }
            }
        }
        catch {
            throw "Unable to parse the root node in '$FileName'"
        }
    }
    else {
        throw "Unable to find the root node in '$FileName'"
    }

    if ($null -eq $Version) {
        throw "Unable to find the latest-sdk node in '$FileName'"
    }

    return $Version
}

function Get-Latest-Runtime-Version([string] $FileName, [string] $SdkVersion, [bool] $AllowNull = $false) {

    if (-Not (Test-Path $FileName)) {
        throw "Unable to find '$FileName'"
    }

    try {
        $JsonContent = Get-Json-From-File $FileName | Select-Object -Expand "releases" -ErrorAction SilentlyContinue
    }
    catch {
        throw "JSON file unreadable: '$FileName'"
    }

    $Version = $null

    if ($JsonContent) {

        Say-Verbose "JSON: $JsonContent"

        try {
            foreach ($_ in $JsonContent) {
                if ($_.sdk.version -eq $SdkVersion) {
                    $Version = $_.sdk."runtime-version"
                    break;
                }
            }
        }
        catch {
            throw "Unable to parse the releases sdk node in '$FileName'"
        }

        if ($null -eq $Version) {
            try {
                foreach ($_ in $JsonContent) {
                    if ($_.sdks.version -eq $SdkVersion) {
                        $Version = $_.sdks."runtime-version" | Select -First 1
                        break;
                    }
                }
            }
            catch {
                throw "Unable to parse the releases sdks node in '$FileName'"
            }
        }
    }
    else {
        throw "Unable to find the releases node in '$FileName'"
    }

    if (($null -eq $Version) -And ($AllowNull -eq $false)) {
        throw "Unable to find the releases node in '$FileName' for SDK version $SdkVersion"
    }

    return $Version
}

function Get-Json-From-File([string]$FileName) {

    if (-Not (Test-Path $FileName)) {
        throw "Unable to find '$FileName'"
    }

    try {
        return Get-Content($FileName) -Raw | ConvertFrom-Json
    }
    catch {
        throw "JSON file unreadable: '$FileName'"
    }
}

function Say([string] $message) {
    Write-Host "update-dotnet-sdk: $message"
}

function Say-Verbose([string] $message) {
    Write-Verbose "update-dotnet-sdk: $message"
}

$CurrentSDKVersion = Get-Global-Json-Version $GlobalJsonFile

Say "Current .NET SDK version is $CurrentSDKVersion"

$ReleaseNotesUrl = "https://raw.githubusercontent.com/dotnet/core/master/release-notes/$Channel/releases.json"
$ReleaseNotesPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())

Say-Verbose "Downloading .NET $Channel release notes JSON from $ReleaseNotesUrl to $ReleaseNotesPath..."

$OldProgressPreference = $ProgressPreference
$ProgressPreference = "SilentlyContinue"

try {
    Invoke-WebRequest $ReleaseNotesUrl -OutFile $ReleaseNotesPath -UseBasicParsing
}
catch {
    Say "Cannot download $ReleaseNotesUrl"
    throw
}
finally {
    $ProgressPreference = $OldProgressPreference
}

$LatestSDKVersion = Get-Latest-SDK-Version $ReleaseNotesPath
$LatestRuntimeVersion = Get-Latest-Runtime-Version $ReleaseNotesPath $LatestSDKVersion
$CurrentRuntimeVersion = Get-Latest-Runtime-Version $ReleaseNotesPath $CurrentSDKVersion -AllowNull ($LatestSDKVersion -lt $CurrentSDKVersion)

if ($null -eq $CurrentRuntimeVersion) {
    Say "Unable to determine runtime version for .NET SDK version $CurrentSDKVersion."
    return
}

Say-Verbose "Current .NET runtime version is $CurrentRuntimeVersion"

Say "Latest .NET SDK version for channel '$Channel' is $LatestSDKVersion (runtime version $LatestRuntimeVersion)"

if ($CurrentSDKVersion -ge $LatestSDKVersion) {
    Say "The .NET SDK version specified by '$GlobalJsonFile' is up-to-date"
    return
}

Say-Verbose "Updating SDK version in '$GlobalJsonFile' to $LatestSDKVersion..."

$GlobalJson = Get-Json-From-File $GlobalJsonFile
$GlobalJson.sdk.version = $LatestSDKVersion

if ($DryRun) {
    Say "Skipped update of SDK version in '$GlobalJsonFile' to $LatestSDKVersion"
}
else {
    $GlobalJson | ConvertTo-Json | Set-Content -Path $GlobalJsonFile
    Say "Updated SDK version in '$GlobalJsonFile' to $LatestSDKVersion"
}

if (-Not $BranchName) {
    $BranchName = "update-dotnet-sdk-$LatestSDKVersion".ToLowerInvariant()
}

if (-Not $CommitMessage) {
    $CommitMessage = "Update .NET SDK`n`nUpdate .NET SDK to version $LatestSDKVersion."
}

Say-Verbose "Commit message: $CommitMessage"

# Set the remote for GitHub Actions to detect if the branch/PR has already been created
if ($env:CI) {
    git remote set-url origin https://github.com/$env:GITHUB_REPOSITORY.git | Out-Null

    try {
        git fetch origin | Out-Null
    }
    catch {
        # HACK - It worked, ignore exit code 1
    }
}

$Base = (git rev-parse --abbrev-ref HEAD | Out-String).Trim()
$BranchExists = (git rev-parse --verify --quiet remotes/origin/$BranchName | Out-String).Trim()

if ($BranchExists) {
    Say "The $BranchName branch already exists"
    return
}

if ($UserName) {
    if ($DryRun) {
        Say "Skipped update of git user name to '$UserName'"
    }
    else {
        git config user.name $UserName
        Say "Updated git user name to '$UserName'"
    }
}

if ($UserEmail) {
    if ($DryRun) {
        Say "Skipped update of git user email to '$UserEmail'"
    }
    else {
        git config user.email $UserEmail
        Say "Updated git user email to '$UserEmail'"
    }
}

if ($DryRun) {
    Say "Skipped git checkout for branch $BranchName"
}
else {
    try {
        git checkout -b $BranchName | Out-Null
    }
    catch {
        # HACK - It worked, ignore exit code 1
    }

    Say-Verbose "Created git branch $BranchName"
}

if ($DryRun) {
    Say "Skipped git commit for SDK update on branch $BranchName"
}
else {
    git add $GlobalJsonFile | Out-Null
    Say-Verbose "Staged git commit for '$GlobalJsonFile'"

    git commit -m $CommitMessage | Out-Null
    $GitSha = (git log --format="%H" -n 1 | Out-String).Substring(0, 7)

    Say "Commited .NET SDK update to git ($GitSha)"
}

if ($env:CI -And $GitHubToken) {

    if ($DryRun) {
        Say "Skipped git push to origin of branch $BranchName"
    }
    else {
        try {
            git push -u origin $BranchName | Out-Null
        }
        catch {
            # HACK - It worked, ignore exit code 1
        }

        Say "Pushed changes to repository $env:GITHUB_REPOSITORY"
    }

    $PullRequestUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/pulls"

    $Headers = @{
        "Accept"        = "application/vnd.github.v3+json";
        "Authorization" = "token $GitHubToken";
        "User-Agent"    = "update-dotnet-sdk.ps1";
    }

    $PullRequestBody = "Updates the .NET SDK to version [``$LatestSDKVersion``](https://github.com/dotnet/core/blob/master/release-notes/$Channel/$LatestRuntimeVersion/$LatestSDKVersion-download.md), "

    if ($LatestRuntimeVersion -eq $CurrentRuntimeVersion) {
        $PullRequestBody += "which includes version [``$LatestRuntimeVersion``](https://github.com/dotnet/core/blob/master/release-notes/$Channel/$LatestRuntimeVersion/$LatestRuntimeVersion.md) of the .NET runtime."
    }
    else {
        $PullRequestBody += "which also updates the .NET runtime from version [``$CurrentRuntimeVersion``](https://github.com/dotnet/core/blob/master/release-notes/$Channel/$CurrentRuntimeVersion/$CurrentRuntimeVersion.md) to version [``$LatestRuntimeVersion``](https://github.com/dotnet/core/blob/master/release-notes/$Channel/$LatestRuntimeVersion/$LatestRuntimeVersion.md)."
    }

    $PullRequestBody += "`n`nThis pull request was auto-generated by [GitHub Actions](https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID)."

    $Body = @{
        "title"                 = "Update .NET SDK to $LatestSDKVersion";
        "head"                  = $BranchName;
        "base"                  = $Base;
        "body"                  = $PullRequestBody;
        "maintainer_can_modify" = $true;
        "draft"                 = $false;
    } | ConvertTo-Json

    Say-Verbose "Pull Request: $Body"

    if ($DryRun) {
        Say "Skipped creating GitHub pull request for branch $BranchName to $Base"
    }
    else {
        try {
            $PullRequest = Invoke-RestMethod `
                -Uri $PullRequestUri `
                -Method POST `
                -ContentType "application/json" `
                -Headers $Headers `
                -Body $Body
        }
        catch {
            Say "Failed to open Pull Request"
            $_.Exception | Get-Error
            throw
        }

        Say "Created pull request #$($PullRequest.number): $($PullRequest.title)"
        Say "View the pull request at $($PullRequest.html_url)"
    }
}
