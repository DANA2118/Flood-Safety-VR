using System.Collections;
using UnityEngine;

public class FrontDoorController : MonoBehaviour
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float duration = 0.6f;

    private bool isOpen = false;
    private bool isMoving = false;

    public void ToggleDoor()
    {
        if (isMoving)
            return;

        StartCoroutine(RotateDoor());
    }

    private IEnumerator RotateDoor()
    {
        isMoving = true;

        float startY = doorPivot.localEulerAngles.y;

        float targetY = isOpen ? 0f : openAngle;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float y = Mathf.LerpAngle(startY, targetY, t);

            Vector3 rotation = doorPivot.localEulerAngles;
            rotation.y = y;

            doorPivot.localEulerAngles = rotation;

            yield return null;
        }

        Vector3 finalRotation = doorPivot.localEulerAngles;
        finalRotation.y = targetY;
        doorPivot.localEulerAngles = finalRotation;

        isOpen = !isOpen;
        isMoving = false;
    }
}