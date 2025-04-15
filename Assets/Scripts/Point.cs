using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;


public class Point : MonoBehaviour
{
    public static float radius;
    PhysicsMaterial2D pMaterial;
    FluidSim fluidSim;
    public Particle particle;

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

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.particles[transform.GetSiblingIndex()].velocity = Vector2.Reflect(fluidSim.particles[transform.GetSiblingIndex()].velocity, contact.normal) * 0.5f;
            fluidSim.particles[transform.GetSiblingIndex()].onGround = true;
        }
    }
    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        fluidSim.particles[transform.GetSiblingIndex()].onGround = false;
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.particles[transform.GetSiblingIndex()].velocity = new Vector2();
            fluidSim.particles[transform.GetSiblingIndex()].velocity += contact.normal * 0.01f;
            fluidSim.particles[transform.GetSiblingIndex()].onGround = true;
        }
    }

    void OnDrawGizmos()
    {
    }
}
