using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LitMotion;

[RequireComponent(typeof(RectTransform))]
public abstract class UIAnimationBase : MonoBehaviour, IPointerClickHandler
{
    [Header("Animation Settings")]
    [SerializeField] private UIAnimationTrigger _trigger = UIAnimationTrigger.Manual;
    [SerializeField] private bool _playOnce = false; // chỉ chạy 1 lần

    protected RectTransform _rect;
    private   bool          _hasPlayed;

    protected virtual void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    protected virtual void OnEnable()
    {
        if (_trigger == UIAnimationTrigger.OnEnable)
            TryPlay();
    }

    protected virtual void OnDisable()
    {
        if (_trigger == UIAnimationTrigger.OnDisable)
            TryPlay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_trigger == UIAnimationTrigger.OnClick)
            TryPlay();
    }

    private void TryPlay()
    { 
        if(gameObject.activeInHierarchy == false)
            return;
        
        if (_playOnce && _hasPlayed)
            return;

        PlayAnimation();
        _hasPlayed = true;
    }

    public abstract void PlayAnimation();
}

public enum UIAnimationTrigger
{
    OnEnable,
    OnDisable,
    OnClick,
    Manual // nếu muốn gọi code chạy anim thủ công
}