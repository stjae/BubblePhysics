using UnityEngine;

public class Gate : MonoBehaviour
{
    public Trigger trigger1;
    public Trigger trigger2;
    public float openHeight;
    Vector3 initialPos;
    Vector3 targetPos;
    public bool isOpened;
    float t;

    void Awake()
    {
        initialPos = transform.position;
        targetPos = initialPos + transform.up * openHeight;
    }

    void Update()
    {
        if (!isOpened)
        {
            if (trigger1 != null && trigger2 != null)
            {
                isOpened = trigger1.isTriggered && trigger2.isTriggered;
            }
            else if (trigger1 != null && trigger2 == null)
            {
                isOpened = trigger1.isTriggered;
            }
            else
            {
                Debug.Log("No trigger is assigned!");
                return;
            }
        }

        if (isOpened)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(initialPos, targetPos, t);

            if (trigger1 != null)
            {
                trigger1.isTriggerFromOutside = true;
                trigger1.spriteRenderer.color = trigger1.onColor;
            }
            if (trigger2 != null)
            {
                trigger2.isTriggerFromOutside = true;
                trigger2.spriteRenderer.color = trigger2.onColor;
            }
        }
    }
}
