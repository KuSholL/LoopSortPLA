using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "AnimBlockConfig", menuName = "ScriptableObject/AnimBlockConfig")]
public class AnimBlockConfig : ScriptableObject
{
    public List<DataAnim> DataAnims;

    public DataAnim GetDataAnim(AnimType type)
    {
        return DataAnims.Find(anim => anim.Type == type);
    }
}
[Serializable]
public class DataAnim
{
    public AnimType Type;
    public List<Vector3> LocalScales;
    public float Duration;
    public DG.Tweening.Ease Ease = DG.Tweening.Ease.OutQuad; 
}

[Serializable]
public enum AnimType
{
    None = 0,
    Increase = 1,
    Decrease = 2,
    Full = 3
}
