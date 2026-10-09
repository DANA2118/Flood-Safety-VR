using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MainsBreaker : MonoBehaviour
{
    [SerializeField] private Transform lever;
    [SerializeField] private float leverAngle = 35f;
    [SerializeField] private float leverSpeed = 220f;
    [SerializeField] private Light[] mainsLights;
    [SerializeField] private Light[] emergencyLights;

    private Quaternion onRotation;
    private Quaternion offRotation;
    private bool isOn = true;
    private bool reported;
    private XRSimpleInteractable leverInteractable;

    private void Awake()
    {
        onRotation = lever.localRotation;
        offRotation = onRotation * Quaternion.Euler(leverAngle, 0f, 0f);
        leverInteractable = lever.GetComponent<XRSimpleInteractable>();
        ApplyLightState();
    }

    private void OnEnable()
    {
        if (leverInteractable != null)
            leverInteractable.selectEntered.AddListener(OnLeverSelect);
    }

    private void OnDisable()
    {
        if (leverInteractable != null)
            leverInteractable.selectEntered.RemoveListener(OnLeverSelect);
    }

    private void Update()
    {
        Quaternion target = isOn ? onRotation : offRotation;
        lever.localRotation = Quaternion.RotateTowards(lever.localRotation, target, leverSpeed * Time.deltaTime);
    }

    private void OnLeverSelect(SelectEnterEventArgs args) => ToggleMains();

    public void ToggleMains()
    {
        isOn = !isOn;
        ApplyLightState();
        Debug.Log("[MainsBreaker] power " + (isOn ? "on" : "off"));

        if (!isOn && !reported)
        {
            reported = true;
            TaskEvent.Complete("mains_off");
        }
    }

    private void ApplyLightState()
    {
        foreach (Light mainsLight in mainsLights)
            mainsLight.enabled = isOn;

        foreach (Light emergencyLight in emergencyLights)
            emergencyLight.enabled = false;
    }
}
