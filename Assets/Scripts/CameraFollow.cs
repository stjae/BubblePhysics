using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset = new Vector3(0.0f, 0.0f, -10.0f);
    float smoothTime = 0.1f;
    Vector3 velocity = Vector3.zero;

    [SerializeField]
    Transform target;

    void FixedUpdate()
    {
        Vector3 targetPosition = target.GetChild(0).position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
