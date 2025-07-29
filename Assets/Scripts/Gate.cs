using UnityEngine;

public class Gate : MonoBehaviour
{
    public Trigger trigger1;
    public Trigger trigger2;
    public float openHeight;
    Vector3 initialPos;
    Vector3 targetPos;
    bool isOpening;
    bool isOpened;
    float t;

    void Awake()
    {
        initialPos = transform.position;
        targetPos = new Vector3(initialPos.x, initialPos.y + openHeight, initialPos.z);
    }

    void Update()
    {
        if (isOpened)
        {
            return;
        }

        if (trigger1 != null && trigger2 != null)
        {
            isOpening = trigger1.isTriggered && trigger2.isTriggered;
        }
        else if (trigger1 != null && trigger2 == null)
        {
            isOpening = trigger1.isTriggered;
        }
        else
        {
            Debug.Log("No trigger is assigned!");
            return;
        }

        if (isOpening)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(initialPos, targetPos, t);
        }

        if (t >= 1)
        {
            isOpened = true;
        }
    }
}
