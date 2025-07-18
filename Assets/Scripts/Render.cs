using System;
using UnityEngine;

public class Render : MonoBehaviour
{
    Bubble bubble;
    FluidSim fluidSim;
    Point[] points;
    [SerializeField]
    ComputeShader metaballRenderCS;
    [SerializeField]
    float metaballThreshold;
    Matrix4x4[] objectToWorld;
    Vector4[] worldPositions;
    Vector4[] localPositions;
    float[] pointLifeTimes;
    ComputeBuffer localPositionsBuffer;
    ComputeBuffer pointLifeTimesBuffer;
    Mesh metaballCanvasMesh; // Quad used to display the metaball render texture 
                             // メタボールレンダーテクスチャを適用する四角形メッシュ
    Material metaballCanvasMeshMaterial;
    int mbRenderTextureSize = 128; // Size of a single metaball render texture 
                                   // メタボールのレンダリングに使用するレンダーテクスチャ1枚の解像度
    RenderTexture metaballRenderTexture; // The central(x=0, y=0) render texture for rendering metaballs 
                                         // メタボールのレンダリングに使用する中心(x=0, y=0)レンダーテクスチャ
    RenderTexture[,] mbRenderTexturesX; // Metaball render texture array at X-axis and Y=0 position(except x=0, y=0) 
                                        // X軸上、Y=0の位置に配置された（X=0, y=0を除く）メタボールレンダーテクスチャ配列
    RenderTexture[,] mbRenderTexturesY; // Metaball render texture array at Y-axis and X=0 position(except x=0, y=0) 
                                        // Y軸上、X=0の位置に配置された（X=0, y=0を除く）メタボールレンダーテクスチャ配列
    RenderTexture[,,] mbRenderTexturesPosX; // Metaball render texture array at positive X-axis(except Y=0 position) 
                                            // Dimensions represent: 
                                            // [X position(only positive)][Y direction (+/-)][Y coordinate (excluding Y=0)] 
                                            // X軸の正方向（Y=0を除く）に配置されたメタボール用レンダーテクスチャ配列
                                            // 各次元の意味：
                                            // [X座標（正の値のみ）][Y方向（正/負）][Y座標（Y=0を除く）]
    RenderTexture[,,] mbRenderTexturesNegX; // Metaball render texture array at negative X-axis(except Y=0 position) 
                                            // Dimensions represent: 
                                            // [X position(only negative)][Y direction (+/-)][Y coordinate (excluding Y=0)] 
                                            // X軸の負方向（Y=0を除く）に配置されたメタボール用レンダーテクスチャ配列
                                            // 各次元の意味：
                                            // [X座標（負の値のみ）][Y方向（正/負）][Y座標（Y=0を除く）]
    bool[,] mbRenderFlagsX;
    bool[,] mbRenderFlagsY;
    bool[,,] mbRenderFlagsPosX;
    bool[,,] mbRenderFlagsNegX; // The signal array is used to determine which points should be rendered.
                                // It marks positions where points are located with true, and those positions will be rendered.
                                // Positions with false values are ignored during rendering.
                                // シグナル配列は、ポイントが位置する場所を決定するために使用されます。
                                // 配列内でtrueの値を持つ位置がレンダリングされ、それ以外の場所は無視されます。
    RenderParams renderParams;
    MaterialPropertyBlock materialPropertyBlock;
    static public int textureTileCoverage = 10;
    Vector3 shaderOffset;
    Vector3 positionOffset;

    void Start()
    {
        bubble = transform.GetComponent<Bubble>();
        fluidSim = transform.GetComponent<FluidSim>();
        points = new Point[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i).GetComponent<Point>();
        }
        pointLifeTimes = new float[transform.childCount];
        localPositionsBuffer = new ComputeBuffer(transform.childCount, sizeof(float) * 4, ComputeBufferType.Default);
        pointLifeTimesBuffer = new ComputeBuffer(transform.childCount, sizeof(float), ComputeBufferType.Default);

        CreateCanvasMesh();

        materialPropertyBlock = new MaterialPropertyBlock();
        metaballCanvasMeshMaterial = new Material(Shader.Find("Unlit/Transparent"));
        renderParams = new RenderParams(metaballCanvasMeshMaterial);
        renderParams.matProps = materialPropertyBlock;
        renderParams.layer = LayerMask.NameToLayer("MetaballRender");

