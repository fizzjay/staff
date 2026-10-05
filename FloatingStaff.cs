using System.Collections.Generic;
using UnityEngine;

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
    private SpellBook book;
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
        if (HeldByPlayer == this)
            HeldByPlayer = null;

        DestroyBook();
        DestroyMini();
    }

    private bool IsHeld()
    {
        return pickup != null && (pickup.Interactors.Count > 0 || pickup.IsDocked);
    }

    private bool HeldByLocal()
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
        bool local = HeldByLocal();
        if (local)
            HeldByPlayer = this;
        else if (HeldByPlayer == this)
            HeldByPlayer = null;
        if (local && mini == null && PlayerView.Head() != null)
            mini = MiniBook.Create();
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

    private void SpawnBook()
    {
        if (book != null || PlayerView.Head() == null)
            return;

        Vector3 side = Quaternion.Euler(0f, startRot.eulerAngles.y, 0f) * Vector3.right;
        Vector3 pos = new Vector3(targetPos.x, surfaceY + 1f, targetPos.z) + side * 0.9f;
        book = SpellBook.Create(pos);
    }

    private void DestroyBook()
    {
        if (book != null)
        {
            Destroy(book.gameObject);
            book = null;
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
                SpawnBook();
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

        DestroyBook();
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