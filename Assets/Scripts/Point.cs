using UnityEngine;

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
    }

    void OnCollisionStay2D(Collision2D collisionInfo)
    {
        if (fluidSim.GetParticle(transform.GetSiblingIndex()) == null)
            return;

        foreach (ContactPoint2D contact in collisionInfo.contacts)
        {
            fluidSim.GetParticle(transform.GetSiblingIndex()).velocity = new Vector2();
            fluidSim.GetParticle(transform.GetSiblingIndex()).velocity += contact.normal * 0.01f;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGround = true;
            fluidSim.GetParticle(transform.GetSiblingIndex()).onGroundNormal = contact.normal;
        }
    }
}
