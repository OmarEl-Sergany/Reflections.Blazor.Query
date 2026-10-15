using BenchmarkDotNet.Attributes;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Benchmarks;

[MemoryDiagnoser]
public class RenderStateEffectBenchmarks
{
    private static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(200);
    private static readonly IReadOnlyDictionary<string, string> FirstTargets = new Dictionary<string, string>
    {
        ["opacity"] = "0.5",
        ["color"] = "red"
    };
    private static readonly IReadOnlyDictionary<string, string> SecondTargets = new Dictionary<string, string>
    {
        ["opacity"] = "0.85",
        ["color"] = "blue"
    };

    private QuerySelection _selection = default!;
    private bool _useFirstTargets;

    [Params(1, 10, 100, 1000)]
    public int SelectionSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        for (var index = 0; index < SelectionSize; index++)
            registry.Register($"effect-target-{index}", ".effect-target");

        _selection = new BlazorQuery(state, registry).Select(".effect-target");
        if (_selection.Length != SelectionSize)
            throw new InvalidOperationException($"Expected {SelectionSize} selected elements, observed {_selection.Length}.");
    }

    [Benchmark]
    public int AnimateSelectedElements()
    {
        _useFirstTargets = !_useFirstTargets;
        _selection.Animate(
            _useFirstTargets ? FirstTargets : SecondTargets,
            TransitionDuration);
        return _selection.Length;
    }
}
