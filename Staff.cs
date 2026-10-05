using System.Collections.Generic;
using UnityEngine;

// All staff and spell sources, combined from the original files below.

// ===== BRecognizer.cs =====

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

// ===== BookUI.cs =====

internal class BookUI : MonoBehaviour
{
    private static BookUI instance;

    public static bool IsOpen
    {
        get { return instance != null && instance.open; }
    }

    public static void Open()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("SpellBookUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BookUI>();
        }

        instance.Show();
    }

    private static readonly Color Cream = new Color(0.94f, 0.87f, 0.71f);
    private static readonly Color Stack = new Color(0.82f, 0.74f, 0.57f);
    private static readonly Color Ink = new Color(0.27f, 0.15f, 0.07f);
    private static readonly Color Cover = new Color(0.36f, 0.18f, 0.08f);
    private static readonly Color CoverDark = new Color(0.20f, 0.09f, 0.03f);
    private static readonly Color Gold = new Color(0.80f, 0.62f, 0.22f);
    private static readonly Color CanvasBg = new Color(0.90f, 0.82f, 0.64f);

    private bool open;
    private CursorLockMode prevLock;
    private bool prevVisible;
    private readonly List<List<Vector2>> strokes = new List<List<Vector2>>();
    private List<Vector2> current;
    private string status = "";
    private float toastUntil;
    private Texture2D dot;
    private GUIStyle title;
    private GUIStyle glyph;
    private GUIStyle label;
    private GUIStyle small;
    private GUIStyle button;

    private void Show()
    {
        open = true;
        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;
        strokes.Clear();
        current = null;
        status = "";
    }

    private void Close()
    {
        open = false;
        current = null;
        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
    }

    public static void CloseIfOpen()
    {
        if (instance != null && instance.open)
            instance.Close();
    }

    private void Update()
    {
        KeepCursor();
    }

    private void LateUpdate()
    {
        KeepCursor();
    }

    private void KeepCursor()
    {
        if (!open)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static Texture2D Solid(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.hideFlags = HideFlags.HideAndDontSave;
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void EnsureStyles()
    {
        if (title != null)
            return;

        dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        dot.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                dot.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(16f - d)));
            }
        }
        dot.Apply();

        title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontStyle = FontStyle.BoldAndItalic;
        title.normal.textColor = Ink;

        glyph = new GUIStyle(GUI.skin.label);
        glyph.alignment = TextAnchor.MiddleCenter;
        glyph.fontStyle = FontStyle.Bold;
        glyph.normal.textColor = Cover;

        label = new GUIStyle(GUI.skin.label);
        label.alignment = TextAnchor.MiddleLeft;
        label.fontStyle = FontStyle.Italic;
        label.normal.textColor = Ink;

        small = new GUIStyle(GUI.skin.label);
        small.alignment = TextAnchor.UpperLeft;
        small.fontStyle = FontStyle.Italic;
        small.wordWrap = true;
        small.normal.textColor = Ink;

        button = new GUIStyle(GUI.skin.button);
        button.alignment = TextAnchor.MiddleCenter;
        button.fontStyle = FontStyle.Bold;
        button.normal.background = Solid(Cover);
        button.hover.background = Solid(new Color(0.50f, 0.27f, 0.12f));
        button.active.background = Solid(CoverDark);
        button.normal.textColor = Cream;
        button.hover.textColor = Cream;
        button.active.textColor = Cream;
    }

    private static void Fill(Rect r, Color c)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
    }

    private static void Rounded(Rect r, Color c, float radius)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
    }

    private static Rect Expand(Rect r, float by)
    {
        return new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
    }

    private void OnGUI()
    {
        GUI.depth = -1000;
        EnsureStyles();

        float sc = Mathf.Clamp(Mathf.Min(Screen.width / 1100f, Screen.height / 720f), 0.5f, 1.5f);

        title.fontSize = Mathf.RoundToInt(34f * sc);
        glyph.fontSize = Mathf.RoundToInt(84f * sc);
        label.fontSize = Mathf.RoundToInt(30f * sc);
        small.fontSize = Mathf.RoundToInt(20f * sc);
        button.fontSize = Mathf.RoundToInt(24f * sc);

        if (!open)
        {
            DrawToast(sc);
            return;
        }

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Close();
            e.Use();
            return;
        }

        Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

        float bw = 1000f * sc;
        float bh = 620f * sc;
        Rect book = new Rect((Screen.width - bw) * 0.5f, (Screen.height - bh) * 0.5f, bw, bh);

        Rounded(Expand(book, 32f * sc), CoverDark, 24f * sc);
        Rounded(Expand(book, 24f * sc), Gold, 19f * sc);
        Rounded(Expand(book, 20f * sc), Cover, 17f * sc);

        float gap = 8f * sc;
        Rect left = new Rect(book.x, book.y, bw * 0.5f - gap * 0.5f, bh);
        Rect right = new Rect(book.center.x + gap * 0.5f, book.y, bw * 0.5f - gap * 0.5f, bh);

        Fill(new Rect(left.x + 5f * sc, left.y + 6f * sc, left.width, left.height), Stack);
        Fill(new Rect(right.x - 5f * sc, right.y + 6f * sc, right.width, right.height), Stack);
        Fill(left, Cream);
        Fill(right, Cream);

        for (int i = 0; i < 12; i++)
        {
            Color shade = new Color(0.3f, 0.18f, 0.05f, 0.22f * (1f - i / 12f));
            Fill(new Rect(left.xMax - (i + 1) * 3f * sc, left.y, 3f * sc, left.height), shade);
            Fill(new Rect(right.x + i * 3f * sc, right.y, 3f * sc, right.height), shade);
        }

        Fill(new Rect(book.center.x - 2f * sc, book.y, 4f * sc, bh), new Color(0.25f, 0.13f, 0.05f, 0.7f));

        float pad = 34f * sc;

        GUI.Label(new Rect(left.x + pad, left.y + 18f * sc, left.width - pad * 2f, 48f * sc), "scribe", title);

        float side = Mathf.Min(left.width - pad * 2f, left.height - 190f * sc);
        Rect canvas = new Rect(left.center.x - side * 0.5f, left.y + 76f * sc, side, side);

        Rounded(Expand(canvas, 3f * sc), Gold, 6f * sc);
        Fill(canvas, CanvasBg);

        if (strokes.Count == 0)
        {
            GUIStyle hint = new GUIStyle(label);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.normal.textColor = new Color(Ink.r, Ink.g, Ink.b, 0.35f);
            GUI.Label(canvas, "draw here", hint);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && canvas.Contains(e.mousePosition))
        {
            current = new List<Vector2>();
            strokes.Add(current);
            AddPoint(e.mousePosition, canvas);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && current != null)
        {
            AddPoint(e.mousePosition, canvas);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && current != null)
        {
            current = null;
        }

        if (e.type == EventType.Repaint)
            DrawInk(canvas, sc);

        float btnY = canvas.yMax + 16f * sc;
        Rect castRect = new Rect(canvas.x, btnY, side * 0.58f - 6f * sc, 48f * sc);
        Rect clearRect = new Rect(canvas.x + side * 0.58f + 6f * sc, btnY, side * 0.42f - 6f * sc, 48f * sc);

        if (GUI.Button(castRect, "cast", button))
            DoCast();

        if (GUI.Button(clearRect, "clear", button))
        {
            strokes.Clear();
            current = null;
            status = "";
        }

        GUI.Label(new Rect(right.x + pad, right.y + 18f * sc, right.width - pad * 2f, 48f * sc), "spells", title);

        float rowY = right.y + 96f * sc;
        GUI.Label(new Rect(right.x + pad, rowY, 100f * sc, 100f * sc), "B", glyph);
        GUI.Label(new Rect(right.x + pad + 110f * sc, rowY + 4f * sc, right.width - pad * 2f - 110f * sc, 44f * sc), "fireball", label);
        GUI.Label(new Rect(right.x + pad + 110f * sc, rowY + 50f * sc, right.width - pad * 2f - 110f * sc, 60f * sc), "\"draw it, cast, then click the button", small);

        GUI.Label(new Rect(right.x + pad, right.yMax - 190f * sc, right.width - pad * 2f, 100f * sc), status, small);

        if (GUI.Button(new Rect(right.xMax - pad - 140f * sc, right.yMax - 78f * sc, 140f * sc, 48f * sc), "close", button))
            Close();
    }

    private void AddPoint(Vector2 mouse, Rect canvas)
    {
        Vector2 p = new Vector2(
            Mathf.Clamp01((mouse.x - canvas.x) / canvas.width),
            Mathf.Clamp01((mouse.y - canvas.y) / canvas.height)
        );

        if (current.Count > 0 && (current[current.Count - 1] - p).sqrMagnitude < 0.000016f)
            return;

        current.Add(p);
    }

    private void DrawInk(Rect canvas, float sc)
    {
        float d = 7f * sc;
        float stepPx = d * 0.4f;

        foreach (List<Vector2> stroke in strokes)
        {
            for (int i = 0; i < stroke.Count; i++)
            {
                Vector2 a = new Vector2(canvas.x + stroke[i].x * canvas.width, canvas.y + stroke[i].y * canvas.height);
                Vector2 b = a;

                if (i + 1 < stroke.Count)
                    b = new Vector2(canvas.x + stroke[i + 1].x * canvas.width, canvas.y + stroke[i + 1].y * canvas.height);

                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / stepPx));

                for (int s = 0; s < steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                    GUI.DrawTexture(new Rect(p.x - d * 0.5f, p.y - d * 0.5f, d, d), dot, ScaleMode.StretchToFill, true, 0f, Ink, 0f, 0f);
                }
            }
        }
    }

    private void DoCast()
    {
        int count = 0;
        foreach (List<Vector2> s in strokes)
            count += s.Count;

        if (count < 12)
        {
            status = "draw something first.";
            return;
        }

        string reason;

        if (BRecognizer.IsB(strokes, out reason))
        {
            FireballCaster.Charge();
            toastUntil = Time.unscaledTime + 3f;
            Close();
            return;
        }

        status = "no match (" + reason + "). try a tall b with a straight left side.";
    }

    private void DrawToast(float sc)
    {
        if (Time.unscaledTime > toastUntil)
            return;

        float w = 520f * sc;
        float h = 64f * sc;
        Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - 140f * sc, w, h);

        Rounded(Expand(r, 3f * sc), Gold, 14f * sc);
        Rounded(r, Cover, 12f * sc);

        GUIStyle t = new GUIStyle(label);
        t.alignment = TextAnchor.MiddleCenter;
        t.normal.textColor = Cream;
        GUI.Label(r, "draw it, cast, then click the button", t);
    }
}

