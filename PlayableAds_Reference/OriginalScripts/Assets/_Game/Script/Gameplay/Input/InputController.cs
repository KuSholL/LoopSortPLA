using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    private static bool isActive = true;
    private static int ignoredLayerMask;
    private static int additionalClickableLayerMask;
    private static bool hasClickableLayerOverride;
    private static int clickableLayerOverrideMask;
    public static System.Action<IClickableObject> OnClickableObjectClicked;

    [SerializeField] private LayerMask clickableLayerMask = ~0;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    private float _noInputTimer = 0f;
    private const float NoInputThreshold = 5.0f;

    private void OnEnable()
    {
        _noInputTimer = 0f;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            _noInputTimer = 0f;
            if (IsPointerOverUI())
            {
                return;
            }

            var mainCamera = CameraManager.Instance.MainCamera;
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            TryHandleClick(ray);
        }
        else
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsState(GameState.InGame)
                && LevelManager.Instance != null && !LevelManager.Instance.IsGameEnded
                && !LevelManager.Instance.IsTutorial
                && (LayerManager.Instance == null || !LayerManager.Instance.IsAnyPopupShowing()))
            {
                _noInputTimer += Time.deltaTime;
                if (_noInputTimer >= NoInputThreshold)
                {
                    _noInputTimer = 0f;
                    CheckLoseConditionOnNoInput();
                }
            }
            else
            {
                _noInputTimer = 0f;
            }
        }
    }

    private void CheckLoseConditionOnNoInput()
    {
        if (DeadlockDetector.IsGameDeadlocked())
        {
            GameEventBus.OnLoseTrigger?.Invoke(ELoseReason.Deadlock);
        }
    }

    private void TryHandleClick(Ray ray)
    {
        var effectiveMask = clickableLayerMask | additionalClickableLayerMask;
        var hitCount = Physics.RaycastNonAlloc(ray, HitBuffer, Mathf.Infinity, effectiveMask);
        if (hitCount <= 0)
        {
            HandleMissedClickable();
            return;
        }

        System.Array.Sort(HitBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (var i = 0; i < hitCount; i++)
        {
            var collider = HitBuffer[i].collider;
            if (collider == null || IsIgnoredLayer(collider.gameObject.layer)) continue;
            if (!IsAllowedByClickableLayerOverride(collider.gameObject.layer)) continue;
            if (TryHandleColliderClick(collider))
            {
                HeartSystem.Instance.HasPlayedLevel = true;
                return;
            }
        }
        HandleMissedClickable();
    }

    private bool TryHandleColliderClick(Collider collider)
    {
        if (collider.TryGetComponent(out IClickableObject clickableObject))
        {
            if (!clickableObject.Interactable) return false;
            TryPerformNormalClick(clickableObject);
            return true;
        }

        var parent = collider.transform.parent;
        if (parent == null || !parent.TryGetComponent(out IClickableObject clickable)) return false;
        if (!clickable.Interactable) return false;
        TryPerformNormalClick(clickable);
        return true;
    }

    private static bool IsIgnoredLayer(int layer)
    {
        return (ignoredLayerMask & (1 << layer)) != 0;
    }

    private static bool IsAllowedByClickableLayerOverride(int layer)
    {
        if (!hasClickableLayerOverride) return true;
        if (layer < 0) return false;
        return (clickableLayerOverrideMask & (1 << layer)) != 0;
    }

    private void TryPerformNormalClick(IClickableObject clickableObject)
    {
        if (clickableObject.CanBeClicked())
        {
            clickableObject.OnObjectClicked();
            OnClickableObjectClicked?.Invoke(clickableObject);
        }
        else
        {
            clickableObject.OnClickBlocked();
        }
    }

    private static void HandleMissedClickable()
    {
        if (BoosterSystem.Instance == null) return;
        BoosterSystem.Instance.TryCancelClawBoosterOnMissedClick();
    }

    public static void Disable()
    {
        isActive = false;
    }

    public static void Enable()
    {
        isActive = true;
    }

    public static void IgnoreLayer(int layer)
    {
        if (layer < 0) return;
        ignoredLayerMask |= 1 << layer;
    }

    public static void UnignoreLayer(int layer)
    {
        if (layer < 0) return;
        ignoredLayerMask &= ~(1 << layer);
    }

    public static void ResetIgnoredLayers()
    {
        ignoredLayerMask = 0;
    }

    public static void SetAdditionalClickableLayers(params int[] layers)
    {
        additionalClickableLayerMask = 0;
        if (layers == null) return;
        foreach (var layer in layers)
        {
            if (layer < 0) continue;
            additionalClickableLayerMask |= 1 << layer;
        }
    }

    public static void ResetAdditionalClickableLayers()
    {
        additionalClickableLayerMask = 0;
    }

    public static void SetClickableLayerOverrideMask(int layerMask)
    {
        hasClickableLayerOverride = true;
        clickableLayerOverrideMask = layerMask;
    }

    public static void ResetClickableLayerOverrideMask()
    {
        hasClickableLayerOverride = false;
        clickableLayerOverrideMask = 0;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

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

public sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
{
    public static readonly RaycastHitDistanceComparer Instance = new();

    public int Compare(RaycastHit x, RaycastHit y)
    {
        return x.distance.CompareTo(y.distance);
    }
}
