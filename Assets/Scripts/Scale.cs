using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Scale : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    TMPro.TextMeshPro progressText;
    Sensor sensor;
    ScaleEndPoint scaleEndPoint;
    Bubble bubble;
    public int goalPointCount;
    Vector3 initialPos;
    public Color initialColor;
    public Color endColor;
    bool isOnBoard;
    Vector3 velocity;
    public float smoothTime;
    float totalHeight;
    bool clear;

    void Awake()
    {
        spriteRenderer = transform.GetComponent<SpriteRenderer>();
        progressText = transform.Find("Progress Text").GetComponent<TMPro.TextMeshPro>();
        bubble = FindFirstObjectByType<Bubble>();
        sensor = transform.GetComponentInChildren<Sensor>();
        scaleEndPoint = transform.Find("End Point").GetComponent<ScaleEndPoint>();
        initialPos = transform.position;
        totalHeight = initialPos.y - scaleEndPoint.transform.position.y;
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

        if (clear) return;

        float t = Mathf.Clamp01((float)sensor.enteredPointIndices.Count / goalPointCount);
        float targetHeight = scaleEndPoint.transform.position.y;
        float descendHeight = Mathf.Lerp(0, initialPos.y - targetHeight, t);
        float heightRemain = transform.position.y - scaleEndPoint.transform.position.y;

        if (heightRemain < 0.01f)
        {
            transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
            clear = true;
        }

        if (isOnBoard)
        {
            Vector3 targetPos = new Vector3(initialPos.x, initialPos.y - descendHeight, initialPos.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, initialPos, ref velocity, smoothTime);
        }

        t = Mathf.Clamp01((initialPos.y - transform.position.y) / totalHeight);
        Color progressColor = Color.Lerp(initialColor, endColor, t);
        spriteRenderer.color = progressColor;
        progressText.text = ((int)(100 * t)).ToString() + " %";
    }
}
