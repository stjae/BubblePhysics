using Unity.Mathematics;
using UnityEngine;

public class Render : MonoBehaviour
{
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    Shader pointShader;
    GameObject metaballCanvas;
    Mesh metaballCanvasMesh;
    Material metaballCanvasMeshMaterial;
    RenderTexture metaballRenderTexture;
    [SerializeField]
    ComputeShader metaballRenderCS;
    MeshFilter meshFilter;
    [SerializeField]
    Shader metaballShader;
    [SerializeField]
    float threshold;
    Material pointMaterial;
    Material metaballMaterial;
    Matrix4x4[] objectToWorld;
    Vector4[] objectWorldPositions;

    [SerializeField]
    bool renderPoint;
    [SerializeField]
    bool renderMetaball;
    void Start()
    {
        pointMaterial = new Material(pointShader);
        pointMaterial.enableInstancing = true;

        metaballMaterial = new Material(metaballShader);
        metaballMaterial.enableInstancing = true;

        metaballCanvas = new GameObject();
        metaballCanvasMesh = new Mesh();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        CreateMesh();

        metaballRenderTexture = new RenderTexture(512, 512, 24);
        metaballRenderTexture.enableRandomWrite = true;
        metaballRenderTexture.Create();

        metaballCanvasMeshMaterial = new Material(Shader.Find("Unlit/Transparent"));
        metaballCanvasMeshMaterial.mainTexture = metaballRenderTexture;

        renderer.material = metaballCanvasMeshMaterial;
    }

    void Update()
    {
        objectToWorld = new Matrix4x4[transform.childCount];
        objectWorldPositions = new Vector4[1000];
        Vector4[] objectLocalPositions = new Vector4[1000];

        float localMinX = 0;
        float minX = 0;
        float localMaxX = 0;
        float maxX = 0;
        float localMaxY = 0;
        float maxY = 0;
        float localMinY = 0;
        float minY = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            objectToWorld[i] = Matrix4x4.Translate(child.position);
            objectWorldPositions[i] = child.position;
            objectLocalPositions[i] = child.localPosition;

            if (localMinX > child.localPosition.x)
            {
                localMinX = child.localPosition.x;
                minX = child.transform.TransformPoint(new Vector3()).x;
            }
            if (localMaxX < child.localPosition.x)
            {
                localMaxX = child.localPosition.x;
                maxX = child.transform.TransformPoint(new Vector3()).x;
            }
            if (localMaxY < child.localPosition.y)
            {
                localMaxY = child.localPosition.y;
                maxY = child.transform.TransformPoint(new Vector3()).y;
            }
            if (localMinY > child.localPosition.y)
            {
                localMinY = child.localPosition.y;
                minY = child.transform.TransformPoint(new Vector3()).y;
            }
        }

        metaballRenderCS.SetFloat("Threshold", threshold);
        metaballRenderCS.SetFloat("Resolution", metaballRenderTexture.width);
        metaballRenderCS.SetFloat("Radius", Point.radius);
        metaballRenderCS.SetFloat("Width", Bubble.radius * 4);
        metaballRenderCS.SetTexture(0, "Result", metaballRenderTexture);
        metaballRenderCS.SetInt("Count", transform.childCount);
        metaballRenderCS.SetVectorArray("Positions", objectLocalPositions);
        metaballRenderCS.Dispatch(0, metaballRenderTexture.width / 8, metaballRenderTexture.height / 8, 1);

        if (renderPoint && transform.childCount > 0)
            RenderPoint();
        if (renderMetaball && transform.childCount > 0)
            RenderMetaball();
    }

    void RenderMetaball()
    {
        metaballMaterial.SetVectorArray("Positions", objectWorldPositions);
        metaballMaterial.SetInteger("Count", transform.childCount);
        metaballMaterial.SetFloat("Scale", Point.radius * 2);
        metaballMaterial.SetFloat("Threshold", threshold);

        RenderParams rp = new RenderParams(metaballMaterial);
        Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }

    void RenderPoint()
    {
        pointMaterial.SetFloat("Scale", Point.radius * 2);

        RenderParams rp = new RenderParams(pointMaterial);
        Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }

    void CreateMesh()
    {
        // starting from top-left
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-1, 1, 0);
        vertices[1] = new Vector3(1, 1, 0);
        vertices[2] = new Vector3(1, -1, 0);
        vertices[3] = new Vector3(-1, -1, 0);

        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 1);
        uv[1] = new Vector2(1, 1);
        uv[2] = new Vector2(1, 0);
        uv[3] = new Vector2(0, 0);

        metaballCanvasMesh.vertices = vertices;
        metaballCanvasMesh.triangles = triangles;
        metaballCanvasMesh.uv = uv;

        meshFilter.mesh = metaballCanvasMesh;
    }
}