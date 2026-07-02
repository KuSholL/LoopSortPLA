using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    protected static bool AutoCreate = true;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(T)) as T;
            }

            if (_instance == null && AutoCreate)
            {
                GameObject go = new GameObject(nameof(T) + "(MonoSingleton)");
                _instance = Instantiate(go).AddComponent<T>();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance && _instance != this)
        {
#if UNITY_EDITOR
            Debug.Log("Had been exist instance of " + typeof(T).Name);
#endif
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}
