using UnityEngine;

public class TorchController : MonoBehaviour
{
    [SerializeField] private Light torchLight;

    private void Start()
    {
        if (torchLight != null)
        {
            torchLight.enabled = false;
        }
    }

    public void TurnOnTorch()
    {
        if (torchLight != null)
        {
            torchLight.enabled = true;
        }

        Debug.Log("Torch switched ON.");
    }
}