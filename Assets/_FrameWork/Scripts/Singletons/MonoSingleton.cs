using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    protected static bool AutoCreate = true;

    public static T Instance
    {
        get
        {
            if (_instance == null && AutoCreate)
            {
                var singletonObject = new GameObject(typeof(T).Name + " (Runtime)");
                _instance = singletonObject.AddComponent<T>();
            }

            return _instance;
        }
    }

    public static bool HasInstance
    {
        get { return _instance != null; }
    }

    protected virtual void Awake()
    {
        var thisInstance = this as T;
        if (_instance != null && _instance != thisInstance)
        {
            Destroy(gameObject);
            return;
        }

        _instance = thisInstance;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            _instance = null;
        }
    }
}
