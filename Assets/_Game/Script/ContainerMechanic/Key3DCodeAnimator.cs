using System;
using UnityEngine;
using DG.Tweening;

public sealed class Key3DCodeAnimator : MonoBehaviour
{
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform scissorsL;
    [SerializeField] private Transform scissorsR;
    [SerializeField] private float scaleMultiplier = 1f;

    private Sequence _animationSequence;
    private Transform _originalParent;
    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private Vector3 _originalLocalScale;
    private bool _initialized;

    public float ScaleMultiplier => scaleMultiplier;

    private void Awake()
    {
        StoreInitialState();
        ResetToDefault();
    }

    private void OnDisable()
    {
        Cancel();
        ResetToDefault();
    }

    public void SetActiveState(bool active)
    {
        if (rootTransform != null) rootTransform.gameObject.SetActive(active);
    }

    public void PlayFlyAndUnlockAnimation(
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 startScale,
        float flyDuration,
        DG.Tweening.Ease flyEase,
        Action onFirstCut,
        Action onSecondCut)
    {
        Cancel();
        StoreInitialState();
        if (rootTransform == null)
        {
            onFirstCut?.Invoke();
            onSecondCut?.Invoke();
            return;
        }

        rootTransform.SetParent(null, true);
        rootTransform.position = startPosition;
        rootTransform.rotation = startRotation;
        rootTransform.localScale = startScale;
        SetActiveState(true);

        var landingPosition = transform.TransformPoint(
            new Vector3(4.44f, 3.96f, 2.28f) * scaleMultiplier);
        var landingRotation = transform.rotation * Quaternion.Euler(0f, 51.225f, 0f);
        var landingScale = Vector3.one * (1.7707f * scaleMultiplier);

        _animationSequence = DOTween.Sequence().SetUpdate(true);
        _animationSequence.Join(rootTransform.DOMove(landingPosition, flyDuration).SetEase(flyEase));
        _animationSequence.Join(rootTransform.DORotateQuaternion(landingRotation, flyDuration).SetEase(flyEase));
        _animationSequence.Join(rootTransform.DOScale(landingScale, flyDuration).SetEase(flyEase));
        _animationSequence.AppendCallback(() =>
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayOneShot(AudioClipName.sfx_cut);
        });
        _animationSequence.AppendInterval(0.12f);
        _animationSequence.AppendCallback(() => onFirstCut?.Invoke());
        if (scissorsL != null)
            _animationSequence.Join(scissorsL.DOLocalRotate(new Vector3(0f, -18f, 0f), 0.16f));
        if (scissorsR != null)
            _animationSequence.Join(scissorsR.DOLocalRotate(new Vector3(0f, 18f, 0f), 0.16f));
        _animationSequence.AppendInterval(0.18f);
        _animationSequence.AppendCallback(() => onSecondCut?.Invoke());
        _animationSequence.AppendInterval(0.12f);
        _animationSequence.OnComplete(() =>
        {
            _animationSequence = null;
            ResetToDefault();
        });
    }

    public void SetKeyColor(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetStylizedColorConfig()
            : null;
        if (config == null) return;
        var entry = config.GetColorEntry(colorType);
        if (entry == null) return;

        ApplyColor(scissorsL != null ? scissorsL.GetComponent<Renderer>() : null, entry);
        ApplyColor(scissorsR != null ? scissorsR.GetComponent<Renderer>() : null, entry);
    }

    private static void ApplyColor(Renderer target, StylizedColorEntry entry)
    {
        if (target == null) return;
        for (var i = 0; i < target.sharedMaterials.Length; i++)
        {
            target.ApplyColorEntry(entry, i);
        }
    }

    private void StoreInitialState()
    {
        if (_initialized || rootTransform == null) return;
        _initialized = true;
        _originalParent = rootTransform.parent;
        _originalLocalPosition = rootTransform.localPosition;
        _originalLocalRotation = rootTransform.localRotation;
        _originalLocalScale = rootTransform.localScale;
    }

    private void Cancel()
    {
        if (_animationSequence != null)
        {
            _animationSequence.Kill();
            _animationSequence = null;
        }
    }

    private void ResetToDefault()
    {
        if (rootTransform == null || !_initialized) return;
        rootTransform.SetParent(_originalParent, false);
        rootTransform.localPosition = _originalLocalPosition;
        rootTransform.localRotation = _originalLocalRotation;
        rootTransform.localScale = _originalLocalScale;
        if (scissorsL != null) scissorsL.localRotation = Quaternion.identity;
        if (scissorsR != null) scissorsR.localRotation = Quaternion.identity;
        SetActiveState(false);
    }

}
