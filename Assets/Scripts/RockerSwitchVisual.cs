using UnityEngine;

public class RockerSwitchVisual : MonoBehaviour
{
    private bool isOn = true;

    public void ToggleVisual()
    {
        isOn = !isOn;

        float angle = isOn ? -12f : 12f;

        transform.localRotation =
            Quaternion.Euler(0f, 0f, angle);
    }
}