using System.Collections;
using UnityEngine;

public class GovernmentFloodAlertSequence : MonoBehaviour
{
    [SerializeField] private AudioSource announcementSource;
    [SerializeField] private AudioSource sirenSource;
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

        // Government announcement first
        announcementSource.Play();

        // Wait until the announcement really finishes
        yield return new WaitWhile(() => announcementSource.isPlaying);

        // Small natural pause
        yield return new WaitForSeconds(delayBeforeSiren);

        // Then start flood siren
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