# Camera Free-Look Swivel — Design

**Date:** 2026-06-09
**Status:** Approved (pending spec review)

## Goal

Let the player swivel the camera around the cat by holding the right mouse
button and moving the mouse, so they can look around before committing to a
jump. Releasing the right button leaves the camera at its new angle. This is a
pure usability/view feature — it never changes the cat's rotation. The held
free-look angle is cleared automatically when the player locks in a jump
direction, so the jump itself uses the normal chase camera.

## Current behavior (context)

- `cameraFollow.LateUpdate()` drives the camera entirely from `cat.forward`:
  it slerps the camera's rotation toward `LookRotation(cat.forward) * tilt`
  and parks the camera `distance` behind and `height` above the cat
  (`Assets/Scripts/cameraFollow.cs:45-64`).
- The cat only changes its real rotation on the first Space press in the
  aiming flow (`Aiming → Locked`), via `transform.Rotate(0f, currentYaw, 0f)`
  (`Assets/Scripts/PlayerMovement.cs:79`). During aiming, only the aim arrow
  oscillates, so `cat.forward` is stable while the player decides.
- Input uses the new Input System (`UnityEngine.InputSystem`); mouse access is
  via `Mouse.current`.

## Design

### Orbit offset layered on the existing follow

Add two persistent fields to `cameraFollow`: `yawOffset` and `pitchOffset`
(degrees). The target rotation each `LateUpdate` becomes:

1. Start from `LookRotation(cat.forward)`.
2. Apply `yawOffset` as a rotation around world up (`Vector3.up`).
3. Apply `pitchOffset` around the resulting right axis.
4. Apply the existing `tiltAngle` (preserved as-is).

Camera position stays derived from `transform.forward * distance + Vector3.up * height`,
exactly as today. Because position follows `transform.forward`, changing the
offsets makes the camera orbit around the cat while keeping it framed.

The existing rotation `Slerp` and position `Lerp` provide the eased,
smoothed feel with no extra work — the offsets only move the target the camera
eases toward.

### Input

- Read `Mouse.current` each `Update` (or `LateUpdate`).
- While the right button (`mouse.rightButton.isPressed`) is held:
  - Read `mouse.delta.ReadValue()`, multiply by `lookSensitivity`.
  - Add the X component to `yawOffset` (free, no clamp — full 360° orbit).
  - Subtract the Y component into `pitchOffset` and clamp to
    `[pitchMin, pitchMax]` (defaults ~ -30°..70°) so the camera cannot flip
    under or over the cat.
- On button release: do nothing — offsets persist, holding the angle.
- Guard the whole input read behind `GameOptions.GameStarted` so the player
  cannot swivel before the game begins.

### Cursor handling while dragging

- On right-button press: `Cursor.lockState = CursorLockMode.Confined` and
  `Cursor.visible = true` — the cursor is penned inside the game window but
  stays visible (per requirement: "lock it, but don't hide it"). `mouse.delta`
  still drives the look in Confined mode.
- On right-button release: restore `Cursor.lockState = CursorLockMode.None`.
- Note: `CursorLockMode.Locked` is deliberately NOT used — it force-hides the
  cursor regardless of `Cursor.visible`.

### Reset on jump direction lock

- `PlayerMovement` exposes a C# event, `JumpDirectionLocked`, invoked at the
  `Aiming → Locked` transition (the first Space press, right where
  `transform.Rotate(...)` happens — `PlayerMovement.cs:79`). This is the
  "commit to a jump" moment confirmed with the user (phase one / first press).
- `cameraFollow` resolves the cat's `PlayerMovement` via its existing `cat`
  reference (`cat.GetComponent<PlayerMovement>()`), subscribes in `OnEnable`,
  and unsubscribes in `OnDisable`.
- On the event, `cameraFollow` sets a "recentering" flag; each `LateUpdate`
  while recentering it eases `yawOffset` and `pitchOffset` toward `0` at
  `recenterSpeed`, clearing the flag once both are within a small epsilon.
  This gives a smooth glide back behind the (now re-facing) cat rather than a
  snap.
- The cat's own rotation is never touched by `cameraFollow`.

### New serialized fields on `cameraFollow`

| Field | Purpose | Suggested default |
|-------|---------|-------------------|
| `lookSensitivity` | Degrees of swivel per unit of mouse delta | `0.2f` |
| `pitchMin` | Lower clamp on pitch offset | `-30f` |
| `pitchMax` | Upper clamp on pitch offset | `70f` |
| `recenterSpeed` | How fast offsets ease back to 0 on commit | `5f` |

(Defaults are tunable in the Inspector; exact feel to be dialed in during play.)

## Edge cases

- **Game not started:** input guarded by `GameOptions.GameStarted`.
- **No PlayerMovement on cat:** free-look still works; it simply never
  auto-resets. Subscription is skipped if the component is absent.
- **Component disabled / scene teardown:** `OnDisable` unsubscribes from
  `JumpDirectionLocked` alongside the existing renderer-restore cleanup, so no
  dangling delegate.
- **Cursor state on disable:** restore `Cursor.lockState = None` in `OnDisable`
  if a drag was in progress, so the cursor is never left confined.

## Out of scope (YAGNI)

- No zoom / distance control on the mouse wheel.
- No inversion toggle or per-axis sensitivity (single `lookSensitivity`).
- No collision/occlusion changes — the existing occlusion logic continues to
  run against the new camera position unchanged.
- No persistence of the look angle across scenes or restarts.

## Files touched

- `Assets/Scripts/cameraFollow.cs` — orbit offset, input, cursor handling,
  recenter logic, event subscription. (Bulk of the work.)
- `Assets/Scripts/PlayerMovement.cs` — add and fire the `JumpDirectionLocked`
  event at the `Aiming → Locked` transition.
