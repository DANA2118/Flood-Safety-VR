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

    private int lastCount = -1;

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
            kitchenTaskPanelPoint == null)
        {
            Debug.LogError(
                "FloodTaskManager: Assign all Inspector fields.",
                this
            );

            enabled = false;
            return;
        }

        // Remember the panel's original bedroom location.
        startingPanelPosition = taskPanel.position;
        startingPanelRotation = taskPanel.rotation;

        hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        int count = bagPacking.PackedCount;

        if (count == lastCount)
            return;

        bool firstUpdate = lastCount == -1;
        bool wasPacked = lastCount == 4;
        bool isPacked = count == 4;

        // Keep the original kitchen text visible until complete.
        packingInstructions.SetActive(!isPacked);
        packingProgress.SetActive(!isPacked);

        if (firstUpdate || wasPacked != isPacked)
        {
            hintText.gameObject.SetActive(false);

            if (isPacked)
            {
                // Show the next task at the kitchen.
                taskPanel.SetPositionAndRotation(
                    kitchenTaskPanelPoint.position,
                    kitchenTaskPanelPoint.rotation
                );
            }
            else
            {
                // Restore the starting panel if packing is undone.
                taskPanel.SetPositionAndRotation(
                    startingPanelPosition,
                    startingPanelRotation
                );
            }
        }

        lastCount = count;

        if (isPacked)
        {
            ShowStoreBagTask();
        }
        else
        {
            ShowPackingTask(count);
        }
    }

    private void ShowPackingTask(int count)
    {
        objectiveText.text =
            "CURRENT TASK: Pack your emergency bag";

        locationText.text =
            "Location: Kitchen";

        instructionText.text =
            "Place and release the water bottle, medicine, " +
            "torch, and document pouch inside the bag.";

        progressText.text =
            $"Progress: {count} of 4 packed";

        progressText.color = Color.white;

        hintText.text =
            "Release each item inside the bag. " +
            "Items still held in your hand do not count.";
    }

    private void ShowStoreBagTask()
    {
        objectiveText.text =
            "CURRENT TASK: Store your emergency bag";

        locationText.text =
            "Location: Upstairs safe room";

        instructionText.text =
            "Carry the packed bag upstairs and place it " +
            "in the marked storage area.";

        progressText.text =
            "Essentials packed: 4 of 4";

        progressText.color = new Color(0.3f, 1f, 0.4f);

        hintText.text =
            "Take the whole bag. You will collect it " +
            "again before leaving the house.";
    }
}