using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDataSO", menuName = "ScriptableObjects/SoundSO", order = 10)]
public class SoundDataSO : ScriptableObject
{
	public List<SoundData> soundDataList = new List<SoundData>();

	private Dictionary<AudioClipName, SoundData> _soundDataCache;

	public SoundData GetSoundData(AudioClipName clipName)
	{
		if (_soundDataCache == null)
		{
			BuildCache();
		}
		_soundDataCache.TryGetValue(clipName, out var soundData);
		return soundData;
	}

	public List<SoundData> GetSoundDataByName(string searchString)
	{
		List<SoundData> results = new List<SoundData>();
		if (string.IsNullOrEmpty(searchString))
		{
			return new List<SoundData>(soundDataList);
		}
		searchString = searchString.ToLower();
		foreach (SoundData soundData in soundDataList)
		{
			if (soundData.Name.ToString().ToLower().Contains(searchString))
			{
				results.Add(soundData);
			}
		}
		return results;
	}

	public void RebuildCache()
	{
		BuildCache();
	}

	private void BuildCache()
	{
		_soundDataCache = new Dictionary<AudioClipName, SoundData>();
		if (soundDataList == null)
		{
			return;
		}
		foreach (SoundData soundData in soundDataList)
		{
			if (!_soundDataCache.ContainsKey(soundData.Name))
			{
				_soundDataCache[soundData.Name] = soundData;
			}
			else
			{
				Debug.LogWarning($"[SoundDataSO] Duplicate sound data found: {soundData.Name}. Keeping first occurrence.");
			}
		}
	}

	private void OnValidate()
	{
		_soundDataCache = null;
	}
}
