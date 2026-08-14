using Microsoft.CodeAnalysis;

namespace Reflections.Blazor.Query.Generators;

[Generator]
public sealed class QueryRenderStateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output => output.AddSource(
            "QueryGeneratedMetadata.g.cs",
            """
            namespace Reflections.Blazor.Query;

            internal static class QueryGeneratedMetadata
            {
                public const string Architecture = "LibraryOwnedRenderState";
                public const string GeneratorVersion = "0.1.0";
                public static readonly string[] SupportedOperations =
                [
                    "Select", "Css", "BeginBatch"
                ];
            }
            """));
    }
}
