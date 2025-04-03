using System.Threading;
using System;
using System.Collections.Concurrent;
using Python.Runtime;
using System.Collections.Generic;

public static class PythonDispatcher
{
    static bool isRunning = false;
    static Thread thread;
    static readonly ConcurrentQueue<Action> pythonThreadQueue = new ConcurrentQueue<Action>();
    static volatile List<Action> continousActions = new List<Action>();

    public static int AddContinousAction(Action action)
    {
        continousActions.Add(action);
        return continousActions.IndexOf(action);
    }

    public static void RemoveContinousAction(int id)
    {
        if (id < continousActions.Count) continousActions.RemoveAt(id);
    }

    public static void RunInPythonThread(Action action)
    {
        pythonThreadQueue.Enqueue(action);
    }

    public static void Initialize()
    {
        if (isRunning) return;
        isRunning = true;
        thread = new Thread(() =>
        {
            while (isRunning)
            {
                if (!pythonThreadQueue.IsEmpty)
                {
                    pythonThreadQueue.TryDequeue(out var action);
                    using (Py.GIL())
                    {
                        foreach (var caction in continousActions)
                        {
                            caction?.Invoke();
                        }
                        action?.Invoke();
                    }
                }
            }
        });
        thread.Start();
    }

    public static void Shutdown()
    {
        isRunning = false;
    }

}