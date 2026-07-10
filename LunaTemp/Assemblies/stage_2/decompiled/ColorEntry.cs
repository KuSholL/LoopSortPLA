using System;
using UnityEngine;

[Serializable]
public class ColorEntry
{
	public EBlockColorType BlockColorType;

	public Color Color = Color.white;

	public Color ShadowColor = Color.white;

	public Color SpecularColor = Color.white;

	[Header("Custom Cat Cube")]
	public Color RimColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

	public Color MatCapColor = Color.white;

	public Color OutlineColor = Color.black;
}
