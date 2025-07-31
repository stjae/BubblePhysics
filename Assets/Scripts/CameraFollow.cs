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
    public Player player;
    Bubble bubble;
    Point[] points;
    float initialSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        bubble = player.transform.Find("Bubble").GetComponent<Bubble>();
        transform.position = player.transform.position;
        points = new Point[bubble.transform.childCount];
        for (int i = 0; i < bubble.transform.childCount; i++)
        {
            points[i] = bubble.transform.GetChild(i).GetComponent<Point>();
        }
        initialSize = cam.orthographicSize;
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                maxSize,
                ref sizeVelocity,
                sizeSmoothTime
            );
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
        {
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                initialSize,
                ref sizeVelocity,
                sizeSmoothTime
            );
        }

        UpdateCameraBounds();
    }

    void UpdateCameraBounds()
    {
        int count = 0;
        Bounds bounds = new Bounds();
        foreach (Point point in points)
        {
            if (count == 0) bounds = new Bounds(transform.position, Vector3.zero);
            bounds.Encapsulate(point.transform.position);
            count++;
        }
        if (count == 0) return;

        Vector3 targetPos = bounds.center + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref posVelocity, posSmoothTime);

        float aspect = cam.aspect;
        float halfHeight = bounds.size.y * 0.5f;
        float halfWidth = bounds.size.x * 0.5f;

        float targetSize = Mathf.Max(halfHeight, halfWidth / aspect);
        targetSize *= 1f + padding;
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, sizeSmoothTime);
    }
}
