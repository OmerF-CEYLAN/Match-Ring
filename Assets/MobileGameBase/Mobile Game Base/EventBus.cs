// 
//  EventBus.cs  —  MODIFIED FROM ORIGINAL.
//  Change: replaced HashSet + ToArray() with a List and
//  an isPublishing guard. Zero heap allocation on Publish.
//

using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    static readonly List<IEventBinding<T>> bindings = new();
    static readonly List<IEventBinding<T>> pendingUnsub = new();
    static bool isPublishing;

    public static void Subscribe(IEventBinding<T> binding)
    {
        if (!bindings.Contains(binding))
            bindings.Add(binding);
    }

    public static void Unsubscribe(IEventBinding<T> binding)
    {
        // If we unsubscribe during a Publish loop, defer it to avoid
        // modifying the list mid-iteration. Common when a handler
        // unsubscribes itself after the first event it receives.
        if (isPublishing)
            pendingUnsub.Add(binding);
        else
            bindings.Remove(binding);
    }

    public static void Publish(T eventToPublish)
    {
        isPublishing = true;

        for (int i = 0; i < bindings.Count; i++)
        {
            bindings[i].OnEvent.Invoke(eventToPublish);
            bindings[i].OnEventNoArgs.Invoke();
        }

        isPublishing = false;

        // Flush deferred unsubscriptions
        if (pendingUnsub.Count == 0) return;
        foreach (var b in pendingUnsub)
            bindings.Remove(b);
        pendingUnsub.Clear();
    }

    public static void Clear()
    {
        Debug.Log($"[EventBus] Clearing {typeof(T).Name} bindings.");
        bindings.Clear();
        pendingUnsub.Clear();
    }
}