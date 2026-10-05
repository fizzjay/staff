using System.Collections.Generic;
using UnityEngine;

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