using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool isPersistent = true;

    private static T _instance;

    public static T Instance
    {
        get
        {
            if (!_instance)
                _instance = FindFirstObjectByType<T>();

            if (!_instance)
                _instance = new GameObject(typeof(T).Name).AddComponent<T>();

            return _instance;
        }
    }

    void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        else if (isPersistent)
            DontDestroyOnLoad(gameObject);

        OnAwaken();
    }

    protected virtual void OnAwaken() { }
}
