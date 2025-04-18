using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EyeAnimation : MonoBehaviour
{
    [SerializeField]
    GameObject leftEye;
    [SerializeField]
    GameObject rightEye;
    [SerializeField]
    GameObject target;
    Bubble bubble;
    Vector3 direction = Vector3.one;
    [SerializeField]
    float eyeScale;
    [SerializeField]
    Vector2 offset;
    float smoothTime = 0.1f;
    Vector3 velocity;
    Vector3 originalScale;
    Vector3 smoothNormal;
    float smoothSpeed = 2f;
    void Start()
    {
        transform.position = target.transform.position;
        bubble = target.transform.GetComponent<Bubble>();
        smoothNormal = Vector3.up;
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
            yield return StartCoroutine(BlinkOnce());
        }
    }

    IEnumerator BlinkOnce()
    {
        originalScale = transform.localScale;

        for (float t = 0; t <= 1f; t += Time.deltaTime * 20f)
        {
            float y = Mathf.Lerp(originalScale.y, 0f, t);
            transform.localScale = new Vector3(originalScale.x, y, originalScale.z);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        for (float t = 0; t <= 1f; t += Time.deltaTime * 20f)
        {
            float y = Mathf.Lerp(0f, originalScale.y, t);
            transform.localScale = new Vector3(originalScale.x, y, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, target.transform.position, ref velocity, smoothTime);
    }

    void Update()
    {
        if (Vector3.Angle(Vector3.up, bubble.groundHit.normal) <= 60 && bubble.groundHit)
            smoothNormal = Vector3.Lerp(smoothNormal, bubble.onGroundAvgNormal, Time.deltaTime * smoothSpeed).normalized;
        else
            smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, Time.deltaTime * smoothSpeed).normalized;
        transform.up = smoothNormal;

        float scale = (float)bubble.mainCluster.Count / bubble.MinPointCount;
        transform.localScale = direction * (float)Math.Sqrt(scale);
        if (Input.GetKeyDown(KeyCode.A))
            direction = new Vector3(1, 1, 1);
        else if (Input.GetKeyDown(KeyCode.D))
            direction = new Vector3(-1, 1, 1);

        rightEye.transform.localScale = Vector2.one * eyeScale;
        leftEye.transform.localScale = Vector2.one * eyeScale;
        rightEye.transform.localPosition = offset;
        leftEye.transform.localPosition = offset * Vector2.left;
    }
}
