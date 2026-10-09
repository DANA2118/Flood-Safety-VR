using System.Collections;
using UnityEngine;

public class RescueBoatController : MonoBehaviour
{
    [Header("Boat")]
    [SerializeField] private GameObject rescueBoat;
    [SerializeField] private Transform boatStartPoint;
    [SerializeField] private Transform boatStopPoint;

    [Header("Flood Water")]
    [SerializeField] private FloodWaterController floodWaterController;
    [SerializeField] private float boatWaterOffset = 0.10f;

    [Header("Timing")]
    [SerializeField] private float delayAfterSOS = 5f;
    [SerializeField] private float travelDuration = 12f;

    [Header("Audio")]
    [SerializeField] private AudioSource boatEngine;

    [Header("UI")]
    [SerializeField] private GameObject sosStatusUI;
    [SerializeField] private GameObject rescueCompleteUI;

    [Header("SOS")]
    [SerializeField] private bool sosEnabled = true;

    private bool sosTriggered;

    private void Start()
    {
        if (rescueBoat != null)
            rescueBoat.SetActive(false);

        if (sosStatusUI != null)
            sosStatusUI.SetActive(false);

        if (rescueCompleteUI != null)
            rescueCompleteUI.SetActive(false);
    }

    public void EnableSOS()
    {
        sosEnabled = true;
    }

    public void DisableSOS()
    {
        sosEnabled = false;
    }

    public void TriggerSOS()
    {
        if (!sosEnabled)
        {
            Debug.Log("SOS is not available yet.");
            return;
        }

        if (sosTriggered)
            return;

        sosTriggered = true;

        StartCoroutine(RescueSequence());
    }

    private IEnumerator RescueSequence()
    {
        if (sosStatusUI != null)
            sosStatusUI.SetActive(true);

        yield return new WaitForSeconds(delayAfterSOS);

        if (rescueBoat == null ||
            boatStartPoint == null ||
            boatStopPoint == null)
        {
            Debug.LogError("Rescue boat references are missing.");
            yield break;
        }

        Vector3 direction =
            boatStopPoint.position -
            boatStartPoint.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            rescueBoat.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );
        }

        Vector3 startPosition = GetWaterPosition(boatStartPoint);
        rescueBoat.transform.position = startPosition;

        rescueBoat.SetActive(true);

        if (boatEngine != null)
            boatEngine.Play();

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / travelDuration
                );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            // Recalculate water height every frame
            // because the flood may still be rising.
            Vector3 start =
                GetWaterPosition(boatStartPoint);

            Vector3 end =
                GetWaterPosition(boatStopPoint);

            rescueBoat.transform.position =
                Vector3.Lerp(
                    start,
                    end,
                    smoothT
                );

            yield return null;
        }

        rescueBoat.transform.position =
            GetWaterPosition(boatStopPoint);

        if (boatEngine != null)
            boatEngine.Stop();

        if (sosStatusUI != null)
            sosStatusUI.SetActive(false);

        if (rescueCompleteUI != null)
            rescueCompleteUI.SetActive(true);

        Debug.Log("Rescue boat arrived. Training complete.");
    }

    private Vector3 GetWaterPosition(Transform point)
    {
        Vector3 position = point.position;

        if (floodWaterController != null)
        {
            position.y =
                floodWaterController.CurrentWaterLevel
                + boatWaterOffset;
        }

        return position;
    }
}