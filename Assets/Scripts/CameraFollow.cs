using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset = new Vector3(0.0f, 0.0f, -10.0f);
    float smoothTime = 0.1f;
    Vector3 velocity = Vector3.zero;

    void FixedUpdate()
    {
        Vector3 targetPosition = Bubble.center + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
