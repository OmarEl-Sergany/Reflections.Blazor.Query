[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $ExpectedVersion
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$expectedPackages = @(
    'Reflections.Blazor.Query',
    'Reflections.Blazor.Query.Generators'
)

$packageFiles = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg' -File)
if ($packageFiles.Count -ne $expectedPackages.Count) {
    throw "Expected exactly $($expectedPackages.Count) NuGet packages in '$PackageDirectory'; found $($packageFiles.Count)."
}

foreach ($packageId in $expectedPackages) {
    $packagePath = Join-Path $PackageDirectory "$packageId.$ExpectedVersion.nupkg"
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Expected package '$packageId' version '$ExpectedVersion' was not found."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead([System.IO.Path]::GetFullPath($packagePath))
    try {
        $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "Package '$packagePath' does not contain a nuspec manifest."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml] $manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $manifest.SelectSingleNode("//*[local-name()='metadata']")
        $actualId = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
        $actualVersion = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
        if ($actualId -cne $packageId -or $actualVersion -cne $ExpectedVersion) {
            throw "Package manifest mismatch in '$packagePath': found '$actualId' version '$actualVersion'."
        }
    }
    finally {
        $archive.Dispose()
    }

    $indexUri = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/index.json"
    try {
        $index = Invoke-RestMethod -Uri $indexUri -Method Get -TimeoutSec 30
    }
    catch {
        $statusCode = [int] $_.Exception.Response.StatusCode
        if ($statusCode -eq 404) {
            continue
        }

        throw "Unable to check NuGet.org for '$packageId' (HTTP $statusCode); refusing to publish. $($_.Exception.Message)"
    }

    if (@($index.versions | Where-Object { $_ -ieq $ExpectedVersion }).Count -gt 0) {
        throw "NuGet.org already contains '$packageId' version '$ExpectedVersion'. Package versions are immutable; refusing the release."
    }
}

Write-Host "Release preflight passed for both packages at version $ExpectedVersion."
