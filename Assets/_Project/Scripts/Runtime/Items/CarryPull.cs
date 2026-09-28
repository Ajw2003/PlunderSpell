using UnityEngine;

/// <summary>One holder's pull on a held item: what the holder's own machine works out from its grab
/// point, aim and view, sent to whichever machine controls the body so it can apply it.</summary>
public struct CarryPull
{
    public Vector3 GripLocal;
    public Vector3 Target;
    public Vector3 TargetVelocity;
    public Quaternion WantedRotation;
    public bool IsTowing;
    public Vector3 TowFeet;
    public Vector3 TowVelocity;
    public float TowRope;
    public Vector3 UprightLocalUp;

    // The holder's own strength (N), so it travels with the pull instead of being read from the
    // item: a later per-player upgrade only has to set these, and ApplyPull caps each holder's
    // force by their own numbers rather than the item's (#169, "Add strengths together").
    public float GripStrength;
    public float HaulStrength;
}
