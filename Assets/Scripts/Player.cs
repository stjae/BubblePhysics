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
        // RaycastHit2D hitGround = Physics2D.Raycast(bubble.transform.position, Vector2.down, bubble.radius, (-1) - (1 << LayerMask.NameToLayer("Point")));
        bool hitGround = true;

        if (Input.GetKey(KeyCode.A) && hitGround)
            inputVector += Vector2.left * FluidSim.deltaTime * speed;
        if (Input.GetKey(KeyCode.D) && hitGround)
            inputVector += Vector2.right * FluidSim.deltaTime * speed;
        if (Input.GetKey(KeyCode.Space) && hitGround)
            inputVector += Vector2.up * jumpForce * 0.005f;

        bubble.Move(inputVector);
    }

    void OnDrawGizmos()
    {
    }
}
