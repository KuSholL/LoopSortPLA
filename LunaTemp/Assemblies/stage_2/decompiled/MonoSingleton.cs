using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;

	protected static bool AutoCreate = true;

	public static T Instance
	{
		get
		{
			if ((Object)_instance == (Object)null && AutoCreate)
			{
				GameObject singletonObject = new GameObject(typeof(T).Name + " (Runtime)");
				_instance = singletonObject.AddComponent<T>();
			}
			return _instance;
		}
	}

	public static bool HasInstance => (Object)_instance != (Object)null;

	protected virtual void Awake()
	{
		T thisInstance = this as T;
		if ((Object)_instance != (Object)null && (Object)_instance != (Object)thisInstance)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = thisInstance;
		}
	}

	protected virtual void OnDestroy()
	{
		if ((Object)_instance == (Object)(this as T))
		{
			_instance = null;
		}
	}
}
