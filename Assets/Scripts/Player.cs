using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    Bubble bubble;
    [SerializeField]
    float speed;
    [SerializeField]
    float jumpForce;
    float currentJumpForce;
    float smoothSpeed = 2f;
    Vector3 inputVector;
    Vector3 smoothNormal;
    bool isAbleToJump;
    bool isJumping = false;
    float jumpTimeCounter;
    [SerializeField]
    float maxJumpTime = 0.3f;

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

        if (Vector3.Angle(Vector3.up, bubble.groundHit.normal) <= 60 && bubble.groundHit)
        {
            isAbleToJump = true;
            smoothNormal = Vector3.Lerp(smoothNormal, bubble.onGroundAvgNormal, Time.deltaTime * smoothSpeed).normalized;
        }
        else
        {
            isAbleToJump = false;
            smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, Time.deltaTime * smoothSpeed).normalized;
        }

        inputVector = new Vector3();

        if (Input.GetKey(KeyCode.A))
            inputVector += Vector3.Cross(smoothNormal, Vector3.back) * FluidSim.deltaTime * speed;
        if (Input.GetKey(KeyCode.D))
            inputVector += Vector3.Cross(smoothNormal, Vector3.forward) * FluidSim.deltaTime * speed;

        if (Input.GetKey(KeyCode.Space) && isAbleToJump)
        {
            isJumping = true;
            currentJumpForce = jumpForce;
        }

        if (Input.GetKey(KeyCode.Space) && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
                jumpTimeCounter = maxJumpTime;
                currentJumpForce = 0;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            isJumping = false;
            currentJumpForce = 0;
        }

        if (bubble.transform.position.y < -10)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
    void FixedUpdate()
    {
        bubble.Move(inputVector);
        bubble.Jump(smoothNormal.normalized * currentJumpForce);
    }
}
