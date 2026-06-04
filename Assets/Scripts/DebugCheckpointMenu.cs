// Entire file compiles only in the editor and development builds, so the
// teleport cheat menu can never ship in a release build.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug-only on-screen menu for teleporting the player between checkpoints at
/// any time. Press F1 (or the backtick `~` key) to toggle the panel.
///
/// It is a pure consumer: it discovers checkpoints via CheckpointTrigger and
/// routes every teleport through CheckpointManager.JumpToCheckpoint(), so all
/// player-movement / state logic stays owned by the manager.
///
/// Setup:
///   • Add this component to the same GameObject that holds CheckpointManager
///     (e.g. "GameManager"). No other wiring required — checkpoints are found
///     automatically, including procedurally-generated ones.
/// </summary>
public class DebugCheckpointMenu : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("Show/hide the teleport panel. F1 by default; backtick (`) is also accepted.")]
    [SerializeField] Key toggleKey = Key.F1;

    [Tooltip("Open the panel automatically when play starts.")]
    [SerializeField] bool openOnStart = false;

    [Header("Layout")]
    [Tooltip("Width of the panel in pixels.")]
    [SerializeField] float panelWidth = 260f;

    [Tooltip("Max height of the panel before the checkpoint list starts scrolling.")]
    [SerializeField] float panelMaxHeight = 420f;

    // ── Private state ─────────────────────────────────────────────────────────
    bool isOpen;
    Vector2 scroll;
    readonly List<CheckpointTrigger> checkpoints = new List<CheckpointTrigger>();

    void Start()
    {
        isOpen = openOnStart;
        if (isOpen) RefreshCheckpoints();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool pressed = kb[toggleKey].wasPressedThisFrame
                       || kb.backquoteKey.wasPressedThisFrame;

        if (pressed)
        {
            isOpen = !isOpen;
            if (isOpen) RefreshCheckpoints();   // re-scan each time we open
        }
    }

    /// <summary>
    /// Rebuild the checkpoint list from the live scene, sorted low → high by
    /// spawn Y so the panel reads bottom-of-level to top. Re-scanned on every
    /// open so procedurally-generated checkpoints are always picked up.
    /// </summary>
    void RefreshCheckpoints()
    {
        checkpoints.Clear();
        checkpoints.AddRange(FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None));
        checkpoints.Sort((a, b) => a.SpawnPosition.y.CompareTo(b.SpawnPosition.y));
    }

    void OnGUI()
    {
        if (!isOpen) return;

        float margin = 10f;
        var area = new Rect(margin, margin, panelWidth, panelMaxHeight);

        GUILayout.BeginArea(area, GUI.skin.box);

        GUILayout.Label("<b>Debug: Checkpoint Teleport</b>", RichLabel());

        var mgr = CheckpointManager.Instance;
        if (mgr == null)
        {
            GUILayout.Label("No CheckpointManager in scene.");
            GUILayout.EndArea();
            return;
        }

        // Current player position header
        if (mgr.Player != null)
        {
            Vector3 p = mgr.Player.position;
            GUILayout.Label($"Player: ({p.x:F1}, {p.y:F1}, {p.z:F1})");
        }

        if (GUILayout.Button("↻ Rescan checkpoints"))
            RefreshCheckpoints();

        GUILayout.Space(4f);

        if (checkpoints.Count == 0)
        {
            GUILayout.Label("No checkpoints in scene.");
            GUILayout.EndArea();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;   // destroyed (e.g. level regenerated)

            Vector3 pos = cp.SpawnPosition;
            string mark = cp.IsActivated ? "● " : "○ ";
            if (GUILayout.Button($"{mark}{cp.CheckpointName}  (y {pos.y:F1})"))
                mgr.JumpToCheckpoint(pos);
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    static GUIStyle _richLabel;
    static GUIStyle RichLabel()
    {
        if (_richLabel == null)
            _richLabel = new GUIStyle(GUI.skin.label) { richText = true };
        return _richLabel;
    }
}
#endif
