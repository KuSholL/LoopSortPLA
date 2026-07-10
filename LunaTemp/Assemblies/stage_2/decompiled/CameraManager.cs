using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private Camera highlightCamera;

	private readonly List<ActiveCameraPlace> _cameraLocks = new List<ActiveCameraPlace>();

	private Vector3 _originalMainCameraPosition;

	private int _defaultHighlightMask;

	private Coroutine _shakeRoutine;

	public Camera MainCamera => mainCamera;

	protected override void Awake()
	{
		base.Awake();
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}
		if (mainCamera != null)
		{
			_originalMainCameraPosition = mainCamera.transform.localPosition;
			if (mainCamera.GetComponent<AudioListener>() == null)
			{
				mainCamera.gameObject.AddComponent<AudioListener>();
			}
		}
		_defaultHighlightMask = ((highlightCamera != null) ? highlightCamera.cullingMask : 0);
		GameEventBus.OnActiveCameraGameplay = (Action<ActiveCameraPlace, bool>)Delegate.Combine(GameEventBus.OnActiveCameraGameplay, new Action<ActiveCameraPlace, bool>(SetActiveCameraPlace));
	}

	protected override void OnDestroy()
	{
		GameEventBus.OnActiveCameraGameplay = (Action<ActiveCameraPlace, bool>)Delegate.Remove(GameEventBus.OnActiveCameraGameplay, new Action<ActiveCameraPlace, bool>(SetActiveCameraPlace));
		base.OnDestroy();
	}

	private void SetActiveCameraPlace(ActiveCameraPlace place, bool active)
	{
		if (active)
		{
			_cameraLocks.Remove(place);
		}
		else if (!_cameraLocks.Contains(place))
		{
			_cameraLocks.Add(place);
		}
		if (mainCamera != null)
		{
			mainCamera.enabled = _cameraLocks.Count == 0;
		}
	}

	public Coroutine ShakeCamera(float duration, float magnitude)
	{
		if (_shakeRoutine != null)
		{
			StopCoroutine(_shakeRoutine);
			RestoreCameraPosition();
		}
		_shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
		return _shakeRoutine;
	}

	private IEnumerator ShakeRoutine(float duration, float magnitude)
	{
		if (!(mainCamera == null))
		{
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				Vector3 offset = UnityEngine.Random.insideUnitSphere * magnitude;
				offset.z = 0f;
				mainCamera.transform.localPosition = _originalMainCameraPosition + offset;
				yield return null;
			}
			RestoreCameraPosition();
			_shakeRoutine = null;
		}
	}

	private void RestoreCameraPosition()
	{
		if (mainCamera != null)
		{
			mainCamera.transform.localPosition = _originalMainCameraPosition;
		}
	}

	public void SetHighlightCameraActive(bool active, params int[] sizes)
	{
		if (!(highlightCamera == null))
		{
			highlightCamera.gameObject.SetActive(active);
			if (active && sizes != null && sizes.Length != 0)
			{
				SetHighlightCameraMaskBySize(sizes);
			}
			if (GameEventBus.OnHighlightCameraActiveChanged != null)
			{
				GameEventBus.OnHighlightCameraActiveChanged(active);
			}
		}
	}

	public void ResetHighlightCameraMask()
	{
		if (highlightCamera != null)
		{
			highlightCamera.cullingMask = _defaultHighlightMask;
		}
	}

	public void SetHighlightCameraToCarrier()
	{
		SetHighlightMask(true);
	}

	public void SetHighlightCameraToCarrierAndHighlightTargets()
	{
		SetHighlightMask(true, 1, 2, 3, 4);
	}

	public void SetHighlightCameraToCarrierAndSelectedSource()
	{
		SetHighlightMask(true, 1, 2, 3, 4);
	}

	private void SetHighlightCameraMaskBySize(int[] sizes)
	{
		SetHighlightMask(false, sizes);
	}

	private void SetHighlightMask(bool includeCarrier, params int[] sizes)
	{
		if (highlightCamera == null)
		{
			return;
		}
		int mask = 0;
		if (includeCarrier)
		{
			AddLayer(ref mask, "Carrier");
		}
		if (sizes != null)
		{
			for (int i = 0; i < sizes.Length; i++)
			{
				if (sizes[i] >= 1 && sizes[i] <= 4)
				{
					AddLayer(ref mask, "HighlightSlime" + sizes[i] + "x");
				}
			}
		}
		AddLayer(ref mask, "ClawMachine");
		highlightCamera.cullingMask = mask;
	}

	private static void AddLayer(ref int mask, string layerName)
	{
		int layer = LayerMask.NameToLayer(layerName);
		if (layer >= 0)
		{
			mask |= 1 << layer;
		}
	}

	public void SyncOrthographicCamera(int orthographicSize)
	{
		if (mainCamera != null)
		{
			mainCamera.orthographicSize = orthographicSize;
		}
		if (highlightCamera != null)
		{
			highlightCamera.orthographicSize = orthographicSize;
		}
	}
}
