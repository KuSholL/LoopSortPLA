using System;
using System.Collections.Generic;
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

    [SerializeField] private LayerMask clickableLayerMask = ~0;
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
                var cameraManager = CameraManager.Instance;
                var camera = cameraManager != null ? cameraManager.MainCamera : Camera.main;
                if (camera != null)
                {
                    TryHandleClick(camera.ScreenPointToRay(Input.mousePosition));
                }
            }
            return;
        }

        var level = LevelManager.Instance;
        if (level != null && level.IsLevelLoaded && !level.IsGameEnded && !level.IsTutorial)
        {
            _noInputTimer += Time.deltaTime;
            if (_noInputTimer >= NoInputThreshold)
            {
                _noInputTimer = 0f;
                if (DeadlockDetector.IsGameDeadlocked())
                {
                    if (GameEventBus.OnLoseTrigger != null)
                    {
                        GameEventBus.OnLoseTrigger(ELoseReason.Deadlock);
                    }
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
        var effectiveMask = clickableLayerMask | _additionalClickableLayerMask;
        var hitCount = Physics.RaycastNonAlloc(ray, HitBuffer, Mathf.Infinity, effectiveMask);
        if (hitCount <= 0)
        {
            return;
        }

        Array.Sort(HitBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (var i = 0; i < hitCount; i++)
        {
            var collider = HitBuffer[i].collider;
            if (collider == null || IsIgnoredLayer(collider.gameObject.layer))
            {
                continue;
            }

            if (!IsAllowedByClickableLayerOverride(collider.gameObject.layer))
            {
                continue;
            }

            IClickableObject clickable;
            if (!TryGetClickable(collider, out clickable) || !clickable.Interactable)
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
            return;
        }
    }

    private static bool TryGetClickable(Collider collider, out IClickableObject clickable)
    {
        if (collider.TryGetComponent(out clickable))
        {
            return true;
        }

        var parent = collider.transform.parent;
        return parent != null && parent.TryGetComponent(out clickable);
    }

    private static bool IsIgnoredLayer(int layer)
    {
        return (_ignoredLayerMask & (1 << layer)) != 0;
    }

    private static bool IsAllowedByClickableLayerOverride(int layer)
    {
        return !_hasClickableLayerOverride ||
               (layer >= 0 && (_clickableLayerOverrideMask & (1 << layer)) != 0);
    }

    public static void Disable() { _isActive = false; }
    public static void Enable() { _isActive = true; }

    public static void IgnoreLayer(int layer)
    {
        if (layer >= 0) _ignoredLayerMask |= 1 << layer;
    }

    public static void UnignoreLayer(int layer)
    {
        if (layer >= 0) _ignoredLayerMask &= ~(1 << layer);
    }

    public static void ResetIgnoredLayers() { _ignoredLayerMask = 0; }

    public static void SetAdditionalClickableLayers(params int[] layers)
    {
        _additionalClickableLayerMask = 0;
        if (layers == null) return;
        for (var i = 0; i < layers.Length; i++)
        {
            if (layers[i] >= 0) _additionalClickableLayerMask |= 1 << layers[i];
        }
    }

    public static void ResetAdditionalClickableLayers() { _additionalClickableLayerMask = 0; }

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
        if (EventSystem.current == null) return false;
        for (var i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
            {
                return true;
            }
        }
        return EventSystem.current.IsPointerOverGameObject();
    }
}

public sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
{
    public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

    public int Compare(RaycastHit x, RaycastHit y)
    {
        return x.distance.CompareTo(y.distance);
    }
}
