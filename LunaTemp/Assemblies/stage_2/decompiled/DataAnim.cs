using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class DataAnim
{
	public AnimType Type;

	public List<Vector3> LocalScales;

	public float Duration;

	public Ease Ease = Ease.OutQuad;
}
