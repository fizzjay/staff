using UnityEngine;

internal class MiniBook : MonoBehaviour
{
    public static KeyCode OpenKey = KeyCode.None;

    private Transform runes;
    private Vector3 smooth;
    private float shown;
    private float seed;
    private bool placed;
    private bool needLeave;

    public static MiniBook Create()
    {
        GameObject go = new GameObject("MiniBook");
        MiniBook b = go.AddComponent<MiniBook>();
        b.runes = BookModel.Build(go.transform);
        b.seed = Random.value * 10f;
        go.transform.localScale = Vector3.zero;
        return b;
    }

    private void Update()
    {
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