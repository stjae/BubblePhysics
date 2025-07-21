using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset = new Vector3(0.0f, 0.0f, -10.0f);
    Vector3 posVelocity;
    readonly float posSmoothTime = 0.2f;

    readonly float minSize = 5f;
    readonly float maxSize = 20f;
    readonly float padding = 0.3f;
    readonly float sizeSmoothTime = 0.3f;
    float sizeVelocity;

    Camera cam;
    public Transform target;
    Bubble bubble;
    Point[] points;

    void Awake()
    {
        cam = GetComponent<Camera>();
        bubble = target.GetComponent<Bubble>();
        transform.position = bubble.MainClusterPos;
        points = new Point[bubble.transform.childCount];
        for (int i = 0; i < bubble.transform.childCount; i++)
        {
            points[i] = bubble.transform.GetChild(i).GetComponent<Point>();
        }
    }

    void FixedUpdate()
    {
        Vector3 targetPos = bubble.CenterPos + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref posVelocity, posSmoothTime);

        int count = 0;
        Bounds bounds = new Bounds();
        foreach (Point point in points)
        {
            // if (!point.gameObject.activeSelf) continue;
            if (count == 0) bounds = new Bounds(transform.position, Vector3.zero);
            bounds.Encapsulate(point.transform.position);
            count++;
        }
        if (count == 0) return;

        float aspect = cam.aspect;
        float halfHeight = bounds.size.y * 0.5f;
        float halfWidth = bounds.size.x * 0.5f;

        float targetSize = Mathf.Max(halfHeight, halfWidth / aspect);
        targetSize *= 1f + padding;
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, sizeSmoothTime);
    }
}