        metaballRenderTexture = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
        metaballRenderTexture.enableRandomWrite = true;
        metaballRenderTexture.Create();
        CreateRenderTextures1D();
        CreateRenderTextures2D();
    }

    void Update()
    {
        objectToWorld = new Matrix4x4[bubble.MaxPointCount];
        worldPositions = new Vector4[bubble.MaxPointCount];
        localPositions = new Vector4[bubble.MaxPointCount];

        mbRenderFlagsX = new bool[2, textureTileCoverage];
        mbRenderFlagsY = new bool[2, textureTileCoverage];

        mbRenderFlagsPosX = new bool[textureTileCoverage, 2, textureTileCoverage];
        mbRenderFlagsNegX = new bool[textureTileCoverage, 2, textureTileCoverage];

        shaderOffset = transform.position - bubble.Position;
        positionOffset = bubble.Position - transform.position;

        Vector3[] pointOffset = new Vector3[9]; // Since a point is too small, an offset is used to expand the area around the point 
                                                // ポイントは非常に小さいため、オフセットを使用してポイント周辺の範囲も有効として扱う
        pointOffset[0] = Vector3.zero;
        pointOffset[1] = 3 * Point.radius * Vector3.up;
        pointOffset[2] = 3 * Point.radius * (Vector3.up + Vector3.right).normalized;
        pointOffset[3] = 3 * Point.radius * Vector3.right;
        pointOffset[4] = 3 * Point.radius * (Vector3.right + Vector3.down).normalized;
        pointOffset[5] = 3 * Point.radius * Vector3.down;
        pointOffset[6] = 3 * Point.radius * (Vector3.down + Vector3.left).normalized;
        pointOffset[7] = 3 * Point.radius * Vector3.left;
        pointOffset[8] = 3 * Point.radius * (Vector3.left + Vector3.up).normalized;

        for (int i = 0; i < fluidSim.particles.Length; i++)
        {
            if (!fluidSim.particles[i].isActive)
                continue;

            Particle particle = fluidSim.particles[i];
            objectToWorld[i] = Matrix4x4.Translate(particle.position);
            worldPositions[i] = particle.position;
            localPositions[i] = particle.localPosition;
            localPositions[i].w = 1; // use W coord for active particle flag
            pointLifeTimes[i] = points[i].lifeTime;

            Vector3 shaderPosition = shaderOffset + (Vector3)particle.localPosition;

            for (int j = 0; j < 9; j++)
            {
                Vector3 signalPosition = shaderPosition + pointOffset[j];

                if (Math.Abs(signalPosition.x) > textureTileCoverage || Math.Abs(signalPosition.y) > textureTileCoverage)
                    continue;

                SignalRenderFlagsX(signalPosition);
                SignalRenderFlagsY(signalPosition);
                SignalRenderFlagsPosX(signalPosition);
                SignalRenderFlagsNegX(signalPosition);
            }
        }

        localPositionsBuffer.SetData(localPositions);
        pointLifeTimesBuffer.SetData(pointLifeTimes);

        metaballRenderCS.SetFloat("Threshold", metaballThreshold);
        metaballRenderCS.SetFloat("Resolution", mbRenderTextureSize);
        metaballRenderCS.SetFloat("Radius", Point.radius);
        metaballRenderCS.SetFloat("MaxLifeTime", bubble.MaxPointLifeTime);
        metaballRenderCS.SetInt("Count", bubble.MaxPointCount);
        metaballRenderCS.SetBuffer(0, "PositionsBuffer", localPositionsBuffer);
        metaballRenderCS.SetBuffer(0, "LifeTimesBuffer", pointLifeTimesBuffer);

        RenderMetaballTiles();
    }

    void RenderMetaballTiles()
    {
        RenderCenterTextureTile();
        RenderTextureTiles1D();
        RenderTextureTiles2D();
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
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                mbRenderTexturesX[i, j] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                mbRenderTexturesX[i, j].enableRandomWrite = true;
                mbRenderTexturesX[i, j].Create();
            }
        }

        mbRenderTexturesY = new RenderTexture[2, textureTileCoverage];
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                mbRenderTexturesY[i, j] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                mbRenderTexturesY[i, j].enableRandomWrite = true;
                mbRenderTexturesY[i, j].Create();
            }
        }
    }

    void CreateRenderTextures2D()
    {
        mbRenderTexturesPosX = new RenderTexture[textureTileCoverage, 2, textureTileCoverage];
        mbRenderTexturesNegX = new RenderTexture[textureTileCoverage, 2, textureTileCoverage];

        for (int i = 0; i < textureTileCoverage; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < textureTileCoverage; k++)
                {
                    mbRenderTexturesPosX[i, j, k] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                    mbRenderTexturesPosX[i, j, k].enableRandomWrite = true;
                    mbRenderTexturesPosX[i, j, k].Create();

                    mbRenderTexturesNegX[i, j, k] = new RenderTexture(mbRenderTextureSize, mbRenderTextureSize, 0, RenderTextureFormat.ARGBFloat);
                    mbRenderTexturesNegX[i, j, k].enableRandomWrite = true;
                    mbRenderTexturesNegX[i, j, k].Create();
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

    void SignalRenderFlagsPosX(Vector3 position)
    {
        if (position.x > 0.5)
        {
            if (position.y > 0.5)
                mbRenderFlagsPosX[(int)Math.Round(position.x) - 1, 0, (int)Math.Round(position.y) - 1] = true;
            else if (position.y < -0.5)
                mbRenderFlagsPosX[(int)Math.Round(position.x) - 1, 1, (int)Math.Round(Math.Abs(position.y)) - 1] = true;
        }
    }

    void SignalRenderFlagsNegX(Vector3 position)
    {
        if (position.x < -0.5)
        {
            if (position.y > 0.5)
                mbRenderFlagsNegX[(int)Math.Round(Math.Abs(position.x)) - 1, 0, (int)Math.Round(position.y) - 1] = true;
            else if (position.y < -0.5)
                mbRenderFlagsNegX[(int)Math.Round(Math.Abs(position.x)) - 1, 1, (int)Math.Round(Math.Abs(position.y)) - 1] = true;
        }
    }

    void RenderCenterTextureTile() // Render the render texture located at origin
                                   // 原点に位置するレンダーテクスチャをレンダリングする
    {
        metaballRenderCS.SetTexture(0, "Result", metaballRenderTexture);
        metaballRenderCS.SetVector("Offset", shaderOffset);
        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
        materialPropertyBlock.SetTexture("_MainTex", metaballRenderTexture);
        Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position));
    }

    void RenderTextureTiles1D() // Render the render textures along the x-axis and y-axis direction (excluding the origin)
                                // x軸,y軸方向にあるレンダーテクスチャをレンダリングする（原点を除く）
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < textureTileCoverage; j++)
            {
                if (mbRenderFlagsX[i, j])
                {
                    materialPropertyBlock.SetTexture("_MainTex", mbRenderTexturesX[i, j]);
                    metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesX[i, j]);

                    if (i == 0)
                    {
                        Vector3 tileOffset = Vector3.right * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                    else
                    {
                        Vector3 tileOffset = Vector3.left * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                }

                if (mbRenderFlagsY[i, j])
                {
                    materialPropertyBlock.SetTexture("_MainTex", mbRenderTexturesY[i, j]);
                    metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesY[i, j]);

                    if (i == 0)
                    {
                        Vector3 tileOffset = Vector3.up * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                    else
                    {
                        Vector3 tileOffset = Vector3.down * (j + 1);
                        metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                        metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                        Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                    }
                }
            }
        }
    }

    void RenderTextureTiles2D() // Render the remaining textures
                                // 他のレンダーテクスチャをレンダリングする
    {
        for (int i = 0; i < textureTileCoverage; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < textureTileCoverage; k++)
                {
                    if (mbRenderFlagsPosX[i, j, k])
                    {
                        materialPropertyBlock.SetTexture("_MainTex", mbRenderTexturesPosX[i, j, k]);
                        metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesPosX[i, j, k]);

                        Vector3 tileOffset = Vector3.right * (i + 1);
                        if (j == 0)
                        {
                            tileOffset += Vector3.up * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                        else
                        {
                            tileOffset += Vector3.down * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                    }

                    if (mbRenderFlagsNegX[i, j, k])
                    {
                        materialPropertyBlock.SetTexture("_MainTex", mbRenderTexturesNegX[i, j, k]);
                        metaballRenderCS.SetTexture(0, "Result", mbRenderTexturesNegX[i, j, k]);

                        Vector3 tileOffset = Vector3.left * (i + 1);
                        if (j == 0)
                        {
                            tileOffset += Vector3.up * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                        else
                        {
                            tileOffset += Vector3.down * (k + 1);
                            metaballRenderCS.SetVector("Offset", shaderOffset - tileOffset);
                            metaballRenderCS.Dispatch(0, mbRenderTextureSize / 8, mbRenderTextureSize / 8, 1);
                            Graphics.RenderMesh(renderParams, metaballCanvasMesh, 0, Matrix4x4.Translate(positionOffset + transform.position + tileOffset));
                        }
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        if (localPositionsBuffer != null) localPositionsBuffer.Release();
        if (pointLifeTimesBuffer != null) pointLifeTimesBuffer.Release();

    }
}