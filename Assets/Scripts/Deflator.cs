using UnityEngine;

public class Deflator : MonoBehaviour
{
    Bubble bubble;

    void Awake()
    {
        bubble = FindFirstObjectByType<Bubble>();
    }
    void OnTriggerEnter2D(Collider2D colliderInfo)
    {
        if (colliderInfo.TryGetComponent<Point>(out Point point))
        {
            point.gameObject.SetActive(false);

            int activePointCount = 0;
            for (int i = 0; i < bubble.mainCluster.Count; i++)
            {
                if (bubble.points[bubble.mainCluster[i]].gameObject.activeSelf)
                    activePointCount++;
            }

            if (activePointCount < 1)
            {
                bubble.Init();
            }
        }
    }
}
