using System.Collections;
using UnityEngine;

public class FloodWaterController : MonoBehaviour
{
    [Header("Water")]
    [SerializeField] private Transform waterSurface;

    [Header("Water Levels")]
    [SerializeField] private float startY = -0.15f;
    [SerializeField] private float targetY = 1.45f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float riseDuration = 300f;

    public bool HasReachedDangerLevel { get; private set; }
    public bool IsRising { get; private set; }

    public float CurrentWaterLevel
    {
        get
        {
            if (waterSurface == null)
                return startY;

            return waterSurface.position.y;
        }
    }

    private bool countdownStarted;

    private void Awake()
    {
        if (waterSurface != null)
        {
            waterSurface.gameObject.SetActive(true);

            Renderer renderer = waterSurface.GetComponent<Renderer>();

            if (renderer != null)
                renderer.enabled = true;

            Vector3 position = waterSurface.position;
            position.y = startY;
            waterSurface.position = position;
        }

        HasReachedDangerLevel = false;
        IsRising = false;
    }

    public void BeginFloodCountdown()
    {
        if (countdownStarted)
            return;

        countdownStarted = true;
        StartCoroutine(FloodSequence());
    }

    private IEnumerator FloodSequence()
    {
        // Wait 10 seconds after government warning starts.
        yield return new WaitForSeconds(startDelay);

        IsRising = true;

        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / riseDuration
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = waterSurface.position;

            position.y = Mathf.Lerp(
                startY,
                targetY,
                smoothT
            );

            waterSurface.position = position;

            yield return null;
        }

        // Lock the flood permanently at final height.
        Vector3 finalPosition = waterSurface.position;

        finalPosition.y = targetY;

        waterSurface.position = finalPosition;

        waterSurface.gameObject.SetActive(true);

        Renderer renderer =
            waterSurface.GetComponent<Renderer>();

        if (renderer != null)
            renderer.enabled = true;

        IsRising = false;
        HasReachedDangerLevel = true;

        Debug.Log(
            "Flood reached final level: " + targetY
        );
    }
}