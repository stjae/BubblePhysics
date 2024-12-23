using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;


public class Point : MonoBehaviour
{
    public static float radius;
    PhysicsMaterial2D pMaterial;
    FluidSim fluidSim;

    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Point");
        transform.GetComponent<CircleCollider2D>().radius = radius;
        pMaterial = new PhysicsMaterial2D();
        pMaterial.friction = 0;
        fluidSim = transform.parent.GetComponent<FluidSim>();
    }

    void FixedUpdate()
    {
        // TODO: if statement
        CheckDetached();
        UpdateSprings();
        transform.GetComponent<CircleCollider2D>().radius = radius;
        transform.position = fluidSim.particles[transform.GetSiblingIndex()].position;
    }

    void UpdateSprings()
    {
        int requiredSize = fluidSim.particles.Count - transform.GetSiblingIndex() - 1;
        fluidSim.particles[transform.GetSiblingIndex()].springRestLengths.Clear();
        for (int i = 0; i < requiredSize; i++)
            fluidSim.particles[transform.GetSiblingIndex()].springRestLengths.Add(null);
    }

    void CheckDetached()
    {
        if ((transform.position - transform.parent.position).magnitude > Bubble.radius * 4.0f)
        {
            fluidSim.particles[transform.GetSiblingIndex()].isDetached = true;
            Bubble.radius = (float)Math.Sqrt((transform.parent.childCount - 1) / (Math.PI * 10));
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.particles[transform.GetSiblingIndex()].velocity = Vector2.Reflect(fluidSim.particles[transform.GetSiblingIndex()].velocity, contact.normal) * 0.5f;
        }
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.particles[transform.GetSiblingIndex()].velocity = new Vector2();
            fluidSim.particles[transform.GetSiblingIndex()].velocity += contact.normal * 0.01f;
        }
    }

    void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        Handles.Label(transform.position, fluidSim.particles[transform.GetSiblingIndex()].density.ToString(), style);
    }
}
