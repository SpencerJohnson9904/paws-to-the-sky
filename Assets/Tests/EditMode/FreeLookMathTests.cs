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
    public void ApplyLookDelta_AccumulatesFromNonZeroPitch_StaysClamped()
    {
        // Starting near the top of the range, a further upward look (positive
        // delta.y is subtracted) must not push past pitchMax.
        Vector2 result = FreeLookMath.ApplyLookDelta(
            new Vector2(0f, 60f), new Vector2(0f, -50f),
            sensitivity: 1f, pitchMin: -30f, pitchMax: 70f);

        // pitch = 60 - (-50) = 110 -> clamped to 70.
        Assert.AreEqual(70f, result.y, 0.0001f);
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
    public void OrbitRotation_Yaw90_OrbitsAboutWorldUp_RegardlessOfCatFacing()
    {
        // Cat faces world +X. A 90-deg yaw orbits about world up, so the look
        // direction must swing to world -Z (back) — confirming yaw is applied
        // about world up, not the cat's local axes.
        Quaternion rot = FreeLookMath.OrbitRotation(
            Vector3.right, yawOffset: 90f, pitchOffset: 0f, tiltAngle: 0f);

        Vector3 fwd = rot * Vector3.forward;
        Assert.Less(Vector3.Distance(fwd, Vector3.back), 0.001f);
    }

    [Test]
    public void OrbitRotation_PositivePitch_LooksDownward()
    {
        Quaternion rot = FreeLookMath.OrbitRotation(
            Vector3.forward, yawOffset: 0f, pitchOffset: 30f, tiltAngle: 0f);

        // Positive pitch about local right tilts the look down by exactly 30 deg:
        // the forward vector's Y component is -sin(30 deg).
        Vector3 fwd = rot * Vector3.forward;
        Assert.AreEqual(-Mathf.Sin(30f * Mathf.Deg2Rad), fwd.y, 0.001f);
    }

    [Test]
    public void StepRecenter_HalvesTowardZero_AtHalfFactor()
    {
        // factor = clamp01(0.1 * 5) = 0.5 -> lerp(100, 0, 0.5) = 50.
        float result = FreeLookMath.StepRecenter(100f, recenterSpeed: 5f, deltaTime: 0.1f);
        Assert.AreEqual(50f, result, 0.0001f);
    }

    [Test]
    public void StepRecenter_ReachesZero_WhenFactorClampsToOne()
    {
        // factor = clamp01(1 * 100) = 1 -> lerp(100, 0, 1) = 0.
        float result = FreeLookMath.StepRecenter(100f, recenterSpeed: 100f, deltaTime: 1f);
        Assert.AreEqual(0f, result, 0.0001f);
    }

    [Test]
    public void StepRecenter_MovesTowardZero_FromNegative()
    {
        // factor = clamp01(0.1 * 5) = 0.5 -> lerp(-80, 0, 0.5) = -40.
        float result = FreeLookMath.StepRecenter(-80f, recenterSpeed: 5f, deltaTime: 0.1f);
        Assert.AreEqual(-40f, result, 0.0001f);
    }
}
