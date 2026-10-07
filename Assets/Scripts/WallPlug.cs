using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WallPlug : MonoBehaviour
{
    [SerializeField] private float unplugDistance = 0.15f;
    [SerializeField] private float reseatSpeed = 1.2f;
    [SerializeField] private float reseatTurnSpeed = 540f;

    private Rigidbody body;
    private XRGrabInteractable grabInteractable;
    private Vector3 seatedPosition;
    private Quaternion seatedRotation;
    private bool pulledOut;

    public bool IsPulledOut => pulledOut;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.throwOnDetach = false;
        seatedPosition = transform.position;
        seatedRotation = transform.rotation;
    }

    private void Update()
    {
        if (pulledOut)
            return;

        if (grabInteractable.isSelected)
        {
            if (Vector3.Distance(transform.position, seatedPosition) >= unplugDistance)
                PullOut();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, seatedPosition, reseatSpeed * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, seatedRotation, reseatTurnSpeed * Time.deltaTime);
    }

    private void PullOut()
    {
        pulledOut = true;
        grabInteractable.throwOnDetach = true;
        body.isKinematic = false;
        body.useGravity = true;

        foreach (WallPlug plug in FindObjectsByType<WallPlug>(FindObjectsSortMode.None))
        {
            if (!plug.pulledOut)
                return;
        }

        TaskEvent.Complete("plugs_out");
    }
}
