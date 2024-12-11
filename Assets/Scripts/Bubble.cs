using System;
using Unity.VisualScripting;
using UnityEngine;


public class Bubble : MonoBehaviour
{
    [SerializeField]
    Point point;
    [SerializeField]
    public float radius { get; private set; }
    [SerializeField]
    int mass;
    [SerializeField]
    float maxRadius;
    [SerializeField]
    float minRadius;
    [SerializeField]
    float radiusStep;
    [SerializeField]
    uint inflationSpeed;
    [SerializeField]
    float deflationSpeed;
    Vector2 center;

    void Start()
    {
        radius = minRadius;
    }

    void Update()
    {
        AdjustPointCount();
    }

    void FixedUpdate()
    {
    }

    public void Inflate()
    {
        radius += radiusStep * inflationSpeed;
        radius = Math.Min(maxRadius, radius);
    }

    public void Deflate()
    {
        radius -= radiusStep * deflationSpeed;
        radius = Math.Max(minRadius, radius);
    }

    void AdjustPointCount()
    {
        if (transform.childCount < (int)(Math.PI * radius * radius * mass))
        {
            System.Random rand = new System.Random();

            Point pointInstance = Instantiate(point, transform, false);
            pointInstance.transform.position += new Vector3((float)rand.NextDouble(), 0, 0);
        }
        else if (transform.childCount > (int)(Math.PI * radius * radius * mass))
        {
            Destroy(transform.GetChild(0).gameObject);
        }
    }

    public void Move(Vector2 inputVector)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Point>().particle.velocity += inputVector;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, radius);
        // print(transform.childCount);
    }
}
