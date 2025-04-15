using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Rendering.Universal;


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
    List<List<int>> clusters;
    List<int> largestCluster;
    List<bool> visited;
    [SerializeField]
    float neighborRadius;
    public bool isOnGround { get; private set; }
    Vector2 onGroundAvgPos;

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
        StartCoroutine(InflateInitialCoroutine());
        clusters = new List<List<int>>();
        largestCluster = new List<int>();
        visited = new List<bool>();
    }

    void Update()
    {
        Point.radius = pointRadius;
    }

    void FixedUpdate()
    {
        fluidSim.Simulate();
        GetLargestCluster();
        UpdatePosition();
        CheckOnGround();
    }

    void UpdatePosition()
    {
        Vector2 center = new Vector2();
        foreach (int i in largestCluster)
        {
            center += fluidSim.particles[i].position;
        }
        transform.position = center / largestCluster.Count;
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
        foreach (int i in largestCluster)
        {
            fluidSim.particles[i].velocity += inputVector;
        }
    }

    public void Jump(float force)
    {
        foreach (int i in largestCluster)
        {
            fluidSim.particles[i].velocity.y += force;
        }
    }

    public void EndJump()
    {
        foreach (int i in largestCluster)
        {
            fluidSim.particles[i].velocity.y *= 0.8f;
        }
    }

    void CheckOnGround()
    {
        int onGroundCount = 0;
        onGroundAvgPos = new Vector2();
        foreach (int i in largestCluster)
        {
            if (fluidSim.particles[i].onGround)
            {
                onGroundAvgPos += fluidSim.particles[i].position;
                onGroundCount++;
            }
        }
        onGroundAvgPos /= onGroundCount;
        int layerMask = (-1) - (1 << LayerMask.NameToLayer("Point"));
        float length = ((Vector2)transform.position - onGroundAvgPos).magnitude + pointRadius * 2;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, onGroundAvgPos - (Vector2)transform.position, length, layerMask);
        RaycastHit2D onGroundHit = Physics2D.Raycast(transform.position, -hit.normal, length, layerMask);
        Debug.DrawRay(transform.position, onGroundAvgPos - (Vector2)transform.position);
        if (onGroundHit)
            isOnGround = true;
        else
            isOnGround = false;
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
        fluidSim.particles.Add(new Particle() { position = pointInstance.transform.position, springRestLengths = springs });

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
    void GetLargestCluster()
    {
        visited.Clear();
        clusters.Clear();
        visited.Capacity = fluidSim.particles.Count;
        for (int i = 0; i < fluidSim.particles.Count; i++)
            visited.Add(false);

        for (int i = 0; i < fluidSim.particles.Count; ++i)
        {
            if (visited[i]) continue;

            Queue<int> q = new Queue<int>();
            List<int> cluster = new List<int>();
            q.Enqueue(i);
            visited[i] = true;

            while (q.Count > 0)
            {
                int current = q.Peek();
                q.Dequeue();
                cluster.Add(current);

                List<int> neighbors = GetNeighbors(current);
                foreach (int neighbor in neighbors)
                {
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        q.Enqueue(neighbor);
                    }
                }
            }
            clusters.Add(cluster);
        }

        largestCluster.Clear();
        foreach (List<int> cluster in clusters)
        {
            if (cluster.Count > largestCluster.Count)
            {
                largestCluster = cluster;
            }
        }
    }
    List<int> GetNeighbors(int i)
    {
        List<int> neighbors = new List<int>();
        for (int j = 0; j < fluidSim.particles.Count; ++j)
        {
            if (i == j) continue;

            float dist = (fluidSim.particles[i].position - fluidSim.particles[j].position).magnitude;
            if (dist < neighborRadius)
            {
                neighbors.Add(j);
            }
        }
        return neighbors;
    }

    Vector2 GetClusterPos(List<int> cluster)
    {
        Vector2 pos = new Vector2();
        foreach (int i in cluster)
        {
            pos += fluidSim.particles[i].position;
        }
        return pos / cluster.Count;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(onGroundAvgPos, Point.radius);
    }

}
