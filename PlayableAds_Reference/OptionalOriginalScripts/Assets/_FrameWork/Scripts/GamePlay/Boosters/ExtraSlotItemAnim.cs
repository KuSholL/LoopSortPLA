using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

public class ExtraSlotItemAnim : MonoBehaviour
{
    [Header("Visual Elements")]
    [SerializeField] private Transform iconVisual;

    [Header("VFX Prefabs")]
    [SerializeField] private ParticleSystem vfxPopup;
    [SerializeField] private ParticleSystem vfxFly;
    [Header("Animations Config")]
    [SerializeField] private float popupDuration = 0.4f;
    [SerializeField] private float flyDuration = 0.7f;

    private void OnEnable()
    {
        vfxPopup.gameObject.SetActive(false);
        vfxFly.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        vfxPopup.gameObject.SetActive(false);
        vfxFly.gameObject.SetActive(false);
    }
    private async UniTask WaitForParticleCompleteAndDisable(ParticleSystem particleSystem)
    {
        if (particleSystem == null || !particleSystem.gameObject.activeSelf)
        {
            return;
        }

        await UniTask.WaitUntil(
            () => particleSystem == null || !particleSystem.IsAlive(true),
            cancellationToken: this.GetCancellationTokenOnDestroy());

        if (particleSystem != null)
        {
            particleSystem.gameObject.SetActive(false);
        }
    }
    

    public async UniTask PlayAnimPopup(Vector3 startPos)
    {
        vfxPopup.gameObject.SetActive(false);
        vfxFly.gameObject.SetActive(false);
        
        iconVisual.position = startPos;
        if (iconVisual != null) iconVisual.localScale = Vector3.one;
        
        vfxPopup.gameObject.SetActive(true);
        vfxPopup.transform.position = startPos;
        vfxPopup.Play();
        
        if (iconVisual != null)
        {
            await LMotion.Create(Vector3.one, Vector3.one * 1.5f, popupDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(iconVisual);
        }
        
        vfxPopup.gameObject.SetActive(false);
    }

    public async UniTask PlayAnimFly(Vector3 startPos, Vector3 targetPos)
    {
        if (vfxFly != null)
        {
            vfxFly.transform.position = startPos;
            vfxFly.gameObject.SetActive(true);
            vfxFly.Play();
        }

        var moveTask = LMotion.Create(startPos, targetPos, flyDuration)
            .WithEase(Ease.InOutQuad)
            .Bind(value =>
            {
                if (iconVisual != null) iconVisual.position = value;
                if (vfxFly != null)
                {
                    vfxFly.transform.position = value;
                }
            });

        await moveTask.ToUniTask();
        
        WaitForParticleCompleteAndDisable(vfxFly);
    }
}
