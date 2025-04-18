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
    float smoothSpeed = 2f;
    Vector3 inputVector;
    Vector3 smoothNormal;
    bool isAbleToJump;

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
        if (Input.GetKeyDown(KeyCode.Space) && isAbleToJump)
            inputVector += Vector3.up * jumpForce;


        bubble.Move(inputVector);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        Debug.DrawRay(bubble.transform.position, inputVector.normalized);
    }
}
