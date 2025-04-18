using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset = new Vector3(0.0f, 0.0f, -10.0f);
    float smoothTime = 0.2f;
    Vector3 velocity = Vector3.zero;
    public Transform target;
    Vector3 targetPosition;

    void Start()
    {
        transform.position = target.transform.position;
    }

    void FixedUpdate()
    {
        targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
