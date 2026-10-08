using UnityEngine;

public class RockerSwitchVisual : MonoBehaviour
{
    [SerializeField] private bool isOn = true;

    [SerializeField] private float onAngle = -12f;
    [SerializeField] private float offAngle = 12f;

    private void Start()
    {
        ApplyVisual();
    }

    public void ToggleVisual()
    {
        isOn = !isOn;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        float angle = isOn ? onAngle : offAngle;

        transform.localRotation =
            Quaternion.Euler(0f, 0f, angle);
    }
}