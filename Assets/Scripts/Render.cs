using UnityEngine;

public class Render : MonoBehaviour
{
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    Shader pointShader;
    Material pointMaterial;
    void Start()
    {
        pointMaterial = new Material(pointShader);
        pointMaterial.enableInstancing = true;
    }

    void Update()
    {
        RenderParams rp = new RenderParams(pointMaterial);
        Matrix4x4[] objectToWorld = new Matrix4x4[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            objectToWorld[i] = Matrix4x4.Translate(transform.GetChild(i).position);
            pointMaterial.SetFloat("_Scale", transform.GetChild(i).GetComponent<Point>().radius * 2);
        }

        if (transform.childCount > 0)
            Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }
}
