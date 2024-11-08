using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public class Point : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Shader shader;
    Material material;
    MaterialPropertyBlock mpBlock;
    [SerializeField]
    int mass;
    [SerializeField]
    float scale;

    void Start()
    {
        material = new Material(shader);
        material.enableInstancing = true;
        material.SetFloat("scale", scale);

        mpBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        rp.matProps = mpBlock;
        Matrix4x4[] objectToWorld = new Matrix4x4[transform.childCount];
        float[] scaleArray = new float[1024];

        for (int i = 0; i < transform.childCount; ++i)
        {
            Vector3 toCenter = transform.position - transform.GetChild(i).position;
            transform.GetChild(i).GetComponent<CircleCollider2D>().radius = Math.Max(0, scale / 2 - (float)Math.Pow(toCenter.magnitude, 2) * 0.5f);
            transform.GetChild(i).GetComponent<Rigidbody2D>().AddForce(-1.0f * new Vector2((transform.GetChild(i).position - transform.position).x, (transform.GetChild(i).position - transform.position).y));
            objectToWorld[i] = Matrix4x4.Translate(transform.GetChild(i).position);
            scaleArray[i] = Math.Max(0, (scale / 2 - (float)Math.Pow(toCenter.magnitude, 2) * 0.5f) * 2);
        }

        mpBlock.SetColor("_Color", UnityEngine.Random.ColorHSV());
        mpBlock.SetFloatArray("_Scale", scaleArray);

        if (transform.childCount > 0)
            Graphics.DrawMeshInstanced(mesh, 0, material, objectToWorld, transform.childCount, mpBlock);
    }

    public void Increase(float volume)
    {
        if (transform.childCount < volume * mass)
        {
            GameObject point = new GameObject("Point");
            point.layer = LayerMask.NameToLayer("Point");
            point.transform.SetParent(transform, false);
            point.transform.Translate(new Vector3(UnityEngine.Random.Range(0, 0.01f), UnityEngine.Random.Range(0, 0.01f), 0)); // prevent points get piled up
            point.AddComponent<Rigidbody2D>().gravityScale = 0;
            point.AddComponent<CircleCollider2D>().radius = scale / 2;
        }
    }
    public void Reduce(float volume)
    {
        if (transform.childCount > volume * mass)
            Destroy(transform.GetChild(0).gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
