using System.Collections;
using TMPro;
using UnityEngine;

public class CheckpointNotification : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] float holdDuration  = 1.5f;
    [SerializeField] float fadeDuration  = 0.5f;

    Coroutine activeRoutine;

    void Start()
    {
        if (label == null) return;
        label.ForceMeshUpdate();
        SetAlpha(0f);
    }

    public void Show()
    {
        if (!GameOptions.CheckpointsEnabled)
        {
            Hide();
            return;
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        SetAlpha(0f);
    }

    IEnumerator ShowRoutine()
    {
        SetAlpha(1f);

        yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - elapsed / fadeDuration);
            yield return null;
        }

        SetAlpha(0f);
        activeRoutine = null;
    }

    void SetAlpha(float a)
    {
        if (label == null) return;
        var c = label.color;
        c.a = a;
        label.color = c;
    }
}
