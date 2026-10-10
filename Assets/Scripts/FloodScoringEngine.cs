using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Final scoring and game-ending system for the Flood Safety VR scenario.
///
/// Designed for the current FloodHouse scene. It automatically finds the
/// existing task objects, monitors the player's performance, waits for the
/// rescue boat to arrive, displays a final /100 score, then permanently ends
/// the gameplay session while leaving the final result visible.
///
/// Score:
///   Preparedness / correct emergency actions : 50
///   Time efficiency                         : 30
///   Flood safety                            : 20
///
/// No score UI needs to be created manually.
/// </summary>
public class FloodScoringEngine : MonoBehaviour
{
    [Header("Optional References - Leave Empty For Auto Find")]
    [SerializeField] private EmergencyBagPacking bagPacking;
    [SerializeField] private SafeRoomCupboard safeRoomCupboard;
    [SerializeField] private WallPlug tvPlug;
    [SerializeField] private MainsBreaker mainsBreaker;
    [SerializeField] private RescueBoatController rescueBoatController;
    [SerializeField] private FloodWaterController floodWaterController;
    [SerializeField] private XRGrabInteractable bedroomTorch;
    [SerializeField] private BoxCollider verandaZone;
    [SerializeField] private Transform playerHead;
    [SerializeField] private CharacterController characterController;

    [Header("Timing Score")]
    [Tooltip("Fixed wake-up/tutorial time excluded from the completion time.")]
    [SerializeField] private float introGraceSeconds = 6f;

    [Tooltip("Complete at or before this time for the full 30 time points.")]
    [SerializeField] private float excellentCompletionSeconds = 300f;

    [Tooltip("At or after this time the time score becomes 0/30.")]
    [SerializeField] private float maximumCompletionSeconds = 600f;

    [Tooltip("How long the existing rescue-arrived message remains visible before the score screen.")]
    [SerializeField] private float scoreScreenDelay = 1.5f;

    [Header("Safety Score")]
    [Tooltip("Fallback only. The script reads the actual FloodDangerWarning value when possible.")]
    [SerializeField] private float fallbackUpperFloorHeight = 2.5f;

    [Tooltip("Fallback only. Current FloodDangerWarning uses 10 seconds.")]
    [SerializeField] private float fallbackDrowningSeconds = 10f;

    [Tooltip("Danger exposure reaches its maximum 10-point penalty at this many seconds.")]
    [SerializeField] private float exposureForMaximumPenalty = 30f;

    [Tooltip("Marks deducted each time the player drowns and respawns.")]
    [SerializeField] private int marksPerDrowningRespawn = 10;

    [Header("Game End")]
    [Tooltip("Ends/freezes gameplay after the final score is displayed. This is a terminal end state, not a resumable pause.")]
    [FormerlySerializedAs("pauseGameAtEnd")]
    [SerializeField] private bool endGameAtEnd = true;

    [Tooltip("Stops all scene audio when the training ends.")]
    [FormerlySerializedAs("pauseAudioAtEnd")]
    [SerializeField] private bool stopAudioAtEnd = true;

    // Existing scene objects found without requiring extra Inspector work.
    private MonoBehaviour dangerWarning;
    private GameObject rescueBoatRoot;
    private Transform boatStopPoint;
    private GameObject rescueCompleteUI;
    private GameObject rescueStatusCanvas;
    private Transform floodRespawnPoint;

    // Run state.
    private float gameStartTime;
    private bool torchCollected;
    private bool verandaReached;
    private bool sosDetected;
    private bool sosReadinessCaptured;
    private bool sosSentWithCorrectPreparation;

    private float dangerousExposureSeconds;
    private float currentDangerEpisodeSeconds;
    private bool drowningCountedThisEpisode;
    private bool wasDangerActiveLastFrame;
    private Vector3 previousPlayerPosition;
    private bool hasPreviousPlayerPosition;
    private int drowningRespawnCount;

    private bool finishStarted;
    private bool finished;

    private const BindingFlags MemberFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    private void Awake()
    {
        // Important when repeatedly entering Play Mode after the previous run paused.
        Time.timeScale = 1f;
        AudioListener.pause = false;

        AutoFindSceneReferences();
    }

