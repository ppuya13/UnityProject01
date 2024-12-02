using UnityEngine;
using System.Collections.Generic;
using System;
using Game;

public class UnityMainThreadDispatcher : DDSingletonManager<UnityMainThreadDispatcher>
{
    private readonly Queue<GameMessage> executionQueue = new();
    private readonly object queueLock = new object();

    public Queue<GameMessage> ExecutionQueue => executionQueue;

    public void Enqueue(GameMessage message)
    {
        if(message == null)
        {
            Debug.LogError("[Dispatcher] Attempted to enqueue null message");
            return;
        }

        lock (queueLock)
        {
            // Debug.Log($"[Dispatcher] Enqueueing message type: {message.PayloadCase}");
            executionQueue.Enqueue(message);
            // Debug.Log($"[Dispatcher] Current queue size: {executionQueue.Count}");
        }
    }

    // private void Update()
    // {
    //     if(executionQueue.Count > 0)
    //     {
    //         Debug.Log($"[Dispatcher] Update - Queue size: {executionQueue.Count}");
    //     }
    // }

}