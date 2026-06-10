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
}
