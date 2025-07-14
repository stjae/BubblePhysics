using System;
using System.Collections;
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
    Vector3 velocity;
    Vector3 originalScale;
    Vector3 smoothNormal;
    [SerializeField]
    float positionSmoothTime;
    [SerializeField]
    float normalSmoothSpeed;

    void Start()
    {
        transform.position = target.transform.position;
        bubble = target.transform.GetComponent<Bubble>();
        smoothNormal = Vector3.up;
        StartCoroutine(BlinkRoutine());
    }

    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, target.transform.position, ref velocity, positionSmoothTime);
    }

    void Update()
    {
        AlignEyeAngle();
        AdjustEyeDirection();
        AdjustEyePositionScale();
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

    void AlignEyeAngle() // Make the eye align with the slope when on ground with an inclination of 60 degrees or less
                         // 傾斜が60度以下の地面にいるとき、目の向きをその傾斜に合わせる
    {
        if (Vector3.Angle(Vector3.up, bubble.groundHit.normal) <= 60 && bubble.groundHit)
            smoothNormal = Vector3.Lerp(smoothNormal, bubble.onGroundAvgNormal, Time.deltaTime * normalSmoothSpeed).normalized;
        else
            smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, Time.deltaTime * normalSmoothSpeed).normalized;
        transform.up = smoothNormal;

    }

    void AdjustEyeDirection()
    {
        float scale = (float)bubble.playerControlledIndex.Count / bubble.MinPointCount;
        transform.localScale = direction * (float)Math.Sqrt(scale);
        if (Input.GetKeyDown(KeyCode.A))
            direction = new Vector3(1, 1, 1);
        else if (Input.GetKeyDown(KeyCode.D))
            direction = new Vector3(-1, 1, 1);
    }

    void AdjustEyePositionScale()
    {
        rightEye.transform.localPosition = new Vector2(offset.x, offset.y);
        leftEye.transform.localPosition = new Vector2(-1f * offset.x, offset.y);

        rightEye.transform.localScale = Vector2.one * eyeScale;
        leftEye.transform.localScale = Vector2.one * eyeScale;
    }
}
