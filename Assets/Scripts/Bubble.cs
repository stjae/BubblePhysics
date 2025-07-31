using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SearchService;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public Vector3 CenterPos { get; private set; }
    [SerializeField]
    float speedLimit;
    [SerializeField]
    [Tooltip("Point of bubble, updated by particle from the fluid simulation")]
    Point point; // 流体シミュレーションのパーティクルによって更新されるバブルのポイント
    public Point[] points;
    public float pointRadius;
    [SerializeField]
    float maxPointLifeTime;
    public float MaxPointLifeTime { get { return maxPointLifeTime; } private set { maxPointLifeTime = value; } }
    [SerializeField]
    int maxPointCount;
    public int MaxPointCount { get { return maxPointCount; } private set { maxPointCount = value; } }
    [SerializeField]
    int minPointCount;
    public int MinPointCount { get { return minPointCount; } private set { minPointCount = value; } }
    [SerializeField]
    int initialPointCount;
    public int InitialPointCount { get { return initialPointCount; } private set { initialPointCount = value; } }
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
    public Vector3 MainClusterPos { get; private set; }
    Vector3 initialPos;
    int mainClusterIndex;
    List<bool> visited;
    [SerializeField]
    [Tooltip("The maximum distance between points within a cluster")]
    float neighborRadius; // クラスタ内のポイント間の最大距離
    public RaycastHit2D GroundHit { get; private set; }
    public Vector3 GroundNormal { get; private set; }
    Vector2 averageContactPoint;
    [SerializeField]
    float minMoveSpeed;
    [SerializeField]
    float maxMoveSpeed;
    [SerializeField]
    float minJumpForce;
    [SerializeField]
    float maxJumpForce;
    bool onPlatform;

    void Awake()
    {
        fluidSim = GetComponent<FluidSim>();
        points = new Point[MaxPointCount];
        initialPos = transform.parent.position;
        MainClusterPos = transform.parent.position;
        PhysicsMaterial2D pointPhysicsMat = new PhysicsMaterial2D();
        pointPhysicsMat.friction = 0;
        for (int i = 0; i < MaxPointCount; i++)
        {
            points[i] = Instantiate(point, transform, false);
            points[i].gameObject.SetActive(false);
            points[i].radius = pointRadius;
            points[i].GetComponent<Collider2D>().sharedMaterial = pointPhysicsMat;
        }
        isInflating = true;
        StartCoroutine(InflateInitialCoroutine());
        clusters = new List<List<int>>();
        visited = new List<bool>();
        mainCluster = new List<int>();
    }

    void Update()
    {
        CheckRestart();

        for (int i = 0; i < clusters.Count; i++)
        {
            float clusterAvgLifeTime = 0.0f;
            for (int j = 0; j < clusters[i].Count; j++)
            {
                int k = clusters[i][j];
                if (i == mainClusterIndex)
                {
                    points[k].lifeTime = maxPointLifeTime;
                }
                else
                {
                    points[k].lifeTime -= Time.deltaTime;
                }

                clusterAvgLifeTime += points[k].lifeTime;

                // adjust point radius according to the points count of each cluster
                // small amount of points -> small radius
                float ratio = Math.Clamp((float)clusters[i].Count / initialPointCount, 1, 2);
                float currentRadius = pointRadius * ratio;
                points[k].radius = currentRadius;
            }

            clusterAvgLifeTime /= clusters[i].Count;

            foreach (int c in clusters[i])
            {
                points[c].lifeTime = clusterAvgLifeTime;
            }
        }
    }

    void CheckRestart()
    {
        if (mainCluster.Count < 1 && !isInflating)
        {
            MainClusterPos = initialPos;
            isInflating = true;
            StartCoroutine(InflateInitialCoroutine());
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
                p.transform.position = MainClusterPos;
                p.InitParticle();
                p.GetParticle().isActive = false;
            }
        }
        fluidSim.Simulate();
        SetMainCluster();
        UpdatePosition();
        CheckOnGround();
    }

    void UpdatePosition()
    {
        MainClusterPos = GetClusterPos(mainCluster);
        Vector3 newCenterPos = new Vector3();
        foreach (List<int> cluster in clusters)
        {
            Vector2 clusterCenter = new Vector2();
            foreach (int i in cluster)
            {
                clusterCenter += fluidSim.particles[i].position;
            }
            clusterCenter /= cluster.Count;
            newCenterPos += (Vector3)clusterCenter;
        }

        if (clusters.Count > 0)
        {
            CenterPos = newCenterPos / clusters.Count;
        }
    }

    public void Inflate() // Increase the number of points 
                          // ポイントの数を増やす
    {
        if (isInflating || !onPlatform)
            return;

        isInflating = true;
        StartCoroutine(InflateCoroutine());
    }

    public void Deflate() // Decrease the number of points
                          // ポイントの数を減らす
    {
        if (isDeflating || mainCluster.Count <= minPointCount)
            return;

        isDeflating = true;
        StartCoroutine(DeflateCoroutine());
    }

    public void Move(Vector2 inputVector) // Moves the particles of the player-controlled cluster according to keyboard input
                                          // プレイヤー制御のクラスタのパーティクルをキーボード入力に従って移動させる
    {
        float ratio = (float)mainCluster.Count / maxPointCount;
        float speed = Mathf.Lerp(maxMoveSpeed, minMoveSpeed, ratio);

        foreach (int i in mainCluster)
        {
            if (fluidSim.particles[i].velocity.magnitude < speedLimit)
                fluidSim.particles[i].velocity += inputVector * speed;
        }
    }

    public void Jump(Vector2 inputVector)
    {
        float ratio = (float)mainCluster.Count / maxPointCount;
        float force = Mathf.Lerp(maxJumpForce, minJumpForce, ratio);

        foreach (int i in mainCluster)
        {
            fluidSim.particles[i].velocity += inputVector * force;
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
        int excludedMask = (1 << LayerMask.NameToLayer("Point")) | (1 << LayerMask.NameToLayer("Ignore Raycast"));
        int layerMask = ~excludedMask;
        float length = ((Vector2)MainClusterPos - averageContactPoint).magnitude + pointRadius * 8f;
        // If no point in the player-controlled cluster hits the ground
        // プレイヤーが操作するクラスターのいずれのPointも地面に接触していない場合
        if (averageContactPoint.magnitude == 0)
        {
            length = 0;
        }
        GroundHit = Physics2D.Raycast(MainClusterPos, -GroundNormal, length, layerMask);

        onPlatform = GroundHit && GroundHit.collider.name == "Platform";
    }

    IEnumerator InflateInitialCoroutine() // Increase the number of points to the value of initialPointCount
                                          // ポイントの数を initialPointCount まで増やす
    {
        while (ActivePointCount < initialPointCount)
        {
            for (int i = 0; i < MaxPointCount; i++)
            {
                if (!fluidSim.particles[i].isActive)
                {
                    Vector3 randomPos = MainClusterPos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    points[i].gameObject.SetActive(true);
                    points[i].lifeTime = maxPointLifeTime;
                    fluidSim.particles[i].position = randomPos;
                    fluidSim.particles[i].localPosition = fluidSim.particles[i].position - (Vector2)transform.position;
                    fluidSim.particles[i].prevPosition = randomPos;
                    fluidSim.particles[i].isActive = true;
                    break;
                }
            }
            yield return new WaitForSeconds(inflationInterval);
            isInflating = false;
        }
    }

    IEnumerator InflateCoroutine()
    {
        for (int i = 0; i < fluidSim.particles.Length; i++)
        {
            if (!fluidSim.particles[i].isActive)
            {
                Vector3 randomPos = MainClusterPos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                points[i].gameObject.SetActive(true);
                points[i].lifeTime = maxPointLifeTime;
                fluidSim.particles[i].position = randomPos;
                fluidSim.particles[i].localPosition = fluidSim.particles[i].position - (Vector2)transform.position;
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
        foreach (int i in mainCluster)
        {
            fluidSim.particles[i].isActive = false;
            transform.GetChild(i).gameObject.SetActive(false);

            break;
        }

        yield return new WaitForSeconds(deflationInterval);
        isDeflating = false;
    }

    void SetMainCluster() // Find all clusters in the simulation and set the closest one as the player-controlled cluster
                          // シミュレーション内のすべてのクラスタを検出し、最も近いクラスタをプレイヤー制御クラスタとして設定する
    {
        visited.Clear();
        clusters.Clear();
        mainCluster.Clear();
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

        if (clusters.Count < 1) return;

        mainCluster = clusters[0];
        mainClusterIndex = 0;
        float shortestDist = (GetClusterPos(clusters[0]) - (Vector2)MainClusterPos).magnitude; // distance from the current bubble to the closest cluster

        for (int i = 1; i < clusters.Count; i++)
        {
            float dist = (GetClusterPos(clusters[i]) - (Vector2)MainClusterPos).magnitude;
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
}