    private void Start()
    {
        gameStartTime = Time.time;

        Debug.Log(
            "FloodScoringEngine started. " +
            "Final score will be calculated when the rescue boat arrives."
        );
    }

    private void Update()
    {
        if (finished)
            return;

        TrackTaskMilestones();
        TrackSafetyPerformance();
        TrackSOSReadiness();

        if (!finishStarted && HasRescueBoatArrived())
        {
            finishStarted = true;
            StartCoroutine(FinishTraining());
        }
    }

    // ---------------------------------------------------------------------
    // AUTO FIND
    // ---------------------------------------------------------------------

    private void AutoFindSceneReferences()
    {
        if (bagPacking == null)
            bagPacking = FindSceneComponent<EmergencyBagPacking>();

        if (safeRoomCupboard == null)
            safeRoomCupboard = FindSceneComponent<SafeRoomCupboard>();

        if (tvPlug == null)
            tvPlug = FindSceneComponent<WallPlug>();

        if (mainsBreaker == null)
            mainsBreaker = FindSceneComponent<MainsBreaker>();

        if (rescueBoatController == null)
            rescueBoatController = FindSceneComponent<RescueBoatController>();

        if (floodWaterController == null)
            floodWaterController = FindSceneComponent<FloodWaterController>();

        if (characterController == null)
            characterController = FindSceneComponent<CharacterController>();

        if (bedroomTorch == null)
        {
            GameObject torchObject = FindSceneObjectByName("BedroomTorch");
            if (torchObject != null)
                bedroomTorch = torchObject.GetComponent<XRGrabInteractable>();
        }

        if (verandaZone == null)
        {
            GameObject veranda = FindSceneObjectByName("VerandaZone");
            if (veranda != null)
                verandaZone = veranda.GetComponent<BoxCollider>();
        }

        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        dangerWarning = FindSceneBehaviourByTypeName("FloodDangerWarning");

        rescueBoatRoot = FindSceneObjectByName("RescueBoatRoot");

        GameObject stop = FindSceneObjectByName("BoatStopPoint");
        if (stop != null)
            boatStopPoint = stop.transform;

        // Current scene uses this panel under RescueStatusCanvas.
        rescueCompleteUI = FindSceneObjectByName("RescueCompletePanel");
        rescueStatusCanvas = FindSceneObjectByName("RescueStatusCanvas");

        GameObject respawn = FindSceneObjectByName("FloodRespawnPoint");
        if (respawn != null)
            floodRespawnPoint = respawn.transform;

        LogMissingReference("EmergencyBagPacking", bagPacking);
        LogMissingReference("SafeRoomCupboard", safeRoomCupboard);
        LogMissingReference("WallPlug", tvPlug);
        LogMissingReference("MainsBreaker", mainsBreaker);
        LogMissingReference("RescueBoatController", rescueBoatController);
        LogMissingReference("BedroomTorch XRGrabInteractable", bedroomTorch);
        LogMissingReference("VerandaZone", verandaZone);
        LogMissingReference("Player Head / Main Camera", playerHead);
    }

    private T FindSceneComponent<T>() where T : Component
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();

        foreach (T item in all)
        {
            if (item != null &&
                item.gameObject != null &&
                item.gameObject.scene.IsValid())
            {
                return item;
            }
        }

