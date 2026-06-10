using TMPro;
using UnityEngine;

public class StartOverlayController : MonoBehaviour
{
    [SerializeField] GameObject overlayPanel;
    [SerializeField] GameObject resetButtonRoot;
    [SerializeField] TextMeshProUGUI checkpointButtonLabel;
    [SerializeField] bool pauseGameUntilStart = true;
    [SerializeField] FlyingCar flyingCar;

 

    void Start()
    {
        GameOptions.GameStarted = false;

        if (overlayPanel != null)
            overlayPanel.SetActive(true);

        SetResetButtonVisible(false);

        if (pauseGameUntilStart)
            Time.timeScale = 0f;

        RefreshCheckpointButton();
    }

    public void StartGame()
    {
        GameOptions.GameStarted = true;

        if (pauseGameUntilStart)
            Time.timeScale = 1f;

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (flyingCar != null)
            flyingCar.StartEngineSound(); // 👈 add this

        RefreshResetButton();
    }

    public void ToggleCheckpoints()
    {
        GameOptions.CheckpointsEnabled = !GameOptions.CheckpointsEnabled;
        RefreshCheckpointButton();
        RefreshResetButton();

        if (!GameOptions.CheckpointsEnabled)
            HideCheckpointNotifications();
    }

    void RefreshCheckpointButton()
    {
        if (checkpointButtonLabel == null) return;

        checkpointButtonLabel.text = GameOptions.CheckpointsEnabled
            ? "Checkpoints: On"
            : "Checkpoints: Off";
    }

    void RefreshResetButton()
    {
        SetResetButtonVisible(GameOptions.GameStarted && GameOptions.CheckpointsEnabled);
    }

    void SetResetButtonVisible(bool visible)
    {
        if (resetButtonRoot != null)
            resetButtonRoot.SetActive(visible);
    }

    void HideCheckpointNotifications()
    {
        foreach (var notification in FindObjectsByType<CheckpointNotification>(FindObjectsSortMode.None))
            notification.Hide();
    }

    void OnDestroy()
    {
        GameOptions.GameStarted = true;

        if (pauseGameUntilStart && Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
