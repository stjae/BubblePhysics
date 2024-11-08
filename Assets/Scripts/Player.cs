using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    Bubble bubble;
    Bubble bubbleInstance;

    void Start()
    {
        bubbleInstance = Instantiate(bubble);
        bubbleInstance.transform.SetParent(gameObject.transform, false);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            bubbleInstance.Inflate();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bubbleInstance.Close();
            if (bubbleInstance.IsClosed)
            {
                bubbleInstance = Instantiate(bubble, gameObject.transform);
                bubbleInstance.transform.SetParent(gameObject.transform, false);
            }
        }
    }
}
