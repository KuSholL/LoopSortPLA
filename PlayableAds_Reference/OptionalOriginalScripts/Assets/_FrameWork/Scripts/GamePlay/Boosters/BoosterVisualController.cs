using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum BoosterState
{
    Locked,
    Available,
    Empty,
    Disabled
}

public class BoosterVisualController : MonoBehaviour
{
    [Header("State Objects")] [SerializeField]
    protected Button activeButton;
    
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private Button disableButton;
    [SerializeField] protected GameObject activeRoot;
    [SerializeField] protected GameObject lockedRoot;
    [SerializeField] protected GameObject amountRoot;
    [SerializeField] protected GameObject plusIcon;
    [SerializeField] protected GameObject disabledIcon;
    [SerializeField] protected Image iconBooster;
    [SerializeField] protected Canvas canvas;

    [Header("Texts")] [SerializeField] protected TextMeshProUGUI amountText;
    [SerializeField] protected TextMeshProUGUI lockLevelText;

    [Header("Icon Sprites")] 
    [SerializeField] private Sprite disabledSprite;
    [SerializeField] private Sprite activeSprite;

    protected virtual void Awake()
    {
        if (disableButton != null)
        {
            disableButton.onClick.AddListener(OnDisableButtonClicked);
        }
    }

    private void OnDisableButtonClicked()
    {
        GameEventBus.OnShowSthWrong?.Invoke(boosterType);
    }


    public virtual void ApplyState(BoosterState state, int amount = 0, string lockInfo = "")
    {
        // Reset common elements
        plusIcon?.SetActive(false);
        amountRoot?.SetActive(false);
        lockedRoot?.SetActive(false);
        activeRoot?.SetActive(false);
        activeButton.interactable = false;
        if (disabledIcon != null) disabledIcon.SetActive(false);
        iconBooster.sprite = activeSprite;

        switch (state)
        {
            case BoosterState.Locked:
                HandleLockedState(lockInfo);
                break;
            case BoosterState.Available:
                HandleAvailableState(amount);
                break;
            case BoosterState.Empty:
                HandleEmptyState();
                break;
            case BoosterState.Disabled:
                HandleDisabledState(amount);
                break;
        }
    }

    public void HandleTutorial(bool isOn)
    {
        canvas.overrideSorting = isOn;
    }

    protected virtual void HandleLockedState(string lockInfo)
    {
        lockedRoot?.SetActive(true);
        lockLevelText.text = lockInfo;
    }

    protected virtual void HandleAvailableState(int amount)
    {
        activeRoot.SetActive(true);
        amountRoot?.SetActive(true);
        amountText.text = amount.ToString();
        activeButton.interactable = true;
    }

    protected virtual void HandleEmptyState()
    {
        activeRoot.SetActive(true);
        plusIcon?.SetActive(true);
        activeButton.interactable = true;
    }

    protected virtual void HandleDisabledState(int amount)
    {
        activeRoot.SetActive(true);
        if (disabledIcon != null) disabledIcon.SetActive(true);
        iconBooster.sprite = disabledSprite;

        if (amount > 0)
        {
            amountRoot?.SetActive(true);
            amountText.text = amount.ToString();
        }
        else
        {
            plusIcon?.SetActive(true);
        }
    }
}