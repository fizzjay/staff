
using UnityEngine;

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

        if (Input.GetKeyDown(FireKey))
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