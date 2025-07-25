using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    public HashSet<int> enteredPointIndices;

    void Start()
    {
        enteredPointIndices = new HashSet<int>();
    }

    void OnTriggerEnter2D(Collider2D colliderInfo)
    {
        Point point = colliderInfo.GetComponent<Point>();

        if (point)
        {
            enteredPointIndices.Add(point.GetIndex());
        }
    }

    void OnTriggerExit2D(Collider2D colliderInfo)
    {
        Point point = colliderInfo.GetComponent<Point>();

        if (point)
        {
            enteredPointIndices.Remove(point.GetIndex());
        }
    }

}
