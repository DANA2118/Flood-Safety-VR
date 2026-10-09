using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SafeRoomCupboard : MonoBehaviour
{
    [Header("Storage Areas")]
    [SerializeField] private BoxCollider[] storageAreas;

    [Header("Required Items")]
    [SerializeField] private EmergencyBagPacking emergencyBag;
    [SerializeField] private XRGrabInteractable bedroomLaptop;
    [SerializeField] private XRGrabInteractable livingRoomLaptop;
    [SerializeField] private XRGrabInteractable phone;
    [SerializeField] private XRGrabInteractable documentsBox;

    [Header("Display")]
    [SerializeField] private TMP_Text progressText;

    [Header("Detection")]
    [SerializeField, Min(0f)]
    private float confirmationDelay = 0.5f;

    [SerializeField, Min(0f)]
    private float edgeTolerance = 0.015f;

    public int StoredCount { get; private set; }
    public int ValuablesStoredCount { get; private set; }
    public bool BagStored => stored[0];
    public bool AllStored => StoredCount == 5;

    private XRGrabInteractable[] items;
    private Collider[][] itemColliders;

    private readonly bool[] stored = new bool[5];
    private readonly float[] insideTime = new float[5];

    private bool initialized;
    private bool completionSent;

    private void Start()
    {
        items = new XRGrabInteractable[]
        {
            emergencyBag != null
                ? emergencyBag.GetComponent<XRGrabInteractable>()
                : null,
            bedroomLaptop,
            livingRoomLaptop,
            phone,
            documentsBox
        };

        if (storageAreas == null || storageAreas.Length == 0)
        {
            StopWithError("Assign the four Storage Areas.");
            return;
        }

        foreach (BoxCollider area in storageAreas)
        {
            if (area == null || !area.isTrigger)
            {
                StopWithError(
                    "Each Storage Area needs a Box Collider " +
                    "with Is Trigger enabled."
                );
                return;
            }
        }

        itemColliders = new Collider[items.Length][];
        var uniqueItems = new HashSet<XRGrabInteractable>();

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || !uniqueItems.Add(items[i]))
            {
                StopWithError(
                    "Assign five different required items."
                );
                return;
            }

            Rigidbody body = items[i].GetComponent<Rigidbody>();

            if (body == null)
            {
                StopWithError(items[i].name + " needs a Rigidbody.");
                return;
            }

            var solids = new List<Collider>();

            foreach (Collider c in
                     items[i].GetComponentsInChildren<Collider>(true))
            {
                // Exclude triggers and separate physics objects,
                // such as the supplies inside the bag.
                if (!c.isTrigger && c.attachedRigidbody == body)
                    solids.Add(c);
            }

            if (solids.Count == 0)
            {
                StopWithError(
                    items[i].name + " needs a solid collider."
                );
                return;
            }

            itemColliders[i] = solids.ToArray();
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        StoredCount = 0;
        ValuablesStoredCount = 0;

        for (int i = 0; i < items.Length; i++)
        {
            bool valid =
                items[i] != null &&
                items[i].isActiveAndEnabled &&
                !items[i].isSelected;

            if (i == 0)
            {
                valid = valid &&
                        emergencyBag != null &&
                        emergencyBag.IsSealed &&
                        emergencyBag.PackedCount == 4;
            }

            if (valid)
                valid = FitsInsideAnyArea(i);

            insideTime[i] = valid
                ? insideTime[i] + Time.deltaTime
                : 0f;

            stored[i] =
                valid && insideTime[i] >= confirmationDelay;

            if (stored[i])
            {
                StoredCount++;

                if (i > 0)
                    ValuablesStoredCount++;
            }
        }

        if (progressText != null)
        {
            progressText.text = $"Items Stored: {StoredCount}/5";

            if (AllStored)
                progressText.text += "\nSafe!";

            progressText.color = AllStored
                ? new Color(0.3f, 1f, 0.4f)
                : Color.white;
        }

        if (AllStored && !completionSent)
        {
            completionSent = true;
            TaskEvent.Complete("safe_room_complete");
        }
    }

    private bool FitsInsideAnyArea(int itemIndex)
    {
        foreach (BoxCollider area in storageAreas)
        {
            if (area == null ||
                !area.enabled ||
                !area.gameObject.activeInHierarchy)
            {
                continue;
            }

            bool foundCollider = false;
            bool fits = true;

            foreach (Collider c in itemColliders[itemIndex])
            {
                if (c == null ||
                    !c.enabled ||
                    !c.gameObject.activeInHierarchy)
                {
                    continue;
                }

                foundCollider = true;

                if (!BoundsFit(area, c.bounds))
                {
                    fits = false;
                    break;
                }
            }

            if (foundCollider && fits)
                return true;
        }

        return false;
    }

    private bool BoundsFit(BoxCollider area, Bounds bounds)
    {
        Vector3 half =
            area.size * 0.5f + Vector3.one * edgeTolerance;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? bounds.min.x : bounds.max.x,
                        y == 0 ? bounds.min.y : bounds.max.y,
                        z == 0 ? bounds.min.z : bounds.max.z
                    );

                    Vector3 local =
                        area.transform.InverseTransformPoint(corner)
                        - area.center;

                    if (Mathf.Abs(local.x) > half.x ||
                        Mathf.Abs(local.y) > half.y ||
                        Mathf.Abs(local.z) > half.z)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public string GetValuablesChecklist()
    {
        return Line(1, "Bedroom laptop") + "\n" +
               Line(2, "Living-room laptop") + "\n" +
               Line(3, "Phone") + "\n" +
               Line(4, "Documents box");
    }

    private string Line(int index, string label)
    {
        return (stored[index] ? "[X] " : "[ ] ") + label;
    }

    private void StopWithError(string message)
    {
        Debug.LogError("SafeRoomCupboard: " + message, this);
        enabled = false;
    }
}