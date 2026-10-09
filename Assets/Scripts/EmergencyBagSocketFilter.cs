using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class EmergencyBagSocketFilter : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] private EmergencyBagPacking requiredBag;

    public bool canProcess => isActiveAndEnabled;

    public bool Process(
        IXRSelectInteractor interactor,
        IXRSelectInteractable interactable)
    {
        if (requiredBag == null)
            return false;

        return interactable.transform == requiredBag.transform
            && requiredBag.IsSealed
            && requiredBag.PackedCount == 4;
    }
}