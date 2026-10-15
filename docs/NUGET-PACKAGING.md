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

## Publication boundary

Package publication is deliberately separate from build-artifact creation. The project uses Apache-2.0, with its canonical license text in the repository-root `LICENSE` file and SPDX metadata in both packages; see the [Apache-2.0 license text](https://www.apache.org/licenses/LICENSE-2.0). Supply a NuGet.org API key through a protected local/CI secret and publish only the exact validated `.nupkg` files. Never commit the key or place it in repository `.env` files.

No NuGet feed push is performed by the current pipeline. This repository’s CI packs and validates packages as build artifacts only.
