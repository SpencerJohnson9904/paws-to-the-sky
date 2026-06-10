using UnityEngine;

/// <summary>
/// The golden "You Win!" overlay shown when the player reaches the win zone.
/// Freezes the game while visible and offers a "Play Again?" button that
/// restarts the run in place via CheckpointManager.
///
/// Setup (see the step list — most of this is wiring in the editor):
///   1. Build a full-screen Panel under your Canvas with the golden
///      "You Win!" text and a "Play Again?" Button.
///   2. Add this component to that Panel (or any object on the Canvas).
///   3. Assign Win Panel = the panel GameObject to show/hide.
///   4. On the button's OnClick(), add this component and select PlayAgain().
/// </summary>
public class WinScreen : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The panel GameObject toggled on when the player wins. " +
             "Starts hidden; shown on Show().")]
    [SerializeField] GameObject winPanel;

    void Start()
    {
        // Ensure the overlay is hidden at the start of the run.
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    /// <summary>
    /// Display the win overlay and freeze the game. Called by WinTrigger.
    /// </summary>
    public void Show()
    {
        if (winPanel == null)
        {
            Debug.LogError("[WinScreen] Win Panel is not assigned in the Inspector — " +
                           "nothing to show. Drag the panel GameObject into the Win Panel field.");
            return;
        }

        winPanel.SetActive(true);

        // Freeze gameplay. UI buttons still receive clicks at timeScale 0.
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Hook this to the "Play Again?" button's OnClick(). Unfreezes the game,
    /// hides the overlay, and restarts the run in place.
    /// </summary>
    public void PlayAgain()
    {
        Time.timeScale = 1f;

        if (winPanel != null)
            winPanel.SetActive(false);

        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("[WinScreen] No CheckpointManager found in scene!");
            return;
        }

        CheckpointManager.Instance.RestartGame();
    }
}
