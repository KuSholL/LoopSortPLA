using TMPro;
using UnityEngine;

public class LevelTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI levelHardText;
    [SerializeField] private Color[] levelColors;
    [SerializeField] private ImageStateHandle[] imageStateHandles;
    [SerializeField] private Color disableColor;
    
    public void SetupLevelText()
    {
        if (DataManager.PlayerData == null || ConfigHolder.Instance == null || ConfigHolder.Instance.LevelConfigSO == null) return;

        var levelDisplay = DataManager.PlayerData.LevelDisplay;
        var levelConfig = ConfigHolder.Instance.LevelConfigSO.GetLevelData(levelDisplay);
        if (levelConfig == null) return;

        var isHard = levelConfig.LevelType == LevelType.Hard;
        var isExtreme = levelConfig.LevelType == LevelType.Extreme;
        var canSHowDefault = !isHard && !isExtreme;

        if (levelText != null)
        {
            levelText.gameObject.SetActive(canSHowDefault);
            levelText.text = TextUtility.GetI2("level") + " " + levelDisplay;
        }

        if (levelHardText != null)
        {
            levelHardText.gameObject.SetActive(!canSHowDefault);
            if (levelColors != null && levelColors.Length >= 2)
            {
                levelHardText.color = isHard ? levelColors[0] : levelColors[1];
            }
            levelHardText.text = TextUtility.GetI2("level") + " " + levelDisplay;
        }

        var state = isHard ? 0 : 1;
        if (imageStateHandles != null)
        {
            foreach (var imageState in imageStateHandles)
            {
                if (imageState != null) imageState.SetupState(state);
            }
        }
    }
    
    private void Awake()
    {
        GameEventBus.OnHighlightCameraActiveChanged += OnHighlightCameraActiveChanged;
    }

    private void OnDestroy()
    {
        GameEventBus.OnHighlightCameraActiveChanged -= OnHighlightCameraActiveChanged;
    }

    private void OnHighlightCameraActiveChanged(bool active)
    {
        bool isBoosterTut = UIClawBoosterTutorial.IsTutorial || UIExtraSlotBoosterLayer.IsTutorial || UIUndoBoosterTutorialLayer.IsTutorial;
        SetDisableLevelText(active && isBoosterTut);
    }

    public void SetDisableLevelText(bool isDisabled)
    {
        if (levelText == null || levelHardText == null) return;

        if (DataManager.PlayerData == null || ConfigHolder.Instance == null || ConfigHolder.Instance.LevelConfigSO == null)
        {
            levelText.color = isDisabled ? disableColor : Color.white;
            levelHardText.color = isDisabled ? disableColor : Color.white;
            return;
        }

        var levelDisplay = DataManager.PlayerData.LevelDisplay;
        var levelConfig = ConfigHolder.Instance.LevelConfigSO.GetLevelData(levelDisplay);
        if (levelConfig == null)
        {
            levelText.color = isDisabled ? disableColor : Color.white;
            levelHardText.color = isDisabled ? disableColor : Color.white;
            return;
        }

        var isHard = levelConfig.LevelType == LevelType.Hard;
        levelText.color = isDisabled ? disableColor : Color.white;

        Color hardColor = Color.white;
        if (levelColors != null && levelColors.Length >= 2)
        {
            hardColor = isHard ? levelColors[0] : levelColors[1];
        }
        levelHardText.color = isDisabled ? disableColor : hardColor;
    }
}
