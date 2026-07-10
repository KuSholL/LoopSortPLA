using UnityEngine;
using DG.Tweening;

public sealed class ContainerKey : MonoBehaviour
{
    [SerializeField] private KeyAnim keyPrefab;
    [SerializeField] private float unlockDuration = 1f;
    [SerializeField] private DG.Tweening.Ease unlockEase = DG.Tweening.Ease.InQuad;
    [SerializeField] private bool useOverrideFlightY;
    [SerializeField] private float flightY;

    private EBlockColorType _colorType;
    private int _targetContainerId = -1;
    private bool _isActive;
    private bool _isConsumed;

    public bool IsConsumed => _isConsumed;

    public void Configure(
        bool isActive,
        EBlockColorType colorType,
        int targetContainerId,
        bool isConsumed = false)
    {
        _isActive = isActive;
        _colorType = colorType;
        _targetContainerId = targetContainerId;
        _isConsumed = isConsumed;
    }

    public void RevealAndUnlock(
        Vector3? customStartPosition = null,
        Quaternion? customStartRotation = null)
    {
        if (!_isActive || _isConsumed) return;
        _isConsumed = true;

        var container = ContainerMechanic.FindTarget(_targetContainerId, _colorType);
        if (container == null)
        {
            ContainerMechanic.UnlockTarget(_targetContainerId, _colorType);
            return;
        }

        container.IsAssignedToUnlock = true;
        var startPosition = customStartPosition ?? transform.position;
        if (useOverrideFlightY) startPosition.y = flightY;
        var startRotation = customStartRotation ?? Quaternion.identity;
        startRotation = Quaternion.Euler(0f, startRotation.eulerAngles.y, 0f);
        var startScale = keyPrefab != null ? keyPrefab.transform.localScale : Vector3.one;
        container.StartUnlockSequence(
            startPosition,
            startRotation,
            startScale,
            unlockDuration,
            unlockEase);
    }
}
