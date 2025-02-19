using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEditor.SearchService;
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
    FluidSim fluidSim;
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

    void UpdatePosition()
    {
        Vector2 center = new Vector2();
        foreach (Particle p in fluidSim.particles)
        {
            center += p.position;
        }
        transform.position = center / fluidSim.particles.Count;
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

    public void Move(Vector2 inputVector)
    {
        foreach (Particle p in fluidSim.particles)
            p.velocity += inputVector;
    }

    IEnumerator InflateInitialCoroutine()
    {
        while (transform.childCount < minPointCount)
        {
            Inflate();
            yield return new WaitForSeconds(inflationInterval);
        }
    }

    IEnumerator InflateCoroutine()
    {
        Point pointInstance = Instantiate(point, transform, false);
        pointInstance.transform.position += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
        List<float?> springs = new List<float?>();
        List<Particle> neighbors = new List<Particle>();
        fluidSim.particles.Add(new Particle() { position = pointInstance.transform.position, springRestLengths = springs, neighborParticles = neighbors });

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

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // Gizmos.color = Color.green;
        // Gizmos.DrawWireSphere(center, radius);
        // Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(center, Render.textureTileCoverage);
        // Gizmos.color = Color.blue;
        // Gizmos.DrawWireSphere(highestDensityParticle.position, Point.radius);
    }

}
