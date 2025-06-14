using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
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
    public List<int> playerControlledCluster { get; private set; }  // A cluster controlled by the player 
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
        StartCoroutine(InflateInitialCoroutine());
        clusters = new List<List<int>>();
        playerControlledCluster = new List<int>();
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
        foreach (int i in playerControlledCluster)
        {
            center += fluidSim.particles[i].position;
        }
        transform.position = center / Math.Max(playerControlledCluster.Count, 1);
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
        foreach (int i in playerControlledCluster)
        {
            fluidSim.particles[i].velocity += inputVector;
        }
    }

    public void Jump(float force)
    {
        foreach (int i in playerControlledCluster)
        {
            fluidSim.particles[i].velocity.y += force;
        }
    }

    void CheckOnGround() // Check if the player-controlled cluster is on the ground
                         // プレイヤー制御のクラスタが地面に接しているかを確認する
    {
        int onGroundCount = 0;
        onGroundAvgPos = new Vector2();
        onGroundAvgNormal = new Vector3();
        foreach (int i in playerControlledCluster)
        {
            if (fluidSim.particles[i].onGround)
            {
                onGroundAvgPos += fluidSim.particles[i].position;
                onGroundAvgNormal += fluidSim.particles[i].onGroundNormal;
                onGroundCount++;
            }
        }
        if (onGroundCount > 0)
        {
            onGroundAvgPos /= onGroundCount;
            onGroundAvgNormal /= onGroundCount;
        }
        int layerMask = (-1) - (1 << LayerMask.NameToLayer("Point"));
        float length = ((Vector2)transform.position - onGroundAvgPos).magnitude + pointRadius * 4;
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

    void SetPlayerControlledCluster() // Find all clusters in the simulation and set the largest one as the player-controlled cluster
                                      // シミュレーション内のすべてのクラスタを検出し、最も大きいクラスタをプレイヤー制御クラスタとして設定する
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

        playerControlledCluster.Clear();
        foreach (List<int> cluster in clusters)
        {
            if (cluster.Count > playerControlledCluster.Count)
            {
                playerControlledCluster = cluster;
            }
        }
    }
    List<int> GetNeighbors(int i) // Get all neighboring particles connected to the particle index i 
                                  // インデックス i のパーティクルに接続されているすべての隣接パーティクルを取得する
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
}
