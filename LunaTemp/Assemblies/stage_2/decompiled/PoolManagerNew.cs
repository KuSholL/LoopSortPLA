using System.Collections.Generic;
using UnityEngine;

public class PoolManagerNew : MonoSingleton<PoolManagerNew>
{
	private readonly Dictionary<GameObject, Queue<GameObject>> _prefabToPool = new Dictionary<GameObject, Queue<GameObject>>();

	private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

	public T PopFromPool<T>(T prefab, Transform parent = null) where T : Component
	{
		if ((Object)prefab == (Object)null)
		{
			return null;
		}
		GameObject instance = PopObject(prefab.gameObject, parent);
		return instance.GetComponent<T>();
	}

	public void PushToPool<T>(T instance) where T : Component
	{
		if (CanPush(instance))
		{
			GameObject instanceObject = instance.gameObject;
			GameObject prefab = _instanceToPrefab[instanceObject];
			PrepareReturnedInstance(instanceObject);
			GetPool(prefab).Enqueue(instanceObject);
		}
	}

	public void Prewarm(GameObject prefab, int count)
	{
		if (!(prefab == null) && count > 0)
		{
			Queue<GameObject> pool = GetPool(prefab);
			for (int i = 0; i < count; i++)
			{
				GameObject instance = CreateInstance(prefab);
				pool.Enqueue(instance);
			}
		}
	}

	private bool CanPush(Component instance)
	{
		return instance != null && _instanceToPrefab.ContainsKey(instance.gameObject);
	}

	private GameObject PopObject(GameObject prefab, Transform parent)
	{
		if (prefab == null)
		{
			return null;
		}
		GameObject instance = TryDequeue(prefab) ?? CreateInstance(prefab);
		PrepareSpawnedInstance(instance, parent);
		return instance;
	}

	private GameObject TryDequeue(GameObject prefab)
	{
		Queue<GameObject> pool = GetPool(prefab);
		return (pool.Count == 0) ? null : pool.Dequeue();
	}

	private Queue<GameObject> GetPool(GameObject prefab)
	{
		if (_prefabToPool.TryGetValue(prefab, out var pool))
		{
			return pool;
		}
		pool = new Queue<GameObject>();
		_prefabToPool[prefab] = pool;
		return pool;
	}

	private GameObject CreateInstance(GameObject prefab)
	{
		GameObject instance = Object.Instantiate(prefab, base.transform);
		_instanceToPrefab[instance] = prefab;
		return instance;
	}

	private void PrepareSpawnedInstance(GameObject instance, Transform parent)
	{
		instance.transform.SetParent(parent, false);
		instance.SetActive(true);
	}

	private void PrepareReturnedInstance(GameObject instance)
	{
		instance.transform.SetParent(base.transform, false);
		instance.SetActive(false);
	}
}
