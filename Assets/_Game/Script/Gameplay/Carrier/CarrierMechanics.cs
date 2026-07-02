using System.Collections.Generic;

public enum ECarrierActionType
{
    Interact = 0,
    Unload = 1,
    Receive = 2,
}

public enum ECarrierVisualKind
{
    None = 0,
    HiddenShell = 1,
    OneWayOverlay = 2,
    SpecialColorReceiver = 3,
}

public struct CarrierActionBlock
{
    public readonly ECarrierActionType ActionType;
    public readonly int Priority;
    public readonly string Reason;

    public CarrierActionBlock(ECarrierActionType actionType, int priority, string reason)
    {
        ActionType = actionType;
        Priority = priority;
        Reason = reason;
    }
}

public struct CarrierActionEvaluationResult
{
    public readonly bool IsAllowed;
    public readonly CarrierActionBlock? Block;

    public CarrierActionEvaluationResult(bool isAllowed, CarrierActionBlock? block)
    {
        IsAllowed = isAllowed;
        Block = block;
    }

    public static CarrierActionEvaluationResult Allow()
    {
        return new CarrierActionEvaluationResult(true, null);
    }

    public static CarrierActionEvaluationResult Blocked(CarrierActionBlock block)
    {
        return new CarrierActionEvaluationResult(false, block);
    }
}

public sealed class CarrierVisualRequest
{
    public ECarrierVisualKind Kind = ECarrierVisualKind.None;
    public int Priority;
    public EBlockColorType ColorType = EBlockColorType.None;
    public bool HideCarrierRenderers;
}

public interface ICarrierMechanicRuntime
{
    ECarrierMechanic Type { get; }
}

public interface ICarrierInteractRuleProvider : ICarrierMechanicRuntime
{
    CarrierActionBlock? GetInteractBlock(Carrier carrier);
}

public interface ICarrierUnloadRuleProvider : ICarrierMechanicRuntime
{
    CarrierActionBlock? GetUnloadBlock(Carrier carrier);
}

public interface ICarrierReceiveRuleProvider : ICarrierMechanicRuntime
{
    CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType);
}

public interface ISpecialColorReceiverMechanic : ICarrierMechanicRuntime
{
    EBlockColorType TargetColor { get; }
}

public interface ICarrierVisualRequestProvider : ICarrierMechanicRuntime
{
    CarrierVisualRequest GetVisualRequest(Carrier carrier);
}

public interface ICarrierEventListener : ICarrierMechanicRuntime
{
    void HandleEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent);
}

public interface ICarrierResettableMechanic : ICarrierMechanicRuntime
{
    void Reset(Carrier carrier);
}

public interface ICarrierMechanicEvent
{
}

public struct CarrierFinishedColorEvent : ICarrierMechanicEvent
{
    public readonly EBlockColorType ColorType;

    public CarrierFinishedColorEvent(EBlockColorType colorType)
    {
        ColorType = colorType;
    }
}

public sealed class CarrierMechanicContainer
{
    private readonly List<ICarrierMechanicRuntime> _mechanics = new List<ICarrierMechanicRuntime>();

    public IReadOnlyList<ICarrierMechanicRuntime> Mechanics => _mechanics;

    public void Rebuild(IEnumerable<CarrierMechanicData> mechanicDatas)
    {
        _mechanics.Clear();
        if (mechanicDatas == null) return;

        foreach (var mechanicData in mechanicDatas)
        {
            var mechanic = CreateMechanicRuntime(mechanicData);
            if (mechanic != null) _mechanics.Add(mechanic);
        }
    }

    public void RemoveMechanic(ECarrierMechanic type)
    {
        _mechanics.RemoveAll(m => m.Type == type);
    }

    public void Reset(Carrier carrier)
    {
        foreach (var mechanic in _mechanics)
        {
            var resettable = mechanic as ICarrierResettableMechanic;
            if (resettable != null) resettable.Reset(carrier);
        }
    }

    public void DispatchEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent)
    {
        foreach (var mechanic in _mechanics)
        {
            var listener = mechanic as ICarrierEventListener;
            if (listener != null) listener.HandleEvent(carrier, carrierEvent);
        }
    }

    private static ICarrierMechanicRuntime CreateMechanicRuntime(CarrierMechanicData mechanicData)
    {
        if (mechanicData == null) return null;

        if (mechanicData.Type == ECarrierMechanic.SpecialColorReceiver)
        {
            var targetColor = mechanicData.TargetColor != EBlockColorType.None
                ? mechanicData.TargetColor
                : mechanicData.UnlockColor;
            return new SpecialColorReceiverMechanicRuntime(targetColor);
        }

        return null;
    }
}

public sealed class CarrierActionGateResolver
{
    public CarrierActionEvaluationResult EvaluateInteract(Carrier carrier)
    {
        return Evaluate(carrier, ECarrierActionType.Interact, null);
    }

    public CarrierActionEvaluationResult EvaluateUnload(Carrier carrier)
    {
        return Evaluate(carrier, ECarrierActionType.Unload, null);
    }

    public CarrierActionEvaluationResult EvaluateReceive(Carrier carrier, EBlockColorType colorType)
    {
        return Evaluate(carrier, ECarrierActionType.Receive, colorType);
    }

    private CarrierActionEvaluationResult Evaluate(Carrier carrier, ECarrierActionType actionType, EBlockColorType? colorType)
    {
        if (carrier.IsLockedByContainer())
            return CarrierActionEvaluationResult.Blocked(
                new CarrierActionBlock(actionType, 100, "Locked by container"));

        CarrierActionBlock? highestPriorityBlock = null;
        foreach (var mechanic in carrier.MechanicContainer.Mechanics)
        {
            CarrierActionBlock? nextBlock = null;
            if (actionType == ECarrierActionType.Interact)
            {
                var interactProvider = mechanic as ICarrierInteractRuleProvider;
                if (interactProvider != null)
                    nextBlock = interactProvider.GetInteractBlock(carrier);
            }
            else if (actionType == ECarrierActionType.Unload)
            {
                var unloadProvider = mechanic as ICarrierUnloadRuleProvider;
                if (unloadProvider != null)
                    nextBlock = unloadProvider.GetUnloadBlock(carrier);
            }
            else if (actionType == ECarrierActionType.Receive && colorType.HasValue)
            {
                var receiveProvider = mechanic as ICarrierReceiveRuleProvider;
                if (receiveProvider != null)
                    nextBlock = receiveProvider.GetReceiveBlock(carrier, colorType.Value);
            }

            if (!nextBlock.HasValue) continue;
            if (!highestPriorityBlock.HasValue || nextBlock.Value.Priority > highestPriorityBlock.Value.Priority)
                highestPriorityBlock = nextBlock;
        }

        return highestPriorityBlock.HasValue
            ? CarrierActionEvaluationResult.Blocked(highestPriorityBlock.Value)
            : CarrierActionEvaluationResult.Allow();
    }
}

public sealed class CarrierVisualResolver
{
    public CarrierVisualRequest Resolve(Carrier carrier)
    {
        CarrierVisualRequest selectedRequest = null;
        foreach (var mechanic in carrier.MechanicContainer.Mechanics)
        {
            var requestProvider = mechanic as ICarrierVisualRequestProvider;
            if (requestProvider == null) continue;
            var nextRequest = requestProvider.GetVisualRequest(carrier);
            if (nextRequest == null || nextRequest.Kind == ECarrierVisualKind.None) continue;

            if (selectedRequest == null || nextRequest.Priority > selectedRequest.Priority)
                selectedRequest = nextRequest;
        }

        return selectedRequest ?? new CarrierVisualRequest();
    }
}
