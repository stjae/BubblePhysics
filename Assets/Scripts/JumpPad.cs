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
    HashSet<int> contactedParticles;

    void Awake()
    {
        bubble = FindFirstObjectByType<Bubble>();
        fluidSim = FindFirstObjectByType<FluidSim>();
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(MoveAndBounceLoop());
        contactedParticles = new HashSet<int>();
    }

    void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        foreach (ContactPoint2D cp in collisionInfo.contacts)
        {
            if (cp.collider.gameObject.layer == LayerMask.NameToLayer("Point"))
            {
                contactedParticles.Add(cp.collider.gameObject.transform.GetSiblingIndex());
            }
        }
    }

    void OnCollisionExit2D(Collision2D collisionInfo)
    {
        int index = collisionInfo.collider.GetComponent<Point>().GetIndex();
        contactedParticles.Remove(index);
    }

    IEnumerator MoveAndBounceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);

            // up
            yield return MoveTo(transform.position + transform.up * moveAmplitude, moveDuration);

            // down
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
        if (bubble == null)
        {
            Debug.Log("Could not find Bubble object to interact");
            return;
        }

        foreach (List<int> cluster in bubble.clusters)
        {
            foreach (int i in cluster)
            {
                if (contactedParticles.Contains(i))
                {
                    fluidSim.particles[i].velocity = (Vector2)transform.up * jumpPower;
                }
            }
        }
    }
}