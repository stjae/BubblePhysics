using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public struct Particle
{
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 force;
    public float radius;
    public float density;
    public float pressure;
}

public class Point : MonoBehaviour
{
    [field: SerializeField]
    public float radius { get; private set; }
    public Particle particle;
    PhysicsMaterial2D pMaterial;

    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Point");
        transform.GetComponent<CircleCollider2D>().radius = radius;
        pMaterial = new PhysicsMaterial2D();
        pMaterial.friction = 0;
        particle = new Particle();
    }
    void FixedUpdate()
    {
        particle.velocity += FluidSim.gravity * FluidSim.deltaTime;
        particle.position = (Vector2)transform.position + particle.velocity;

        transform.position = particle.position;
    }
    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            particle.velocity = Vector2.Reflect(particle.velocity, contact.normal) * 0.5f;
        }
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            particle.velocity += contact.normal * 0.005f;
            // Debug.DrawLine(particle.position, contact.collider.ClosestPoint(particle.position), Color.red);
        }
    }
}
