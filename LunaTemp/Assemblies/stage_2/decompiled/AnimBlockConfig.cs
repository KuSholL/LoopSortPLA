using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimBlockConfig", menuName = "ScriptableObject/AnimBlockConfig")]
public class AnimBlockConfig : ScriptableObject
{
	public List<DataAnim> DataAnims;

	public DataAnim GetDataAnim(AnimType type)
	{
		return DataAnims.Find((DataAnim anim) => anim.Type == type);
	}
}
