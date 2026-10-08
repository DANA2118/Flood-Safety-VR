using UnityEngine;

[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 2f;
    [SerializeField] private float flickerAmount = 0.4f;
    [SerializeField] private float flickerSpeed = 8f;

    private Light torchLight;
    private float noiseOffset;

    private void Awake()
    {
        torchLight = GetComponent<Light>();
        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerSpeed);
        torchLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount;
    }
}
