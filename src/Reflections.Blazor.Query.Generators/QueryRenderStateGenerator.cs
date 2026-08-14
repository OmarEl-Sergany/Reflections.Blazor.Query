using Microsoft.CodeAnalysis;
namespace Reflections.Blazor.Query.Generators;
[Generator] public sealed class QueryRenderStateGenerator : IIncrementalGenerator { public void Initialize(IncrementalGeneratorInitializationContext c) => c.RegisterPostInitializationOutput(x => x.AddSource("QueryGeneratedMetadata.g.cs", "namespace Reflections.Blazor.Query; internal static class QueryGeneratedMetadata { public const string Architecture = \"LibraryOwnedRenderState\"; }")); }
