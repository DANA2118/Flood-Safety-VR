using System.Collections;
using UnityEngine;

public class StartupControlsUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup controlsPanel;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 6f;
    [SerializeField] private float fadeDuration = 0.75f;

    private void Start()
    {
        if (controlsPanel == null)
        {
            Debug.LogWarning("Startup Controls UI: CanvasGroup is missing.");
            return;
        }

        controlsPanel.alpha = 1f;
        controlsPanel.gameObject.SetActive(true);

        StartCoroutine(HideControls());
    }

    private IEnumerator HideControls()
    {
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);

            controlsPanel.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        controlsPanel.alpha = 0f;
        controlsPanel.gameObject.SetActive(false);
    }
}