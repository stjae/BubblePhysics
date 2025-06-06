using System;
using Unity.Mathematics;
using UnityEngine;

public class Render : MonoBehaviour
{
    Bubble bubble;
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    Shader pointShader;
    Material pointMaterial;
    [SerializeField]
    Shader metaballShader;
    Material metaballMaterial;
    [SerializeField]
    ComputeShader metaballRenderCS;
    [SerializeField]
    float metaballThreshold;
    Matrix4x4[] objectToWorld;
    Vector4[] objectWorldPositions;
    Vector4[] objectLocalPositions;
    Mesh metaballCanvasMesh;
    Mesh metaballCanvasMeshFinal;
    Material metaballCanvasMeshMaterial; // center tile
    RenderTexture metaballRenderTexture; // center tile
    int mbRenderTextureSize = 128;
    bool[,] mbRenderFlagsX;
    bool[,] mbRenderFlagsY;
    RenderTexture[,] mbRenderTexturesX;
    RenderTexture[,] mbRenderTexturesY;
    Material[,] mbMaterialsX;
    Material[,] mbMaterialsY;
    bool[,,] mbRenderFlagsPosXY;
    bool[,,] mbRenderFlagsNegXY;
    RenderTexture[,,] mbRenderTexturesPosXY;
    RenderTexture[,,] mbRenderTexturesNegXY;
    Material[,,] mbMaterialsPosXY;
    Material[,,] mbMaterialsNegXY;
    static public int textureTileCoverage = 10;
    Vector3 shaderOffset;
    Vector3 positionOffset;

    [SerializeField]
    bool renderPoint;
    [SerializeField]
    bool renderMetaball;
    void Start()
    {
        bubble = transform.GetComponent<Bubble>();

        pointMaterial = new Material(pointShader);
        pointMaterial.enableInstancing = true;

        metaballMaterial = new Material(metaballShader);
        metaballMaterial.enableInstancing = true;

        CreateCanvasMesh();

        metaballRenderTexture = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
        metaballRenderTexture.enableRandomWrite = true;
        metaballRenderTexture.Create();

        metaballCanvasMeshMaterial = new Material(Shader.Find("Unlit/Transparent"));
        metaballCanvasMeshMaterial.mainTexture = metaballRenderTexture;

        CreateRenderTextures1D();
        CreateRenderTextures2D();
    }

    void Update()
    {
        objectToWorld = new Matrix4x4[transform.childCount];
        objectWorldPositions = new Vector4[1000];
        objectLocalPositions = new Vector4[1000];

        mbRenderFlagsX = new bool[2, textureTileCoverage];
        mbRenderFlagsY = new bool[2, textureTileCoverage];

        mbRenderFlagsPosXY = new bool[textureTileCoverage, 2, textureTileCoverage];
        mbRenderFlagsNegXY = new bool[textureTileCoverage, 2, textureTileCoverage];

        shaderOffset = transform.position - bubble.transform.position;
        positionOffset = bubble.transform.position - transform.position;

        Vector3[] pointOffset = new Vector3[9];
        pointOffset[0] = Vector3.zero;
        pointOffset[1] = Vector3.up * Point.radius * 3;
        pointOffset[2] = (Vector3.up + Vector3.right).normalized * Point.radius * 3;
        pointOffset[3] = Vector3.right * Point.radius * 3;
        pointOffset[4] = (Vector3.right + Vector3.down).normalized * Point.radius * 3;
        pointOffset[5] = Vector3.down * Point.radius * 3;
        pointOffset[6] = (Vector3.down + Vector3.left).normalized * Point.radius * 3;
        pointOffset[7] = Vector3.left * Point.radius * 3;
        pointOffset[8] = (Vector3.left + Vector3.up).normalized * Point.radius * 3;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            objectToWorld[i] = Matrix4x4.Translate(child.position);
            objectWorldPositions[i] = child.position;
            objectLocalPositions[i] = child.localPosition;

            Vector3 shaderPosition = shaderOffset + child.localPosition;

            for (int j = 0; j < 9; j++)
            {
                Vector3 signalPosition = shaderPosition + pointOffset[j];

                if (Math.Abs(signalPosition.x) > textureTileCoverage || Math.Abs(signalPosition.y) > textureTileCoverage)
                    continue;

                SignalRenderFlagsX(signalPosition);
                SignalRenderFlagsY(signalPosition);

                SignalRenderFlagsPosXY(signalPosition);
                SignalRenderFlagsNegXY(signalPosition);
            }
        }

