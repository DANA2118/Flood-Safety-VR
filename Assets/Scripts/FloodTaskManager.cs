using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

    [Header("Safe Room")]
    [SerializeField] private XRSocketInteractor emergencyBagSocket;

    private enum Stage
    {
        None,
        Packing,
        StoreBag,
        Valuables
    }

    private Stage currentStage = Stage.None;
    private int lastCount = -1;

    private XRGrabInteractable bagGrab;
    private Vector3 startingPanelPosition;
    private Quaternion startingPanelRotation;

    private void Start()
    {
        if (bagPacking == null ||
            packingInstructions == null ||
            packingProgress == null ||
            objectiveText == null ||
            locationText == null ||
            instructionText == null ||
            progressText == null ||
            hintText == null ||
            taskPanel == null ||
            kitchenTaskPanelPoint == null ||
            safeRoomTaskPanelPoint == null ||
            emergencyBagSocket == null)
        {
            Debug.LogError(
                "FloodTaskManager: Assign all Inspector fields.",
                this
            );

            enabled = false;
            return;
        }

        bagGrab = bagPacking.GetComponent<XRGrabInteractable>();

        if (bagGrab == null)
        {
            Debug.LogError(
                "FloodTaskManager: The bag needs XR Grab Interactable.",
                this
            );

            enabled = false;
            return;
        }

        startingPanelPosition = taskPanel.position;
        startingPanelRotation = taskPanel.rotation;

        hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        int count = bagPacking.PackedCount;

        Stage nextStage;

        if (count < 4)
            nextStage = Stage.Packing;
        else if (IsBagStored())
            nextStage = Stage.Valuables;
        else
            nextStage = Stage.StoreBag;

        bool stageChanged = nextStage != currentStage;

        if (!stageChanged && count == lastCount)
            return;

        Stage previousStage = currentStage;
        currentStage = nextStage;
        lastCount = count;

        packingInstructions.SetActive(
            currentStage == Stage.Packing
        );

        packingProgress.SetActive(
            currentStage == Stage.Packing
        );

        if (stageChanged)
        {
            hintText.gameObject.SetActive(false);

            if (currentStage == Stage.Packing)
            {
                taskPanel.SetPositionAndRotation(
                    startingPanelPosition,
                    startingPanelRotation
                );
            }
            else if (currentStage == Stage.Valuables)
            {
                MovePanel(safeRoomTaskPanelPoint);
            }
            else
            {
                // If the player removes the stored bag,
                // keep the reminder upstairs.
                MovePanel(
                    previousStage == Stage.Valuables
                        ? safeRoomTaskPanelPoint
                        : kitchenTaskPanelPoint
                );
            }
        }

        RefreshText(count);
    }

    private bool IsBagStored()
    {
        if (!bagPacking.IsSealed ||
            bagPacking.PackedCount != 4 ||
            !emergencyBagSocket.isActiveAndEnabled ||
            !emergencyBagSocket.hasSelection)
        {
            return false;
        }

        // Verify that this socket holds the actual emergency bag.
        // It must not also be held by another interactor.
        return emergencyBagSocket.firstInteractableSelected
                   == (IXRSelectInteractable)bagGrab
            && bagGrab.interactorsSelecting.Count == 1;
    }

    private void MovePanel(Transform destination)
    {
        taskPanel.SetPositionAndRotation(
            destination.position,
            destination.rotation
        );
    }

    private void RefreshText(int count)
    {
        progressText.color = Color.white;

        switch (currentStage)
        {
            case Stage.Packing:
                objectiveText.text =
                    "CURRENT TASK: Pack your emergency bag";

                locationText.text = "Location: Kitchen";

                instructionText.text =
                    "Place and release the water bottle, medicine, " +
                    "torch, and document pouch inside the bag.";

                progressText.text =
                    $"Progress: {count} of 4 packed";

                hintText.text =
                    "Release each item inside the bag. " +
                    "Items held in your hand do not count.";
                break;

            case Stage.StoreBag:
                objectiveText.text =
                    "CURRENT TASK: Store your emergency bag";

                locationText.text =
                    "Location: Upstairs safe room";

                instructionText.text =
                    "Carry the packed bag upstairs. Place it in " +
                    "the cupboard's bag slot and release it.";

                progressText.text =
                    "Essentials packed: 4 of 4 | Bag not stored";

                hintText.text =
                    "Use the emergency-bag slot in the safe-room " +
                    "cupboard. Let go so the bag snaps into place.";
                break;

            case Stage.Valuables:
                objectiveText.text =
                    "CURRENT TASK: Move your valuables";

                locationText.text =
                    "Location: Bring items to the upstairs safe room";

                instructionText.text =
                    "Bring both laptops, the guitar, phone, radio, " +
                    "and documents box to their cupboard slots.";

                progressText.text =
                    "Emergency bag stored successfully";

                progressText.color =
                    new Color(0.3f, 1f, 0.4f);

                hintText.text =
                    "Collect the valuables from the bedroom and " +
                    "living room. Release each in its matching slot.";
                break;
        }
    }
}