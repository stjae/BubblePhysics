using System;
using UnityEngine;

public class Point : MonoBehaviour
{
    public float radius;
    Bubble bubble;
    FluidSim fluidSim;
    public bool isOnGround;
    public Vector3 groundNormal;
    public float collisionForce;
    public float lifeTime;
    public bool isVisible;
    float maxInterval = 1.0f;
    float minInterval = 0.5f;

    void Awake()
    {
        isVisible = true;
        gameObject.layer = LayerMask.NameToLayer("Point");
        transform.GetComponent<CircleCollider2D>().radius = radius;
        bubble = transform.parent.GetComponent<Bubble>();
        fluidSim = transform.parent.GetComponent<FluidSim>();
    }

    void Update()
    {
        UpdateLife();
        Blink();
    }
    void FixedUpdate()
    {
        transform.GetComponent<CircleCollider2D>().radius = radius;
        transform.position = fluidSim.particles[transform.GetSiblingIndex()].position;
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.particles[transform.GetSiblingIndex()].velocity = Vector2.Reflect(fluidSim.particles[transform.GetSiblingIndex()].velocity, contact.normal) * 0.5f;
            isOnGround = true;
            groundNormal = contact.normal;
        }
    }
    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        isOnGround = false;
        collisionForce = 0.01f;
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            collisionForce += 0.02f;
            fluidSim.particles[transform.GetSiblingIndex()].velocity = new Vector2();
            fluidSim.particles[transform.GetSiblingIndex()].velocity += contact.normal * collisionForce;
            isOnGround = true;
            groundNormal = contact.normal;
        }
    }

    public int GetIndex()
    {
        return transform.GetSiblingIndex();
    }

    public Particle GetParticle()
    {
        return fluidSim.particles[transform.GetSiblingIndex()];
    }

    void UpdateLife()
    {
        if (lifeTime <= 0)
        {
            fluidSim.particles[transform.GetSiblingIndex()].isActive = false;
            fluidSim.particles[transform.GetSiblingIndex()] = new Particle();
            gameObject.SetActive(false);
        }
    }

    void Blink()
    {
        if (lifeTime == bubble.MaxPointLifeTime) return;

        float normalized = lifeTime / bubble.MaxPointLifeTime;
        float interval = Mathf.Lerp(minInterval, maxInterval, normalized);
        float cycles = Time.time / interval;
        isVisible = (Mathf.FloorToInt(cycles) % 2) == 0;
    }
}
