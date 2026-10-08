using System.Collections;
using UnityEngine;

public class BedroomDoorController : MonoBehaviour
{
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 120f;

    private bool isOpen = false;
    private bool isMoving = false;

    public void ToggleDoor()
    {
        if (isMoving)
            return;

        float targetAngle = isOpen ? closedAngle : openAngle;
        StartCoroutine(RotateDoor(targetAngle));
    }

    private IEnumerator RotateDoor(float targetAngle)
    {
        isMoving = true;

        Quaternion targetRotation =
            Quaternion.Euler(0f, targetAngle, 0f);

        while (Quaternion.Angle(
                   transform.localRotation,
                   targetRotation) > 0.2f)
        {
            transform.localRotation =
                Quaternion.RotateTowards(
                    transform.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

            yield return null;
        }

        transform.localRotation = targetRotation;

        isOpen = !isOpen;
        isMoving = false;
    }
}