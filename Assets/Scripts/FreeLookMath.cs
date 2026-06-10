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

    /// <summary>
    /// Target camera rotation: orbit the cat's forward by yaw (about world up)
    /// and pitch (about the yawed local right), then apply the fixed downward
    /// tilt. Matches the rotation convention of the original follow camera.
    /// </summary>
    public static Quaternion OrbitRotation(
        Vector3 catForward, float yawOffset, float pitchOffset, float tiltAngle)
    {
        Quaternion baseRot = Quaternion.LookRotation(catForward);
        Quaternion yawed = Quaternion.AngleAxis(yawOffset, Vector3.up) * baseRot;
        Quaternion pitched = yawed * Quaternion.AngleAxis(pitchOffset, Vector3.right);
        // Fixed downward tilt, post-multiplied on the same local-right axis as
        // pitchOffset above (so it's additive). Kept as its own step to mirror
        // the original cameraFollow rotation convention.
        return pitched * Quaternion.Euler(tiltAngle, 0f, 0f);
    }

    /// <summary>
    /// Ease an offset toward zero by a frame-rate-independent factor. Mirrors
    /// the cameraFollow lerp idiom (Time.deltaTime * speed), clamped to [0,1].
    /// </summary>
    public static float StepRecenter(float offset, float recenterSpeed, float deltaTime)
    {
        return Mathf.Lerp(offset, 0f, Mathf.Clamp01(deltaTime * recenterSpeed));
    }
}
