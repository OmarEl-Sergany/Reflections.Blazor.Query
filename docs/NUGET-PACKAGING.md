# NuGet packaging and release gate

The repository builds two packages with the shared version from `src/Directory.Build.props`:

- `Reflections.Blazor.Query` — runtime library, static web assets, package README, and the source generator under `analyzers/dotnet/cs`.
- `Reflections.Blazor.Query.Generators` — standalone analyzer package for consumers that need to reference the generator directly.

The runtime package embeds the analyzer so normal consumers need one package reference. The standalone analyzer package is optional; consumers should not reference both packages in the same project because that can load the same generator twice.

## Build and validate locally

```powershell
dotnet build Reflections.Blazor.Query.slnx --configuration Release
$packageDir = Join-Path $env:TEMP 'rbq-packages'
dotnet pack src/Reflections.Blazor.Query/Reflections.Blazor.Query.csproj --configuration Release --no-build --output $packageDir
dotnet pack src/Reflections.Blazor.Query.Generators/Reflections.Blazor.Query.Generators.csproj --configuration Release --no-build --output $packageDir
./eng/verify-packages.ps1 -PackageDirectory $packageDir
```

Validation checks package identities, shared versions, package readmes, analyzer payloads, descriptions, and Apache-2.0 license metadata. A clean-consumer build should also confirm `QueryGeneratedMetadata.g.cs` is emitted. Use the release flag to require the license declaration:

```powershell
./eng/verify-packages.ps1 -PackageDirectory $packageDir -RequireLicense
```

## Release pipeline

Package publication is deliberately separate from CI artifact creation. The manual release definition is `azure-pipelines-release.yml`; it downloads a selected CI build artifact, validates package contents/license/version, checks that neither package version is already present on NuGet.org, and only then publishes both packages.

Before creating the release pipeline in Azure DevOps:

1. Create a **NuGet** service connection targeting `https://api.nuget.org/v3/index.json`, using the NuGet.org API key as its password. Name it `NuGet.org` and grant access only to the release pipeline. Do not put the key in source, `.env`, YAML, or a command-line argument.
2. Create a separate YAML pipeline from `azure-pipelines-release.yml` and authorize that service connection for this pipeline.
3. Manually run it with the ID of a successful CI build on `master` and the intended package version. The initial validated run is build `864`, version `1.0.0`.
4. Review the release pipeline result and confirm both package pages on NuGet.org after publication. NuGet package versions are immutable; a partially completed push must be handled by publishing only the missing package, not by retrying both blindly.

The release pipeline is intentionally manual and separate from CI. CI continues to build, test, and retain artifacts without publishing publicly. The project uses Apache-2.0, with its canonical license text in the repository-root `LICENSE` file and SPDX metadata in both packages; see the [Apache-2.0 license text](https://www.apache.org/licenses/LICENSE-2.0).
