using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using Alchemy.Inspector;

public class UIAnim_Pulse : UIAnimationBase
{
    [Header("Pulse Settings")]
    [SerializeField] private float _shrinkScale = 0.9f; // Tỉ lệ nhỏ lại
    [SerializeField] private float _duration = 0.25f;   // Tổng thời gian 1 chu kỳ
    [SerializeField] private Ease  _easeIn   = Ease.InOutQuad;
    [SerializeField] private Ease  _easeOut  = Ease.OutBack;

    // Giữ motion để có thể cancel nếu play lại
    private MotionHandle _motionHandle;

    [ContextMenu("play")]
    public override void PlayAnimation()
    {
        if (_rect == null)
        {
            Debug.Log($"{nameof(UIAnim_Pulse)}: RectTransform is null!");
            return;
        }

        // Motion cũ nếu còn chạy
        if (_motionHandle.IsActive())
            _motionHandle.Complete();

        Vector3 startScale  = _rect.localScale;
        Vector3 targetScale = startScale * _shrinkScale;

        // Phase 1: Thu nhỏ
        _motionHandle = LMotion.Create(startScale, targetScale, _duration * 0.4f)
                               .WithEase(_easeIn)
                               .WithOnComplete(() =>
                                {
                                    // Phase 2: Nảy lại to bình thường
                                    _motionHandle = LMotion.Create(targetScale, startScale, _duration * 0.6f)
                                                           .WithEase(_easeOut)
                                                           .BindToLocalScale(_rect);
                                })
                               .BindToLocalScale(_rect);
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        if (_motionHandle.IsActive())
            _motionHandle.Cancel();
        if (_rect != null)
            _rect.localScale = Vector3.one;
    }
}