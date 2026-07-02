using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using Alchemy.Inspector;

public class UIAnim_ShakeBell : UIAnimationBase
{
    [Header("Bell Shake Settings")]
    [SerializeField] private float _maxAngle = 25f;             // Góc lắc ban đầu
    [SerializeField] private float _totalDuration = 1.2f;       // Thời gian tổng
    [SerializeField] private int _shakeCount = 4;               // Số lần lắc (qua lại)
    [SerializeField] private Ease _ease = Ease.InOutSine;       // Độ mượt easing

    private MotionHandle _motionHandle;

    [Button]
    public override void PlayAnimation()
    {
        if (_rect == null)
        {
            Debug.Log($"{nameof(UIAnim_ShakeBell)}: RectTransform is null!");
            return;
        }

        if (_motionHandle.IsActive())
            _motionHandle.Cancel();

        _rect.localRotation = Quaternion.identity;

        float perShakeDuration = _totalDuration / (_shakeCount * 2f);
        float currentAngle = _maxAngle;

        var sequence = LSequence.Create();

        for (int i = 0; i < _shakeCount; i++)
        {
            // Lắc trái
            sequence.Append(
                LMotion.Create(Vector3.zero, new Vector3(0f, 0f, currentAngle), perShakeDuration)
                       .WithEase(_ease)
                       .BindToLocalEulerAngles(_rect)
            );

            // Lắc phải
            sequence.Append(
                LMotion.Create(new Vector3(0f, 0f, currentAngle), new Vector3(0f, 0f, -currentAngle), perShakeDuration * 2f)
                       .WithEase(_ease)
                       .BindToLocalEulerAngles(_rect)
            );

            // Giảm biên độ dần để tạo cảm giác tắt rung
            currentAngle *= 0.6f;
        }

        // Trở về vị trí giữa
        sequence.Append(
            LMotion.Create(_rect.localEulerAngles, Vector3.zero, perShakeDuration)
                   .WithEase(Ease.OutSine)
                   .BindToLocalEulerAngles(_rect)
        );

        _motionHandle = sequence.Run();
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        if (_motionHandle.IsActive())
            _motionHandle.Cancel();
        if (_rect != null)
            _rect.localRotation = Quaternion.identity;
    }
}
