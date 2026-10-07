using UnityEngine;

public class BedroomElectricalController : MonoBehaviour
{
    [Header("Devices")]
    [SerializeField] private CeilingFanController ceilingFan;
    [SerializeField] private Light wallLight;

    [Header("Switch States")]
    [SerializeField] private bool mainPowerOn = true;
    [SerializeField] private bool fanSwitchOn = true;
    [SerializeField] private bool wallLightSwitchOn = true;

    public void ToggleFanSwitch()
    {
        fanSwitchOn = !fanSwitchOn;
        ApplyPowerState();
    }

    public void ToggleWallLightSwitch()
    {
        wallLightSwitchOn = !wallLightSwitchOn;
        ApplyPowerState();
    }

    public void SetMainPower(bool powerOn)
    {
        mainPowerOn = powerOn;
        ApplyPowerState();
    }

    private void Start()
    {
        ApplyPowerState();
    }

    private void ApplyPowerState()
    {
        if (ceilingFan != null)
        {
            ceilingFan.SetPower(mainPowerOn && fanSwitchOn);
        }

        if (wallLight != null)
        {
            wallLight.enabled = mainPowerOn && wallLightSwitchOn;
        }
    }
}