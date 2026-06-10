# Camera Free-Look Swivel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the player hold right mouse + move the mouse to swivel (yaw + pitch) the camera around the cat, holding the angle on release and easing back behind the cat when a jump direction is locked — without ever changing the cat's rotation.

**Architecture:** The swivel is an orbit offset (`yawOffset`, `pitchOffset`) layered on top of `cameraFollow`'s existing `cat.forward`-driven follow. All frame-independent math lives in a new dependency-free static class `FreeLookMath` (unit tested in EditMode). `cameraFollow` reads `Mouse.current`, feeds deltas into `FreeLookMath`, and subscribes to a new `JumpDirectionLocked` event on `PlayerMovement` to trigger the ease-back.

**Tech Stack:** Unity (C#), new Input System (`com.unity.inputsystem` 1.19.0), Unity Test Framework (`com.unity.test-framework` 1.6.0), NUnit.

---

## File Structure

- **Create** `Assets/Scripts/FreeLookMath.cs` — pure static helpers (`ApplyLookDelta`, `OrbitRotation`, `StepRecenter`). No MonoBehaviour, no Input System; only `UnityEngine` for `Vector2`/`Vector3`/`Quaternion`/`Mathf`. This is the only unit-tested unit.
- **Create** `Assets/Tests/EditMode/PawsToTheSky.EditMode.Tests.asmdef` — EditMode test assembly referencing `Assembly-CSharp` so it can see `FreeLookMath`.
- **Create** `Assets/Tests/EditMode/FreeLookMathTests.cs` — NUnit tests for the three helpers.
- **Modify** `Assets/Scripts/PlayerMovement.cs` — add a `JumpDirectionLocked` event, fire it at the `Aiming → Locked` transition.
- **Modify** `Assets/Scripts/cameraFollow.cs` — new serialized fields, input + cursor handling, orbit via `FreeLookMath`, recenter logic, event subscription/cleanup.

A note for the implementer on Unity meta files: when you create a new `.cs`/`.asmdef`, Unity generates a `.meta` file on next focus/import. Add both the source file and its generated `.meta` to git when committing (use `git add Assets/...` after Unity has imported, or `git status` to see the `.meta`). If running headless without launching Unity, commit the source file and the `.meta` will be generated on the next editor open — note it then.

---

## Task 1: EditMode test assembly scaffold

**Files:**
- Create: `Assets/Tests/EditMode/PawsToTheSky.EditMode.Tests.asmdef`
- Create: `Assets/Tests/EditMode/FreeLookMathTests.cs` (temporary smoke test, replaced in later tasks)

- [ ] **Step 1: Create the test assembly definition**

Create `Assets/Tests/EditMode/PawsToTheSky.EditMode.Tests.asmdef`:

```json
{
    "name": "PawsToTheSky.EditMode.Tests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Assembly-CSharp"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Referencing `Assembly-CSharp` by name is what lets the test assembly see `FreeLookMath` (which lives in the default game assembly).

- [ ] **Step 2: Add a smoke test to prove the assembly compiles and runs**

Create `Assets/Tests/EditMode/FreeLookMathTests.cs`:

```csharp
using NUnit.Framework;

public class FreeLookMathTests
{
    [Test]
    public void Scaffold_Compiles()
    {
        Assert.Pass();
    }
}
```

- [ ] **Step 3: Run EditMode tests and verify the assembly is picked up**

In the Unity Editor: open `Window > General > Test Runner`, select the **EditMode** tab, click **Run All**.
(Or, if driving Unity via MCP/CLI: `Unity_RunCommand` to run EditMode tests / capture `Unity_GetConsoleLogs`.)

Expected: `FreeLookMathTests.Scaffold_Compiles` PASSES and the `PawsToTheSky.EditMode.Tests` assembly appears in the runner. If the assembly does not appear, confirm the `.asmdef` imported (a `.meta` was generated) and that `Assembly-CSharp` is in its references.

- [ ] **Step 4: Commit**

```bash
git add Assets/Tests/
git commit -m "test: add EditMode test assembly scaffold"
```

---

## Task 2: `FreeLookMath.ApplyLookDelta`

Accumulate a mouse delta into yaw/pitch offsets. Yaw is unbounded (full 360° orbit); pitch is clamped. Moving the mouse down (positive Unity `delta.y`) should lower the look angle, so pitch subtracts `delta.y`.

**Files:**
- Create: `Assets/Scripts/FreeLookMath.cs`
- Test: `Assets/Tests/EditMode/FreeLookMathTests.cs`

- [ ] **Step 1: Write the failing tests**

Replace the body of `Assets/Tests/EditMode/FreeLookMathTests.cs` with:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run EditMode tests (Test Runner > EditMode > Run All).
Expected: FAIL — `FreeLookMath` does not exist / does not compile (`The name 'FreeLookMath' does not exist`).

- [ ] **Step 3: Write the minimal implementation**

Create `Assets/Scripts/FreeLookMath.cs`:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests.
Expected: all three `ApplyLookDelta_*` tests PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/FreeLookMath.cs Assets/Tests/EditMode/FreeLookMathTests.cs
git commit -m "feat: add FreeLookMath.ApplyLookDelta with tests"
```

---

## Task 3: `FreeLookMath.OrbitRotation`

Build the target camera rotation: orbit `cat.forward` by `yawOffset` around world up and `pitchOffset` around the (yawed) local right, then apply the fixed `tiltAngle`. Tested via the resulting forward vector, which is stable to assert.

**Files:**
- Modify: `Assets/Scripts/FreeLookMath.cs`
- Test: `Assets/Tests/EditMode/FreeLookMathTests.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests inside the `FreeLookMathTests` class in `Assets/Tests/EditMode/FreeLookMathTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run EditMode tests.
Expected: the three `OrbitRotation_*` tests FAIL (`FreeLookMath` has no method `OrbitRotation`); the `ApplyLookDelta_*` tests still pass.

- [ ] **Step 3: Write the minimal implementation**

Add this method to `FreeLookMath` in `Assets/Scripts/FreeLookMath.cs` (inside the class, after `ApplyLookDelta`):

```csharp
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
        return pitched * Quaternion.Euler(tiltAngle, 0f, 0f);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests.
Expected: all `OrbitRotation_*` and `ApplyLookDelta_*` tests PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/FreeLookMath.cs Assets/Tests/EditMode/FreeLookMathTests.cs
git commit -m "feat: add FreeLookMath.OrbitRotation with tests"
```

---

## Task 4: `FreeLookMath.StepRecenter`

Ease a single offset toward zero with a frame-rate-independent factor, matching the existing camera's `Time.deltaTime * speed` lerp idiom.

**Files:**
- Modify: `Assets/Scripts/FreeLookMath.cs`
- Test: `Assets/Tests/EditMode/FreeLookMathTests.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests inside the `FreeLookMathTests` class:

```csharp
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
        float result = FreeLookMath.StepRecenter(-80f, recenterSpeed: 5f, deltaTime: 0.1f);
        Assert.Greater(result, -80f);
        Assert.Less(result, 0f);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run EditMode tests.
Expected: the three `StepRecenter_*` tests FAIL (no method `StepRecenter`); all earlier tests still pass.

- [ ] **Step 3: Write the minimal implementation**

Add this method to `FreeLookMath` in `Assets/Scripts/FreeLookMath.cs`:

```csharp
    /// <summary>
    /// Ease an offset toward zero by a frame-rate-independent factor. Mirrors
    /// the cameraFollow lerp idiom (Time.deltaTime * speed), clamped to [0,1].
    /// </summary>
    public static float StepRecenter(float offset, float recenterSpeed, float deltaTime)
    {
        return Mathf.Lerp(offset, 0f, Mathf.Clamp01(deltaTime * recenterSpeed));
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests.
Expected: all tests in `FreeLookMathTests` PASS (9 total: 3 ApplyLookDelta + 3 OrbitRotation + 3 StepRecenter).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/FreeLookMath.cs Assets/Tests/EditMode/FreeLookMathTests.cs
git commit -m "feat: add FreeLookMath.StepRecenter with tests"
```

---

## Task 5: `PlayerMovement.JumpDirectionLocked` event

Expose an event fired the instant the player locks a jump direction (first Space press, `Aiming → Locked`), so the camera can ease back behind the cat. This is the confirmed "commit" moment.

**Files:**
- Modify: `Assets/Scripts/PlayerMovement.cs` (add field near other state ~`:34-40`; fire inside the `Aiming` case ~`:77-82`)

- [ ] **Step 1: Declare the event**

In `Assets/Scripts/PlayerMovement.cs`, add the event declaration alongside the public members (just after the existing `public bool IsGrounded => grounded;` line, around line 43):

```csharp
    /// <summary>
    /// Raised the moment the player locks in a jump direction (first Space press,
    /// Aiming -> Locked). The free-look camera listens to this to ease back
    /// behind the cat. Does not affect gameplay or the cat's rotation.
    /// </summary>
    public event System.Action JumpDirectionLocked;
```

- [ ] **Step 2: Fire the event at the Aiming → Locked transition**

In the `AimState.Aiming` case, inside the `if (space.wasPressedThisFrame)` block, add the invoke after `state = AimState.Locked;`. The block becomes:

```csharp
            case AimState.Aiming:
                ResetSquish();
                UpdateArrowOscillation();
                if (space.wasPressedThisFrame)
                {
                    transform.Rotate(0f, currentYaw, 0f);
                    if (aimArrow != null) aimArrow.localRotation = Quaternion.identity;
                    state = AimState.Locked;
                    JumpDirectionLocked?.Invoke();
                }
                break;
```

- [ ] **Step 3: Verify compilation**

In the Unity Editor, let scripts recompile and confirm the Console shows no errors.
(Via MCP/CLI: trigger a recompile and check `Unity_GetConsoleLogs` for zero compile errors.)
Expected: clean compile. No behavior change yet — nothing subscribes to the event.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/PlayerMovement.cs
git commit -m "feat: raise JumpDirectionLocked event on jump direction lock"
```

---

## Task 6: Wire free-look into `cameraFollow`

Add serialized tuning fields, mouse input + cursor confinement, orbit rotation via `FreeLookMath`, recenter-on-lock via the event, and cleanup. The MonoBehaviour glue is verified by compile + the play test in Task 7 (input/frame-loop wiring is not unit-testable); the math it calls is already covered by Tasks 2-4.

**Files:**
- Modify: `Assets/Scripts/cameraFollow.cs`

- [ ] **Step 1: Add the Input System using directive**

At the top of `Assets/Scripts/cameraFollow.cs`, add the import below the existing usings:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
```

- [ ] **Step 2: Add serialized fields and free-look state**

In the `cameraFollow` class, add after the existing `public float tiltAngle = 10f;` line:

```csharp
    [Header("Free Look")]
    [Tooltip("Hold right mouse and move the mouse to swivel the camera around the cat. Degrees of swivel per unit of mouse delta.")]
    public float lookSensitivity = 0.2f;
    [Tooltip("Lowest the free-look can pitch (degrees). Stops the camera flipping under the cat.")]
    public float pitchMin = -30f;
    [Tooltip("Highest the free-look can pitch (degrees). Stops the camera flipping over the cat.")]
    public float pitchMax = 70f;
    [Tooltip("How fast the swivel eases back behind the cat after a jump direction is locked.")]
    public float recenterSpeed = 5f;

    // Persistent orbit offset applied on top of the cat-following rotation.
    float yawOffset;
    float pitchOffset;
    // True while we're easing the offsets back to zero after a jump lock.
    bool recentering;
    // True while the right mouse button is held for a swivel.
    bool dragging;
    // Cached so we can unsubscribe; may be null if the cat has no PlayerMovement.
    PlayerMovement playerMovement;
```

- [ ] **Step 3: Subscribe to the lock event on enable**

Add an `OnEnable` method to the class (place it just before the existing `void Start()`):

```csharp
    void OnEnable()
    {
        if (cat == null) return;

        playerMovement = cat.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.JumpDirectionLocked += OnJumpDirectionLocked;
    }

    void OnJumpDirectionLocked()
    {
        // Ease the free-look back behind the cat for the jump itself.
        recentering = true;
    }
```

- [ ] **Step 4: Replace `LateUpdate` to apply the orbit and recenter**

Replace the entire existing `LateUpdate()` method with:

```csharp
    void LateUpdate()
    {
        if (cat == null) return;

        HandleFreeLookInput();

        if (recentering)
        {
            yawOffset = FreeLookMath.StepRecenter(yawOffset, recenterSpeed, Time.deltaTime);
            pitchOffset = FreeLookMath.StepRecenter(pitchOffset, recenterSpeed, Time.deltaTime);
            if (Mathf.Abs(yawOffset) < 0.05f && Mathf.Abs(pitchOffset) < 0.05f)
            {
                yawOffset = 0f;
                pitchOffset = 0f;
                recentering = false;
            }
        }

        Quaternion targetRotation =
            FreeLookMath.OrbitRotation(cat.forward, yawOffset, pitchOffset, tiltAngle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        Vector3 desiredPosition = cat.position
            - transform.forward * distance
            + Vector3.up * height;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );

        UpdateOcclusion();
    }
```

- [ ] **Step 5: Add the input + cursor handling methods**

Add these methods to the class (place them just after `LateUpdate`):

```csharp
    void HandleFreeLookInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Don't allow swivel before the game has started.
        if (!GameOptions.GameStarted)
        {
            if (dragging) EndDrag();
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame)
            BeginDrag();

        if (dragging && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            Vector2 offsets = FreeLookMath.ApplyLookDelta(
                new Vector2(yawOffset, pitchOffset), delta,
                lookSensitivity, pitchMin, pitchMax);
            yawOffset = offsets.x;
            pitchOffset = offsets.y;
            recentering = false; // The player has taken manual control.
        }

        if (mouse.rightButton.wasReleasedThisFrame)
            EndDrag();
    }

    void BeginDrag()
    {
        dragging = true;
        recentering = false;
        // Pen the cursor inside the window but keep it visible (locked, not hidden).
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void EndDrag()
    {
        dragging = false;
        Cursor.lockState = CursorLockMode.None;
    }
```

- [ ] **Step 6: Extend `OnDisable` for cleanup**

Replace the existing `OnDisable()` method with the version below (it keeps the renderer-restore behavior and adds event unsubscribe + cursor release):

```csharp
    void OnDisable()
    {
        // Don't leave the world full of invisible geometry if this component is turned off.
        foreach (var r in hidden)
            if (r != null) r.enabled = true;
        hidden.Clear();

        if (playerMovement != null)
            playerMovement.JumpDirectionLocked -= OnJumpDirectionLocked;

        // Never leave the cursor confined if we're torn down mid-drag.
        if (dragging) EndDrag();
    }
```

- [ ] **Step 7: Verify compilation**

In the Unity Editor, let scripts recompile and confirm the Console shows no errors.
(Via MCP/CLI: recompile and check `Unity_GetConsoleLogs` for zero compile errors.)
Expected: clean compile.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/cameraFollow.cs
git commit -m "feat: right-click free-look camera swivel with ease-back on jump lock"
```

---

## Task 7: In-editor play-test verification

The orbit/clamp/recenter math is unit tested; this task verifies the live input, cursor, and follow integration that unit tests can't reach. Drive the editor via the Unity MCP tools where possible (`Unity_RunCommand` to enter/exit play mode, `Unity_SceneView_Capture2DScene` / `Unity_Camera_Capture` for screenshots, `Unity_GetConsoleLogs` for errors).

- [ ] **Step 1: Enter play mode and start the game**

Enter Play mode and trigger game start (so `GameOptions.GameStarted` is true — use the normal start overlay/flow).
Expected: the cat is visible with the chase camera behind it; no Console errors.

- [ ] **Step 2: Verify yaw swivel**

Hold right mouse, drag left and right.
Expected: camera orbits horizontally around the cat, keeping the cat framed. The cat does **not** rotate. The cursor stays visible but cannot leave the game window.

- [ ] **Step 3: Verify pitch swivel and clamps**

While holding right mouse, drag up and down to the extremes.
Expected: camera tilts up/down but stops at the pitch clamps (no flipping under/over the cat).

- [ ] **Step 4: Verify the angle holds on release**

Release the right mouse button.
Expected: the camera stays at the swiveled angle (does not snap back). The cursor is no longer confined.

- [ ] **Step 5: Verify ease-back on jump lock**

With a swiveled (off-center) camera, press Space once to lock the jump direction.
Expected: the camera smoothly eases back to behind the cat; the cat snaps to face the locked aim direction as before. Completing the charge + release jump works exactly as it did pre-feature.

- [ ] **Step 6: Verify guard before game start**

Stop play mode, re-enter, and before starting the game, hold right mouse and drag.
Expected: no swivel occurs while `GameOptions.GameStarted` is false.

- [ ] **Step 7: Tune feel (optional) and record final values**

If the swivel feels too fast/slow or the pitch range is wrong, adjust `lookSensitivity`, `pitchMin`, `pitchMax`, `recenterSpeed` in the Inspector on the camera. If you change defaults meaningfully, update them in `cameraFollow.cs` so fresh instances match, and note the chosen values in the commit message.

- [ ] **Step 8: Commit any tuning changes**

```bash
git add Assets/Scripts/cameraFollow.cs
git commit -m "tune: free-look swivel sensitivity and pitch range"
```

(If no tuning was needed, skip this commit.)

---

## Self-Review Notes

- **Spec coverage:** orbit offset (T3, T6), yaw+pitch input with pitch clamp (T2, T6), persist-on-release (T6 — release does nothing to offsets), eased feel (existing Slerp/Lerp reused in T6), reset on direction lock (T5 event + T6 recenter), cursor confined-not-hidden (T6 `BeginDrag`/`EndDrag`), `GameOptions.GameStarted` guard (T6), no-PlayerMovement edge case (T6 `OnEnable` null check), disable cleanup (T6 `OnDisable`), serialized fields (T6). All spec sections map to a task.
- **Naming consistency:** `yawOffset`/`pitchOffset`/`recentering`/`dragging`/`playerMovement`, `ApplyLookDelta`/`OrbitRotation`/`StepRecenter`, `JumpDirectionLocked`, `BeginDrag`/`EndDrag`/`OnJumpDirectionLocked` used consistently across tasks.
- **No placeholders:** every code step shows complete code; every test step shows the assertion and the expected run result.
