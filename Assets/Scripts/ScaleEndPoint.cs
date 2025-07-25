using UnityEngine;

public class ScaleEndPoint : MonoBehaviour
{
    Vector3 initialWorldPos;

    void Awake()
    {
        initialWorldPos = transform.position;
    }

    void Update()
    {
        transform.position = initialWorldPos;
    }
}
