using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public Vector3 Position { get; private set; }
    [SerializeField]
    float speedLimit;
    [SerializeField]
    [Tooltip("Point of bubble, updated by particle from the fluid simulation")]
    Point point; // 流体シミュレーションのパーティクルによって更新されるバブルのポイント
    Point[] points;
    [SerializeField]
    float pointRadius;
    [SerializeField]
    float maxPointLifeTime;
    public float MaxPointLifeTime { get { return maxPointLifeTime; } private set { maxPointLifeTime = value; } }
    [SerializeField]
    int maxPointCount;
    public int MaxPointCount { get { return maxPointCount; } private set { maxPointCount = value; } }
    [SerializeField]
    int minPointCount;
    public int MinPointCount { get { return minPointCount; } private set { minPointCount = value; } }
    public int ActivePointCount { get; private set; }
    [SerializeField]
    [Tooltip("Determine how fast the points increase")]
    float inflationInterval; // ポイントの増加速度を決定する
    [SerializeField]
    [Tooltip("Determine how fast the points decrease")]
    float deflationInterval; // ポイントの減少速度を決定する
    FluidSim fluidSim;
    bool isInflating;
    bool isDeflating;
    public List<List<int>> clusters;
    public List<int> mainCluster { get; private set; }  // A cluster controlled by the player 
                                                        // プレイヤーに制御されるクラスタ
    int mainClusterIndex;
    List<bool> visited;
    [SerializeField]
    [Tooltip("The maximum distance between points within a cluster")]
    float neighborRadius; // クラスタ内のポイント間の最大距離
    public RaycastHit2D GroundHit { get; private set; }
    public Vector3 GroundNormal { get; private set; }
    Vector2 averageContactPoint;

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
        points = new Point[MaxPointCount];
        for (int i = 0; i < MaxPointCount; i++)
        {
            points[i] = Instantiate(point, transform, false);
            points[i].gameObject.SetActive(false);
        }
        StartCoroutine(InflateInitialCoroutine());
        clusters = new List<List<int>>();
        visited = new List<bool>();
        mainCluster = new List<int>();
    }

    void Update()
    {
        Point.radius = pointRadius;

        for (int i = 0; i < clusters.Count; i++)
        {
            for (int j = 0; j < clusters[i].Count; j++)
            {
                int k = clusters[i][j];
                if (i == mainClusterIndex)
                {
                    points[k].lifeTime += Time.deltaTime;
                    points[k].lifeTime = Math.Min(points[k].lifeTime, maxPointLifeTime);
                }
                else
                {
                    points[k].lifeTime -= Time.deltaTime;
                }
            }
        }
    }

    void FixedUpdate()
    {
        ActivePointCount = 0;
        foreach (Point p in points)
        {
            if (p.gameObject.activeSelf)
            {
                ActivePointCount++;
            }
            else
            {
                p.transform.position = Position;
                p.GetParticle().localPosition = new Vector2();
            }
        }
        fluidSim.Simulate();
        SetMainCluster();
        Position = GetClusterPos(mainCluster);
        CheckOnGround();
    }

    public void Inflate() // Increase the number of points 
                          // ポイントの数を増やす
    {
        if (isInflating)
            return;

        isInflating = true;
        StartCoroutine(InflateCoroutine());
    }

    public void Deflate() // Decrease the number of points
                          // ポイントの数を減らす
    {
        if (isDeflating)
            return;

        isDeflating = true;
        StartCoroutine(DeflateCoroutine());
    }

    public void Move(Vector2 inputVector) // Moves the particles of the player-controlled cluster according to keyboard input
                                          // プレイヤー制御のクラスタのパーティクルをキーボード入力に従って移動させる
    {
        foreach (int i in mainCluster)
        {
            if (fluidSim.particles[i].velocity.magnitude < speedLimit)
                fluidSim.particles[i].velocity += inputVector;
        }
    }

    public void Jump(Vector2 inputVector)
    {
        foreach (int i in mainCluster)
        {
            fluidSim.particles[i].velocity += inputVector;
        }
    }

    void CheckOnGround() // Check if the player-controlled cluster is on the ground
                         // プレイヤー制御のクラスタが地面に接しているかを確認する
    {
        int onGroundCount = 0;
        averageContactPoint = new Vector2();
        GroundNormal = new Vector3();
        foreach (int i in mainCluster)
        {
            if (points[i].isOnGround)
            {
                averageContactPoint += fluidSim.particles[i].position;
                GroundNormal += points[i].groundNormal;
                onGroundCount++;
            }
        }
        if (onGroundCount > 0)
        {
            averageContactPoint /= onGroundCount;
            GroundNormal /= onGroundCount;
        }
        int excludedMask = 1 << LayerMask.NameToLayer("Point");
        int layerMask = ~excludedMask;
        float length = ((Vector2)Position - averageContactPoint).magnitude + pointRadius * 2;
        // If no point in the player-controlled cluster hits the ground
        // プレイヤーが操作するクラスターのいずれのPointも地面に接触していない場合
        if (averageContactPoint.magnitude == 0)
            length = 0;
        RaycastHit2D hit = Physics2D.Raycast(Position, averageContactPoint - (Vector2)Position, length, layerMask);
        GroundHit = Physics2D.Raycast(Position, -hit.normal, length, layerMask);
    }

    IEnumerator InflateInitialCoroutine() // Increase the number of points to the value of minPointCount
                                          // ポイントの数を minPointCount まで増やす
    {
        while (ActivePointCount < MinPointCount)
        {
            for (int i = 0; i < MaxPointCount; i++)
            {
                if (!fluidSim.particles[i].isActive)
                {
                    Vector3 randomPos = Position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    points[i].gameObject.SetActive(true);
                    points[i].lifeTime = maxPointLifeTime;
                    fluidSim.particles[i].position = randomPos;
                    fluidSim.particles[i].isActive = true;
                    break;
                }
            }
            yield return new WaitForSeconds(inflationInterval);
        }
    }

    IEnumerator InflateCoroutine()
    {
        for (int i = 0; i < fluidSim.particles.Length; i++)
        {
            if (!fluidSim.particles[i].isActive)
            {
                Vector3 randomPos = Position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                points[i].gameObject.SetActive(true);
                points[i].lifeTime = maxPointLifeTime;
                fluidSim.particles[i].position = randomPos;
                fluidSim.particles[i].prevPosition = randomPos;
                fluidSim.particles[i].velocity = transform.GetComponent<Rigidbody2D>().linearVelocity;
                fluidSim.particles[i].isActive = true;
                break;
            }
        }

        yield return new WaitForSeconds(inflationInterval);
        isInflating = false;
    }

    IEnumerator DeflateCoroutine()
    {
        for (int i = 0; i < fluidSim.particles.Length; i++)
        {
            if (fluidSim.particles[i].isActive)
            {
                fluidSim.particles[i].isActive = false;
                transform.GetChild(i).gameObject.SetActive(false);
                break;
            }
        }

        yield return new WaitForSeconds(deflationInterval);
        isDeflating = false;
    }

    void SetMainCluster() // Find all clusters in the simulation and set the closest one as the player-controlled cluster
                          // シミュレーション内のすべてのクラスタを検出し、最も近いクラスタをプレイヤー制御クラスタとして設定する
    {
        visited.Clear();
        clusters.Clear();
        visited.Capacity = fluidSim.particles.Length;

        for (int i = 0; i < fluidSim.particles.Length; i++)
            visited.Add(false);

        for (int i = 0; i < fluidSim.particles.Length; i++)
        {
            if (visited[i] || !fluidSim.particles[i].isActive) continue;

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
                    if (visited[neighbor]) continue;

                    visited[neighbor] = true;
                    q.Enqueue(neighbor);
                }
            }
            clusters.Add(cluster);
        }

        if (clusters.Count < 1)
        {
            return;
        }

        mainCluster.Clear();
        mainCluster = clusters[0];
        mainClusterIndex = 0;
        float shortestDist = (GetClusterPos(clusters[0]) - (Vector2)Position).magnitude; // distance from the current bubble to the closest cluster

        for (int i = 1; i < clusters.Count; i++)
        {
            float dist = (GetClusterPos(clusters[i]) - (Vector2)Position).magnitude;
            if (shortestDist > dist)
            {
                shortestDist = dist;
                mainCluster = clusters[i];
                mainClusterIndex = i;
            }
        }
    }

    List<int> GetNeighbors(int i) // Get all neighboring particles connected to the particle index i 
                                  // インデックス i のパーティクルに接続されているすべての隣接パーティクルを取得する
    {
        List<int> neighbors = new List<int>();
        for (int j = 0; j < fluidSim.particles.Length; j++)
        {
            if (i == j || !fluidSim.particles[j].isActive) continue;

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
        Vector2 center = new Vector2();
        for (int i = 0; i < cluster.Count; i++)
        {
            center += fluidSim.particles[cluster[i]].position;
        }
        return center / Math.Max(cluster.Count, 1);
    }

    void OnDrawGizmos()
    {
        // Gizmos.color = Color.red;
        // for (int i = 0; i < mainCluster.Count; i++)
        // {
        //     Gizmos.DrawSphere(fluidSim.particles[mainCluster[i]].position, 0.1f);
        // }

        // Vector2 center = new Vector2();
        // Gizmos.color = Color.blue;
        // for (int i = 0; i < mainCluster.Count; i++)
        // {
        //     center += fluidSim.particles[mainCluster[i]].position;
        // }
        // Gizmos.DrawSphere(center / Math.Max(mainCluster.Count, 1), 0.1f);
    }
}
