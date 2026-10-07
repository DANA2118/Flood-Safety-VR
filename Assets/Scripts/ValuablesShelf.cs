using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ValuablesShelf : MonoBehaviour
{
    [SerializeField] private XRSocketInteractor[] sockets;
    [SerializeField] private TMP_Text progressText;

    private static readonly Color CompleteColor = new Color(0.3f, 1f, 0.4f);

    private bool complete;
    private int lastCount = -1;

    private void Update()
    {
        int count = 0;
        foreach (XRSocketInteractor socket in sockets)
        {
            if (socket.hasSelection)
                count++;
        }

        if (count == lastCount)
            return;

        lastCount = count;
        progressText.text = $"Valuables: {count}/{sockets.Length}";

        if (sockets.Length > 0 && count >= sockets.Length && !complete)
        {
            complete = true;
            progressText.color = CompleteColor;
            progressText.text += "\nSafe!";
            TaskEvent.Complete("valuables_high");
        }
    }
}
