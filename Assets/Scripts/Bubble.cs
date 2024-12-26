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
    float maxPointCount;
    [SerializeField]
    float minPointCount;
    [SerializeField]
    float inflationInterval;
    [SerializeField]
    float deflationInterval;
    static public float radius;
    FluidSim fluidSim;
    static public Vector3 center { get; private set; }
    static public Vector3 highestDensityPosition { get; private set; }
    bool isInflating;
    bool isDeflating;

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
        StartCoroutine(InflateInitialCoroutine());
    }

    void Update()
    {
        Point.radius = pointRadius;
    }

    void FixedUpdate()
    {
        fluidSim.Simulate();
        UpdatePosition();
    }

    public void Inflate()
    {
        if (isInflating || transform.childCount >= maxPointCount)
            return;

        isInflating = true;
        StartCoroutine(InflateCoroutine());
    }

    public void Deflate()
    {
        if (isDeflating || transform.childCount <= minPointCount)
            return;

        isDeflating = true;
        StartCoroutine(DeflateCoroutine());
    }

    void UpdatePosition()
    {
        if (transform.childCount < 1)
            return;

        center = new Vector2();
        Particle highDensityParticle = fluidSim.particles[0];
        float maxY = 0;
        float minY = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            center += transform.GetChild(i).position;
            if (fluidSim.particles[i].density > highDensityParticle.density && (highDensityParticle.position - fluidSim.particles[i].position).magnitude < Render.textureTileCoverage)
                highDensityParticle = fluidSim.particles[i];
            if (fluidSim.particles[i].localPosition.y > maxY)
                maxY = fluidSim.particles[i].localPosition.y;
            if (fluidSim.particles[i].localPosition.y < minY)
                minY = fluidSim.particles[i].localPosition.y;
        }

        center /= transform.childCount;
        radius = maxY - minY;
        transform.position = highDensityParticle.position;
    }

    public void Move(Vector2 inputVector)
    {
        foreach (Particle p in fluidSim.particles)
            p.velocity += inputVector;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, Render.textureTileCoverage);
    }

    IEnumerator InflateInitialCoroutine()
    {
        while (transform.childCount < minPointCount)
        {
            StartCoroutine(InflateCoroutine());
            yield return new WaitForSeconds(inflationInterval);
        }
    }

    IEnumerator InflateCoroutine()
    {
        Point pointInstance = Instantiate(point, transform, false);
        pointInstance.transform.position += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
        List<float?> list = new List<float?>();
        fluidSim.particles.Add(new Particle() { position = pointInstance.transform.position, springRestLengths = list });

        yield return new WaitForSeconds(inflationInterval);
        isInflating = false;
    }

    IEnumerator DeflateCoroutine()
    {
        Destroy(transform.GetChild(0).gameObject);
        fluidSim.particles.RemoveAt(0);

        yield return new WaitForSeconds(deflationInterval);
        isDeflating = false;
    }
}
