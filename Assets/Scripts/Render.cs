using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

public class Render : MonoBehaviour
{
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    Shader pointShader;
    Material pointMaterial;
    [SerializeField]
    bool renderPoint;

    [SerializeField]
    bool renderMesh;
    [SerializeField]
    float concaveThreshold;
    [SerializeField]
    int interpolationCount;
    List<Vector2> concaveLines;
    List<Vector2> interpolationTest;
    List<int> m_triangles;
    List<Vector3> m_vertices;
    MeshFilter faceMeshFilter;
    void Start()
    {
        pointMaterial = new Material(pointShader);
        pointMaterial.enableInstancing = true;

        faceMeshFilter = GetComponent<MeshFilter>();

        concaveLines = new List<Vector2>();
        interpolationTest = new List<Vector2>();
        m_triangles = new List<int>();
        m_vertices = new List<Vector3>();
    }

    void Update()
    {
        RenderMesh();
        if (renderPoint)
            RenderPoint();
    }

    void RenderMesh()
    {
        if (transform.childCount < 3)
            return;

        GiftWrapper.Wrap(Point.particles.ToArray(), concaveLines, concaveThreshold, interpolationCount, interpolationTest);
        // GetComponent<EdgeCollider2D>().points = interpolationTest.ToArray();

        m_vertices.Clear();
        for (int i = 0; i < concaveLines.Count; i += 2)
        {
            m_vertices.Add(new Vector3());
            m_vertices.Add(concaveLines[i] + concaveLines[i].normalized * Point.radius);
            m_vertices.Add(concaveLines[i + 1] + concaveLines[i + 1].normalized * Point.radius);
        }

        m_triangles.Clear();
        for (int i = 0; i < m_vertices.Count; i++)
        {
            m_triangles.Add(i);
        }

        // TODO: consider move to shader
        Mesh newMesh = new Mesh();
        newMesh.vertices = m_vertices.ToArray();
        newMesh.triangles = m_triangles.ToArray();

        faceMeshFilter.mesh = newMesh;
    }

    void RenderPoint()
    {
        RenderParams rp = new RenderParams(pointMaterial);
        Matrix4x4[] objectToWorld = new Matrix4x4[transform.childCount];

        pointMaterial.SetFloat("_Scale", Point.radius * 2);
        for (int i = 0; i < transform.childCount; i++)
        {
            objectToWorld[i] = Matrix4x4.Translate(transform.GetChild(i).position);
        }

        if (transform.childCount > 0)
            Graphics.RenderMeshInstanced(rp, pointMesh, 0, objectToWorld, transform.childCount);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < interpolationTest.Count; i++)
        {
            //     GUIStyle style = new GUIStyle();
            //     style.normal.textColor = Color.red;
            //     Handles.Label(transform.position + convexHull.ToArray()[i], i.ToString(), style);
            Gizmos.DrawWireSphere((Vector2)transform.position + interpolationTest[i], 0.1f);
        };
    }
}