using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Player : MonoBehaviour
{
    Bubble bubble;
    [SerializeField]
    float speed;
    [SerializeField]
    float jumpForce;

    void Start()
    {
        bubble = transform.Find("Bubble").GetComponent<Bubble>();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.E))
            bubble.Inflate();
        else if (Input.GetKey(KeyCode.Q))
            bubble.Deflate();

        Vector2 inputVector = new Vector2();

        if (Input.GetKey(KeyCode.A))
            inputVector += Vector2.left * FluidSim.deltaTime * speed;
        if (Input.GetKey(KeyCode.D))
            inputVector += Vector2.right * FluidSim.deltaTime * speed;

        bubble.Move(inputVector);

        if (Input.GetKey(KeyCode.Space) && bubble.isOnGround)
        {
            bubble.Jump(jumpForce);
        }

        if(Input.GetKeyUp(KeyCode.Space))
        {
            bubble.EndJump();
        }
    }

    void OnDrawGizmos()
    {
    }
}
