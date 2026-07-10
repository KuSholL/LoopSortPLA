using UnityEngine;

public static class PlayableStylizedColorFallback
{
	public static StylizedColorEntry CreateColorEntry(EBlockColorType colorType)
	{
		ColorEntry colorEntry = PlayableColorFallback.CreateColorEntry(colorType);
		return new StylizedColorEntry
		{
			BlockColorType = colorType,
			Color = colorEntry.Color,
			ShadowColor = colorEntry.ShadowColor,
			SpecularColor = colorEntry.SpecularColor,
			ReflectColor = Color.white
		};
	}
}
