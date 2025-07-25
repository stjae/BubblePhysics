using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Scale : MonoBehaviour
{
    Sensor sensor;
    Bubble bubble;
    public int goalPointCount;
    Vector3 initialPos;
    public float maxDescendHeight;
    bool isOnBoard;
    Vector3 velocity;
    public float smoothTime;

    void Awake()
    {
        bubble = FindFirstObjectByType<Bubble>();
        sensor = transform.GetComponentInChildren<Sensor>();
        initialPos = transform.position;
    }

    void Update()
    {
        if (bubble == null)
        {
            Debug.Log("Could not find Bubble object to interact");
            return;
        }

        if (bubble.GroundHit && bubble.GroundHit.collider.gameObject == gameObject)
        {
            isOnBoard = true;
        }
        else
        {
            isOnBoard = false;
        }

        float descendHeight = Mathf.Lerp(0, maxDescendHeight, Mathf.Clamp01((float)sensor.enteredPointIndices.Count / goalPointCount));
        if (isOnBoard)
        {
            Vector3 targetPos = new Vector3(initialPos.x, initialPos.y - descendHeight, initialPos.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, initialPos, ref velocity, smoothTime);
        }
    }
}
