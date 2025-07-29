using System.Net;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Scale : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    TMPro.TextMeshPro progressText;
    ScaleSensor sensor;
    ScaleEndPoint scaleEndPoint;
    Transform platform;
    Transform square;
    Bubble bubble;
    public int goalPointCount;
    Vector3 initialPos;
    public Color initialColor;
    public Color endColor;
    bool isOnBoard;
    Vector3 velocity;
    public float smoothTime;
    float totalHeight;
    bool hitBottom;
    public bool renderProgressText;

    void Awake()
    {
        spriteRenderer = transform.GetComponentInChildren<SpriteRenderer>();
        progressText = transform.GetComponentInChildren<TMPro.TextMeshPro>();
        bubble = FindFirstObjectByType<Bubble>();
        sensor = transform.GetComponentInChildren<ScaleSensor>();
        scaleEndPoint = transform.GetComponentInChildren<ScaleEndPoint>();
        platform = transform.Find("Platform");
        square = transform.Find("Platform").Find("Square");
        initialPos = platform.position;
        totalHeight = square.position.y - scaleEndPoint.transform.position.y;
    }

    void Update()
    {
        if (bubble == null)
        {
            Debug.Log("Could not find Bubble object to interact");
            return;
        }

        if (bubble.GroundHit && bubble.GroundHit.collider.gameObject == square.gameObject)
        {
            isOnBoard = true;
        }
        else
        {
            isOnBoard = false;
        }

        if (hitBottom) return;

        float t = Mathf.Clamp01((float)sensor.enteredPointIndices.Count / goalPointCount);
        float descendHeight = Mathf.Lerp(0, totalHeight, t);
        float heightRemain = square.position.y - scaleEndPoint.transform.position.y;

        if (heightRemain < 0.1f)
        {
            platform.position = new Vector3(platform.transform.position.x, scaleEndPoint.transform.position.y, platform.transform.position.z);
            hitBottom = true;
        }

        if (isOnBoard)
        {
            Vector3 targetPos = new Vector3(initialPos.x, initialPos.y - descendHeight, initialPos.z);
            platform.position = Vector3.SmoothDamp(platform.position, targetPos, ref velocity, smoothTime);
        }
        else
        {
            platform.position = Vector3.SmoothDamp(platform.position, initialPos, ref velocity, smoothTime);
        }

        t = Mathf.Clamp01((initialPos.y - platform.position.y) / totalHeight);
        Color progressColor = Color.Lerp(initialColor, endColor, t);
        spriteRenderer.color = progressColor;

        if (renderProgressText)
        {
            progressText.text = ((int)(100 * t)).ToString() + " %";
        }
        else
        {
            progressText.text = "";
        }
    }
}
