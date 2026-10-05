using UnityEngine;

internal class SpellBook : MonoBehaviour
{
    public float showRadius = 5f;
    public float touchRadius = 0.7f;

    private Light glow;
    private Transform runes;
    private Vector3 basePos;
    private float shown;
    private float seed;
    private bool needLeave;

    public static SpellBook Create(Vector3 position)
    {
        GameObject go = new GameObject("SpellBook");
        go.transform.position = position;
        SpellBook b = go.AddComponent<SpellBook>();
        b.Build();
        return b;
    }

    private void Build()
    {
        basePos = transform.position;
        seed = Random.value * 10f;
        runes = BookModel.Build(transform);

        GameObject lightObj = new GameObject("BookGlow");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0f, -0.3f);
        glow = lightObj.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.75f, 0.55f, 1f);
        glow.range = 3f;
        glow.intensity = 0f;

        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        Camera head = PlayerView.Head();

        if (head == null)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        Vector3 to = head.transform.position - basePos;
        float dy = Mathf.Abs(to.y);
        to.y = 0f;
        float dist = to.magnitude;

        bool near = dist < showRadius && dy < 3f;
        shown = Mathf.MoveTowards(shown, near ? 1f : 0f, Time.deltaTime * 2.5f);
        float e = Mathf.SmoothStep(0f, 1f, shown);

        transform.localScale = Vector3.one * e;
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 1.6f + seed) * 0.05f);

        if (dist > 0.01f)
            transform.rotation = Quaternion.LookRotation(-to.normalized) * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.2f + seed) * 6f);

        runes.Rotate(Vector3.up, 50f * Time.deltaTime, Space.Self);
        glow.intensity = e * (0.9f + 0.3f * Mathf.Sin(Time.time * 3f));

        if (BookUI.IsOpen)
        {
            needLeave = true;
            return;
        }

        if (needLeave)
        {
            if (dist > touchRadius * 1.6f)
                needLeave = false;

            return;
        }

        if (e > 0.9f && dist < touchRadius && dy < 2f)
            BookUI.Open();
    }
}