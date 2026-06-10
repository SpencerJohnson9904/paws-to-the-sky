using NUnit.Framework;
using UnityEngine;

public class FreeLookMathTests
{
    [Test]
    public void ApplyLookDelta_AccumulatesYaw_Unbounded()
    {
        Vector2 result = FreeLookMath.ApplyLookDelta(
            new Vector2(350f, 0f), new Vector2(500f, 0f),
            sensitivity: 0.2f, pitchMin: -30f, pitchMax: 70f);

        // 350 + 500 * 0.2 = 450 — no wrap, no clamp on yaw.
        Assert.AreEqual(450f, result.x, 0.0001f);
    }

    [Test]
    public void ApplyLookDelta_ClampsPitchToMax()
    {
        Vector2 result = FreeLookMath.ApplyLookDelta(
            new Vector2(0f, 0f), new Vector2(0f, -1000f),
            sensitivity: 1f, pitchMin: -30f, pitchMax: 70f);

        // pitch = 0 - (-1000) = 1000 -> clamped to 70.
        Assert.AreEqual(70f, result.y, 0.0001f);
    }

    [Test]
    public void ApplyLookDelta_ClampsPitchToMin()
    {
        Vector2 result = FreeLookMath.ApplyLookDelta(
            new Vector2(0f, 0f), new Vector2(0f, 1000f),
            sensitivity: 1f, pitchMin: -30f, pitchMax: 70f);

        // pitch = 0 - 1000 = -1000 -> clamped to -30.
        Assert.AreEqual(-30f, result.y, 0.0001f);
    }

    [Test]
    public void OrbitRotation_ZeroOffsets_FacesCatForward()
    {
        Quaternion rot = FreeLookMath.OrbitRotation(
            Vector3.forward, yawOffset: 0f, pitchOffset: 0f, tiltAngle: 0f);

        Vector3 fwd = rot * Vector3.forward;
        Assert.Less(Vector3.Distance(fwd, Vector3.forward), 0.001f);
    }

    [Test]
    public void OrbitRotation_Yaw90_LooksAlongRight()
    {
        Quaternion rot = FreeLookMath.OrbitRotation(
            Vector3.forward, yawOffset: 90f, pitchOffset: 0f, tiltAngle: 0f);

        // +90 about world up takes +Z to +X.
        Vector3 fwd = rot * Vector3.forward;
        Assert.Less(Vector3.Distance(fwd, Vector3.right), 0.001f);
    }

    [Test]
    public void OrbitRotation_PositivePitch_LooksDownward()
    {
        Quaternion rot = FreeLookMath.OrbitRotation(
            Vector3.forward, yawOffset: 0f, pitchOffset: 30f, tiltAngle: 0f);

        // Positive pitch about local right tilts the look downward (negative Y).
        Vector3 fwd = rot * Vector3.forward;
        Assert.Less(fwd.y, 0f);
    }
}
