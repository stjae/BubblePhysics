using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;


public class Point : MonoBehaviour
{
    [field: SerializeField]
    public float radius { get; private set; }
    PhysicsMaterial2D pMaterial;
    public static List<Particle> particles; // from fluid simulation

    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Point");
        transform.GetComponent<CircleCollider2D>().radius = radius;
        pMaterial = new PhysicsMaterial2D();
        pMaterial.friction = 0;
    }

    void FixedUpdate()
    {
        UpdateSprings();
    }


    public void UpdateSprings()
    {
        int requiredSize = particles.Count - transform.GetSiblingIndex() - 1;
        particles[transform.GetSiblingIndex()].springRestLengths.Clear();
        for (int i = 0; i < requiredSize; i++)
            particles[transform.GetSiblingIndex()].springRestLengths.Add(null);
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            particles[transform.GetSiblingIndex()].velocity = Vector2.Reflect(particles[transform.GetSiblingIndex()].velocity, contact.normal) * 0.5f;
        }
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            particles[transform.GetSiblingIndex()].velocity += contact.normal * 0.01f;
        }
    }

    void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        // Handles.Label(transform.position, transform.GetSiblingIndex().ToString(), style);
    }
}
