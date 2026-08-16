namespace Reflections.Blazor.Query;

public enum MeasurementKind
{
    Height,
    Width,
    InnerHeight,
    InnerWidth,
    OuterHeight,
    OuterWidth
}

public sealed record MeasurementRequest(string ElementKey, MeasurementKind Kind);

public sealed record MeasurementReading(string ElementKey, MeasurementKind Kind, double? Value);

public interface IQueryMeasurementBridge
{
    ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
        IReadOnlyList<MeasurementRequest> requests,
        CancellationToken cancellationToken);
}
