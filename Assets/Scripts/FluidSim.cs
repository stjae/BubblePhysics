using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class Particle
{
    public Vector2 position;
    public Vector2 localPosition;
    public Vector2 prevPosition;
    public Vector2 velocity;
    public float density;
    public float nearDensity;
    public Vector2 force;
    public float radius;
    public List<float?> springRestLengths;
    public bool onGround;
    public Vector3 onGroundNormal;
}
public class FluidSim : MonoBehaviour
{
    public static float deltaTime { get; } = 0.0002f;
    public static Vector2 gravity { get; } = new Vector2(0.0f, -9.8f);

    [SerializeField]
    float restDensity;
    [SerializeField]
    float stiffness;
    [SerializeField]
    float nearStiffness;
    [SerializeField]
    float springStiffness;
    [SerializeField]
    float yieldRatio;
    [SerializeField]
    float plasticity;
    [SerializeField]
    float linearViscosity;
    [SerializeField]
    float quadraticViscosity;
    [SerializeField]
    float interactionRadius;
    [SerializeField]
    float dt;
    List<Particle> particles;
    public int ParticleCount { get { return particles.Count; } }

    void Awake()
    {
        particles = new List<Particle>();
    }


    public void Simulate()
    {
        ApplyGravity();
        ApplyViscosity();
        Parallel.For(0, particles.Count, i =>
        {
            particles[i].prevPosition = particles[i].position;
            particles[i].position += dt * particles[i].velocity;
        });
        AdjustSprings();
        ApplySpringDisplacement();
        DoubleDensityRelaxation();

        float dtInv = 1 / dt;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            particles[i].velocity = (particles[i].position - particles[i].prevPosition) * dtInv;
            particles[i].localPosition = particles[i].position - (Vector2)transform.position;
        }
    }
    void ApplyGravity()
    {
        Parallel.For(0, particles.Count, i =>
        {
            particles[i].velocity += gravity * deltaTime;
            particles[i].position += particles[i].velocity;
        });
    }
    void DoubleDensityRelaxation()
    {
        Parallel.For(0, particles.Count, i =>
        {
            float density = 0.0f;
            float nearDensity = 0.0f;

            // compute density & near-density
            for (int j = 0; j < particles.Count; j++)
            {
                float r = (particles[j].position - particles[i].position).magnitude;
                float h = interactionRadius;
                float q = r / h;

                if (q < 1)
                {
                    density += (1 - q) * (1 - q);
                    nearDensity += (1 - q) * (1 - q) * (1 - q);
                }
            }
            particles[i].density = density;
            particles[i].nearDensity = nearDensity;

            // compute pressure & near-pressure
            float pressure = stiffness * (density - restDensity);
            float nearPressure = nearStiffness * nearDensity;

            Vector2 dx = new Vector2();
            for (int j = 0; j < particles.Count; j++)
            {
                float r = (particles[j].position - particles[i].position).magnitude;
                Vector2 ur = (particles[j].position - particles[i].position).normalized;
                float h = interactionRadius;
                float q = r / h;

                if (q < 1)
                {
                    // apply displacements
                    Vector2 D = dt * dt * (pressure * (1 - q) + nearPressure * (1 - q) * (1 - q)) * ur;
                    particles[j].position += D / 2;
                    dx -= D / 2;
                }
            }
            particles[i].position += dx;
        });
    }

    void ApplySpringDisplacement()
    {
        Parallel.For(0, particles.Count, i =>
        {
            for (int j = 0; j < particles[i].springRestLengths.Count; j++)
            {
                if (particles[i].springRestLengths[j] == null)
                    continue;
                float L = (float)particles[i].springRestLengths[j];
                float h = interactionRadius;
                float r = (particles[j].position - particles[i].position).magnitude;
                Vector2 rUnit = (particles[j].position - particles[i].position).normalized;

                Vector2 D = dt * dt * springStiffness * (1 - L / h) * (L - r) * rUnit;

                particles[i].position -= D / 2;
                particles[j].position += D / 2;
            }
        });
    }
    void AdjustSprings()
    {
        Parallel.For(0, particles.Count, i =>
        {
            for (int j = 0; j < particles[i].springRestLengths.Count; j++)
            {
                float r = (particles[j].position - particles[i].position).magnitude;
                float h = interactionRadius;
                float q = r / h;

                if (q < 1)
                {
                    if (!particles[i].springRestLengths[j].HasValue)
                        particles[i].springRestLengths[j] = h;

                    float restLength = (float)particles[i].springRestLengths[j];
                    float d = yieldRatio * restLength; // tolerable deformation

                    if (r > restLength + d)
                        restLength += dt * plasticity * (r - restLength - d);
                    else if (r < restLength - d)
                        restLength -= dt * plasticity * (restLength - d - r);
                }
            }
        });
        Parallel.For(0, particles.Count, i =>
        {
            for (int j = 0; j < particles[i].springRestLengths.Count; j++)
            {
                if (particles[i].springRestLengths[j] == null)
                    continue;
                float L = (float)particles[i].springRestLengths[j];
                float h = interactionRadius;
                if (L > h)
                    particles[i].springRestLengths[j] = null;
            }
        });
    }

    void ApplyViscosity()
    {
        Parallel.For(0, particles.Count, i =>
        {
            for (int j = 0; j < particles.Count; j++)
            {
                float r = (particles[j].position - particles[i].position).magnitude;
                Vector2 rUnit = (particles[j].position - particles[i].position).normalized;
                float h = interactionRadius;
                float q = r / h;

                if (q < 1)
                {
                    // inward radial velocity
                    float u = Vector2.Dot(particles[i].velocity - particles[j].velocity, rUnit);
                    if (u > 0)
                    {
                        // linear and quadratic impulses
                        Vector2 I = dt * (1 - q) * (linearViscosity * u + quadraticViscosity * u * u) * rUnit;
                        particles[i].velocity -= I / 2;
                        particles[j].velocity += I / 2;
                    }
                }
            }
        });
    }

#nullable enable
    public Particle? GetParticle(int index)
    {
        if (index > particles.Count - 1)
            return null;
        else
            return particles[index];
    }
#nullable disable

    public void AddParticle(Particle p)
    {
        particles.Add(p);
    }

    public void RemoveParticle(int i)
    {
        particles.RemoveAt(i);
    }
}
