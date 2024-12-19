using UnityEngine;

public class Render : MonoBehaviour
{
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    Shader pointShader;
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
    }

    void Update()
    {
        objectToWorld = new Matrix4x4[transform.childCount];
        objectWorldPositions = new Vector4[1000];

        for (int i = 0; i < transform.childCount; i++)
        {
            objectToWorld[i] = Matrix4x4.Translate(transform.GetChild(i).position);
            objectWorldPositions[i] = transform.GetChild(i).position;
        }

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
}