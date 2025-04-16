using UnityEngine;
using UnityEngine.Rendering;

public class OverlayCamera : MonoBehaviour
{
    [SerializeField]
    RenderTexture metaballRenderTexture;
    [SerializeField]
    RenderTexture backgroundRenderTexture;
    [SerializeField]
    Shader clearOverlayShader;
    [SerializeField]
    Shader refractionOverlayShader;
    [SerializeField]
    Shader metaballOverlayShader;
    [SerializeField]
    Texture2D noiseTexture;
    Material clearMaterial;
    Material refractionMaterial;
    Material metaballMaterial;
    Mesh quadMesh;

    void Start()
    {
        CreateQuadMesh();

        clearMaterial = new Material(clearOverlayShader);
        clearMaterial.SetTexture("_MainTex", metaballRenderTexture);
        clearMaterial.SetTexture("_BackgroundTexture", backgroundRenderTexture);

        refractionMaterial = new Material(refractionOverlayShader);
        refractionMaterial.SetTexture("_MainTex", metaballRenderTexture);
        refractionMaterial.SetTexture("_BackgroundTexture", backgroundRenderTexture);
        refractionMaterial.SetTexture("_NoiseTexture", noiseTexture);

        metaballMaterial = new Material(metaballOverlayShader);
        metaballMaterial.SetTexture("_MainTex", metaballRenderTexture);
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
    }

    private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        OnPostRender();
    }

    void OnPostRender()
    {
        clearMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
        refractionMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
        metaballMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
    }

    void CreateQuadMesh()
    {
        quadMesh = new Mesh();

        // starting from top-left
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-1.0f, 1.0f, 0);
        vertices[1] = new Vector3(1.0f, 1.0f, 0);
        vertices[2] = new Vector3(1.0f, -1.0f, 0);
        vertices[3] = new Vector3(-1.0f, -1.0f, 0);

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

        quadMesh.vertices = vertices;
        quadMesh.triangles = triangles;
        quadMesh.uv = uv;
    }
}
