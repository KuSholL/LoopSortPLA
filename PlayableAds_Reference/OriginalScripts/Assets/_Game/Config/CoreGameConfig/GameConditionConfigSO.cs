using UnityEngine;

[CreateAssetMenu(
    fileName = "GameConditionConfigSO",
    menuName = "ScriptableObjects/GameConditionConfigSO")]
public class GameConditionConfigSO : ScriptableObject
{
    [Tooltip("Thời gian delay trước khi hiện popup Thắng.")]
    public float WinDelaySeconds = 1f;

    [Tooltip("Thời gian delay (giảm tốc) trước khi hiện popup Thua/PreLose.")]
    public float LoseDelaySeconds = 1f;

    [Tooltip("Tốc độ tối thiểu của các Cube và Shader băng truyền khi giảm tốc xong (PreLose Target Speed).")]
    public float PreloseTargetSpeedMultiplier = 0.2f;

    [Tooltip("Thời gian rung camera sau khi giảm tốc xong.")]
    public float LoseShakeDuration = 0.5f;

    [Tooltip("Độ mạnh rung camera sau khi giảm tốc xong.")]
    public float LoseShakeMagnitude = 0.15f;

    [Header("Win Guarantee Speed Up Config")]
    [Tooltip("Bật/Tắt tính năng tự động tăng tốc khi chắc chắn thắng.")]
    public bool EnableWinGuaranteeSpeedUp = true;

    [Tooltip("Tốc độ tăng tốc khi chắc chắn thắng.")]
    public float WinGuaranteeSpeedMultiplier = 5f;
}
