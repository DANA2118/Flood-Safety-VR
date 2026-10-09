using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EmergencyBagPacking : MonoBehaviour
{
    [Header("Packing Area")]
    [SerializeField] private BoxCollider packingZone;

    [Header("Required Items")]
    [SerializeField] private XRGrabInteractable waterBottle;
    [SerializeField] private XRGrabInteractable medicineBox;
    [SerializeField] private XRGrabInteractable torch;
    [SerializeField] private XRGrabInteractable documentPouch;

    [Header("Original Packing Display")]
    [SerializeField] private TMP_Text progressText;

    public int PackedCount { get; private set; }
    public bool IsSealed { get; private set; }

    private XRGrabInteractable bagGrab;
    private XRGrabInteractable[] items;
    private int previousCount = -1;

    private void Awake()
    {
        bagGrab = GetComponent<XRGrabInteractable>();

        items = new XRGrabInteractable[]
        {
            waterBottle,
            medicineBox,
            torch,
            documentPouch
        };

        if (bagGrab == null || packingZone == null)
        {
            Debug.LogError(
                "EmergencyBagPacking: Assign Packing Zone and add " +
                "XR Grab Interactable to this bag.",
                this
            );

            enabled = false;
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                Debug.LogError(
                    "EmergencyBagPacking: Assign all four items.",
                    this
                );

                enabled = false;
                return;
            }

            for (int j = 0; j < i; j++)
            {
                if (items[i] == items[j])
                {
                    Debug.LogError(
                        "EmergencyBagPacking: Each item must be different.",
                        this
                    );

                    enabled = false;
                    return;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (bagGrab != null)
        {
            bagGrab.selectEntered.AddListener(OnBagGrabbed);
        }
    }

    private void OnDisable()
    {
        if (bagGrab != null)
        {
            bagGrab.selectEntered.RemoveListener(OnBagGrabbed);
        }
    }

    private void Update()
    {
        PackedCount = IsSealed ? items.Length : CountPackedItems();

        UpdateProgressDisplay();
    }

    private int CountPackedItems()
    {
        int count = 0;

        foreach (XRGrabInteractable item in items)
        {
            if (IsPacked(item))
                count++;
        }

        return count;
    }

    private bool IsPacked(XRGrabInteractable item)
    {
        if (item == null ||
            !item.isActiveAndEnabled ||
            item.isSelected)
        {
            return false;
        }

        Vector3 localPosition =
            packingZone.transform.InverseTransformPoint(
                item.transform.position
            ) - packingZone.center;

        Vector3 halfSize = packingZone.size * 0.5f;

        return Mathf.Abs(localPosition.x) <= halfSize.x
            && Mathf.Abs(localPosition.y) <= halfSize.y
            && Mathf.Abs(localPosition.z) <= halfSize.z;
    }

    private void OnBagGrabbed(SelectEnterEventArgs args)
    {
        if (IsSealed)
            return;

        // Check the actual item positions at the moment of grabbing.
        if (CountPackedItems() != items.Length)
            return;

        foreach (XRGrabInteractable item in items)
        {
            SecureItem(item);
        }

        IsSealed = true;
        PackedCount = items.Length;

        UpdateProgressDisplay();

        Debug.Log("Emergency bag contents secured.", this);
    }

    private void SecureItem(XRGrabInteractable item)
    {
        // Packed items can no longer be grabbed individually.
        item.enabled = false;

        // Prevent the contents from colliding with the moving bag.
        foreach (Collider itemCollider in
                 item.GetComponentsInChildren<Collider>(true))
        {
            itemCollider.enabled = false;
        }

        Rigidbody itemBody = item.GetComponent<Rigidbody>();

        if (itemBody != null)
        {
            if (!itemBody.isKinematic)
            {
                itemBody.linearVelocity = Vector3.zero;
                itemBody.angularVelocity = Vector3.zero;
            }

            itemBody.useGravity = false;
            itemBody.isKinematic = true;
            itemBody.detectCollisions = false;
            itemBody.interpolation = RigidbodyInterpolation.None;
        }

        // Preserve its current position, then move it with the bag.
        item.transform.SetParent(transform, true);
    }

    private void UpdateProgressDisplay()
    {
        if (PackedCount == previousCount)
            return;

        previousCount = PackedCount;

        if (progressText == null)
            return;

        bool complete = PackedCount == items.Length;

        progressText.text = complete
            ? "Packed: 4/4\nComplete!"
            : $"Packed: {PackedCount}/4";

        progressText.color = complete
            ? new Color(0.3f, 1f, 0.4f)
            : Color.white;
    }
}