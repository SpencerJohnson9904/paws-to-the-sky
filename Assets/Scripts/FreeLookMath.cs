using UnityEngine;

/// <summary>
/// Pure, frame-independent math for the right-click free-look camera swivel.
/// Deliberately free of MonoBehaviour and Input System dependencies so it can
/// be unit tested in EditMode.
/// </summary>
public static class FreeLookMath
{
    /// <summary>
    /// Accumulate a mouse delta into yaw/pitch offsets (degrees).
    /// Yaw is unbounded (full orbit); pitch is clamped to [pitchMin, pitchMax].
    /// Mouse-down (positive delta.y) lowers the look, so pitch subtracts delta.y.
    /// </summary>
    public static Vector2 ApplyLookDelta(
        Vector2 currentOffsets, Vector2 mouseDelta, float sensitivity,
        float pitchMin, float pitchMax)
    {
        float yaw = currentOffsets.x + mouseDelta.x * sensitivity;
        float pitch = Mathf.Clamp(
            currentOffsets.y - mouseDelta.y * sensitivity, pitchMin, pitchMax);
        return new Vector2(yaw, pitch);
    }
}
