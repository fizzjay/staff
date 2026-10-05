using System.Collections.Generic;
using UnityEngine;

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