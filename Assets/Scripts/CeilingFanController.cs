using UnityEngine;

public class CeilingFanController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool hasPower = true;

    void Update()
    {
        if (hasPower)
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        }
    }

    public void SetPower(bool powerState)
    {
        hasPower = powerState;
    }
}