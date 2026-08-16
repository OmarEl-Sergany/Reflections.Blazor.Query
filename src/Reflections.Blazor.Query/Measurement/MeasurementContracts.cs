namespace Reflections.Blazor.Query;

public enum MeasurementKind
{
    Height,
    Width,
    InnerHeight,
    InnerWidth,
    OuterHeight,
    OuterWidth,
    Offset,
    Position,
    OffsetParent
}

public sealed record MeasurementRequest(string ElementKey, MeasurementKind Kind);

public sealed record QueryPoint(double Left, double Top);

public sealed record MeasurementReading(
    string ElementKey,
    MeasurementKind Kind,
    double? Value = null,
    QueryPoint? Point = null,
    string? RelatedElementKey = null);

public interface IQueryMeasurementBridge
{
    ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
        IReadOnlyList<MeasurementRequest> requests,
        CancellationToken cancellationToken);
}
