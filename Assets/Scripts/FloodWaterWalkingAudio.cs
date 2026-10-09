using UnityEngine;

public class FloodWaterWalkingAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloodWaterController floodWaterController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private AudioSource waterWalkingAudio;

    [Header("Detection")]
    [SerializeField] private float minimumWaterDepth = 0.05f;
    [SerializeField] private float movementThreshold = 0.15f;

    private Vector3 previousPosition;

    private void Start()
    {
        if (playerRoot != null)
        {
            previousPosition = playerRoot.position;
        }

        if (waterWalkingAudio != null)
        {
            waterWalkingAudio.Stop();
        }
    }

    private void Update()
    {
        if (floodWaterController == null ||
            characterController == null ||
            playerRoot == null ||
            waterWalkingAudio == null)
        {
            return;
        }

        // -----------------------------
        // CHECK IF PLAYER IS IN WATER
        // -----------------------------

        float playerFeetY =
            characterController.bounds.min.y;

        float waterY =
            floodWaterController.CurrentWaterLevel;

        bool feetInsideWater =
            waterY > playerFeetY + minimumWaterDepth;


        // -----------------------------
        // CHECK IF PLAYER IS MOVING
        // -----------------------------

        Vector3 currentPosition =
            playerRoot.position;

        Vector3 movement =
            currentPosition - previousPosition;

        // Ignore vertical movement.
        movement.y = 0f;

        float horizontalSpeed =
            movement.magnitude /
            Mathf.Max(Time.deltaTime, 0.0001f);

        bool playerMoving =
            horizontalSpeed > movementThreshold;

        previousPosition = currentPosition;


        // -----------------------------
        // PLAY / STOP WATER SOUND
        // -----------------------------

        bool shouldPlay =
            feetInsideWater && playerMoving;

        if (shouldPlay)
        {
            if (!waterWalkingAudio.isPlaying)
            {
                waterWalkingAudio.Play();
            }
        }
        else
        {
            if (waterWalkingAudio.isPlaying)
            {
                waterWalkingAudio.Stop();
            }
        }
    }
}