        // RenderPoint();
        // RenderMetaball();

        RenderMetaballTiles();
    }

    void RenderMetaballTiles()
    {
        metaballRenderCS.SetFloat("Threshold", metaballThreshold);
        metaballRenderCS.SetFloat("Resolution", mbRenderTextureSize);
        metaballRenderCS.SetFloat("Radius", Point.radius);
        metaballRenderCS.SetInt("Count", transform.childCount);
        metaballRenderCS.SetVectorArray("Positions", objectLocalPositions);

        // render center tile
        RenderParams rp = new RenderParams(metaballCanvasMeshMaterial);
        rp.layer = LayerMask.NameToLayer("MetaballRender");
        metaballRenderCS.SetTexture(0, "Result", metaballRenderTexture);
        metaballRenderCS.SetVector("Offset", shaderOffset);
        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
        Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position));

        RenderTextureTiles1D();
        RenderTextureTiles2D();
    }

    void RenderMetaball()
    {
        metaballMaterial.SetVectorArray("Positions", objectWorldPositions);
        metaballMaterial.SetInteger("Count", transform.childCount);
        metaballMaterial.SetFloat("Scale", Point.radius * 2);
        metaballMaterial.SetFloat("Threshold", metaballThreshold);

        RenderParams rp = new RenderParams(metaballMaterial);
        Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }

    void RenderPoint()
    {
        pointMaterial.SetFloat("Scale", Point.radius * 2);

        RenderParams rp = new RenderParams(pointMaterial);
        rp.layer = LayerMask.NameToLayer("PointRender");
        Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }

    void CreateCanvasMesh()
    {
        metaballCanvasMesh = new Mesh();

        // starting from top-left
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.5f, 0.5f, 0);
        vertices[1] = new Vector3(0.5f, 0.5f, 0);
        vertices[2] = new Vector3(0.5f, -0.5f, 0);
        vertices[3] = new Vector3(-0.5f, -0.5f, 0);

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
    }
    void CreateRenderTextures1D()
    {
        mbRenderTexturesX = new RenderTexture[2, textureTileCoverage];
        mbMaterialsX = new Material[2, textureTileCoverage];
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                mbRenderTexturesX[i, j] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                mbRenderTexturesX[i, j].enableRandomWrite = true;
                mbRenderTexturesX[i, j].Create();

                mbMaterialsX[i, j] = new Material(Shader.Find("Unlit/Transparent"));
                mbMaterialsX[i, j].mainTexture = mbRenderTexturesX[i, j];
            }
        }

        mbRenderTexturesY = new RenderTexture[2, textureTileCoverage];
        mbMaterialsY = new Material[2, textureTileCoverage];
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                mbRenderTexturesY[i, j] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                mbRenderTexturesY[i, j].enableRandomWrite = true;
                mbRenderTexturesY[i, j].Create();

                mbMaterialsY[i, j] = new Material(Shader.Find("Unlit/Transparent"));
                mbMaterialsY[i, j].mainTexture = mbRenderTexturesY[i, j];
            }
        }
    }

    void CreateRenderTextures2D()
    {
        mbRenderTexturesPosXY = new RenderTexture[textureTileCoverage, 2, textureTileCoverage];
        mbRenderTexturesNegXY = new RenderTexture[textureTileCoverage, 2, textureTileCoverage];
        mbMaterialsPosXY = new Material[textureTileCoverage, 2, textureTileCoverage];
        mbMaterialsNegXY = new Material[textureTileCoverage, 2, textureTileCoverage];

        for (int i = 0; i < textureTileCoverage; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < textureTileCoverage; k++)
                {
                    mbRenderTexturesPosXY[i, j, k] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                    mbRenderTexturesPosXY[i, j, k].enableRandomWrite = true;
                    mbRenderTexturesPosXY[i, j, k].Create();

                    mbRenderTexturesNegXY[i, j, k] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                    mbRenderTexturesNegXY[i, j, k].enableRandomWrite = true;
                    mbRenderTexturesNegXY[i, j, k].Create();

                    mbMaterialsPosXY[i, j, k] = new Material(Shader.Find("Unlit/Transparent"));
                    mbMaterialsPosXY[i, j, k].mainTexture = mbRenderTexturesPosXY[i, j, k];

                    mbMaterialsNegXY[i, j, k] = new Material(Shader.Find("Unlit/Transparent"));
                    mbMaterialsNegXY[i, j, k].mainTexture = mbRenderTexturesNegXY[i, j, k];
                }
            }
        }
    }

    void SignalRenderFlagsX(Vector3 position)
    {
        if (Math.Abs(position.y) > 0.5)
            return;

        // position to index
        if (position.x > 0.5)
            mbRenderFlagsX[0, (int)Math.Round(position.x) - 1] = true;
        else if (position.x < -0.5)
            mbRenderFlagsX[1, (int)Math.Round(Math.Abs(position.x)) - 1] = true;
    }

    void SignalRenderFlagsY(Vector3 position)
    {
        if (Math.Abs(position.x) > 0.5)
            return;

        // position to index
        if (position.y > 0.5)
            mbRenderFlagsY[0, (int)Math.Round(position.y) - 1] = true;
        else if (position.y < -0.5)
            mbRenderFlagsY[1, (int)Math.Round(Math.Abs(position.y)) - 1] = true;
    }

    void SignalRenderFlagsPosXY(Vector3 position)
    {
        if (position.x > 0.5)
        {
            if (position.y > 0.5)
                mbRenderFlagsPosXY[(int)Math.Round(position.x) - 1, 0, (int)Math.Round(position.y) - 1] = true;
            else if (position.y < -0.5)
                mbRenderFlagsPosXY[(int)Math.Round(position.x) - 1, 1, (int)Math.Round(Math.Abs(position.y)) - 1] = true;
        }
    }

    void SignalRenderFlagsNegXY(Vector3 position)
    {
        if (position.x < -0.5)
        {
            if (position.y > 0.5)
                mbRenderFlagsNegXY[(int)Math.Round(Math.Abs(position.x)) - 1, 0, (int)Math.Round(position.y) - 1] = true;
            else if (position.y < -0.5)
                mbRenderFlagsNegXY[(int)Math.Round(Math.Abs(position.x)) - 1, 1, (int)Math.Round(Math.Abs(position.y)) - 1] = true;
        }
    }

    void RenderTextureTileCenter()
    {

    }

    void RenderTextureTiles1D()
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                if (mbRenderFlagsX[i, j])
                {
                    RenderParams rp = new RenderParams(mbMaterialsX[i, j]);
                    rp.layer = LayerMask.NameToLayer("MetaballRender");
                    metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesX[i, j]);

                    if (i == 0)
                    {
                        Vector3 tileOffset = Vector3.right * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                    else
                    {
                        Vector3 tileOffset = Vector3.left * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                }

                if (mbRenderFlagsY[i, j])
                {
                    RenderParams rp = new RenderParams(mbMaterialsY[i, j]);
                    rp.layer = LayerMask.NameToLayer("MetaballRender");
                    metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesY[i, j]);

                    if (i == 0)
                    {
                        Vector3 tileOffset = Vector3.up * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                    else
                    {
                        Vector3 tileOffset = Vector3.down * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                }
            }
        }
    }

    void RenderTextureTiles2D()
    {
        for (int i = 0; i < textureTileCoverage; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < textureTileCoverage; k++)
                {
                    if (mbRenderFlagsPosXY[i, j, k])
                    {
                        RenderParams rp = new RenderParams(mbMaterialsPosXY[i, j, k]);
                        rp.layer = LayerMask.NameToLayer("MetaballRender");
                        metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesPosXY[i, j, k]);

                        Vector3 tileOffset = Vector3.right * (i + 1);
                        if (j == 0)
                        {
                            tileOffset += Vector3.up * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                        else
                        {
                            tileOffset += Vector3.down * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                    }

                    if (mbRenderFlagsNegXY[i, j, k])
                    {
                        RenderParams rp = new RenderParams(mbMaterialsNegXY[i, j, k]);
                        rp.layer = LayerMask.NameToLayer("MetaballRender");
                        metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesNegXY[i, j, k]);

                        Vector3 tileOffset = Vector3.left * (i + 1);
                        if (j == 0)
                        {
                            tileOffset += Vector3.up * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                        else
                        {
                            tileOffset += Vector3.down * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(rp, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
    }
}