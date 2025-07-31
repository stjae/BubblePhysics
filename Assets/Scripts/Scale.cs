using UnityEngine;
using UnityEngine.SceneManagement;

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
    public bool fixAfterWhenHitBottom;
    public bool useAsLevelClearFlag;
    public Canvas uiCanvas;

    void Awake()
    {
        spriteRenderer = transform.GetComponentInChildren<SpriteRenderer>();
        progressText = transform.GetComponentInChildren<TMPro.TextMeshPro>();
        bubble = FindFirstObjectByType<Bubble>();
        sensor = transform.GetComponentInChildren<ScaleSensor>();
        scaleEndPoint = transform.GetComponentInChildren<ScaleEndPoint>();
        scaleEndPoint.GetComponent<SpriteRenderer>().enabled = false;
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

        if (hitBottom && useAsLevelClearFlag)
        {
            uiCanvas.gameObject.SetActive(true);
        }

        if (hitBottom && fixAfterWhenHitBottom)
        {
            return;
        }

        float t = Mathf.Clamp01((float)sensor.enteredPointIndices.Count / goalPointCount);
        float descendHeight = Mathf.Lerp(0, totalHeight, t);

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
        if (t > 0.999)
        {
            hitBottom = true;
            t = 1;
        }
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