// ===== Fireball.cs =====

internal class Fireball : MonoBehaviour
{
    private const float Speed = 18f;
    private const float Radius = 0.16f;
    private const float Life = 6f;
    internal const float BlastRadius = 2.5f;
    private const float BlastForce = 10f;

    private Vector3 dir;
    private float age;
    private float emberTimer;

    public static void Launch(Vector3 position, Vector3 direction)
    {
        Color c = new Color(1f, 0.45f, 0.05f);
        GameObject go = GlowSphere("Fireball", position, 0.32f, c, 3f);

        Light l = go.AddComponent<Light>();
        l.color = c;
        l.range = 7f;
        l.intensity = 3.5f;

        Fireball f = go.AddComponent<Fireball>();
        f.dir = direction.normalized;
    }

    internal static GameObject GlowSphere(string name, Vector3 pos, float size, Color color, float emission)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;

        Renderer r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material.color = color;

        if (r.material.HasProperty("_EmissionColor"))
        {
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", color * emission);
        }

        return go;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age > Life)
        {
            Destroy(gameObject);
            return;
        }

        float step = Speed * Time.deltaTime;

        if (age > 0.1f)
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, Radius, dir, out hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                Explode(hit.point + hit.normal * Radius);
                return;
            }
        }

        transform.position += dir * step;
        transform.localScale = Vector3.one * (0.32f + 0.04f * Mathf.Sin(age * 30f));

        emberTimer += Time.deltaTime;
        if (emberTimer > 0.03f)
        {
            emberTimer = 0f;
            GameObject ember = GlowSphere("Ember", transform.position, 0.16f, new Color(1f, 0.6f, 0.1f), 2f);
            ember.AddComponent<Ember>();
        }
    }

    private void Explode(Vector3 point)
    {
        Collider[] hits = Physics.OverlapSphere(point, BlastRadius);

        foreach (Collider c in hits)
        {
            Rigidbody body = c.attachedRigidbody;

            if (body != null && !body.isKinematic)
                body.AddExplosionForce(BlastForce * body.mass, point, BlastRadius, 0.6f, ForceMode.Impulse);
        }

        Color col = new Color(1f, 0.5f, 0.1f);
        GameObject blast = GlowSphere("FireballBlast", point, 0.5f, col, 4f);

        Light l = blast.AddComponent<Light>();
        l.color = col;
        l.range = 9f;
        l.intensity = 6f;

        blast.AddComponent<Blast>();

        Destroy(gameObject);
    }
}

