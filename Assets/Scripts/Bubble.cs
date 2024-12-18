using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Bubble : MonoBehaviour
{
    [SerializeField]
    Point point;
    [SerializeField]
    float pointRadius;
    [SerializeField]
    static public float radius { get; private set; }
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
    public static Vector2 center;
    FluidSim fluidSim;

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
    }

    void Update()
    {
        if (radius < minRadius)
            radius += radiusStep;
        Point.radius = pointRadius;
        AdjustPointCount();
    }

    void FixedUpdate()
    {
        center = new Vector2();
        fluidSim.Simulate();
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).position = fluidSim.particles[i].position;
            center += fluidSim.particles[i].position;
        }
        if (fluidSim.particles.Count > 0)
            center /= fluidSim.particles.Count;
        transform.position = center;
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
            Point pointInstance = Instantiate(point, transform, false);
            pointInstance.transform.position += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            List<float?> list = new List<float?>();
            fluidSim.particles.Add(new Particle() { position = pointInstance.transform.position, springRestLengths = list });

            Point.particles = fluidSim.particles;
        }
        else if (transform.childCount > (int)(Math.PI * radius * radius * mass))
        {
            Destroy(transform.GetChild(0).gameObject);
            fluidSim.particles.RemoveAt(0);

            Point.particles = fluidSim.particles;
        }
        // print(transform.childCount);
    }

    public void Move(Vector2 inputVector)
    {
        foreach (Particle p in fluidSim.particles)
            p.velocity += inputVector;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, radius);
    }
}
