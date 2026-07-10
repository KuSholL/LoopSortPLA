using System;
using UnityEngine;

[Serializable]
public class StylizedColorEntry
{
	public EBlockColorType BlockColorType;

	public Color Color = Color.white;

	public Color ShadowColor = Color.white;

	public Color SpecularColor = Color.white;

	public Color ReflectColor = Color.white;
}
