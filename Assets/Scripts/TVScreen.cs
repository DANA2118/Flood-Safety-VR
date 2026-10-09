using UnityEngine;

public class TVScreen : MonoBehaviour
{
    [Header("Screen Materials")]
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;

    [Header("TV Power Plug")]
    [SerializeField] private WallPlug tvPlug;

    private MeshRenderer screenRenderer;
    private bool mainPowerOn = true;
    private bool previousScreenState;
    private bool hasAppliedState;

    private void Awake()
    {
        screenRenderer = GetComponent<MeshRenderer>();

        if (screenRenderer == null)
        {
            Debug.LogError(
                "TVScreen: This object needs a MeshRenderer.",
                this
            );

            enabled = false;
        }
    }

    private void Start()
    {
        if (tvPlug == null)
        {
            Debug.LogError(
                "TVScreen: Assign the TV_WallPlug object.",
                this
            );
        }

        RefreshScreen();
    }

    private void Update()
    {
        RefreshScreen();
    }

    public void SetMainPower(bool powerOn)
    {
        mainPowerOn = powerOn;
        RefreshScreen();
    }

    private void RefreshScreen()
    {
        if (screenRenderer == null)
            return;

        bool screenOn =
            mainPowerOn &&
            tvPlug != null &&
            !tvPlug.IsPulledOut;

        if (hasAppliedState && screenOn == previousScreenState)
            return;

        Material targetMaterial =
            screenOn ? onMaterial : offMaterial;

        if (targetMaterial == null)
            return;

        screenRenderer.sharedMaterial = targetMaterial;

        previousScreenState = screenOn;
        hasAppliedState = true;
    }
}