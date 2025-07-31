using UnityEngine;

public class Deflator : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D colliderInfo)
    {
        if (colliderInfo.TryGetComponent<Point>(out Point point))
        {
            point.gameObject.SetActive(false);
        }
    }
}
