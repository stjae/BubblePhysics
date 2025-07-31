using System;
using UnityEngine;

public class Point : MonoBehaviour
{
    public float radius;
    Bubble bubble;
    FluidSim fluidSim;
    public bool isOnGround;
    public Vector3 groundNormal;
    public float lifeTime;
    public bool isVisible;
    float maxInterval = 1.0f;
    float minInterval = 0.5f;
    float restitution = 0.8f;                  // 0~1 (1이면 완전 반사)
    float friction = 0.2f;                  // 0~1 (1이면 접선 성분 소멸)

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
        int idx = transform.GetSiblingIndex();
        Vector2 v = fluidSim.particles[idx].velocity;

        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            Vector2 n = contact.normal.normalized;
            float vDotN = Vector2.Dot(v, n);

            if (vDotN < 0f)
            {
                Vector2 vPerp = n * vDotN;
                Vector2 vTan = v - vPerp;

                Vector2 newPerp = -vPerp * restitution;
                Vector2 newTan = vTan * (1f - friction);

                v = newPerp + newTan;
            }

            isOnGround = true;
            groundNormal = n;
        }

        fluidSim.particles[idx].velocity = v;
    }

    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        isOnGround = false;
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        int idx = transform.GetSiblingIndex();
        Vector2 v = fluidSim.particles[idx].velocity;

        foreach (var contact in collisionInfo.contacts)
        {
            Vector2 n = contact.normal;
            float vDotN = Vector2.Dot(v, n);

            if (vDotN < 0f)
            {
                Vector2 vPerp = n * vDotN;
                Vector2 vTan = v - vPerp;

                Vector2 newPerp = -vPerp * restitution;
                Vector2 newTan = vTan * (1f - friction);

                v = newPerp + newTan;
            }

            isOnGround = true;
            groundNormal = contact.normal;
        }

        fluidSim.particles[idx].velocity = v;
    }

    public int GetIndex()
    {
        return transform.GetSiblingIndex();
    }

    public Particle GetParticle()
    {
        return fluidSim.particles[transform.GetSiblingIndex()];
    }
    public void InitParticle()
    {
        fluidSim.particles[transform.GetSiblingIndex()] = new Particle();
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
