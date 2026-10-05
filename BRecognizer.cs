using System.Collections.Generic;
using UnityEngine;

internal static class BRecognizer
{
    private const int Bins = 20;

    public static bool IsB(List<List<Vector2>> strokes, out string reason)
    {
        List<Vector2> pts = Densify(strokes);

        if (pts.Count < 12)
        {
            reason = "too small";
            return false;
        }

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

        foreach (Vector2 p in pts)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float h = maxY - minY;

        if (h < 0.2f || maxX - minX <= 0f)
        {
            reason = "too small";
            return false;
        }

        List<Vector2> u = new List<Vector2>(pts.Count);
        foreach (Vector2 p in pts)
            u.Add(new Vector2((p.x - minX) / h, (p.y - minY) / h));

        float[] left = new float[Bins];
        float[] right = new float[Bins];

        if (!Profile(u, left, right))
        {
            reason = "gap";
            return false;
        }

        float meanV = 0.5f;
        float meanL = 0f;
        for (int i = 0; i < Bins; i++)
            meanL += left[i];
        meanL /= Bins;

        float num = 0f, den = 0f;
        for (int i = 0; i < Bins; i++)
        {
            float v = (i + 0.5f) / Bins;
            num += (v - meanV) * (left[i] - meanL);
            den += (v - meanV) * (v - meanV);
        }

        float slope = num / den;

        if (Mathf.Abs(slope) > 0.45f)
        {
            reason = "tilted";
            return false;
        }

        float minU = float.MaxValue, maxU = float.MinValue;
        for (int i = 0; i < u.Count; i++)
        {
            Vector2 q = u[i];
            q.x -= slope * (q.y - 0.5f);
            u[i] = q;
            if (q.x < minU) minU = q.x;
            if (q.x > maxU) maxU = q.x;
        }

        float w = maxU - minU;

        if (w < 0.3f || w > 1.1f)
        {
            reason = "wrong shape";
            return false;
        }

        for (int i = 0; i < u.Count; i++)
            u[i] = new Vector2((u[i].x - minU) / w, u[i].y);

        if (!Profile(u, left, right))
        {
            reason = "gap";
            return false;
        }

        int hug = 0;
        for (int i = 0; i < Bins; i++)
            if (left[i] < 0.32f) hug++;

        if (hug < Bins - 2)
        {
            reason = "no straight stem";
            return false;
        }

        if (left[Bins / 2 - 1] > 0.32f || left[Bins / 2] > 0.32f)
        {
            reason = "no stem in middle";
            return false;
        }

        float up = 0f, lo = 0f;
        for (int i = 2; i <= 8; i++) up = Mathf.Max(up, right[i]);
        for (int i = 11; i <= 17; i++) lo = Mathf.Max(lo, right[i]);

        if (up < 0.5f || lo < 0.5f)
        {
            reason = "needs two bumps";
            return false;
        }

        float notch = float.MaxValue;
        for (int i = 8; i <= 11; i++) notch = Mathf.Min(notch, right[i]);

        if (notch > Mathf.Min(up, lo) - 0.12f)
        {
            reason = "no middle notch";
            return false;
        }

        if (Mathf.Max(right[Bins - 2], right[Bins - 1]) > lo - 0.1f)
        {
            reason = "bottom flares out";
            return false;
        }

        reason = "ok";
        return true;
    }

    private static bool Profile(List<Vector2> pts, float[] left, float[] right)
    {
        for (int i = 0; i < Bins; i++)
        {
            left[i] = float.MaxValue;
            right[i] = float.MinValue;
        }

        foreach (Vector2 p in pts)
        {
            int b = Mathf.Clamp((int)(p.y * Bins), 0, Bins - 1);
            if (p.x < left[b]) left[b] = p.x;
            if (p.x > right[b]) right[b] = p.x;
        }

        for (int i = 0; i < Bins; i++)
            if (left[i] == float.MaxValue)
                return false;

        return true;
    }

    private static List<Vector2> Densify(List<List<Vector2>> strokes)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

        foreach (List<Vector2> s in strokes)
        {
            foreach (Vector2 p in s)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
        }

        float size = Mathf.Max(maxX - minX, maxY - minY);
        float step = Mathf.Max(size * 0.008f, 0.0005f);
        List<Vector2> result = new List<Vector2>();

        foreach (List<Vector2> s in strokes)
        {
            for (int i = 0; i < s.Count; i++)
            {
                result.Add(s[i]);

                if (i + 1 >= s.Count)
                    continue;

                int k = (int)(Vector2.Distance(s[i], s[i + 1]) / step);
                for (int j = 1; j <= k; j++)
                    result.Add(Vector2.Lerp(s[i], s[i + 1], j / (float)(k + 1)));
            }
        }

        return result;
    }
}