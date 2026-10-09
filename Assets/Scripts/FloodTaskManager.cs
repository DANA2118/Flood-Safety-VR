using TMPro;
using UnityEngine;

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
    [SerializeField] private SafeRoomCupboard safeRoomCupboard;

    private enum Stage
    {
        None,
        Packing,
        StoreBag,
        Valuables,
        StorageComplete
    }

    private Stage currentStage = Stage.None;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool reachedSafeRoom;
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
        valid &= Check(safeRoomCupboard, "Safe Room Cupboard");

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

        Stage nextStage;

        if (bagPacking.PackedCount < 4)
            nextStage = Stage.Packing;
        else if (!safeRoomCupboard.BagStored)
            nextStage = Stage.StoreBag;
        else if (!safeRoomCupboard.AllStored)
            nextStage = Stage.Valuables;
        else
            nextStage = Stage.StorageComplete;

        if (nextStage != currentStage)
        {
            currentStage = nextStage;

            taskPanel.gameObject.SetActive(true);
            hintText.gameObject.SetActive(false);

            bool packing = currentStage == Stage.Packing;
            packingInstructions.SetActive(packing);
            packingProgress.SetActive(packing);

            if (packing)
            {
                taskPanel.SetPositionAndRotation(
                    startingPosition,
                    startingRotation
                );
            }
            else
            {
                Transform point =
                    currentStage == Stage.StoreBag && !reachedSafeRoom
                        ? kitchenTaskPanelPoint
                        : safeRoomTaskPanelPoint;

                taskPanel.SetPositionAndRotation(
                    point.position,
                    point.rotation
                );
            }
        }

        RefreshText();
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
                    "Release each supply inside the bag. " +
                    "Once all four are packed, grab the bag " +
                    "to secure its contents."
                );
                break;

            case Stage.StoreBag:
                SetText(
                    "CURRENT TASK: Store your emergency bag",
                    "Location: Upstairs safe room",
                    "Place the packed bag fully inside the " +
                    "bottom-right cupboard compartment and release it.",
                    "Essentials packed: 4/4 | Bag not stored",
                    "Rest the bag on the cupboard floor. " +
                    "Keep it fully inside, then release it " +
                    "and wait a moment."
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
                    "box. Release each fully inside a compartment " +
                    "where it fits."
                );
                break;

            case Stage.StorageComplete:
                SetText(
                    "COMPLETED: Emergency items stored",
                    "Location: Upstairs safe room",
                    "Your emergency bag and four valuables " +
                    "are stored in the cupboard.",
                    "Items stored: 5/5",
                    "The bag remains grabbable so you can " +
                    "collect it before leaving."
                );

                progressText.color = new Color(0.3f, 1f, 0.4f);
                break;
        }
    }
}