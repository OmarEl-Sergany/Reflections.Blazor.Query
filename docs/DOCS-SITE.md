# Documentation Site

The documentation site is generated with the repository-pinned DocFX 2.81.0 tool. The build combines the Markdown guides in this directory with a managed API reference generated from the library project.

## Prerequisites

- .NET 10 SDK.
- Network access the first time the local DocFX tool is restored.

## Build and preview

From the repository root:

```powershell
dotnet tool restore
dotnet tool run docfx -- docs/docfx.json
dotnet tool run docfx -- serve docs/_site
```

The generated static site is in `docs/_site/`. Generated API metadata is in `docs/api/`; both output directories are ignored by Git and recreated by the build.

## Content workflow

- Add or revise conceptual articles in `docs/` and include them in `docs/toc.yml`.
- The API reference is generated from the library project; do not hand-edit files under `docs/api/`.
- Keep the published package status and host compatibility claims aligned with current build and browser evidence.
- Run a site build before merging documentation changes. Warnings are actionable, especially broken links and missing TOC targets.

## CI and release boundary

Azure Pipelines builds the site and publishes the generated files as a build artifact for review. That artifact publication is not a deployment. Hosting provider, public URL, and deployment credentials remain intentionally unconfigured until a separate release/deployment decision is authorized.
