using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public class ContainerKey : MonoBehaviour
{
    [SerializeField] private KeyAnim keyPrefab;
    [SerializeField] private float unlockDuration = 1f;
    [SerializeField] private Ease unlockEase = Ease.InQuad;
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private Ease rotationEase = Ease.InOutQuad;
    [SerializeField] private Vector3 targetScale = Vector3.zero * 0.25f;

    [Header("Flight Height Config")]
    [SerializeField] private bool useOverrideFlightY = false;
    [SerializeField] private float flightY = 0f;

    [Header("Sound Config")]
    [SerializeField] private float soundDelay = 0f;

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
        if (!_isActive || _isConsumed) return;
        _isConsumed = true;

        var targetContainer = ContainerMechanic.FindTarget(_targetContainerId, _colorType);
        if (targetContainer != null)
        {
            targetContainer.IsAssignedToUnlock = true;
        }

        var spawnPosition = customStartPosition ?? transform.position;
        var spawnRotation = customStartRotation ?? Quaternion.Euler(0f, 0f, 0f);
        spawnRotation = Quaternion.Euler(0f, spawnRotation.eulerAngles.y, 0f);

        if (targetContainer != null && targetContainer.KeyAnimator != null)
        {
            targetContainer.StartUnlockSequence(
                spawnPosition, 
                spawnRotation, 
                keyPrefab != null ? keyPrefab.transform.localScale : Vector3.one, 
                unlockDuration, 
                unlockEase
            );
        }
        else if (keyPrefab != null)
        {
            var cloneKey = PoolManagerNew.Instance.PopFromPool(keyPrefab);

            if (cloneKey != null)
            {
                cloneKey.transform.position = spawnPosition;
                cloneKey.transform.rotation = spawnRotation;
                cloneKey.transform.localScale = keyPrefab.transform.localScale;

                cloneKey.SetKeyTexture(_colorType);
                PlayUnlockSequenceAsync(cloneKey.gameObject, spawnPosition, spawnRotation).Forget();
            }
        }
    }

    private async UniTaskVoid PlayUnlockSequenceAsync(GameObject cloneObj, Vector3 startPosition, Quaternion startRotation)
    {
        int targetContainerId = _targetContainerId;
        EBlockColorType colorType = _colorType;

        cloneObj.transform.SetParent(null);
        cloneObj.SetActive(true);
        startPosition.y = flightY;
        cloneObj.transform.position = startPosition;

        // Force initial rotation to lie flat
        startRotation = Quaternion.Euler(0f, startRotation.eulerAngles.y, 0f);
        cloneObj.transform.rotation = startRotation;

        var targetContainer = ContainerMechanic.FindTarget(targetContainerId, colorType);
        float scaleMultiplier = 0.4f;
        if (targetContainer != null && targetContainer.KeyAnimator != null)
        {
            scaleMultiplier = targetContainer.KeyAnimator.ScaleMultiplier;
        }

        Vector3 unlockTarget;
        Quaternion targetRot;
        Vector3 finalScale;

        if (targetContainer != null)
        {
            unlockTarget = targetContainer.transform.position + targetContainer.transform.rotation * (new Vector3(4.44f, 3.96f, 2.28f) * scaleMultiplier);
            targetRot = targetContainer.transform.rotation * Quaternion.Euler(0f, 51.225f, 0f);
            finalScale = Vector3.one * (1.7707f * scaleMultiplier);
        }
        else
        {
            unlockTarget = GetUnlockTargetPosition(targetContainerId, colorType, startPosition);
            if (useOverrideFlightY)
            {
                unlockTarget.y = flightY;
            }
            targetRot = startRotation;
            finalScale = targetScale;
        }

        Vector3 initialScale = keyPrefab.transform.localScale;
        var moveHandle = LMotion.Create(0f, 1f, unlockDuration)
            .WithEase(unlockEase)
            .Bind(t => {
                if (cloneObj != null)
                {
                    cloneObj.transform.position = Vector3.Lerp(startPosition, unlockTarget, t);
                    cloneObj.transform.localScale = Vector3.Lerp(initialScale, finalScale, t);
                    
                    Quaternion baseRot = Quaternion.Slerp(startRotation, targetRot, t);
                    cloneObj.transform.rotation = baseRot;
                }
            });

        await moveHandle.ToUniTask();

        if (SoundManager.Instance != null)
        {
            PlaySoundWithDelay(soundDelay).Forget();
        }

        ContainerMechanic.UnlockTarget(targetContainerId, colorType);
        
        if (cloneObj != null)
        {
            var keyAnim = cloneObj.GetComponent<KeyAnim>();
            if (keyAnim != null)
            {
                PoolManagerNew.Instance.PushToPool(keyAnim);
            }
            else
            {
                Destroy(cloneObj);
            }
        }
    }

    private Vector3 GetUnlockTargetPosition(int targetContainerId, EBlockColorType colorType, Vector3 startPosition)
    {
        var target = ContainerMechanic.FindTarget(targetContainerId, colorType);
        return target != null ? target.transform.position : startPosition;
    }

    private async UniTaskVoid PlaySoundWithDelay(float delay)
    {
        if (delay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_cut);
        }
    }
}
