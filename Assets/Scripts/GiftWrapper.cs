using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

static public class GiftWrapper
{
    static Stack<Vector2> m_convexList = new Stack<Vector2>();
    static Stack<Particle> m_convexParticles = new Stack<Particle>();
    static List<Vector2> m_innerPoints = new List<Vector2>();

    static public Stack<Particle> Wrap(Particle[] p)
    {
        GetConvexParticles(p);
        return m_convexParticles;
    }
    static public void Wrap(Particle[] p, List<Vector2> convexLines, float threshold, int interpolationCount, List<Vector2> interpolationTest)
    {
        GetConvexLines(p, convexLines);
        GetConcaveLines(threshold, convexLines);
        Interpolate(interpolationCount, convexLines, interpolationTest);
    }
    static void GetConvexLines(Particle[] p, List<Vector2> convexLines) // graham scan algorithm
    {
        m_convexList.Clear();
        m_innerPoints.Clear();
        convexLines.Clear();

        Vector2 lowestPosition = GetLowestPosition(p);
        Array.Sort(p, delegate (Particle a, Particle b) { return Compare(a.localPosition, b.localPosition, lowestPosition); });

        m_convexList.Push(p[0].localPosition);
        m_convexList.Push(p[1].localPosition);
        for (int i = 2; i < p.Length; i++)
        {
            Vector2 next = p[i].localPosition;
            Vector2 p0 = m_convexList.Pop();
            m_innerPoints.Add(p0);
            while (m_convexList.Count > 0 && IsCCW(m_convexList.Peek(), p0, next) <= 0)
            {
                p0 = m_convexList.Pop();
                m_innerPoints.Add(p0);
            }
            m_convexList.Push(p0);
            m_innerPoints.RemoveAt(m_innerPoints.Count - 1);
            m_convexList.Push(next);
        }
        m_convexList.Push(lowestPosition);

        // convert points to lines
        for (int i = 1; i < m_convexList.Count; i++)
        {
            convexLines.Add(m_convexList.ToArray()[i - 1]);
            convexLines.Add(m_convexList.ToArray()[i]);
        }
        convexLines.Add(m_convexList.ToArray()[m_convexList.Count - 1]);
        convexLines.Add(m_convexList.ToArray()[0]);
    }

    static void GetConvexParticles(Particle[] p)
    {
        m_convexParticles.Clear();

        Particle lowestParticle = GetLowestParticle(p);
        Array.Sort(p, delegate (Particle a, Particle b) { return Compare(a.localPosition, b.localPosition, lowestParticle.localPosition); });

        m_convexParticles.Push(p[0]);
        m_convexParticles.Push(p[1]);
        for (int i = 2; i < p.Length; i++)
        {
            Particle next = p[i];
            Particle p0 = m_convexParticles.Pop();
            while (m_convexParticles.Count > 0 && IsCCW(m_convexParticles.Peek().localPosition, p0.localPosition, next.localPosition) <= 0)
            {
                p0 = m_convexParticles.Pop();
            }
            m_convexParticles.Push(p0);
            m_convexParticles.Push(next);
        }
        m_convexParticles.Push(lowestParticle);
    }
    static Vector2 GetLowestPosition(Particle[] p)
    {
        Vector2 lowestPoint = p[0].localPosition;

        for (int i = 1; i < p.Length; i++)
            if (p[i].localPosition.y < lowestPoint.y)
                lowestPoint = p[i].localPosition;

        return lowestPoint;
    }
    public static Particle GetLowestParticle(Particle[] p)
    {
        Particle lowestParticle = p[0];

        for (int i = 1; i < p.Length; i++)
            if (p[i].localPosition.y < lowestParticle.localPosition.y)
                lowestParticle = p[i];

        return lowestParticle;
    }

    static Vector2 GetLowestPosition(Vector2[] p)
    {
        Vector2 lowestPoint = p[0];

        for (int i = 1; i < p.Length; i++)
            if (p[i].y < lowestPoint.y)
                lowestPoint = p[i];

        return lowestPoint;
    }

    static int Compare(Vector2 a, Vector2 b, Vector2 c)
    {
        float angleCA = Vector2.Angle(Vector2.right, a - c);
        float angleCB = Vector2.Angle(Vector2.right, b - c);

        return angleCA < angleCB ? -1 : 1;
    }
    static float IsCCW(Vector2 v0, Vector2 v1, Vector2 v2)
    {
        return Vector3.Cross(v1 - v0, v2 - v1).z;
    }

    static void GetConcaveLines(float threshold, List<Vector2> convexLines)
    {
        int prevSize = 0;
        while (prevSize != m_innerPoints.Count && m_innerPoints.Count > 0)
        {
            prevSize = m_innerPoints.Count;

            for (int i = 0; i < convexLines.Count && m_innerPoints.Count > 0; i += 2)
            {
                Vector2 np = m_innerPoints[0]; // nearest point
                float nd = (np - convexLines[i + 1]).magnitude + (np - convexLines[i]).magnitude; // nearest distance

                // find nearest inner point
                int npIdx = 0;
                for (int j = 1; j < m_innerPoints.Count; j++)
                {
                    float cd = (m_innerPoints[j] - convexLines[i + 1]).magnitude + (m_innerPoints[j] - convexLines[i]).magnitude;
                    if (cd < nd)
                    {
                        np = m_innerPoints[j];
                        nd = cd;
                        npIdx = j;
                    }
                }

                float eh = (convexLines[i + 1] - convexLines[i]).magnitude;
                float dd = Math.Min((np - convexLines[i + 1]).magnitude, (np - convexLines[i]).magnitude);

                if (eh / dd > threshold)
                {
                    // convexLines.Add(convexLines[i]);
                    // convexLines.Add(np);
                    // convexLines.Add(np);
                    // convexLines.Add(convexLines[i + 1]);
                    Vector2 p1 = convexLines[i];
                    Vector2 p2 = convexLines[i + 1];

                    convexLines.RemoveAt(i + 1);
                    convexLines.RemoveAt(i);

                    convexLines.Insert(i, p2);
                    convexLines.Insert(i, np);
                    convexLines.Insert(i, np);
                    convexLines.Insert(i, p1);

                    m_innerPoints.RemoveAt(npIdx);
                }
            }
        }
    }

    static void Interpolate(int interpolationCount, List<Vector2> lines, List<Vector2> interpolationTest)
    {
        // if (interpolationCount < 1)
        //     return;
        interpolationCount = 1;
        interpolationTest.Clear();

        for (int j = 0; j < interpolationCount; j++)
        {
            for (int i = 2; i < lines.Count; i += 2)
            {
                Vector2 l1 = lines[i - 2] + (lines[i - 1] - lines[i - 2]) * 0.5f;
                Vector2 l2 = l1 + (lines[i + 1] - l1) * 0.5f;

                Vector2 mid = lines[i - 2] + (l2 - lines[i - 2]) * 0.5f;
                // Vector2 interpolated = mid + (l1 - mid) * 0.5f;

                // interpolationTest.Add(interpolated);
                lines.RemoveAt(i - 1);
                lines.RemoveAt(i);

                lines.Insert(i - 1, mid);
                lines.Insert(i - 1, mid);
            }

            lines.Add(lines[lines.Count - 1]);
            lines.Add(lines[0]);
        }
    }
}