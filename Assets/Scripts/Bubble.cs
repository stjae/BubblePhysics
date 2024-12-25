using System;
using System.Collections;
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
    static public float radius;
    [SerializeField]
    public int mass;
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
    static public float decisionDistance;
    FluidSim fluidSim;
    static public Vector3 center { get; private set; }
    static public Vector3 highestDensityPosition { get; private set; }

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
        StartCoroutine(SetInitialRadius());
    }

    void Update()
    {
        Point.radius = pointRadius;
        decisionDistance = radius * 3;
        AdjustPointCount();
    }

    void FixedUpdate()
    {
        fluidSim.Simulate();
        UpdatePosition();
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
        }
        else if (transform.childCount > (int)(Math.PI * radius * radius * mass))
        {
            Destroy(transform.GetChild(0).gameObject);
            fluidSim.particles.RemoveAt(0);
        }
        // print(transform.childCount);
        // print(fluidSim.particles.Count);
    }

    void UpdatePosition()
    {
        if (transform.childCount < 1)
            return;

        center = new Vector2();
        Particle highDensityParticle = fluidSim.particles[0];

        for (int i = 0; i < transform.childCount; i++)
        {
            center += transform.GetChild(i).position;
            if (fluidSim.particles[i].density > highDensityParticle.density && (highDensityParticle.position - fluidSim.particles[i].position).magnitude < decisionDistance)
                highDensityParticle = fluidSim.particles[i];
        }

        center /= transform.childCount;
        transform.position = highDensityParticle.position;
    }

    public void Move(Vector2 inputVector)
    {
        foreach (Particle p in fluidSim.particles)
            p.velocity += inputVector;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.color = Color.green;
        // Gizmos.DrawWireSphere(transform.position, decisionDistance);
        Gizmos.DrawWireSphere(center, decisionDistance);
    }

    IEnumerator SetInitialRadius()
    {
        while (radius < minRadius)
        {
            radius += radiusStep;
            yield return null;
        }
    }
}