internal class Ember : MonoBehaviour
{
    private float age;

    private void Update()
    {
        age += Time.deltaTime;

        if (age > 0.45f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0f, age / 0.45f);
        transform.position += Vector3.up * (0.6f * Time.deltaTime);
    }
}

internal class Blast : MonoBehaviour
{
    private float age;
    private Light l;
    private float startIntensity;

    private void Start()
    {
        l = GetComponent<Light>();
        startIntensity = l != null ? l.intensity : 0f;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age > 0.45f)
        {
            Destroy(gameObject);
            return;
        }

        float t = age / 0.45f;
        transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 3.2f, Mathf.Sqrt(t));

        if (l != null)
            l.intensity = startIntensity * (1f - t);
    }
}

// ===== FireballCaster.cs =====

internal class FireballCaster : MonoBehaviour
{
    public static KeyCode FireKey = KeyCode.Period;

    private static FireballCaster instance;

    public static void Charge()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("FireballCaster");
        instance = go.AddComponent<FireballCaster>();
        instance.BuildRing();
    }

    public static void Cancel()
    {
        FireballCaster caster = instance;
        if (caster == null)
            return;

        instance = null;
        caster.enabled = false;

        if (caster.ring != null)
            caster.ring.gameObject.SetActive(false);

        Destroy(caster.gameObject);
    }

    private static readonly Color FireColor = new Color(1f, 0.45f, 0.05f);

    private const int Segments = 28;

    private Transform ring;
    private float spin;
    private float seed;
    private bool hasTarget;
    private Vector3 targetPoint;

    private void BuildRing()
    {
        seed = Random.value * 10f;

        GameObject root = new GameObject("TargetRing");
        ring = root.transform;

        float r = Fireball.BlastRadius;

        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Vector3 scale = new Vector3(Mathf.PI * 2f * r / Segments * 0.75f, 0.02f, 0.06f);
            Piece(PrimitiveType.Cube, pos, scale, new Vector3(0f, -(a * Mathf.Rad2Deg + 90f), 0f));
        }

        Piece(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.3f, 0.01f, 0.3f), Vector3.zero);
        Piece(PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 0.015f, 0.04f), Vector3.zero);
        Piece(PrimitiveType.Cube, Vector3.zero, new Vector3(0.04f, 0.015f, 0.9f), Vector3.zero);

        GameObject lightObj = new GameObject("RingLight");
        lightObj.transform.SetParent(ring, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light l = lightObj.AddComponent<Light>();
        l.color = FireColor;
        l.range = r * 2f;
        l.intensity = 1.5f;

        ring.gameObject.SetActive(false);
    }

    private void Piece(PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(ring, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localEulerAngles = euler;

        Renderer rend = g.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.material.color = FireColor;

        if (rend.material.HasProperty("_EmissionColor"))
        {
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", FireColor * 3f);
        }
    }

    private void OnDestroy()
    {
        if (ring != null)
            Destroy(ring.gameObject);

        if (instance == this)
            instance = null;
    }

    private static bool CastPressed()
    {
        if (Input.GetKeyDown(FireKey)
            || Input.GetKeyDown(KeyCode.Period)
            || Input.GetKeyDown(KeyCode.KeypadPeriod))
            return true;

        string typed = Input.inputString;
        return !string.IsNullOrEmpty(typed) && typed.IndexOf('.') >= 0;
    }

    private void Update()
    {
        Camera head = PlayerView.Head();

        if (head == null)
            return;

        if (BookUI.IsOpen)
        {
            ring.gameObject.SetActive(false);
            return;
        }

        FindTarget(head);

        if (hasTarget)
        {
            RaycastHit hit;
            Vector3 normal = Vector3.up;

            if (Physics.Raycast(targetPoint + Vector3.up * 0.2f, Vector3.down, out hit, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                normal = hit.normal;

            spin += 40f * Time.deltaTime;
            ring.gameObject.SetActive(true);
            ring.position = targetPoint + normal * 0.03f;
            ring.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, spin, 0f);
            ring.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 6f + seed));
        }
        else
        {
            ring.gameObject.SetActive(false);
        }

        if (CastPressed())
            Fire(head);
    }

    private void FindTarget(Camera head)
    {
        hasTarget = false;

        RaycastHit[] hits = Physics.RaycastAll(head.transform.position, head.transform.forward, 80f, ~0, QueryTriggerInteraction.Ignore);
        FloatingStaff held = FloatingStaff.HeldByPlayer;
        float best = float.MaxValue;

        foreach (RaycastHit h in hits)
        {
            if (h.distance < 0.6f || h.distance >= best)
                continue;

            if (h.collider is CharacterController)
                continue;

            if (held != null && h.collider.transform.IsChildOf(held.transform))
                continue;

            best = h.distance;
            targetPoint = h.point;
            hasTarget = true;
        }
    }

    private void Fire(Camera head)
    {
        Vector3 origin;

        if (FloatingStaff.HeldByPlayer != null)
        {
            origin = FloatingStaff.HeldByPlayer.TopPoint();
        }
        else
        {
            Quaternion yaw = Quaternion.Euler(0f, head.transform.eulerAngles.y, 0f);
            origin = head.transform.position + yaw * new Vector3(PlayerHands.FreeSideSign() * 0.25f, -0.3f, 0.3f);
        }

        Vector3 target = hasTarget ? targetPoint : head.transform.position + head.transform.forward * 60f;
        Vector3 dir = target - origin;
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : head.transform.forward;

        Fireball.Launch(origin + dir * 0.25f, dir);
        Destroy(gameObject);
    }
}

