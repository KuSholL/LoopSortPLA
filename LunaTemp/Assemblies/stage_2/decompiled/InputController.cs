using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
	private const float NoInputThreshold = 5f;

	private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

	private static bool _isActive = true;

	private static int _ignoredLayerMask;

	private static int _additionalClickableLayerMask;

	private static bool _hasClickableLayerOverride;

	private static int _clickableLayerOverrideMask;

	public static Action<IClickableObject> OnClickableObjectClicked;

	[SerializeField]
	private LayerMask clickableLayerMask = -1;

	private float _noInputTimer;

	private void OnEnable()
	{
		_noInputTimer = 0f;
	}

	private void Update()
	{
		if (!_isActive)
		{
			return;
		}
		if (Input.GetMouseButtonDown(0))
		{
			_noInputTimer = 0f;
			if (!IsPointerOverUI())
			{
				CameraManager cameraManager = MonoSingleton<CameraManager>.Instance;
				Camera camera = ((cameraManager != null) ? cameraManager.MainCamera : Camera.main);
				if (camera != null)
				{
					TryHandleClick(camera.ScreenPointToRay(Input.mousePosition));
				}
			}
			return;
		}
		LevelManager level = MonoSingleton<LevelManager>.Instance;
		if (level != null && level.IsLevelLoaded && !level.IsGameEnded && !level.IsTutorial)
		{
			_noInputTimer += Time.deltaTime;
			if (_noInputTimer >= 5f)
			{
				_noInputTimer = 0f;
				if (DeadlockDetector.IsGameDeadlocked() && GameEventBus.OnLoseTrigger != null)
				{
					GameEventBus.OnLoseTrigger(ELoseReason.Deadlock);
				}
			}
		}
		else
		{
			_noInputTimer = 0f;
		}
	}

	private void TryHandleClick(Ray ray)
	{
		int effectiveMask = (int)clickableLayerMask | _additionalClickableLayerMask;
		int hitCount = Physics.RaycastNonAlloc(ray, HitBuffer, float.PositiveInfinity, effectiveMask);
		if (hitCount <= 0)
		{
			return;
		}
		Array.Sort(HitBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);
		for (int i = 0; i < hitCount; i++)
		{
			Collider collider = HitBuffer[i].collider;
			if (collider == null || IsIgnoredLayer(collider.gameObject.layer) || !IsAllowedByClickableLayerOverride(collider.gameObject.layer) || !TryGetClickable(collider, out var clickable) || !clickable.Interactable)
			{
				continue;
			}
			if (clickable.CanBeClicked())
			{
				clickable.OnObjectClicked();
				if (OnClickableObjectClicked != null)
				{
					OnClickableObjectClicked(clickable);
				}
			}
			else
			{
				clickable.OnClickBlocked();
			}
			break;
		}
	}

	private static bool TryGetClickable(Collider collider, out IClickableObject clickable)
	{
		if (collider.TryGetComponent<IClickableObject>(out clickable))
		{
			return true;
		}
		Transform parent = collider.transform.parent;
		return parent != null && parent.TryGetComponent<IClickableObject>(out clickable);
	}

	private static bool IsIgnoredLayer(int layer)
	{
		return (_ignoredLayerMask & (1 << layer)) != 0;
	}

	private static bool IsAllowedByClickableLayerOverride(int layer)
	{
		return !_hasClickableLayerOverride || (layer >= 0 && (_clickableLayerOverrideMask & (1 << layer)) != 0);
	}

	public static void Disable()
	{
		_isActive = false;
	}

	public static void Enable()
	{
		_isActive = true;
	}

	public static void IgnoreLayer(int layer)
	{
		if (layer >= 0)
		{
			_ignoredLayerMask |= 1 << layer;
		}
	}

	public static void UnignoreLayer(int layer)
	{
		if (layer >= 0)
		{
			_ignoredLayerMask &= ~(1 << layer);
		}
	}

	public static void ResetIgnoredLayers()
	{
		_ignoredLayerMask = 0;
	}

	public static void SetAdditionalClickableLayers(params int[] layers)
	{
		_additionalClickableLayerMask = 0;
		if (layers == null)
		{
			return;
		}
		for (int i = 0; i < layers.Length; i++)
		{
			if (layers[i] >= 0)
			{
				_additionalClickableLayerMask |= 1 << layers[i];
			}
		}
	}

	public static void ResetAdditionalClickableLayers()
	{
		_additionalClickableLayerMask = 0;
	}

	public static void SetClickableLayerOverrideMask(int layerMask)
	{
		_hasClickableLayerOverride = true;
		_clickableLayerOverrideMask = layerMask;
	}

	public static void ResetClickableLayerOverrideMask()
	{
		_hasClickableLayerOverride = false;
		_clickableLayerOverrideMask = 0;
	}

	private static bool IsPointerOverUI()
	{
		if (EventSystem.current == null)
		{
			return false;
		}
		for (int i = 0; i < Input.touchCount; i++)
		{
			if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
			{
				return true;
			}
		}
		return EventSystem.current.IsPointerOverGameObject();
	}
}
