using UnityEngine;

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