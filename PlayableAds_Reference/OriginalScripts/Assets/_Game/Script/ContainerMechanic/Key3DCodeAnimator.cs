using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;

public class Key3DCodeAnimator : MonoBehaviour
{
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform scissorsL;
    [SerializeField] private Transform scissorsR;
    [SerializeField] private float scaleMultiplier = 1f;
    public float ScaleMultiplier => scaleMultiplier;

    private MotionHandle _cutMotionHandle;
    
    private Vector3 _origRootLocalPos;
    private Quaternion _origRootLocalRot;
    private Vector3 _origRootLocalScale;
    private bool _hasStoredRootTransform;

    private void StoreRootTransform()
    {
        if (rootTransform == null || _hasStoredRootTransform) return;
        _hasStoredRootTransform = true;
        _origRootLocalPos = rootTransform.localPosition;
        _origRootLocalRot = rootTransform.localRotation;
        _origRootLocalScale = rootTransform.localScale;
    }

    // Cut Animation Curves
    private AnimationCurve _cutPosX;
    private AnimationCurve _cutPosY;
    private AnimationCurve _cutPosZ;
    private AnimationCurve _cutRotX;
    private AnimationCurve _cutRotY;
    private AnimationCurve _cutRotZ;
    private AnimationCurve _cutScale;
    private AnimationCurve _cutScissorsLRotY;
    private AnimationCurve _cutScissorsRRotY;

    private void Awake()
    {
        InitializeCurves();
        ResetToDefault();
    }

    public void SetActiveState(bool active)
    {
        if (rootTransform != null)
        {
            rootTransform.gameObject.SetActive(active);
        }
    }

    private void ResetToDefault()
    {
        _cutMotionHandle.TryCancel();

        SetActiveState(false);
        if (rootTransform != null)
        {
            rootTransform.SetParent(transform);
            if (_hasStoredRootTransform)
            {
                rootTransform.localPosition = _origRootLocalPos;
                rootTransform.localRotation = _origRootLocalRot;
                rootTransform.localScale = _origRootLocalScale;
            }
            else
            {
                rootTransform.localPosition = Vector3.zero;
                rootTransform.localRotation = Quaternion.identity;
                rootTransform.localScale = Vector3.one * scaleMultiplier;
            }
        }
        if (scissorsL != null) scissorsL.localRotation = Quaternion.identity;
        if (scissorsR != null) scissorsR.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        _cutMotionHandle.TryCancel();
    }

    private void InitializeCurves()
    {
        // Key3D_Cut Curves (Duration: 1.6666666s)
        _cutPosX = CreateCurve(
            (0f, 4.44f), (0.3f, 5.19f), (0.36666667f, 0.26f), (0.43333334f, -3.9f),
            (0.6333333f, -4.76f), (0.8333333f, -5.15f), (0.9f, -0.28f),
            (0.96666664f, 3.67f), (1.1666666f, 5.63f), (1.6666666f, 5.65f)
        );
        _cutPosY = CreateCurve(
            (0f, 3.96f), (0.3f, 3.96f), (0.36666667f, 3.62f), (0.43333334f, 3.96f),
            (0.6333333f, 3.96f), (0.8333333f, 3.96f), (0.9f, 3.96f),
            (0.96666664f, 3.53f), (1.1666666f, 3.18f), (1.6666666f, 3.96f)
        );
        _cutPosZ = CreateCurve(
            (0f, 2.28f), (0.3f, 2.77f), (0.36666667f, -0.28f), (0.43333334f, -1.56f),
            (0.6333333f, 3.64f), (0.8333333f, 4.19f), (0.9f, 0.2f),
            (0.96666664f, -2.42f), (1.1666666f, -3.72f), (1.6666666f, -3.74f)
        );

        _cutRotX = CreateCurve(
            (0f, 0f), (0.3f, 0f), (0.36666667f, 0f), (0.43333334f, 0f),
            (0.6333333f, 32.91f), (0.8333333f, 0f), (0.9f, 0f),
            (0.96666664f, 0f), (1.1666666f, 0f), (1.6666666f, 0f)
        );
        _cutRotY = CreateCurve(
            (0f, -33.015f), (0.3f, -33.015f), (0.36666667f, -23.392f), (0.43333334f, -5.55f),
            (0.6333333f, 177.411f), (0.8333333f, 226.985f), (0.9f, 220.346f),
            (0.96666664f, 213.708f), (1.6666666f, 213.708f)
        );
        _cutRotZ = CreateCurve(
            (0f, 0f), (0.3f, 12.846f), (0.36666667f, 13.549f), (0.43333334f, -4.861f),
            (0.6333333f, 0f), (0.8333333f, 10.176f), (0.9f, 7.157f),
            (0.96666664f, 7.031f), (1.6666666f, 7.031f)
        );

        _cutScale = CreateCurve(
            (0f, 1.7707f), (0.3f, 2.2998857f), (0.36666667f, 1.3416668f), (0.43333334f, 1.7707f),
            (0.6333333f, 1.7707f), (0.8333333f, 2.3150363f), (0.9f, 1.3945271f),
            (0.96666664f, 1.5237536f), (1.1666666f, 1.3648576f), (1.6666666f, 2.290149f)
        );

        _cutScissorsLRotY = CreateCurve(
            (0f, -8.236f), (0.3f, 6.09f), (0.36666667f, -12.515f), (0.6333333f, -12.515f),
            (0.8333333f, 6.09f), (0.9f, -12.515f), (0.96666664f, -12.515f), (1.6666666f, -12.515f)
        );
        _cutScissorsRRotY = CreateCurve(
            (0f, 7.231f), (0.3f, -2.568f), (0.36666667f, 6.917f), (0.6333333f, 6.917f),
            (0.8333333f, -2.568f), (0.9f, 6.917f), (0.96666664f, 6.917f), (1.6666666f, 6.917f)
        );
    }