        return null;
    }

    private MonoBehaviour FindSceneBehaviourByTypeName(string typeName)
    {
        MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in all)
        {
            if (behaviour == null ||
                behaviour.gameObject == null ||
                !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            if (behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in all)
        {
            if (obj != null &&
                obj.scene.IsValid() &&
                obj.name == objectName)
            {
                return obj;
            }
        }

        return null;
    }

    private void LogMissingReference(string label, UnityEngine.Object value)
    {
        if (value == null)
        {
            Debug.LogWarning(
                $"FloodScoringEngine: Could not auto-find {label}. " +
                "That part of the score will use 0 until the reference exists.",
                this
            );
        }
    }

    // ---------------------------------------------------------------------
    // GAMEPLAY TRACKING
    // ---------------------------------------------------------------------

    private void TrackTaskMilestones()
    {
        // Once achieved, these stay achieved even if the player later releases
        // the torch or walks away from the veranda.
        if (!torchCollected &&
            bedroomTorch != null &&
            bedroomTorch.isActiveAndEnabled &&
            bedroomTorch.isSelected)
        {
            torchCollected = true;
        }

        if (!verandaReached && IsPlayerInsideVeranda())
            verandaReached = true;
    }

    private bool IsPlayerInsideVeranda()
    {
        if (verandaZone == null ||
            playerHead == null ||
            !verandaZone.enabled ||
            !verandaZone.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 local =
            verandaZone.transform.InverseTransformPoint(playerHead.position) -
            verandaZone.center;

        Vector3 half = verandaZone.size * 0.5f;

        return Mathf.Abs(local.x) <= half.x &&
               Mathf.Abs(local.y) <= half.y &&
               Mathf.Abs(local.z) <= half.z;
    }

    private void TrackSOSReadiness()
    {
        if (sosReadinessCaptured || rescueBoatController == null)
            return;

        bool triggered =
            ReadBoolMember(rescueBoatController, "SOSTriggered") ||
            ReadBoolMember(rescueBoatController, "sosTriggered");

        if (!triggered)
            return;

        sosDetected = true;
        sosReadinessCaptured = true;

        // Snapshot whether the emergency procedure was properly completed
        // when SOS was actually sent. This prevents pressing SOS early and
        // completing the safety tasks afterward for full marks.
        sosSentWithCorrectPreparation =
            GetPackedCount() >= 4 &&
            IsBagStored() &&
            GetValuablesCount() >= 4 &&
            IsTVUnplugged() &&
            IsMainPowerOff() &&
            torchCollected &&
            verandaReached;
    }

    private void TrackSafetyPerformance()
    {
        bool dangerActive = IsPlayerInDangerousFloodWater();
        float actualDeathTime = GetActualDrowningSeconds();

        Vector3 currentPlayerPosition =
            characterController != null
                ? characterController.transform.position
                : (playerHead != null ? playerHead.position : Vector3.zero);

        bool teleportedToFloodRespawn = false;

        if (hasPreviousPlayerPosition && floodRespawnPoint != null)
        {
            float frameJump = Vector3.Distance(
                previousPlayerPosition,
                currentPlayerPosition
            );

            float distanceToRespawn = Vector3.Distance(
                currentPlayerPosition,
                floodRespawnPoint.position
            );

            // Natural walking cannot move this far in one frame. This catches
            // the existing FloodDangerWarning teleport even when its Update()
            // runs before this scoring script's Update().
            teleportedToFloodRespawn =
                frameJump >= 1.25f && distanceToRespawn <= 1.0f;
        }

        if (dangerActive)
        {
            dangerousExposureSeconds += Time.deltaTime;
            currentDangerEpisodeSeconds += Time.deltaTime;

            // If this Update executes before FloodDangerWarning's death Update,
            // count the drowning here. Only one count is allowed per episode.
            if (!drowningCountedThisEpisode &&
                currentDangerEpisodeSeconds >= actualDeathTime)
            {
                RegisterDrowningRespawn();
            }
        }
        else
        {
            // If FloodDangerWarning executed first, it may already have
            // teleported the XR Origin upstairs and turned the warning off.
            // Detect that actual teleport so the respawn penalty is never lost
            // because of Unity script execution order.
            if (wasDangerActiveLastFrame &&
                !drowningCountedThisEpisode &&
                teleportedToFloodRespawn)
            {
                RegisterDrowningRespawn();
            }

            currentDangerEpisodeSeconds = 0f;
            drowningCountedThisEpisode = false;
        }

        wasDangerActiveLastFrame = dangerActive;
        previousPlayerPosition = currentPlayerPosition;
        hasPreviousPlayerPosition = true;
    }

    private void RegisterDrowningRespawn()
    {
        drowningRespawnCount++;
        drowningCountedThisEpisode = true;

        Debug.Log(
            $"FloodScoringEngine: drowning/respawn #{drowningRespawnCount}. " +
            $"-{marksPerDrowningRespawn} safety marks."
        );
    }

    private bool IsPlayerInDangerousFloodWater()
    {
        // Best source: use the actual warning system's internal state so the
        // scoring rule exactly follows the danger warning seen by the player.
        if (dangerWarning != null && HasMember(dangerWarning, "warningActive"))
            return ReadBoolMember(dangerWarning, "warningActive");

        // Fallback for future versions of the danger script.
        if (floodWaterController == null ||
            characterController == null ||
            !characterController.enabled)
        {
            return false;
        }

        bool floodDangerous =
            ReadBoolMember(floodWaterController, "HasReachedDangerLevel") ||
            ReadBoolMember(floodWaterController, "hasReachedDangerLevel");

        if (!floodDangerous)
            return false;

        float upperFloorHeight = GetActualUpperFloorHeight();
        float playerFeetY = characterController.bounds.min.y;

        return playerFeetY < upperFloorHeight;
    }

    private float GetActualUpperFloorHeight()
    {
        if (dangerWarning != null)
        {
            float value = ReadFloatMember(
                dangerWarning,
                "upperFloorHeight",
                fallbackUpperFloorHeight
            );

            return value;
        }

        return fallbackUpperFloorHeight;
    }

    private float GetActualDrowningSeconds()
    {
        if (dangerWarning != null)
        {
            float value = ReadFloatMember(
                dangerWarning,
                "deathTime",
                fallbackDrowningSeconds
            );

            return Mathf.Max(0.5f, value);
        }

        return Mathf.Max(0.5f, fallbackDrowningSeconds);
    }

    // ---------------------------------------------------------------------
    // RESCUE COMPLETION DETECTION
    // ---------------------------------------------------------------------

    private bool HasRescueBoatArrived()
    {
        if (rescueBoatController != null)
        {
            if (ReadBoolMember(rescueBoatController, "HasArrived") ||
                ReadBoolMember(rescueBoatController, "hasArrived"))
            {
                return true;
            }
        }

        // Existing rescue UI is activated when the boat reaches the end.
        if (rescueCompleteUI != null && rescueCompleteUI.activeInHierarchy)
            return true;

        // Final fallback uses the current FloodHouse boat objects directly.
        if (rescueBoatRoot != null &&
            boatStopPoint != null &&
            rescueBoatRoot.activeInHierarchy)
        {
            float distance = Vector3.Distance(
                rescueBoatRoot.transform.position,
                boatStopPoint.position
            );

            return distance <= 0.15f;
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // SCORE CALCULATION
    // ---------------------------------------------------------------------

    private IEnumerator FinishTraining()
    {
        // Let the existing "RESCUE BOAT ARRIVED" message be visible briefly.
        yield return new WaitForSecondsRealtime(scoreScreenDelay);

        // Remove the old rescue UI before drawing the final result.
        // This prevents RESCUE BOAT ARRIVED / YOU ARE SAFE / TRAINING COMPLETE
        // from appearing behind the score screen.
        HideExistingRescueUI();

        float completionSeconds = Mathf.Max(
            0f,
            Time.time - gameStartTime - introGraceSeconds
        );

        ScoreResult result = CalculateScore(completionSeconds);

        CreateFinalScoreUI(result);
        finished = true;

        // Force the new score UI to complete its layout before gameplay ends.
        Canvas.ForceUpdateCanvases();
        yield return null;

        EndGameplay();

        Debug.Log(
            $"TRAINING ENDED | FINAL SCORE {result.totalScore}/100 | " +
            $"Preparedness {result.preparednessScore}/50 | " +
            $"Time {result.timeScore}/30 | " +
            $"Safety {result.safetyScore}/20 | " +
            $"Drowning Respawns {result.drowningRespawns}"
        );
    }

    private void HideExistingRescueUI()
    {
        if (rescueStatusCanvas == null)
            rescueStatusCanvas = FindSceneObjectByName("RescueStatusCanvas");

        if (rescueStatusCanvas != null)
        {
            rescueStatusCanvas.SetActive(false);
            return;
        }

        // Fallback if only the completion panel can be found.
        if (rescueCompleteUI != null)
            rescueCompleteUI.SetActive(false);
    }

    private void EndGameplay()
    {
        // Disable the main gameplay managers so this is a real terminal state,
        // not a pause menu that can be resumed.
        FloodTaskManager taskManager = FindSceneComponent<FloodTaskManager>();
        if (taskManager != null)
            taskManager.enabled = false;

        if (dangerWarning != null)
            dangerWarning.enabled = false;

        if (rescueBoatController != null)
            rescueBoatController.enabled = false;

        if (floodWaterController != null)
            floodWaterController.enabled = false;

        if (bedroomTorch != null)
            bedroomTorch.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (stopAudioAtEnd)
        {
            AudioSource[] allAudio =
                Resources.FindObjectsOfTypeAll<AudioSource>();

            foreach (AudioSource source in allAudio)
            {
                if (source != null &&
                    source.gameObject != null &&
                    source.gameObject.scene.IsValid())
                {
                    source.Stop();
                }
            }

            AudioListener.pause = true;
        }

        if (endGameAtEnd)
        {
            // Unity has no "stop but keep the final screen visible" runtime API.
            // Application.Quit() would close the build immediately.
            // A terminal freeze is therefore used after all gameplay systems
            // above have been disabled. There is no resume path in this script.
            Time.timeScale = 0f;
        }
    }

    private ScoreResult CalculateScore(float completionSeconds)
    {
        int packedCount = GetPackedCount();
        int valuablesCount = GetValuablesCount();

        bool bagStored = IsBagStored();
        bool tvUnplugged = IsTVUnplugged();
        bool mainPowerOff = IsMainPowerOff();
        bool rescueArrived = HasRescueBoatArrived();

        // -------------------------------------------------------------
        // A. PREPAREDNESS / CORRECT EMERGENCY ACTIONS: 50 points
        // -------------------------------------------------------------
        // Emergency bag contents (4 x 3)            = 12
        // Emergency bag stored upstairs             =  5
        // Valuables secured (4 x 3)                 = 12
        // TV unplugged                               =  4
        // Main power OFF                             =  5
        // Bedroom torch collected                    =  4
        // Veranda reached                            =  3
        // SOS sent only after correct preparation    =  2
        // Rescue boat arrival                        =  3
        //                                             ----
        //                                              50
        int preparedness = 0;

        preparedness += packedCount * 3;
        preparedness += bagStored ? 5 : 0;
        preparedness += valuablesCount * 3;
        preparedness += tvUnplugged ? 4 : 0;
        preparedness += mainPowerOff ? 5 : 0;
        preparedness += torchCollected ? 4 : 0;
        preparedness += verandaReached ? 3 : 0;
        preparedness += sosSentWithCorrectPreparation ? 2 : 0;
        preparedness += rescueArrived ? 3 : 0;

        preparedness = Mathf.Clamp(preparedness, 0, 50);

        // -------------------------------------------------------------
        // B. TIME EFFICIENCY: 30 points
        // -------------------------------------------------------------
        // Full points <= excellentCompletionSeconds.
        // Linear reduction until maximumCompletionSeconds.
        // 0 points >= maximumCompletionSeconds.
        int timeScore;

        if (completionSeconds <= excellentCompletionSeconds)
        {
            timeScore = 30;
        }
        else if (completionSeconds >= maximumCompletionSeconds)
        {
            timeScore = 0;
        }
        else
        {
            float t = Mathf.InverseLerp(
                excellentCompletionSeconds,
                maximumCompletionSeconds,
                completionSeconds
            );

            timeScore = Mathf.RoundToInt(Mathf.Lerp(30f, 0f, t));
        }

        // -------------------------------------------------------------
        // C. FLOOD SAFETY: 20 points
        // -------------------------------------------------------------
        // Up to 10 marks lost for unnecessary dangerous-water exposure.
        // Each drowning/respawn deducts marksPerDrowningRespawn (default 10).
        float exposureRatio = Mathf.Clamp01(
            dangerousExposureSeconds /
            Mathf.Max(1f, exposureForMaximumPenalty)
        );

        int exposurePenalty = Mathf.RoundToInt(10f * exposureRatio);
        int respawnPenalty = drowningRespawnCount * marksPerDrowningRespawn;

        int safetyScore = Mathf.Clamp(
            20 - exposurePenalty - respawnPenalty,
            0,
            20
        );

        int total = Mathf.Clamp(
            preparedness + timeScore + safetyScore,
            0,
            100
        );

        return new ScoreResult
        {
            totalScore = total,
            preparednessScore = preparedness,
            timeScore = timeScore,
            safetyScore = safetyScore,
            completionSeconds = completionSeconds,

            packedCount = packedCount,
            valuablesCount = valuablesCount,
            bagStored = bagStored,
            tvUnplugged = tvUnplugged,
            mainPowerOff = mainPowerOff,
            torchCollected = torchCollected,
            verandaReached = verandaReached,
            sosDetected = sosDetected,
            correctSOS = sosSentWithCorrectPreparation,
            rescueArrived = rescueArrived,

            dangerousExposureSeconds = dangerousExposureSeconds,
            exposurePenalty = exposurePenalty,
            drowningRespawns = drowningRespawnCount,
            respawnPenalty = respawnPenalty,

            grade = GetGrade(total)
        };
    }

    private int GetPackedCount()
    {
        return bagPacking != null
            ? Mathf.Clamp(bagPacking.PackedCount, 0, 4)
            : 0;
    }

    private int GetValuablesCount()
    {
        return safeRoomCupboard != null
            ? Mathf.Clamp(safeRoomCupboard.ValuablesStoredCount, 0, 4)
            : 0;
    }

    private bool IsBagStored()
    {
        return safeRoomCupboard != null && safeRoomCupboard.BagStored;
    }

    private bool IsTVUnplugged()
    {
        return tvPlug != null && tvPlug.IsPulledOut;
    }

    private bool IsMainPowerOff()
    {
        return mainsBreaker != null && !mainsBreaker.IsOn;
    }

    private string GetGrade(int score)
    {
        if (score >= 90) return "EXCELLENT";
        if (score >= 75) return "VERY GOOD";
        if (score >= 60) return "GOOD";
        if (score >= 45) return "PASS";
        return "NEEDS IMPROVEMENT";
    }

    // ---------------------------------------------------------------------
    // FINAL SCORE UI - CREATED AUTOMATICALLY
    // ---------------------------------------------------------------------

    private void CreateFinalScoreUI(ScoreResult result)
    {
        Camera targetCamera = Camera.main;

        GameObject canvasObject = new GameObject(
            "FinalScoreCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();

        // Matches the other FloodHouse camera UI systems better than Overlay,
        // and remains suitable for the XR camera / desktop simulator.
        if (targetCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = targetCamera;
            canvas.planeDistance = 0.2f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = CreateImage(
            "Background",
            canvasObject.transform,
            new Color(0.025f, 0.045f, 0.070f, 1f)
        );

        Stretch(background.GetComponent<RectTransform>());

        TMP_Text title = CreateText(
            "Title",
            background.transform,
            "FLOOD SAFETY TRAINING COMPLETE",
            42f,
            FontStyles.Bold
        );

        SetRect(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -55f),
            new Vector2(1500f, 65f)
        );

        title.color = new Color(0.90f, 0.96f, 1f);

        TMP_Text finalScore = CreateText(
            "FinalScore",
            background.transform,
            $"FINAL SCORE   {result.totalScore} / 100",
            68f,
            FontStyles.Bold
        );

        SetRect(
            finalScore.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -145f),
            new Vector2(1300f, 90f)
        );

        finalScore.color =
            result.totalScore >= 75
                ? new Color(0.35f, 1f, 0.58f)
                : new Color(1f, 0.78f, 0.32f);

        TMP_Text grade = CreateText(
            "Grade",
            background.transform,
            result.grade,
            30f,
            FontStyles.Bold
        );

        SetRect(
            grade.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -225f),
            new Vector2(900f, 45f)
        );

        TMP_Text breakdown = CreateText(
            "Breakdown",
            background.transform,
            BuildBreakdown(result),
            26f,
            FontStyles.Normal
        );

        SetRect(
            breakdown.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -85f),
            new Vector2(1550f, 500f)
        );

        breakdown.enableAutoSizing = true;
        breakdown.fontSizeMin = 18f;
        breakdown.fontSizeMax = 26f;
        breakdown.lineSpacing = 8f;
        breakdown.alignment = TextAlignmentOptions.Center;

        TMP_Text footer = CreateText(
            "Footer",
            background.transform,
            "TRAINING ENDED  •  FINAL RESULT",
            22f,
            FontStyles.Bold
        );

        SetRect(
            footer.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 36f),
            new Vector2(900f, 42f)
        );

        footer.color = new Color(0.68f, 0.76f, 0.84f);
    }

    private string BuildBreakdown(ScoreResult result)
    {
        string time = FormatTime(result.completionSeconds);

        return
            $"PREPAREDNESS / TASKS     {result.preparednessScore} / 50\n" +
            $"TIME EFFICIENCY          {result.timeScore} / 30\n" +
            $"FLOOD SAFETY             {result.safetyScore} / 20\n\n" +

            $"Completion Time: {time}\n" +
            $"Emergency Bag Items: {result.packedCount}/4   |   " +
            $"Bag Upstairs: {YesNo(result.bagStored)}\n" +
            $"Valuables Secured: {result.valuablesCount}/4\n" +
            $"TV Unplugged: {YesNo(result.tvUnplugged)}   |   " +
            $"Main Power OFF: {YesNo(result.mainPowerOff)}\n" +
            $"Bedroom Torch Collected: {YesNo(result.torchCollected)}   |   " +
            $"Veranda Reached: {YesNo(result.verandaReached)}\n" +
            $"SOS Sent After Correct Preparation: {YesNo(result.correctSOS)}\n\n" +

            $"Dangerous-Water Exposure: {result.dangerousExposureSeconds:0.0}s " +
            $"(-{result.exposurePenalty})\n" +
            $"Drowning / Respawns: {result.drowningRespawns} " +
            $"(-{result.respawnPenalty})";
    }

    private string YesNo(bool value)
    {
        return value ? "YES" : "NO";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int minutes = totalSeconds / 60;
        int remaining = totalSeconds % 60;

        return $"{minutes:00}:{remaining:00}";
    }

    private GameObject CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return obj;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }

    private void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    // ---------------------------------------------------------------------
    // REFLECTION HELPERS
    // Allows compatibility with the current RescueBoatController and danger
    // scripts without requiring their private state fields to be changed.
    // ---------------------------------------------------------------------

    private bool HasMember(object target, string memberName)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        return type.GetProperty(memberName, MemberFlags) != null ||
               type.GetField(memberName, MemberFlags) != null;
    }

    private bool ReadBoolMember(object target, string memberName)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        PropertyInfo property = type.GetProperty(memberName, MemberFlags);
        if (property != null && property.PropertyType == typeof(bool))
        {
            object value = property.GetValue(target);
            return value is bool boolValue && boolValue;
        }

        FieldInfo field = type.GetField(memberName, MemberFlags);
        if (field != null && field.FieldType == typeof(bool))
        {
            object value = field.GetValue(target);
            return value is bool boolValue && boolValue;
        }

        return false;
    }

    private float ReadFloatMember(
        object target,
        string memberName,
        float fallback)
    {
        if (target == null)
            return fallback;

        Type type = target.GetType();

        PropertyInfo property = type.GetProperty(memberName, MemberFlags);
        if (property != null && property.PropertyType == typeof(float))
        {
            object value = property.GetValue(target);
            if (value is float floatValue)
                return floatValue;
        }

        FieldInfo field = type.GetField(memberName, MemberFlags);
        if (field != null && field.FieldType == typeof(float))
        {
            object value = field.GetValue(target);
            if (value is float floatValue)
                return floatValue;
        }

        return fallback;
    }

    private class ScoreResult
    {
        public int totalScore;
        public int preparednessScore;
        public int timeScore;
        public int safetyScore;
        public float completionSeconds;

        public int packedCount;
        public int valuablesCount;
        public bool bagStored;
        public bool tvUnplugged;
        public bool mainPowerOff;
        public bool torchCollected;
        public bool verandaReached;
        public bool sosDetected;
        public bool correctSOS;
        public bool rescueArrived;

        public float dangerousExposureSeconds;
        public int exposurePenalty;
        public int drowningRespawns;
        public int respawnPenalty;

        public string grade;
    }
}