// ===== FloatingStaff.cs =====

internal class FloatingStaff : MonoBehaviour
{
    public float restTime = 5f;
    public float riseDuration = 1.5f;
    public float hoverClearance = 0.45f;
    public float maxTopHeight = 2.2f;
    public float spinSpeed = 45f;
    public float bobAmount = 0.05f;
    public Vector3 lengthAxis = Vector3.up;
    public Color glowColor = new Color(0.4f, 0.8f, 1f);
    public float glowIntensity = 2f;
    public float topSign = 1f;
    public static FloatingStaff HeldByPlayer;
    private enum State { Idle, Rising, Floating }

    private State state;
    private Rigidbody rb;
    private Pickup pickup;
    private MiniBook mini;
    private Light glow;
    private readonly List<Material> materials = new List<Material>();
    private float restTimer;
    private float riseT;
    private float spinAngle;
    private float surfaceY;
    private bool wasKinematic;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Quaternion startRot;
    private Quaternion uprightRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pickup = GetComponent<Pickup>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            materials.AddRange(r.materials);

        GameObject lightObj = new GameObject("StaffGlow");
        lightObj.transform.SetParent(transform, false);
        glow = lightObj.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 4f;
        glow.color = glowColor;
        glow.intensity = 0f;

        SetGlow(0f);
    }

    private void OnDestroy()
    {
        ClearHeldState();
        DestroyMini();
    }

    private void ClearHeldState()
    {
        if (HeldByPlayer != this)
            return;

        HeldByPlayer = null;
        FireballCaster.Cancel();
        BookUI.CloseIfOpen();
    }

    private bool IsHeld()
    {
        return pickup != null && (pickup.Interactors.Count > 0 || pickup.IsDocked);
    }

    internal bool IsHeldByLocal()
    {
        if (pickup == null)
            return false;

        foreach (var i in pickup.Interactors)
        {
            if (i != null && i.IsLocal)
                return true;
        }

        return false;
    }

    private void UpdateMini()
    {
        bool local = IsHeldByLocal();
        if (local)
            HeldByPlayer = this;
        else
            ClearHeldState();

        if (local && mini == null && PlayerView.Head() != null)
            mini = MiniBook.Create(this);
        else if (!local && mini != null)
            DestroyMini();
    }
    public Vector3 TopPoint()
    {
        Vector3 axis = transform.TransformDirection(lengthAxis).normalized * topSign;
        MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer>();

        if (meshes.Length == 0)
            return transform.position + axis * 0.8f;

        Bounds b = meshes[0].bounds;
        for (int i = 1; i < meshes.Length; i++)
            b.Encapsulate(meshes[i].bounds);

        Vector3 e = b.extents;
        float ext = Mathf.Abs(axis.x) * e.x + Mathf.Abs(axis.y) * e.y + Mathf.Abs(axis.z) * e.z;
        return b.center + axis * ext;
    }
    private void DestroyMini()
    {
        if (mini != null)
        {
            Destroy(mini.gameObject);
            mini = null;
        }
    }

    private void VerticalExtent(Quaternion delta, out float minY, out float maxY)
    {
        minY = float.MaxValue;
        maxY = float.MinValue;

        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;

            Bounds b = mf.sharedMesh.bounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));

                Vector3 off = delta * (mf.transform.TransformPoint(corner) - transform.position);
                if (off.y < minY) minY = off.y;
                if (off.y > maxY) maxY = off.y;
            }
        }

        if (minY == float.MaxValue)
        {
            minY = -0.8f;
            maxY = 0.8f;
        }
    }

    private float FindSurfaceY(float fallback)
    {
        RaycastHit[] hits = Physics.RaycastAll(startPos + Vector3.up * 0.3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
        bool found = false;
        float best = fallback;

        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(transform))
                continue;

            if (h.collider is CharacterController)
                continue;

            Rigidbody body = h.collider.attachedRigidbody;
            if (body != null && !body.isKinematic)
                continue;

            if (!found || h.point.y > best)
            {
                best = h.point.y;
                found = true;
            }
        }

        return best;
    }

    private void Update()
    {
        bool held = IsHeld();
        UpdateMini();

        if (state == State.Idle)
        {
            bool resting = !held
                && rb != null
                && rb.velocity.sqrMagnitude < 0.01f
                && rb.angularVelocity.sqrMagnitude < 0.01f;

            restTimer = resting ? restTimer + Time.deltaTime : 0f;

            if (restTimer >= restTime)
                BeginRise();

            return;
        }

        if (held)
        {
            Release();
            return;
        }

        if (state == State.Rising)
        {
            riseT = Mathf.Min(1f, riseT + Time.deltaTime / riseDuration);
            float e = Mathf.SmoothStep(0f, 1f, riseT);

            transform.position = Vector3.Lerp(startPos, targetPos, e);
            transform.rotation = Quaternion.Slerp(startRot, uprightRot, e);
            SetGlow(e);

            if (riseT >= 1f)
            {
                state = State.Floating;
            }

            return;
        }

        spinAngle += spinSpeed * Time.deltaTime;

        float bob = Mathf.Sin(Time.time * 1.5f) * bobAmount;
        transform.position = targetPos + Vector3.up * bob;
        transform.rotation = Quaternion.AngleAxis(spinAngle, Vector3.up) * uprightRot;

        SetGlow(1f + 0.25f * Mathf.Sin(Time.time * 3f));
    }

    private void BeginRise()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        uprightRot = Quaternion.FromToRotation(transform.TransformDirection(lengthAxis), Vector3.up) * startRot;

        float curMin, curMax, upMin, upMax;
        VerticalExtent(Quaternion.identity, out curMin, out curMax);
        VerticalExtent(uprightRot * Quaternion.Inverse(startRot), out upMin, out upMax);

        surfaceY = FindSurfaceY(startPos.y + curMin);

        float centerY = surfaceY + hoverClearance - upMin;
        float overshoot = centerY + upMax - (surfaceY + maxTopHeight);

        if (overshoot > 0f)
            centerY -= overshoot;

        centerY = Mathf.Max(centerY, surfaceY + 0.15f - upMin);

        targetPos = new Vector3(startPos.x, centerY, startPos.z);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        riseT = 0f;
        spinAngle = 0f;
        state = State.Rising;
    }

    private void Release()
    {
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.WakeUp();
        }

        state = State.Idle;
        restTimer = 0f;
        SetGlow(0f);
    }

    private void SetGlow(float k)
    {
        glow.intensity = k * glowIntensity;

        foreach (Material m in materials)
        {
            if (m != null && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", glowColor * (k * glowIntensity));
            }
        }
    }
}

