using System;
using Unity.VisualScripting;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    float volume;
    float prevVolume = -0.1f;
    bool isInflating;
    [SerializeField]
    bool isClosed;
    public bool IsClosed { get { return isClosed; } }

    [SerializeField]
    float inflationSpeed;
    [SerializeField]
    float deflationSpeed;

    void Update()
    {
        isInflating = prevVolume < volume;

        if (!isInflating && !isClosed && volume > 0.0f)
            Deflate();

        prevVolume = volume;
    }
    public void Inflate()
    {
        volume += Time.deltaTime * inflationSpeed;

        GetComponent<Point>().Increase(volume);
    }

    public void Close()
    {
        if (volume > 0.0f)
        {
            isClosed = true;
            GetComponent<Rigidbody2D>().simulated = true;
        }
    }

    void Deflate()
    {
        volume -= Time.deltaTime * deflationSpeed;
        volume = Math.Max(0.0f, volume);

        GetComponent<Point>().Reduce(volume);
    }
}
