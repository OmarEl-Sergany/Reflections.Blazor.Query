# Reflections.Blazor.Query.Generators

The Roslyn incremental source generator used by [Reflections.Blazor.Query](../../README.md).

Most applications should reference the `Reflections.Blazor.Query` package, which includes this analyzer and activates it automatically. This package is published separately for build setups that need to reference the generator directly.

The analyzer targets `netstandard2.0` for compiler-host compatibility and is packaged under `analyzers/dotnet/cs`. It generates compile-time metadata; it does not add a runtime dependency on Roslyn.

Copyright 2026 Reflections Digital Agency. This package is licensed under [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0); see the packaged `LICENSE` and `NOTICE` files.
