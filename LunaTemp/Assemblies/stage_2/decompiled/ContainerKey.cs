using DG.Tweening;
using UnityEngine;

public sealed class ContainerKey : MonoBehaviour
{
	[SerializeField]
	private KeyAnim keyPrefab;

	[SerializeField]
	private float unlockDuration = 1f;

	[SerializeField]
	private Ease unlockEase = Ease.InQuad;

	[SerializeField]
	private bool useOverrideFlightY;

	[SerializeField]
	private float flightY;

	private EBlockColorType _colorType;

	private int _targetContainerId = -1;

	private bool _isActive;

	private bool _isConsumed;

	public bool IsConsumed => _isConsumed;

	public void Configure(bool isActive, EBlockColorType colorType, int targetContainerId, bool isConsumed = false)
	{
		_isActive = isActive;
		_colorType = colorType;
		_targetContainerId = targetContainerId;
		_isConsumed = isConsumed;
	}

	public void RevealAndUnlock(Vector3? customStartPosition = null, Quaternion? customStartRotation = null)
	{
		if (!_isActive || _isConsumed)
		{
			return;
		}
		_isConsumed = true;
		ContainerMechanic container = ContainerMechanic.FindTarget(_targetContainerId, _colorType);
		if (container == null)
		{
			ContainerMechanic.UnlockTarget(_targetContainerId, _colorType);
			return;
		}
		container.IsAssignedToUnlock = true;
		Vector3 startPosition = customStartPosition ?? base.transform.position;
		if (useOverrideFlightY)
		{
			startPosition.y = flightY;
		}
		Quaternion startRotation = Quaternion.Euler(0f, (customStartRotation ?? Quaternion.identity).eulerAngles.y, 0f);
		Vector3 startScale = ((keyPrefab != null) ? keyPrefab.transform.localScale : Vector3.one);
		container.StartUnlockSequence(startPosition, startRotation, startScale, unlockDuration, unlockEase);
	}
}
