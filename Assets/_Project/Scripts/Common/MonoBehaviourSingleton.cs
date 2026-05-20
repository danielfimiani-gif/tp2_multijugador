using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool isPersistent = true;

    private static T instance;

    public static T Instance
    {
        get
        {
            if (!instance)
                instance = FindFirstObjectByType<T>();

            if (!instance)
                instance = new GameObject(typeof(T).Name).AddComponent<T>();

            return instance;
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
