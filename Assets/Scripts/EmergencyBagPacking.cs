using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EmergencyBagPacking : MonoBehaviour
{
    [SerializeField] private BoxCollider packingZone;
    [SerializeField] private XRGrabInteractable waterBottle;
    [SerializeField] private XRGrabInteractable medicineBox;
    [SerializeField] private XRGrabInteractable torch;
    [SerializeField] private XRGrabInteractable documentPouch;
    [SerializeField] private TMP_Text progressText;

    private int previousCount = -1;

    private void Update()
    {
        int count = 0;

        if (IsPacked(waterBottle))
            count++;

        if (medicineBox != waterBottle && IsPacked(medicineBox))
            count++;

        if (torch != waterBottle &&
            torch != medicineBox &&
            IsPacked(torch))
        {
            count++;
        }

        if (documentPouch != waterBottle &&
            documentPouch != medicineBox &&
            documentPouch != torch &&
            IsPacked(documentPouch))
        {
            count++;
        }

        if (count == previousCount)
            return;

        previousCount = count;

        if (progressText != null)
        {
            bool complete = count == 4;

            progressText.text = complete
                ? "Packed: 4/4\nComplete!"
                : $"Packed: {count}/4";

            progressText.color = complete
                ? new Color(0.3f, 1f, 0.4f)
                : Color.white;
        }
    }

    private bool IsPacked(XRGrabInteractable item)
    {
        if (packingZone == null ||
            item == null ||
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
}