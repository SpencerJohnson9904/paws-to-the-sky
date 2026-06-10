using UnityEngine;

public static class GameOptions
{
    const string CheckpointsEnabledKey = "PawsToTheSky.CheckpointsEnabled";

    public static bool GameStarted { get; set; } = true;

    public static bool CheckpointsEnabled
    {
        get => PlayerPrefs.GetInt(CheckpointsEnabledKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(CheckpointsEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
