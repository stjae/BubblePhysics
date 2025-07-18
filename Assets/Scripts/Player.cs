using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    Bubble bubble;
    [SerializeField]
    float speed;
    [SerializeField]
    float jumpForce;
    public float currentJumpForce;
    float smoothSpeed = 2f;
    Vector3 inputVector;
    Vector3 smoothNormal;
    public bool isAbleToJump;
    public bool isJumping = false;
    public float jumpTimeCounter;
    [SerializeField]
    float maxJumpTime;

    void Start()
    {
        bubble = transform.Find("Bubble").GetComponent<Bubble>();
    }

    void Update()
    {
        Vector3 leftVec = Vector3.Cross(smoothNormal, Vector3.back);
        Vector3 rightVec = Vector3.Cross(smoothNormal, Vector3.forward);

        if (Input.GetKey(KeyCode.E))
            bubble.Inflate();
        else if (Input.GetKey(KeyCode.Q))
            bubble.Deflate();

        if (Vector3.Angle(Vector3.up, bubble.GroundHit.normal) <= 60 && bubble.GroundHit)
        {
            isAbleToJump = true;
            smoothNormal = Vector3.Lerp(smoothNormal, bubble.GroundNormal, Time.deltaTime * smoothSpeed).normalized;
        }
        else
        {
            isAbleToJump = false;
            smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, Time.deltaTime * smoothSpeed).normalized;
        }

        inputVector = new Vector3();

        if (Input.GetKey(KeyCode.A))
            inputVector += leftVec * FluidSim.deltaTime * speed;

        if (Input.GetKey(KeyCode.D))
            inputVector += rightVec * FluidSim.deltaTime * speed;

        if (isAbleToJump)
        {
            jumpTimeCounter = maxJumpTime;
        }

        if (isJumping)
        {
            jumpTimeCounter -= Time.deltaTime;
        }

        if (jumpTimeCounter <= 0 || Input.GetKeyUp(KeyCode.Space))
        {
            isJumping = false;
            currentJumpForce = 0;
        }

        if (Input.GetKey(KeyCode.Space) && jumpTimeCounter > 0 && isAbleToJump)
        {
            isJumping = true;
            currentJumpForce = jumpForce;
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
