using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public Color onColor;
    public Color offColor;
    SpriteRenderer spriteRenderer;
    HashSet<int> enteredPointIndices;
    public bool isTriggered;

    void Awake()
    {
        enteredPointIndices = new HashSet<int>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = offColor;
    }

    void Update()
    {
        if (enteredPointIndices.Count != 0)
        {
            spriteRenderer.color = onColor;
            isTriggered = true;
        }
        else
        {
            spriteRenderer.color = offColor;
            isTriggered = false;
        }
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
