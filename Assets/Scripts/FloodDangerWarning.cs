using UnityEngine;

public class FloodDangerWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodWaterController floodWaterController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CanvasGroup warningCanvas;
    [SerializeField] private AudioSource warningAlarm;

    [Header("Floor Detection")]
    [SerializeField] private float upperFloorHeight = 2.75f;

    [Header("Warning Pulse")]
    [SerializeField] private float minimumAlpha = 0.10f;
    [SerializeField] private float maximumAlpha = 0.75f;
    [SerializeField] private float pulseSpeed = 3.0f;

    private bool warningActive;

    private void Start()
    {
        if (warningCanvas != null)
            warningCanvas.alpha = 0f;

        if (warningAlarm != null)
            warningAlarm.Stop();
    }

    private void Update()
    {
        if (floodWaterController == null ||
            characterController == null ||
            warningCanvas == null)
        {
            return;
        }

        bool waterReachedDangerLevel =
            floodWaterController.HasReachedDangerLevel;

        // Player's feet position.
        float playerFeetY =
            characterController.bounds.min.y;

        bool playerIsOnUpperFloor =
            playerFeetY >= upperFloorHeight;

        bool shouldWarn =
            waterReachedDangerLevel &&
            !playerIsOnUpperFloor;

        if (shouldWarn)
        {
            ShowWarning();
        }
        else
        {
            HideWarning();
        }
    }

    private void ShowWarning()
    {
        float pulse =
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        warningCanvas.alpha =
            Mathf.Lerp(
                minimumAlpha,
                maximumAlpha,
                pulse
            );

        if (!warningActive)
        {
            warningActive = true;

            if (warningAlarm != null)
                warningAlarm.Play();
        }
    }

    private void HideWarning()
    {
        warningCanvas.alpha = 0f;

        if (warningActive)
        {
            warningActive = false;

            if (warningAlarm != null)
                warningAlarm.Stop();
        }
    }
}