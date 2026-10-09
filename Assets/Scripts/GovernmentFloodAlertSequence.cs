using System.Collections;
using UnityEngine;

public class GovernmentFloodAlertSequence : MonoBehaviour
{
    [SerializeField] private AudioSource announcementSource;
    [SerializeField] private AudioSource sirenSource;

    [Header("Flood System")]
    [SerializeField] private FloodWaterController floodWaterController;

    [SerializeField] private float delayBeforeSiren = 0.3f;

    private IEnumerator Start()
    {
        if (announcementSource == null || sirenSource == null)
        {
            Debug.LogError("Flood alert audio sources are not assigned.");
            yield break;
        }

        announcementSource.Stop();
        sirenSource.Stop();

        // Start government flood announcement.
        announcementSource.Play();

        // Start the flood countdown at EXACTLY the same time.
        if (floodWaterController != null)
        {
            floodWaterController.BeginFloodCountdown();
        }

        // Wait until announcement finishes.
        yield return new WaitWhile(() => announcementSource.isPlaying);

        yield return new WaitForSeconds(delayBeforeSiren);

        // Start warning siren.
        sirenSource.Play();
    }

    public void StopSiren()
    {
        if (sirenSource != null)
        {
            sirenSource.Stop();
        }
    }
}