// ===== bookmodel.cs =====

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

// ===== minibook.cs =====

internal class MiniBook : MonoBehaviour
{
    public static KeyCode OpenKey = KeyCode.None;

    private FloatingStaff owner;
    private Transform runes;
    private Vector3 smooth;
    private float shown;
    private float seed;
    private bool placed;
    private bool needLeave;

    public static MiniBook Create(FloatingStaff owner)
    {
        GameObject go = new GameObject("MiniBook");
        MiniBook b = go.AddComponent<MiniBook>();
        b.owner = owner;
        b.runes = BookModel.Build(go.transform);
        b.seed = Random.value * 10f;
        go.transform.localScale = Vector3.zero;
        return b;
    }

    private void Update()
    {
        if (owner == null || !owner.IsHeldByLocal())
            return;

        Camera head = PlayerView.Head();

        if (head == null)
            return;

        float side = PlayerHands.FreeSideSign();
        Quaternion yaw = Quaternion.Euler(0f, head.transform.eulerAngles.y, 0f);
        Vector3 target = head.transform.position + yaw * new Vector3(side * 0.34f, -0.3f, 0.3f);
        target += Vector3.up * (Mathf.Sin(Time.time * 1.8f + seed) * 0.015f);

        if (!placed || (target - smooth).sqrMagnitude > 25f)
        {
            smooth = target;
            placed = true;
        }
        else
        {
            smooth = Vector3.Lerp(smooth, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        transform.position = smooth;

        Vector3 toHead = head.transform.position - smooth;
        toHead.y = 0f;

        if (toHead.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(-toHead.normalized) * Quaternion.Euler(-15f, 0f, 0f);

        shown = Mathf.MoveTowards(shown, 1f, Time.deltaTime * 3f);
        transform.localScale = Vector3.one * (0.45f * Mathf.SmoothStep(0f, 1f, shown));

        runes.Rotate(Vector3.up, 70f * Time.deltaTime, Space.Self);

        bool key = OpenKey != KeyCode.None && Input.GetKeyDown(OpenKey);
        bool touching = false;
        bool anyNear = false;

        foreach (Interactor hand in PlayerHands.All())
        {
            if (hand == null || hand.InteractingWith != null)
                continue;

            float d = Vector3.Distance(hand.InteractionPoint.position, smooth);

            if (d < 0.16f) touching = true;
            if (d < 0.3f) anyNear = true;
        }

        if (BookUI.IsOpen)
        {
            needLeave = true;
            return;
        }

        if (key && shown > 0.5f)
        {
            BookUI.Open();
            return;
        }

        if (needLeave)
        {
            if (!anyNear)
                needLeave = false;

            return;
        }

        if (touching && shown > 0.9f)
            BookUI.Open();
    }
}

// ===== playerhands.cs =====

internal static class PlayerView
{
    public static Camera Head()
    {
        if (Camera.main != null)
            return Camera.main;

        Camera[] all = Camera.allCameras;
        return all.Length > 0 ? all[0] : null;
    }
}

internal static class PlayerHands
{
    public static KeyCode GripKey = KeyCode.Mouse0;

    private static Interactor[] hands = new Interactor[0];
    private static float nextRefresh;

    public static Interactor[] All()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + (hands.Length == 0 ? 0.25f : 1f);

            List<Interactor> list = new List<Interactor>();
            foreach (Interactor i in UnityEngine.Object.FindObjectsOfType<Interactor>())
            {
                if (i != null && i.IsLocal && i.InteractionPoint != null)
                    list.Add(i);
            }

            hands = list.ToArray();
        }

        return hands;
    }

    public static bool IsGripping(Interactor i)
    {
        if (i == null)
            return false;

        if (i.HasGripAnimator && i.GripAnimator != null && i.GripAnimator.IsGripping)
            return true;

        return GripKey != KeyCode.None && Input.GetKey(GripKey);
    }

    public static float FreeSideSign()
    {
        foreach (Interactor i in All())
        {
            if (i == null || i.InteractingWith == null || i.Controller == null)
                continue;

            return i.Controller.Hand.HandIndex.ToString() == "Left" ? 1f : -1f;
        }

        return 1f;
    }
}
