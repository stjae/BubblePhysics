using UnityEngine;
using UnityEngine.Rendering;

public class OverlayCamera : MonoBehaviour
{
    const int textureWidth = 1920;
    const int textureHeight = 1080;
    [SerializeField]
    RenderTexture pointRT;
    [SerializeField]
    RenderTexture metaballRT;
    [SerializeField]
    RenderTexture backgroundRT;
    [SerializeField]
    RenderTexture eyeRT;
    [SerializeField]
    Texture2D noiseTexture;
    [SerializeField]
    RenderTexture metaballMaskRT;
    [SerializeField]
    RenderTexture metaballMaskInverseRT;
    [SerializeField]
    RenderTexture metaballMaskBlurXRT;
    [SerializeField]
    RenderTexture metaballMaskBlurYRT;
    [SerializeField]
    RenderTexture metaballMaskSeparableGaussianBlurRT;
    [SerializeField]
    RenderTexture metaballMaskBlurUpscaleRT;
    [SerializeField]
    RenderTexture metaballMaskBlurOutlineRT;
    [SerializeField]
    Shader backgroundOverlayShader;
    [SerializeField]
    Shader eyeOverlayShader;
    [SerializeField]
    Shader clearOverlayShader;
    [SerializeField]
    Shader refractionOverlayShader;
    [SerializeField]
    Shader metaballOverlayShader;
    [SerializeField]
    Shader blurXShader;
    [SerializeField]
    Shader blurYShader;
    [SerializeField]
    Shader separableGaussianBlurShader;
    Material backgroundMaterial;
    Material eyeMaterial;
    Material clearMaterial;
    Material refractionMaterial;
    Material metaballMaterial;
    Material blurXMaterial;
    Material blurYMaterial;
    Material separableGaussianBlurMaterial;
    Mesh quadMesh;
    [SerializeField]
    float blurRadius;
    [SerializeField]
    float downSampleFactor = 4;

    void Start()
    {
        CreateQuadMesh();

        backgroundMaterial = new Material(backgroundOverlayShader);
        backgroundMaterial.SetTexture("_MainTex", backgroundRT);
        backgroundMaterial.SetTexture("_Mask", metaballMaskBlurOutlineRT);
        backgroundMaterial.SetTexture("_NoiseTexture", noiseTexture);

        clearMaterial = new Material(clearOverlayShader);
        clearMaterial.SetTexture("_MainTex", metaballRT);
        clearMaterial.SetTexture("_Mask", metaballMaskBlurOutlineRT);
        clearMaterial.SetTexture("_BackgroundTexture", backgroundRT);

        refractionMaterial = new Material(refractionOverlayShader);
        refractionMaterial.SetTexture("_MainTex", metaballMaskBlurOutlineRT);
        refractionMaterial.SetTexture("_BackgroundTexture", backgroundRT);
        refractionMaterial.SetTexture("_NoiseTexture", noiseTexture);

        eyeMaterial = new Material(eyeOverlayShader);
        eyeMaterial.SetTexture("_MainTex", eyeRT);

        metaballMaterial = new Material(metaballOverlayShader);
        metaballMaterial.SetTexture("_MainTex", metaballRT);

        blurXMaterial = new Material(blurXShader);
        blurXMaterial.SetTexture("_MainTex", metaballMaskInverseRT);
        blurXMaterial.SetFloat("_Width", textureWidth / downSampleFactor);
        blurXMaterial.SetFloat("_Height", textureHeight / downSampleFactor);
        blurYMaterial = new Material(blurYShader);
        blurYMaterial.SetTexture("_MainTex", metaballMaskBlurXRT);
        blurYMaterial.SetFloat("_Width", textureWidth / downSampleFactor);
        blurYMaterial.SetFloat("_Height", textureHeight / downSampleFactor);

        separableGaussianBlurMaterial = new Material(separableGaussianBlurShader);
        separableGaussianBlurMaterial.SetFloat("_Width", textureWidth);
        separableGaussianBlurMaterial.SetFloat("_Height", textureHeight);
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
        GetBlurredMask();
        backgroundMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
        refractionMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
        metaballMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
        eyeMaterial.SetPass(0);
        Graphics.DrawMeshNow(quadMesh, Matrix4x4.identity);
    }

    void GetBlurredMask()
    {
        RenderTexture currentActive = RenderTexture.active;

        RenderTexture.active = metaballMaskBlurXRT;
        Graphics.SetRenderTarget(metaballMaskBlurXRT);
        separableGaussianBlurMaterial.SetFloat("_Radius", blurRadius);
        Graphics.Blit(metaballMaskInverseRT, metaballMaskBlurXRT, separableGaussianBlurMaterial, 0);
        RenderTexture.active = metaballMaskBlurYRT;
        Graphics.SetRenderTarget(metaballMaskBlurYRT);
        Graphics.Blit(metaballMaskBlurXRT, metaballMaskBlurYRT, separableGaussianBlurMaterial, 1);

        // upscale
        Graphics.Blit(metaballMaskBlurYRT, metaballMaskBlurUpscaleRT);

        RenderTexture.active = currentActive;
        Graphics.SetRenderTarget(currentActive);
    }

    void DebugRenderTexturePixel(RenderTexture target, int x, int y)
    {
        RenderTexture.active = target;
        Texture2D tex = new Texture2D(target.width, target.height);
        tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        tex.Apply();
        Debug.Log(tex.GetPixel(x, y));
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
