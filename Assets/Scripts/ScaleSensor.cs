using System.Collections.Generic;
using UnityEngine;

public class ScaleSensor : MonoBehaviour
{
    public HashSet<int> enteredPointIndices;

    void Start()
    {
        enteredPointIndices = new HashSet<int>();
    }

    void OnTriggerEnter2D(Collider2D colliderInfo)
    {
        if (colliderInfo.TryGetComponent<Point>(out Point point))
        {
            enteredPointIndices.Add(point.GetIndex());
        }
    }

    void OnTriggerExit2D(Collider2D colliderInfo)
    {
        if (colliderInfo.TryGetComponent<Point>(out var point))
        {
            enteredPointIndices.Remove(point.GetIndex());
        }
    }
}
