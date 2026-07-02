using DG.Tweening;
using UnityEngine;

public class BlockSolidProgressAnimator : MonoBehaviour
{
    private static readonly int TriggerActiveId = Animator.StringToHash("TrigActive");

    [SerializeField] private Transform animatedTarget;
    [SerializeField] private bool isBlock4X;
    [SerializeField] private Animator _animator;

    private Sequence _progressSequence;
    private float _lastProgress;
    private bool _isProgressTweenPlaying;
    private AnimType _currentAnimType = AnimType.None;

    private void Awake()
    {
        SetAnimatorEnabled(false);
        ResetState();
    }

    private void OnDisable()
    {
        StopProgressTween(true);
        SetAnimatorEnabled(false);
        ResetState();
    }

    public void SetProgress(float progress, bool suppressAnimation = false, bool forceFullAnimation = false)
    {
        progress = Mathf.Clamp01(progress);

        if (suppressAnimation)
        {
            _lastProgress = progress;
            return;
        }
        if (Application.isPlaying) PlayAnimationByProgress(progress, forceFullAnimation);
        _lastProgress = progress;
    }

    public void PlayAnimationByProgress(float progress, bool forceFullAnimation = false)
    {
        progress = Mathf.Clamp01(progress);
        var animType = GetAnimTypeByProgress(progress, forceFullAnimation);
        var configManager = ConfigManager.Instance;
        if (configManager == null)
        {
            return;
        }

        var animConfig = configManager.GetAnimBlockConfig();
        if (animConfig == null)
        {
            return;
        }

        PlayProgressTween(animConfig.GetDataAnim(animType));
    }

    public void ReplayCurrentProgressAnimation(bool forceFullAnimation = false)
    {
        if (_lastProgress <= 0f) return;

        var progress = _lastProgress;
        _lastProgress = 0f;
        PlayAnimationByProgress(progress, forceFullAnimation);
        _lastProgress = progress;
    }

    public void PlayTriggerActiveAnimation()
    {
        if (_animator == null) return;

        if (_isProgressTweenPlaying || _progressSequence != null)
        {
            StopProgressTween(true);
        }

        SetAnimatorEnabled(true);
        _animator.ResetTrigger(TriggerActiveId);
        _animator.SetTrigger(TriggerActiveId);
    }

    public void SetAnimatorEnabled(bool isEnabled)
    {
        if (!isBlock4X || _animator == null) return;
        _animator.enabled = isEnabled;
    }

    private AnimType GetAnimTypeByProgress(float progress, bool forceFullAnimation)
    {
        if (progress >= 1f && forceFullAnimation)
        {
            return AnimType.Full;
        }

        if (progress <= 0f)
        {
            return AnimType.None;
        }

        if (progress >= 1f)
        {
            return AnimType.Full;
        }

        return progress > _lastProgress ? AnimType.Increase : AnimType.Decrease;
    }

    private void PlayProgressTween(DataAnim animData)
    {
        if (animatedTarget == null) return;
        if (animData == null || animData.LocalScales == null || animData.LocalScales.Count < 2) return;

        if (_currentAnimType == animData.Type &&  _isProgressTweenPlaying)
        {
            return;
        }
        
        StopProgressTween(true);

        animatedTarget.localScale = Vector3.one;
        _isProgressTweenPlaying = true;
        _currentAnimType = animData.Type;

        var sequence = DOTween.Sequence().SetTarget(this);

        for (var index = 0; index < animData.LocalScales.Count - 1; index++)
        {
            var targetScale = animData.LocalScales[index + 1];
            var motion = animatedTarget
                .DOScale(targetScale, animData.Duration)
                .SetEase(animData.Ease);

            if (index == animData.LocalScales.Count - 2)
            {
                motion.OnComplete(OnProgressTweenCompleted);
            }

            sequence.Append(motion);
        }

        _progressSequence = sequence;

        void OnProgressTweenCompleted()
        {
            _isProgressTweenPlaying = false;
            _currentAnimType = AnimType.None;
            _progressSequence = null;
            if (animatedTarget != null)
            {
                animatedTarget.localScale = Vector3.one;
            }

            if (isBlock4X && animData.Type == AnimType.Full) SetAnimatorEnabled(true);

            if (animData.Type == AnimType.Full)
            {
                SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_squash_end);
            }
        }
    }

    private void StopProgressTween(bool resetScale)
    {
        if (_progressSequence != null)
        {
            _progressSequence.Kill();
            _progressSequence = null;
        }

        _isProgressTweenPlaying = false;
        _currentAnimType = AnimType.None;

        if (resetScale && animatedTarget != null)
        {
            animatedTarget.localScale = Vector3.one;
        }
    }

    private void ResetState()
    {
        _lastProgress = 0f;
        _isProgressTweenPlaying = false;
        _currentAnimType = AnimType.None;
    }
}
