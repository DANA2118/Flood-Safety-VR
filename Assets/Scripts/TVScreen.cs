using UnityEngine;

public class TVScreen : MonoBehaviour
{
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;

    private MeshRenderer screenRenderer;
    private WallPlug[] plugs;
    private bool on = true;

    private void Start()
    {
        screenRenderer = GetComponent<MeshRenderer>();
        plugs = FindObjectsByType<WallPlug>(FindObjectsSortMode.None);
        Apply();
    }

    private void Update()
    {
        if (!on || plugs.Length == 0)
            return;

        foreach (WallPlug plug in plugs)
        {
            if (!plug.IsPulledOut)
                return;
        }

        on = false;
        Apply();
        Debug.Log("[TVScreen] power off (plugs out)");
    }

    private void Apply()
    {
        screenRenderer.sharedMaterial = on ? onMaterial : offMaterial;
    }
}
