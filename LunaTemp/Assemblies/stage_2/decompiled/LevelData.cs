using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level/LevelData")]
public class LevelData : ScriptableObject
{
	public int LevelId;

	public int BaselineCapacity;

	public int Capacity;

	public int OrthographicSize = 11;

	public int GoldReward;

	public LevelType LevelType;

	public CarrierLayoutData CarrierLayout = new CarrierLayoutData();

	public SplinePathData SplineLayout = new SplinePathData();
}
