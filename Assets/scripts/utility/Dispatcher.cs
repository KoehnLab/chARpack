using System.Threading;
using System;
using UnityEngine;
using System.Collections.Concurrent;

public class Dispatcher : MonoBehaviour
{
    static Dispatcher _instance;
    public static readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    public static void RunAsync(Action action)
    {
        ThreadPool.QueueUserWorkItem(o => action());
    }

    public static void RunAsync(Action<object> action, object state)
    {
        ThreadPool.QueueUserWorkItem(o => action(o), state);
    }

    public static void RunOnMainThread(Action action)
    {
        mainThreadQueue.Enqueue(action);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            _instance = new GameObject("Dispatcher").AddComponent<Dispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    private void Update()
    {
        if (!mainThreadQueue.IsEmpty)
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}