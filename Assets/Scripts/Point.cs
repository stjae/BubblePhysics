using UnityEngine;

public class Point : MonoBehaviour
{
    public static float radius;
    FluidSim fluidSim;

    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Point");
        transform.GetComponent<CircleCollider2D>().radius = radius;
        fluidSim = transform.parent.GetComponent<FluidSim>();
    }

    void FixedUpdate()
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        UpdateSprings();
        transform.GetComponent<CircleCollider2D>().radius = radius;
        transform.position = fluidSim.GetParticle(transform.GetSiblingIndex()).position;
    }

    void UpdateSprings()
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        int requiredSize = fluidSim.ParticleCount - transform.GetSiblingIndex() - 1;
        fluidSim.GetParticle(transform.GetSiblingIndex()).springRestLengths.Clear();
        for (int i = 0; i < requiredSize; i++)
            fluidSim.GetParticle(transform.GetSiblingIndex()).springRestLengths.Add(null);
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.GetParticle(transform.GetSiblingIndex()).velocity = Vector2.Reflect(fluidSim.GetParticle(transform.GetSiblingIndex()).velocity, contact.normal) * 0.5f;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGround = true;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGroundNormal = contact.normal;
        }
    }
    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        fluidSim.GetParticle(transform.GetSiblingIndex()).onGround = false;
        fluidSim.GetParticle(transform.GetSiblingIndex()).collisionForce = 0.01f;
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.GetParticle(transform.GetSiblingIndex()).collisionForce += 0.02f;
            fluidSim.GetParticle(transform.GetSiblingIndex()).velocity = new Vector2();
            fluidSim.GetParticle(transform.GetSiblingIndex()).velocity += contact.normal * fluidSim.GetParticle(transform.GetSiblingIndex()).collisionForce;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGround = true;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGroundNormal = contact.normal;
        }
    }

    public Particle GetParticle()
    {
        return fluidSim.GetParticle(transform.GetSiblingIndex());
    }

    public int GetIndex()
    {
        return transform.GetSiblingIndex();
    }
}
