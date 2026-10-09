using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FloodTaskManager : MonoBehaviour
{
    [Header("Packing")]
    [SerializeField] private EmergencyBagPacking bagPacking;

    [Header("Original Kitchen Text")]
    [SerializeField] private GameObject packingInstructions;
    [SerializeField] private GameObject packingProgress;

    [Header("Guidance Panel")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;

    [Header("Panel Locations")]
    [SerializeField] private Transform taskPanel;
    [SerializeField] private Transform kitchenTaskPanelPoint;
    [SerializeField] private Transform safeRoomTaskPanelPoint;
    [SerializeField] private Transform verandaTaskPanelPoint;

    [Header("Safe Room")]
    [SerializeField] private SafeRoomCupboard safeRoomCupboard;

    [Header("New Tasks")]
    [SerializeField] private WallPlug tvPlug;
    [SerializeField] private MainsBreaker mainsBreaker;
    [SerializeField] private XRGrabInteractable bedroomTorch;

    [Header("Player and Veranda")]
    [SerializeField] private Transform playerHead;
    [SerializeField] private BoxCollider verandaZone;

    [Header("Nearby Guidance")]
    [SerializeField, Min(0.5f)] private float guidanceDistance = 2f;
    [SerializeField, Min(0.5f)] private float panelDistance = 1.2f;

    private enum Stage
    {
        None,
        Packing,
        StoreBag,
        Valuables,
        UnplugTV,
        SwitchOffPower,
        CollectTorch,
        GoToVeranda,
        VerandaReached
    }

    private Stage currentStage = Stage.None;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private bool reachedSafeRoom;
    private bool storageCompleted;
    private bool nearbyPanelPlaced;
    private bool initialized;

    private void Start()
    {
        bool valid = true;

        valid &= Check(bagPacking, "Bag Packing");
        valid &= Check(packingInstructions, "Packing Instructions");
        valid &= Check(packingProgress, "Packing Progress");

        valid &= Check(objectiveText, "Objective Text");
        valid &= Check(locationText, "Location Text");
        valid &= Check(instructionText, "Instruction Text");
        valid &= Check(progressText, "Progress Text");
        valid &= Check(hintText, "Hint Text");

        valid &= Check(taskPanel, "Task Panel");
        valid &= Check(kitchenTaskPanelPoint, "Kitchen Task Panel Point");
        valid &= Check(safeRoomTaskPanelPoint, "Safe Room Task Panel Point");
        valid &= Check(verandaTaskPanelPoint, "Veranda Task Panel Point");

        valid &= Check(safeRoomCupboard, "Safe Room Cupboard");
        valid &= Check(tvPlug, "TV Plug");
        valid &= Check(mainsBreaker, "Mains Breaker");
        valid &= Check(bedroomTorch, "Bedroom Torch");
        valid &= Check(playerHead, "Player Head");
        valid &= Check(verandaZone, "Veranda Zone");

        if (!valid)
        {
            enabled = false;
            return;
        }

        startingPosition = taskPanel.position;
        startingRotation = taskPanel.rotation;

        taskPanel.gameObject.SetActive(true);
        hintText.gameObject.SetActive(false);

        initialized = true;
    }

    private bool Check(UnityEngine.Object value, string field)
    {
        if (value != null)
            return true;

        Debug.LogError(
            $"FloodTaskManager: Assign '{field}' in the Inspector.",
            this
        );

        return false;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (safeRoomCupboard.BagStored)
            reachedSafeRoom = true;

        if (safeRoomCupboard.AllStored)
            storageCompleted = true;

        Stage nextStage = DetermineStage();

        if (nextStage != currentStage)
            EnterStage(nextStage);

        UpdateNearbyGuidance();
        RefreshText();
    }

    private Stage DetermineStage()
    {
        if (!storageCompleted)
        {
            if (bagPacking.PackedCount < 4)
                return Stage.Packing;

            if (!safeRoomCupboard.BagStored)
                return Stage.StoreBag;

            return Stage.Valuables;
        }

        if (!tvPlug.IsPulledOut)
            return Stage.UnplugTV;

        if (mainsBreaker.IsOn)
            return Stage.SwitchOffPower;

        if (!IsHoldingTorch())
            return Stage.CollectTorch;

        if (!IsInsideVeranda())
            return Stage.GoToVeranda;

        return Stage.VerandaReached;
    }

    private bool IsHoldingTorch()
    {
        return bedroomTorch.isActiveAndEnabled &&
               bedroomTorch.isSelected;
    }

    private bool IsInsideVeranda()
    {
        if (!verandaZone.enabled ||
            !verandaZone.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 local =
            verandaZone.transform.InverseTransformPoint(
                playerHead.position
            ) - verandaZone.center;

        Vector3 half = verandaZone.size * 0.5f;

        return Mathf.Abs(local.x) <= half.x &&
               Mathf.Abs(local.y) <= half.y &&
               Mathf.Abs(local.z) <= half.z;
    }

    private void EnterStage(Stage nextStage)
    {
        Stage previousStage = currentStage;
        currentStage = nextStage;
        nearbyPanelPlaced = false;

        taskPanel.gameObject.SetActive(true);
        hintText.gameObject.SetActive(false);

        bool packing = currentStage == Stage.Packing;
        packingInstructions.SetActive(packing);
        packingProgress.SetActive(packing);

        switch (currentStage)
        {
            case Stage.Packing:
                taskPanel.SetPositionAndRotation(
                    startingPosition,
                    startingRotation
                );
                break;

            case Stage.StoreBag:
                MovePanel(
                    reachedSafeRoom
                        ? safeRoomTaskPanelPoint
                        : kitchenTaskPanelPoint
                );
                break;

            case Stage.Valuables:
            case Stage.UnplugTV:
                MovePanel(safeRoomTaskPanelPoint);
                break;

            case Stage.SwitchOffPower:
                if (previousStage == Stage.Valuables ||
                    previousStage == Stage.StoreBag)
                {
                    MovePanel(safeRoomTaskPanelPoint);
                }
                else
                {
                    PlacePanelNearPlayer();
                }
                break;

            case Stage.CollectTorch:
            case Stage.GoToVeranda:
                PlacePanelNearPlayer();
                break;

            case Stage.VerandaReached:
                MovePanel(verandaTaskPanelPoint);
                break;
        }
    }

    private void UpdateNearbyGuidance()
    {
        if (nearbyPanelPlaced)
            return;

        Transform target = null;

        switch (currentStage)
        {
            case Stage.UnplugTV:
                target = tvPlug.transform;
                break;

            case Stage.SwitchOffPower:
                target = mainsBreaker.transform;
                break;

            case Stage.CollectTorch:
                target = bedroomTorch.transform;
                break;
        }

        if (target == null)
            return;

        if (Vector3.Distance(
                playerHead.position,
                target.position) <= guidanceDistance)
        {
            PlacePanelNearPlayer();
            nearbyPanelPlaced = true;
        }
    }

    private void MovePanel(Transform point)
    {
        taskPanel.SetPositionAndRotation(
            point.position,
            point.rotation
        );
    }

    private void PlacePanelNearPlayer()
    {
        // Place once, rather than continuously following the head.
        Vector3 forward = Vector3.ProjectOnPlane(
            playerHead.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        taskPanel.SetPositionAndRotation(
            playerHead.position +
            forward * panelDistance -
            Vector3.up * 0.1f,
            Quaternion.LookRotation(forward, Vector3.up)
        );
    }

    public void ShowHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(true);
    }

    public void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void SetText(
        string objective,
        string location,
        string instruction,
        string progress,
        string hint)
    {
        objectiveText.text = objective;
        locationText.text = location;
        instructionText.text = instruction;
        progressText.text = progress;
        hintText.text = hint;
    }

    private void RefreshText()
    {
        progressText.color = Color.white;

        switch (currentStage)
        {
            case Stage.Packing:
                SetText(
                    "CURRENT TASK: Pack your emergency bag",
                    "Location: Kitchen",
                    "Place and release the water bottle, medicine, " +
                    "torch, and document pouch inside the bag.",
                    $"Progress: {bagPacking.PackedCount} of 4 packed",
                    "Release each supply inside the bag. Once all " +
                    "four are packed, grab the bag to secure them."
                );
                break;

            case Stage.StoreBag:
                SetText(
                    "CURRENT TASK: Store your emergency bag",
                    "Location: Upstairs safe room",
                    "Place the packed bag fully inside the " +
                    "bottom-right cupboard compartment and release it.",
                    "Essentials packed: 4/4 | Bag not stored",
                    "Rest the bag on the cupboard floor. Keep it " +
                    "fully inside, release it, and wait a moment."
                );
                break;

            case Stage.Valuables:
                SetText(
                    "CURRENT TASK: Store your valuables",
                    "Location: Upstairs safe room",
                    safeRoomCupboard.GetValuablesChecklist(),
                    $"Valuables stored: " +
                    $"{safeRoomCupboard.ValuablesStoredCount}/4",
                    "Collect both laptops, the phone, and documents " +
                    "box. Release each inside a compartment where it fits."
                );
                break;

            case Stage.UnplugTV:
                SetText(
                    "CURRENT TASK: Unplug the TV",
                    "Location: Living room",
                    "Find the TV's wall plug. Grab it and pull " +
                    "it out of the socket.",
                    "Storage completed | TV plug connected",
                    "Pull the plug itself away from the socket. " +
                    "Switching off the TV alone does not count."
                );
                break;

            case Stage.SwitchOffPower:
                SetText(
                    "CURRENT TASK: Switch off the main power",
                    "Location: Living-room main breaker",
                    "Find the main trip switch and switch it OFF.",
                    "TV unplugged | Main power still ON",
                    "Select the main breaker lever. Use the main " +
                    "trip switch, not an individual room light switch."
                );
                break;

            case Stage.CollectTorch:
                SetText(
                    "CURRENT TASK: Collect the bedroom torch",
                    "Location: Bedroom",
                    "Pick up the bedroom torch and carry it with you.",
                    "Power OFF | Bedroom torch required",
                    "Use the separate bedroom torch. The torch " +
                    "packed inside your emergency bag does not count."
                );
                break;

            case Stage.GoToVeranda:
                SetText(
                    "CURRENT TASK: Go to the veranda",
                    "Location: Upstairs veranda",
                    "Carry the bedroom torch upstairs and go " +
                    "through the veranda door.",
                    "Power OFF | Bedroom torch held",
                    "Keep the bedroom torch in your hand while " +
                    "entering the veranda."
                );
                break;

            case Stage.VerandaReached:
                SetText(
                    "COMPLETED: Veranda reached",
                    "Location: Upstairs veranda",
                    "You have reached the veranda carrying " +
                    "the bedroom torch.",
                    "TV unplugged | Power OFF | Torch held",
                    "Stay on the veranda behind the railing " +
                    "and keep the torch with you."
                );

                progressText.color = new Color(0.3f, 1f, 0.4f);
                break;
        }
    }
}