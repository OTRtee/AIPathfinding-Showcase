using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePriorityQueue<T>
{
    private List<(T item, float priority)> _data = new List<(T, float)>();

    public int Count => _data.Count;

    public void Enqueue(T item, float priority)
    {
        _data.Add((item, priority));
    }

    public T Dequeue()
    {
        // find lowest priority
        int best = 0;
        for (int i = 1; i < _data.Count; i++)
            if (_data[i].priority < _data[best].priority)
                best = i;

        var result = _data[best].item;
        _data.RemoveAt(best);
        return result;
    }

    public bool Contains(T item)
    {
        return _data.Exists(x => EqualityComparer<T>.Default.Equals(x.item, item));
    }
}