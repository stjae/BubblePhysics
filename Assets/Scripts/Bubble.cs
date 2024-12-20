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

    void UpdatePosition()
    {
        if (transform.childCount < 1)
            return;

        Particle highDensityParticle = fluidSim.particles[0];
        float decisionDistance = Point.radius * 4;

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).position = fluidSim.particles[i].position;
            if (fluidSim.particles[i].density > highDensityParticle.density && (highDensityParticle.position - fluidSim.particles[i].position).magnitude < decisionDistance)
                highDensityParticle = fluidSim.particles[i];
        }

        if (transform.childCount > 0)
            transform.position = Vector2.Lerp(transform.position, transform.position + ((Vector3)highDensityParticle.position - transform.position), Time.deltaTime * 2.0f);
    }

    public void Move(Vector2 inputVector)
    {
        foreach (Particle p in fluidSim.particles)
            p.velocity += inputVector;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius * 4);
    }
}
