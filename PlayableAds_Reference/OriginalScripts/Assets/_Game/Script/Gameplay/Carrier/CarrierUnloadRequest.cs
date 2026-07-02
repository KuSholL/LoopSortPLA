using System.Collections.Generic;

/// <summary>
/// Mô tả một lượt carrier unload cube ra conveyor.
/// </summary>
public sealed class CarrierUnloadRequest
{
    private static int _nextUndoBatchId;

    public int UndoBatchId { get; }
    public CarrierBase SourceCarrier { get; }
    public int CarrierSessionId { get; }
    public IReadOnlyList<CarrierCubePayload> CubePayloads { get; }
    public IReadOnlyList<UndoSourceBlockSnapshot> SourceBlocks { get; }
    public int CubeCount => CubePayloads?.Count ?? 0;

    /// <summary>
    /// Khởi tạo request unload với carrier nguồn và danh sách cube cần spawn.
    /// </summary>
    public CarrierUnloadRequest(
        CarrierBase sourceCarrier,
        IReadOnlyList<CarrierCubePayload> cubePayloads,
        IReadOnlyList<UndoSourceBlockSnapshot> sourceBlocks)
    {
        UndoBatchId = CreateUndoBatchId();
        SourceCarrier = sourceCarrier;
        CarrierSessionId = sourceCarrier != null ? sourceCarrier.SessionId : 0;
        CubePayloads = cubePayloads;
        SourceBlocks = sourceBlocks;
    }

    private static int CreateUndoBatchId()
    {
        _nextUndoBatchId++;
        if (_nextUndoBatchId <= 0) _nextUndoBatchId = 1;
        return _nextUndoBatchId;
    }
}
