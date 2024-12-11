using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    Bubble bubble;

    void Start()
    {
        bubble = Instantiate(bubble, transform, false);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.E))
            bubble.Inflate();
        else if (Input.GetKey(KeyCode.Q))
            bubble.Deflate();

        Vector2 inputVector = new Vector2();
        float speed = 2.0f;
        if (Input.GetKey(KeyCode.A))
            inputVector += Vector2.left * FluidSim.deltaTime * speed;
        if (Input.GetKey(KeyCode.D))
            inputVector += Vector2.right * FluidSim.deltaTime * speed;

        bubble.Move(inputVector);
    }
}
