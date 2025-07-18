using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset = new Vector3(0.0f, 0.0f, -10.0f);
    Vector3 velocity = Vector3.zero;
    public Transform target;
    Bubble bubble;
    Vector3 targetPosition;
    [SerializeField]
    float positionSmoothTime;

    void Start()
    {
        bubble = target.transform.GetComponent<Bubble>();
        transform.position = bubble.Position;
    }

    void FixedUpdate()
    {
        targetPosition = bubble.Position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, positionSmoothTime);
    }
}
