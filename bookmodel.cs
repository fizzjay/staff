using UnityEngine;

internal static class BookModel
{
    public static Transform Build(Transform root)
    {
        Color leather = new Color(0.34f, 0.16f, 0.07f);
        Color leatherDark = new Color(0.20f, 0.09f, 0.04f);
        Color page = new Color(0.95f, 0.89f, 0.74f);
        Color pageLine = new Color(0.78f, 0.69f, 0.52f);
        Color gold = new Color(0.9f, 0.68f, 0.22f);
        Color ribbon = new Color(0.65f, 0.08f, 0.10f);
        Color gem = new Color(0.65f, 0.40f, 1f);

        Vector3 coverScale = new Vector3(0.33f, 0.43f, 0.03f);

        Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, 0.046f), coverScale, Vector3.zero, leather, 0f, 0.1f);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -0.046f), coverScale, Vector3.zero, leather, 0f, 0.1f);
        Part(root, PrimitiveType.Cube, new Vector3(0.006f, 0f, 0f), new Vector3(0.30f, 0.40f, 0.064f), Vector3.zero, page, 0f, 0f);

        for (int i = 0; i < 5; i++)
            Part(root, PrimitiveType.Cube, new Vector3(0.157f, 0f, -0.024f + i * 0.012f), new Vector3(0.004f, 0.395f, 0.002f), Vector3.zero, pageLine, 0f, 0f);

        Part(root, PrimitiveType.Cylinder, new Vector3(-0.165f, 0f, 0f), new Vector3(0.125f, 0.215f, 0.125f), Vector3.zero, leatherDark, 0f, 0.1f);

        float[] bands = { -0.14f, 0f, 0.14f };
        foreach (float y in bands)
            Part(root, PrimitiveType.Cylinder, new Vector3(-0.165f, y, 0f), new Vector3(0.133f, 0.008f, 0.133f), Vector3.zero, gold, 0.3f, 0.8f);

        float fz = -0.0625f;
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.175f, fz), new Vector3(0.27f, 0.008f, 0.004f), Vector3.zero, gold, 0.3f, 0.8f);
        Part(root, PrimitiveType.Cube, new Vector3(0f, -0.175f, fz), new Vector3(0.27f, 0.008f, 0.004f), Vector3.zero, gold, 0.3f, 0.8f);
        Part(root, PrimitiveType.Cube, new Vector3(-0.135f, 0f, fz), new Vector3(0.008f, 0.35f, 0.004f), Vector3.zero, gold, 0.3f, 0.8f);
        Part(root, PrimitiveType.Cube, new Vector3(0.135f, 0f, fz), new Vector3(0.008f, 0.35f, 0.004f), Vector3.zero, gold, 0.3f, 0.8f);

        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.01f, fz), new Vector3(0.085f, 0.085f, 0.006f), new Vector3(0f, 0f, 45f), gold, 0.4f, 0.8f);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.01f, fz - 0.0008f), new Vector3(0.055f, 0.055f, 0.006f), new Vector3(0f, 0f, 45f), leatherDark, 0f, 0.1f);
        Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.01f, fz - 0.004f), new Vector3(0.032f, 0.032f, 0.018f), Vector3.zero, gem, 2.5f, 0f);

        float[] ys = { -0.205f, 0.205f };
        float[] zs = { -0.046f, 0.046f };
        foreach (float y in ys)
            foreach (float z in zs)
                Part(root, PrimitiveType.Cube, new Vector3(0.15f, y, z), new Vector3(0.032f, 0.032f, 0.036f), Vector3.zero, gold, 0.3f, 0.8f);

        Part(root, PrimitiveType.Cube, new Vector3(0.168f, 0f, 0f), new Vector3(0.012f, 0.07f, 0.128f), Vector3.zero, gold, 0.3f, 0.8f);
        Part(root, PrimitiveType.Sphere, new Vector3(0.176f, 0f, 0f), new Vector3(0.022f, 0.022f, 0.022f), Vector3.zero, gold, 0.5f, 0.8f);

        Part(root, PrimitiveType.Cube, new Vector3(0.06f, -0.27f, 0f), new Vector3(0.022f, 0.15f, 0.004f), new Vector3(0f, 0f, 6f), ribbon, 0f, 0f);

        GameObject pivot = new GameObject("Runes");
        pivot.transform.SetParent(root, false);

        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            Part(pivot.transform, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.32f, 0.05f * (i - 1), Mathf.Sin(a) * 0.32f), Vector3.one * 0.02f, Vector3.zero, gem, 3f, 0f);
        }

        return pivot.transform;
    }

    private static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Color color, float emission, float metallic)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localEulerAngles = euler;

        Renderer r = g.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Material m = r.material;
        m.color = color;

        if (m.HasProperty("_Metallic"))
            m.SetFloat("_Metallic", metallic);

        if (m.HasProperty("_Glossiness"))
            m.SetFloat("_Glossiness", metallic > 0.5f ? 0.65f : 0.35f);

        if (emission > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
        }
    }
}