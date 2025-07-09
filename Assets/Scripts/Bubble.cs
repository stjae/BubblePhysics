using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    [SerializeField]
    float speedLimit;
    [SerializeField]
    [Tooltip("Point of bubble, updated by particle from the fluid simulation")]
    Point point; // 流体シミュレーションのパーティクルによって更新されるバブルのポイント
    [SerializeField]
    float pointRadius;
    [SerializeField]
    int maxPointCount;
    public int MinPointCount { get { return minPointCount; } private set { minPointCount = value; } }
    [SerializeField]
    int minPointCount;
    [SerializeField]
    [Tooltip("Determine how fast the points increase")]
    float inflationInterval; // ポイントの増加速度を決定する
    [SerializeField]
    [Tooltip("Determine how fast the points decrease")]
    float deflationInterval; // ポイントの減少速度を決定する
    FluidSim fluidSim;
    bool isInflating;
    bool isDeflating;
    List<List<int>> clusters;
    public List<int> playerControlledIndex { get; private set; }  // A cluster controlled by the player 
                                                                  // プレイヤーに制御されるクラスタ
    List<bool> visited;
    [SerializeField]
    [Tooltip("The maximum distance between points within a cluster")]
    float neighborRadius; // クラスタ内のポイント間の最大距離
    public RaycastHit2D groundHit { get; private set; }
    Vector2 onGroundAvgPos;
    public Vector3 onGroundAvgNormal { get; private set; }

    void Start()
    {
        fluidSim = GetComponent<FluidSim>();
        playerControlledIndex = new List<int>();
        StartCoroutine(InflateInitialCoroutine());
        clusters = new List<List<int>>();
        visited = new List<bool>();
    }

    void Update()
    {
        Point.radius = pointRadius;
    }

    void FixedUpdate()
    {
        fluidSim.Simulate();
        SetPlayerControlledCluster();
        UpdatePosition();
        CheckOnGround();
    }

    void UpdatePosition() // Set the Bubble object's position to the position of the player-controlled cluster 
                          // プレイヤー制御のクラスタの位置にBubbleオブジェクトの位置を設定する
    {
        Vector2 center = new Vector2();
        foreach (int i in playerControlledIndex)
        {
            if (fluidSim.GetParticle(i) != null)
                center += fluidSim.GetParticle(i).position;
        }
        transform.position = center / Math.Max(playerControlledIndex.Count, 1);
    }

    public void Inflate() // Increase the number of points 
                          // ポイントの数を増やす
    {
        if (isInflating || transform.childCount >= maxPointCount)
            return;

        isInflating = true;
        StartCoroutine(InflateCoroutine());
    }

    public void Deflate() // Decrease the number of points
                          // ポイントの数を減らす
    {
        if (isDeflating || transform.childCount <= minPointCount)
            return;

        isDeflating = true;
        StartCoroutine(DeflateCoroutine());
    }

    public void Move(Vector2 inputVector) // Moves the particles of the player-controlled cluster according to keyboard input
                                          // プレイヤー制御のクラスタのパーティクルをキーボード入力に従って移動させる
    {
        foreach (int i in playerControlledIndex)
        {
            if (fluidSim.GetParticle(i) != null && fluidSim.GetParticle(i).velocity.magnitude < speedLimit)
                fluidSim.GetParticle(i).velocity += inputVector;
        }
    }

    public void Jump(Vector2 inputVector)
    {
        foreach (int i in playerControlledIndex)
        {
            if (fluidSim.GetParticle(i) != null)
                fluidSim.GetParticle(i).velocity += inputVector;
        }
    }

    void CheckOnGround() // Check if the player-controlled cluster is on the ground
                         // プレイヤー制御のクラスタが地面に接しているかを確認する
    {
        int onGroundCount = 0;
        onGroundAvgPos = new Vector2();
        onGroundAvgNormal = new Vector3();
        foreach (int i in playerControlledIndex)
        {
            if (fluidSim.GetParticle(i) != null && fluidSim.GetParticle(i).onGround)
            {
                onGroundAvgPos += fluidSim.GetParticle(i).position;
                onGroundAvgNormal += fluidSim.GetParticle(i).onGroundNormal;
                onGroundCount++;
            }
        }
        if (onGroundCount > 0)
        {
            onGroundAvgPos /= onGroundCount;
            onGroundAvgNormal /= onGroundCount;
        }
        int layerMask = (-1) - (1 << LayerMask.NameToLayer("Point"));
        float length = ((Vector2)transform.position - onGroundAvgPos).magnitude + pointRadius * 8;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, onGroundAvgPos - (Vector2)transform.position, length, layerMask);
        groundHit = Physics2D.Raycast(transform.position, -hit.normal, length, layerMask);
    }

    IEnumerator InflateInitialCoroutine() // Increase the number of points to the value of minPointCount
                                          // ポイントの数を minPointCount まで増やす
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
        fluidSim.AddParticle(new Particle() { position = pointInstance.transform.position, springRestLengths = springs });
        playerControlledIndex.Add(fluidSim.ParticleCount - 1);

        yield return new WaitForSeconds(inflationInterval);
        isInflating = false;
    }

    IEnumerator DeflateCoroutine()
    {
        int i = playerControlledIndex.Last();
        Destroy(transform.GetChild(i).gameObject);
        fluidSim.RemoveParticle(i);
        playerControlledIndex.RemoveAt(playerControlledIndex.Count - 1);

        yield return new WaitForSeconds(deflationInterval);
        isDeflating = false;
    }

    void SetPlayerControlledCluster() // Find all clusters in the simulation and set the closest one as the player-controlled cluster
                                      // シミュレーション内のすべてのクラスタを検出し、最も近いクラスタをプレイヤー制御クラスタとして設定する
    {
        visited.Clear();
        clusters.Clear();
        visited.Capacity = fluidSim.ParticleCount;

        for (int i = 0; i < fluidSim.ParticleCount; i++)
            visited.Add(false);

        for (int i = 0; i < fluidSim.ParticleCount; ++i)
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

        playerControlledIndex.Clear();
        playerControlledIndex = clusters[0];
        float shortestDist = (GetClusterPos(clusters[0]) - (Vector2)transform.position).magnitude;

        foreach (List<int> cluster in clusters)
        {
            if (shortestDist > (GetClusterPos(cluster) - (Vector2)transform.position).magnitude)
            {
                shortestDist = (GetClusterPos(cluster) - (Vector2)transform.position).magnitude;
                playerControlledIndex = cluster;
            }
        }
    }
    List<int> GetNeighbors(int i) // Get all neighboring particles connected to the particle index i 
                                  // インデックス i のパーティクルに接続されているすべての隣接パーティクルを取得する
    {
        List<int> neighbors = new List<int>();
        for (int j = 0; j < fluidSim.ParticleCount; ++j)
        {
            if (i == j) continue;

            float dist = (fluidSim.GetParticle(i).position - fluidSim.GetParticle(j).position).magnitude;
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
            center += fluidSim.GetParticle(cluster[i]).position;
        }
        return center /= Math.Max(cluster.Count, 1);
    }
}