    private AnimationCurve CreateCurve(params (float time, float value)[] keys)
    {
        var curve = new AnimationCurve();
        foreach (var key in keys)
        {
            curve.AddKey(new Keyframe(key.time, key.value));
        }
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }
        return curve;
    }

    public async UniTask PlayFlyAndUnlockAnimationAsync(
        Vector3 startPosition, 
        Quaternion startRotation, 
        Vector3 startScale,
        float flyDuration, 
        Ease flyEase,
        Action onFirstCut,
        Action onSecondCut)
    {
        _cutMotionHandle.TryCancel();
        
        if (rootTransform == null) return;
        StoreRootTransform();

        // Targets for the flight in world coordinates (matching first frame of cut animation)
        Vector3 landingWorldPos = transform.position + (new Vector3(4.44f, 3.96f, 2.28f) * scaleMultiplier);
        Quaternion landingWorldRot = Quaternion.Euler(0f, 51.225f, 0f);
        float landingScale = 1.7707f * scaleMultiplier;
        Vector3 landingWorldScale = Vector3.one * landingScale;

        // 1. Setup for flight
        rootTransform.SetParent(null);
        SetActiveState(true);

        rootTransform.position = startPosition;
        rootTransform.localScale = landingWorldScale;
        rootTransform.rotation = startRotation;

        bool isKeyBelowContainer = startPosition.z < transform.position.z;

        if (isKeyBelowContainer)
        {
            // Case 1: Key is below container -> Run sequential phases (turn -> fly -> turn)
            
            // Calculate face target rotation
            Vector3 dir = (landingWorldPos - startPosition).normalized;
            float startAngleY = startRotation.eulerAngles.y;
            float targetAngleY = dir != Vector3.zero 
                ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 180f
                : startAngleY;
            Quaternion faceTargetRot = Quaternion.Euler(0f, targetAngleY, 0f);

            // Stage 1: Rotate to face target position
            float rotateDuration = 0.3f;
            var turn1Handle = LMotion.Create(0f, 1f, rotateDuration)
                .WithEase(Ease.InOutQuad)
                .Bind(t =>
                {
                    if (rootTransform != null)
                    {
                        rootTransform.rotation = Quaternion.Slerp(startRotation, faceTargetRot, t);
                    }
                });
            await turn1Handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            // Stage 2: Fly to target position (position & scale)
            var flightHandle = LMotion.Create(0f, 1f, flyDuration)
                .WithEase(flyEase)
                .Bind(t =>
                {
                    if (rootTransform != null)
                    {
                        rootTransform.position = Vector3.Lerp(startPosition, landingWorldPos, t);
                        rootTransform.localScale = landingWorldScale;
                    }
                });
            await flightHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            // Stage 3: Rotate to starting rotation of cut animation
            var turn2Handle = LMotion.Create(0f, 1f, rotateDuration)
                .WithEase(Ease.InOutQuad)
                .Bind(t =>
                {
                    if (rootTransform != null)
                    {
                        rootTransform.rotation = Quaternion.Slerp(faceTargetRot, landingWorldRot, t);
                    }
                });
            await turn2Handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        else
        {
            // Case 2: Key is above container -> Run simultaneous flight (original logic)
            var flightHandle = LMotion.Create(0f, 1f, flyDuration)
                .WithEase(flyEase)
                .Bind(t =>
                {
                    if (rootTransform != null)
                    {
                        rootTransform.position = Vector3.Lerp(startPosition, landingWorldPos, t);
                        rootTransform.localScale = landingWorldScale;
                        rootTransform.rotation = Quaternion.Slerp(startRotation, landingWorldRot, t);
                    }
                });
            await flightHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_cut);
        }

        // Reparent rootTransform back to this key animator transform before starting the cut animation
        if (rootTransform != null)
        {
            rootTransform.SetParent(transform);
        }

        // 2. Play Cut Animation (1.6666666s)
        bool triggeredCut1 = false;
        bool triggeredCut2 = false;
        _cutMotionHandle = LMotion.Create(0f, 1.6666666f, 1.6666666f)
            .Bind(elapsed => 
            {
                SetActiveState(elapsed < 1.1f);
                EvaluateCut(elapsed);
                
                // Trigger first cut callback at 0.366s (Frame 11)
                if (!triggeredCut1 && elapsed >= 0.366f)
                {
                    triggeredCut1 = true;
                    onFirstCut?.Invoke();
                }
                
                // Trigger second cut callback at 0.9s (Frame 27)
                if (!triggeredCut2 && elapsed >= 0.9f)
                {
                    triggeredCut2 = true;
                    onSecondCut?.Invoke();
                }
            });

        await _cutMotionHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (!triggeredCut1)
        {
            triggeredCut1 = true;
            onFirstCut?.Invoke();
        }
        if (!triggeredCut2)
        {
            triggeredCut2 = true;
            onSecondCut?.Invoke();
        }

        // Reset to default states
        ResetToDefault();
    }

    private void EvaluateCut(float time)
    {
        if (rootTransform != null)
        {
            Vector3 localOffset = new Vector3(_cutPosX.Evaluate(time), _cutPosY.Evaluate(time), _cutPosZ.Evaluate(time)) * scaleMultiplier;
            // Use position and rotation directly to ignore any parent scale distortion and rotation on the flight path
            rootTransform.position = transform.position + localOffset;
            rootTransform.rotation = Quaternion.Euler(_cutRotX.Evaluate(time), _cutRotY.Evaluate(time), _cutRotZ.Evaluate(time)) * Quaternion.Euler(0f, 84.24f, 0f);
            
            float scale = _cutScale.Evaluate(time) * scaleMultiplier;
            if (rootTransform.parent != null)
            {
                // Compensate for parent's lossy scale to keep uniform global scale and prevent squishing
                Vector3 parentScale = rootTransform.parent.lossyScale;
                rootTransform.localScale = new Vector3(
                    parentScale.x != 0f ? scale / parentScale.x : scale,
                    parentScale.y != 0f ? scale / parentScale.y : scale,
                    parentScale.z != 0f ? scale / parentScale.z : scale
                );
            }
            else
            {
                rootTransform.localScale = new Vector3(scale, scale, scale);
            }
        }
        if (scissorsL != null)
        {
            scissorsL.localRotation = Quaternion.Euler(0f, _cutScissorsLRotY.Evaluate(time), 0f);
        }
        if (scissorsR != null)
        {
            scissorsR.localRotation = Quaternion.Euler(0f, _cutScissorsRRotY.Evaluate(time), 0f);
        }
    }

    public void SetKeyColor(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetStylizedColorConfig() : null;
#if UNITY_EDITOR
        if (config == null)
        {
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<StylizedColorConfigSO>("Assets/_Game/Config/CoreGameConfig/StylizedColorConfigSO.asset");
        }
#endif
        if (config == null) return;
        var entry = config.GetColorEntry(colorType);
        if (entry == null) return;

        var propertyBlock = new MaterialPropertyBlock();
        var colorId = Shader.PropertyToID("_Color");
        var shadowColorId = Shader.PropertyToID("_ShadowColor");
        var specularColorId = Shader.PropertyToID("_SpecularColor");
        var reflectColorId = Shader.PropertyToID("_ReflectColor");

        var renderers = new[]
        {
            scissorsL != null ? scissorsL.GetComponent<Renderer>() : null,
            scissorsR != null ? scissorsR.GetComponent<Renderer>() : null
        };

        foreach (var r in renderers)
        {
            if (r == null) continue;
            int matCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 0;
            if (matCount >= 3)
            {
                int[] targetIndices = { 0, 2 };
                foreach (int i in targetIndices)
                {
                    r.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetColor(colorId, entry.Color);
                    propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                    propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                    propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                    r.SetPropertyBlock(propertyBlock, i);
                }
            }
            else
            {
                r.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor(colorId, entry.Color);
                propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                r.SetPropertyBlock(propertyBlock, 0);
            }
        }
    }
}
