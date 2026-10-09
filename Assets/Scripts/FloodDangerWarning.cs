using UnityEngine;
using TMPro;

public class FloodDangerWarning : MonoBehaviour
{
    [Header("Flood")]
    [SerializeField] private FloodWaterController floodWaterController;

    [Header("Player")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private CharacterController characterController;

    [Header("Floor Detection")]
    [SerializeField] private float upperFloorHeight = 2.75f;

    [Header("Danger Warning")]
    [SerializeField] private CanvasGroup dangerCanvas;
    [SerializeField] private AudioSource dangerAlarm;
    [SerializeField] private TMP_Text dangerText;

    [SerializeField] private float pulseSpeed = 3f;

    [Header("Death / Respawn")]
    [SerializeField] private float deathTime = 10f;
    [SerializeField] private Transform respawnPoint;

    private float dangerTimer = 0f;
    private bool warningActive = false;
    private bool isRespawning = false;

    private void Start()
    {
        if (dangerCanvas != null)
            dangerCanvas.alpha = 0f;

        if (dangerAlarm != null)
            dangerAlarm.Stop();

        dangerTimer = 0f;
    }

    private void Update()
    {
        if (floodWaterController == null ||
            characterController == null ||
            playerRoot == null)
        {
            return;
        }

        float playerFeetY = characterController.bounds.min.y;

        bool playerIsOnUpperFloor =
            playerFeetY >= upperFloorHeight;

        bool floodIsDangerous =
            floodWaterController.HasReachedDangerLevel;

        bool playerInDanger =
            floodIsDangerous &&
            !playerIsOnUpperFloor;

        if (playerInDanger && !isRespawning)
        {
            ActivateDanger();
            UpdateDangerTimer();
        }
        else
        {
            DeactivateDanger();
            ResetDangerTimer();
        }
    }

    private void ActivateDanger()
    {
        if (!warningActive)
        {
            warningActive = true;

            if (dangerAlarm != null && !dangerAlarm.isPlaying)
            {
                dangerAlarm.Play();
            }
        }

        if (dangerCanvas != null)
        {
            float pulse =
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            dangerCanvas.alpha =
                Mathf.Lerp(0.10f, 0.75f, pulse);
        }
    }

    private void DeactivateDanger()
    {
        if (!warningActive)
            return;

        warningActive = false;

        if (dangerCanvas != null)
            dangerCanvas.alpha = 0f;

        if (dangerAlarm != null && dangerAlarm.isPlaying)
            dangerAlarm.Stop();
    }

    private void UpdateDangerTimer()
    {
        dangerTimer += Time.deltaTime;

        float timeRemaining =
            Mathf.Max(0f, deathTime - dangerTimer);

        if (dangerText != null)
        {
            dangerText.text =
                "DANGER!\n" +
                "MOVE TO THE UPPER FLOOR\n\n" +
                "DROWNING IN " +
                Mathf.CeilToInt(timeRemaining) +
                " SECONDS";
        }

        if (dangerTimer >= deathTime)
        {
            RespawnPlayer();
        }
    }

    private void ResetDangerTimer()
    {
        dangerTimer = 0f;

        if (dangerText != null)
        {
            dangerText.text =
                "DANGER!\n" +
                "MOVE TO THE UPPER FLOOR";
        }
    }

    private void RespawnPlayer()
    {
        if (isRespawning)
            return;

        isRespawning = true;

        Debug.Log("Player drowned. Respawning...");

        if (dangerAlarm != null)
            dangerAlarm.Stop();

        if (dangerCanvas != null)
            dangerCanvas.alpha = 0f;

        if (respawnPoint == null)
        {
            Debug.LogError(
                "FloodDangerWarning: Respawn Point is missing!"
            );

            isRespawning = false;
            return;
        }

        // Temporarily disable CharacterController
        // so Unity allows us to teleport the XR Origin.
        characterController.enabled = false;

        playerRoot.position = respawnPoint.position;
        playerRoot.rotation = respawnPoint.rotation;

        characterController.enabled = true;

        dangerTimer = 0f;
        warningActive = false;
        isRespawning = false;
    }
}