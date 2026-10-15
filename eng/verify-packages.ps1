param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,
    [switch] $RequireLicense
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$expectedPackages = @(
    @{ Id = 'Reflections.Blazor.Query'; Analyzer = $true },
    @{ Id = 'Reflections.Blazor.Query.Generators'; Analyzer = $true }
)
$packageFiles = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg' -File)
$versions = [System.Collections.Generic.List[string]]::new()

foreach ($expected in $expectedPackages) {
    $packageFile = $packageFiles | Where-Object { $_.Name -like "$($expected.Id).*nupkg" } | Select-Object -First 1
    if (-not $packageFile) {
        throw "Required package '$($expected.Id)' was not found in '$PackageDirectory'."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($packageFile.FullName)
    try {
        $entries = @($archive.Entries | ForEach-Object FullName)
        if ('README.md' -notin $entries) {
            throw "Package '$($packageFile.Name)' is missing its README.md."
        }
        if ($expected.Analyzer -and 'analyzers/dotnet/cs/Reflections.Blazor.Query.Generators.dll' -notin $entries) {
            throw "Package '$($packageFile.Name)' is missing the source generator analyzer."
        }
        if ($expected.Id -eq 'Reflections.Blazor.Query' -and @($entries | Where-Object { $_ -match '(^|/)reflectionsBlazorQuery\.js$' }).Count -gt 0) {
            throw "Package '$($packageFile.Name)' contains the legacy arbitrary-selector DOM style helper."
        }

        $nuspecEntry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        if (-not $nuspecEntry) {
            throw "Package '$($packageFile.Name)' is missing its nuspec."
        }
        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml] $nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.SelectSingleNode("//*[local-name()='metadata']")
        $actualId = $metadata.SelectSingleNode("./*[local-name()='id']").InnerText
        $version = $metadata.SelectSingleNode("./*[local-name()='version']").InnerText
        $description = $metadata.SelectSingleNode("./*[local-name()='description']").InnerText
        $license = $metadata.SelectSingleNode("./*[local-name()='license']")

        if ($actualId -ne $expected.Id) {
            throw "Package '$($packageFile.Name)' contains unexpected id '$actualId'."
        }
        if ([string]::IsNullOrWhiteSpace($description) -or $description -eq 'Package Description') {
            throw "Package '$($packageFile.Name)' has missing or default description metadata."
        }
        if ($RequireLicense -and (-not $license -or $license.InnerText -ne 'Apache-2.0')) {
            throw "Package '$($packageFile.Name)' must declare the approved Apache-2.0 license before release."
        }

        $versions.Add($version)
        Write-Host "Validated $actualId $version"
    }
    finally {
        $archive.Dispose()
    }
}

if (@($versions | Select-Object -Unique).Count -ne 1) {
    throw "Package versions are not aligned: $($versions -join ', ')."
}

Write-Host "All packages passed archive checks at version $($versions[0])."
if (-not $RequireLicense) {
    Write-Warning 'License verification was not requested; public release remains blocked until a license is approved.'
}
