using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MainsBreaker : MonoBehaviour
{
    [Header("Lever")]
    [SerializeField] private Transform lever;
    [SerializeField] private float leverAngle = 35f;
    [SerializeField] private float leverSpeed = 220f;

    [Header("House Lights")]
    [SerializeField] private Light[] mainsLights;
    [SerializeField] private Light[] emergencyLights;

    [Header("Room Electrical Systems")]
    [SerializeField]
    private BedroomElectricalController[] bedroomSystems;

    [Header("TVs")]
    [SerializeField] private TVScreen[] televisions;

    [Header("Fans Not Controlled By A Room System")]
    [SerializeField] private CeilingFanController[] standaloneFans;

    public bool IsOn => isOn;

    private bool isOn = true;
    private bool reported;

    private Quaternion onRotation;
    private Quaternion offRotation;

    private XRSimpleInteractable leverInteractable;

    private void Awake()
    {
        if (lever == null)
        {
            Debug.LogError(
                "MainsBreaker: Assign BreakerLever_Pivot.",
                this
            );

            enabled = false;
            return;
        }

        onRotation = lever.localRotation;

        offRotation =
            onRotation * Quaternion.Euler(leverAngle, 0f, 0f);

        leverInteractable =
            lever.GetComponent<XRSimpleInteractable>();
    }

    private void Start()
    {
        ApplyPowerState();
    }

    private void OnEnable()
    {
        if (leverInteractable != null)
        {
            leverInteractable.selectEntered.AddListener(
                OnLeverSelected
            );
        }
    }

    private void OnDisable()
    {
        if (leverInteractable != null)
        {
            leverInteractable.selectEntered.RemoveListener(
                OnLeverSelected
            );
        }
    }

    private void Update()
    {
        if (lever == null)
            return;

        lever.localRotation = Quaternion.RotateTowards(
            lever.localRotation,
            isOn ? onRotation : offRotation,
            leverSpeed * Time.deltaTime
        );
    }

    private void OnLeverSelected(SelectEnterEventArgs args)
    {
        ToggleMains();
    }

    public void ToggleMains()
    {
        if (lever == null)
            return;

        isOn = !isOn;
        ApplyPowerState();

        Debug.Log(
            "House mains power: " + (isOn ? "ON" : "OFF"),
            this
        );

        if (!isOn && !reported)
        {
            reported = true;
            TaskEvent.Complete("mains_off");
        }
    }

    private void ApplyPowerState()
    {
        if (mainsLights != null)
        {
            foreach (Light houseLight in mainsLights)
            {
                if (houseLight != null)
                    houseLight.enabled = isOn;
            }
        }

        // Retains the behaviour of your existing project.
        if (emergencyLights != null)
        {
            foreach (Light emergencyLight in emergencyLights)
            {
                if (emergencyLight != null)
                    emergencyLight.enabled = false;
            }
        }

        if (bedroomSystems != null)
        {
            foreach (BedroomElectricalController room in bedroomSystems)
            {
                if (room != null)
                    room.SetMainPower(isOn);
            }
        }

        if (televisions != null)
        {
            foreach (TVScreen television in televisions)
            {
                if (television != null)
                    television.SetMainPower(isOn);
            }
        }

        if (standaloneFans != null)
        {
            foreach (CeilingFanController fan in standaloneFans)
            {
                if (fan != null)
                    fan.SetPower(isOn);
            }
        }
    }
}