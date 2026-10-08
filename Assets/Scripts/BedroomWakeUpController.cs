using System.Collections;
using UnityEngine;

public class BedroomWakeUpController : MonoBehaviour
{
    [Header("XR Rig")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameObject locomotionRoot;

    [Header("Bedroom Points")]
    [SerializeField] private Transform wakeUpPoint;
    [SerializeField] private Transform standPoint;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 1.2f;

    [Header("Wake Up Timing")]
    [SerializeField] private float initialBlackDelay = 0.5f;
    [SerializeField] private float timeOnBed = 6f;
    [SerializeField] private float blackTransitionDelay = 0.3f;

    private IEnumerator Start()
    {
        // Do not allow walking while waking up.
        SetLocomotion(false);

        // Start completely black.
        SetFadeInstant(1f);

        // Give XR tracking a moment to initialize.
        yield return null;
        yield return null;

        // Lie on the bed and look at the ceiling.
        MoveCameraToWakeUpPoint();

        yield return new WaitForSeconds(initialBlackDelay);

        // Open eyes.
        yield return Fade(1f, 0f);

        // Stay lying on bed.
        yield return new WaitForSeconds(timeOnBed);

        // Close eyes before getting up.
        yield return Fade(0f, 1f);

        yield return new WaitForSeconds(blackTransitionDelay);

        // Move to standing position near table.
        MoveToStandPoint();

        yield return new WaitForSeconds(0.2f);

        // Open eyes again.
        yield return Fade(1f, 0f);

        // Player can now move.
        SetLocomotion(true);
    }

    private void MoveCameraToWakeUpPoint()
    {
        if (xrOrigin == null || xrCamera == null || wakeUpPoint == null)
            return;

        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        // Match FULL camera rotation to WakeUpPoint.
        // This includes X rotation, so we can look at the ceiling.
        AlignCameraRotation(wakeUpPoint.rotation);

        // Move the actual XR camera exactly onto WakeUpPoint.
        Vector3 movement =
            wakeUpPoint.position -
            xrCamera.transform.position;

        xrOrigin.position += movement;

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void MoveToStandPoint()
    {
        if (xrOrigin == null || xrCamera == null || standPoint == null)
            return;

        if (characterController != null)
            characterController.enabled = false;

        // Return camera to normal upright standing direction.
        AlignCameraRotation(standPoint.rotation);

        // Put player's X/Z above StandPoint.
        Vector3 horizontalMovement = new Vector3(
            standPoint.position.x - xrCamera.transform.position.x,
            0f,
            standPoint.position.z - xrCamera.transform.position.z
        );

        xrOrigin.position += horizontalMovement;

        // StandPoint represents floor height.
        Vector3 originPosition = xrOrigin.position;
        originPosition.y = standPoint.position.y;
        xrOrigin.position = originPosition;

        if (characterController != null)
            characterController.enabled = true;
    }

    private void AlignCameraRotation(Quaternion targetRotation)
    {
        // Work out the rotation needed to make the XR camera
        // match the target point's full rotation.
        Quaternion rotationDifference =
            targetRotation *
            Quaternion.Inverse(xrCamera.transform.rotation);

        xrOrigin.rotation =
            rotationDifference *
            xrOrigin.rotation;
    }

    private void SetLocomotion(bool enabled)
    {
        if (locomotionRoot != null)
            locomotionRoot.SetActive(enabled);
    }

    private void SetFadeInstant(float alpha)
    {
        if (fadeCanvas != null)
            fadeCanvas.alpha = alpha;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / fadeDuration
            );

            fadeCanvas.alpha =
                Mathf.Lerp(from, to, t);

            yield return null;
        }

        fadeCanvas.alpha = to;
    }
}