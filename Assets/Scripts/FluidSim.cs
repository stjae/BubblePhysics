using System.Threading.Tasks;
using UnityEngine;

public class Particle
{
    public Vector2 position;
    public Vector2 localPosition;
    public Vector2 prevPosition;
    public Vector2 velocity;
    public float density;
    public float nearDensity;
    public bool isActive;
}
public class FluidSim : MonoBehaviour
{
    Bubble bubble;
    public static float deltaTime { get; } = 0.0002f;
    public static Vector2 gravity { get; } = new Vector2(0.0f, -9.8f);

    [SerializeField]
    float restDensity;
    [SerializeField]
    float stiffness;
    [SerializeField]
    float nearStiffness;
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
    public Particle[] particles;

    void Awake()
    {
        bubble = GetComponent<Bubble>();
        particles = new Particle[bubble.MaxPointCount];
        for (int i = 0; i < bubble.MaxPointCount; i++)
        {
            particles[i] = new Particle();
            particles[i].position = transform.position;
        }
    }

    public void Simulate()
    {
        ApplyGravity();
        ApplyViscosity();
        Parallel.For(0, particles.Length, i =>
        {
            if (particles[i].isActive)
            {
                particles[i].prevPosition = particles[i].position;
                particles[i].position += dt * particles[i].velocity;
            }
        });
        DoubleDensityRelaxation();

        float dtInv = 1 / dt;
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i].isActive)
            {
                particles[i].velocity = (particles[i].position - particles[i].prevPosition) * dtInv;
                particles[i].localPosition = particles[i].position - (Vector2)transform.position;
            }
        }
    }
    void ApplyGravity()
    {
        Parallel.For(0, particles.Length, i =>
        {
            if (particles[i].isActive)
            {
                particles[i].velocity += gravity * deltaTime;
                particles[i].position += particles[i].velocity;
            }
        });
    }
    void DoubleDensityRelaxation()
    {
        Parallel.For(0, particles.Length, i =>
        {
            if (particles[i].isActive)
            {
                float density = 0.0f;
                float nearDensity = 0.0f;

                // compute density & near-density
                for (int j = 0; j < particles.Length; j++)
                {
                    if (!particles[j].isActive || i == j)
                        continue;

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
                for (int j = 0; j < particles.Length; j++)
                {
                    if (!particles[j].isActive || i == j)
                        continue;

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
            }
        });
    }

    void ApplyViscosity()
    {
        Parallel.For(0, particles.Length, i =>
        {
            if (particles[i].isActive)
            {
                for (int j = 0; j < particles.Length; j++)
                {
                    if (!particles[j].isActive || i == j)
                        continue;

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
            }
        });
    }
}
