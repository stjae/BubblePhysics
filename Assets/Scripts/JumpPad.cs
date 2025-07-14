using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    FluidSim fluidSim;
    Bubble bubble;
    [SerializeField]
    float cooldown;
    [SerializeField]
    float jumpPower;
    [SerializeField]
    float moveAmplitude = 2f;
    [SerializeField]
    float moveDuration = 0.1f;

    Rigidbody2D rb;
    // HashSet<int> contactedClusters;
    HashSet<int> contactedParticles;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(MoveAndBounceLoop());
        contactedParticles = new HashSet<int>();
    }

    void Update()
    {
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        if (collisionInfo.gameObject.name == "Bubble")
        {
            bubble = collisionInfo.gameObject.GetComponent<Bubble>();
            fluidSim = collisionInfo.gameObject.GetComponent<FluidSim>();
        }

        foreach (ContactPoint2D cp in collisionInfo.contacts)
        {
            if (cp.collider.gameObject.layer == LayerMask.NameToLayer("Point"))
            {
                contactedParticles.Add(cp.collider.GetComponent<Point>().GetIndex());
            }
        }
    }

    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        int index = collisionInfo.collider.GetComponent<Point>().GetIndex();
        if (contactedParticles.Contains(index))
        {
            contactedParticles.Remove(index);
        }
    }

    IEnumerator MoveAndBounceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);

            // 아래→위
            yield return MoveTo(transform.position + transform.up * moveAmplitude, moveDuration);

            // 위→아래
            yield return MoveTo(transform.position - transform.up * moveAmplitude, moveDuration);
        }
    }

    IEnumerator MoveTo(Vector3 targetPos, float duration)
    {
        Vector3 origin = rb.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector2 nextPos = Vector3.Lerp(origin, targetPos, elapsed / duration);
            rb.MovePosition(nextPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
            Bounce();
        }
        rb.MovePosition(targetPos);
    }

    void Bounce()
    {
        foreach (List<int> cluster in bubble.clusters)
        {
            foreach (int particleIndex in cluster)
            {
                if (contactedParticles.Contains(particleIndex))
                {
                    fluidSim.GetParticle(particleIndex).velocity = (Vector2)transform.up * jumpPower;
                }
            }
        }
    }
}