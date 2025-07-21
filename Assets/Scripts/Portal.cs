using UnityEditor;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Transform target;
    Transform hole;
    SpriteRenderer holeSprite;
    Bubble bubble;
    [SerializeField]
    float power;

    void Start()
    {
        bubble = target.GetComponent<Bubble>();
        hole = transform.GetChild(0);
        holeSprite = hole.GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        foreach (Point p in bubble.points)
        {
            if (!p.gameObject.activeSelf) continue;

            float dist = (transform.position - p.transform.position).magnitude;
            if (dist < transform.localScale.x * 0.5f)
            {
                p.GetParticle().velocity += (Vector2)(transform.position - p.transform.position) * power;
                if ((transform.position - p.transform.position).magnitude <= holeSprite.bounds.size.x * 0.5f)
                {
                    p.lifeTime = 0;
                }
            }
        }

    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, holeSprite.bounds.size.x * 0.5f);
    }
}
