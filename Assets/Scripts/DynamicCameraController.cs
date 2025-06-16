using UnityEngine;
using Unity.Cinemachine; // Required for Cinemachine functionality
using System.Collections.Generic;

public enum VCamType
{
    StaticOpening, // For the initial static camera
    Panoramic,
    PlayerFocus,
    GuestFocus,
    OverShoulderPlayer,
    OverShoulderGuest,
    GroupShotActiveGuests // Optional, for framing multiple active guests
}

public class DynamicCameraController : MonoBehaviour
{
    [Header("Cinemachine Brain")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera vCamStaticOpening;
    [SerializeField] private CinemachineCamera vCamPanoramic;
    [SerializeField] private CinemachineCamera vCamPlayerFocus;
    [SerializeField] private CinemachineCamera vCamGuestFocus; // A single VCam that will be retargeted
    [SerializeField] private CinemachineCamera vCamOverShoulderPlayer;
    [SerializeField] private CinemachineCamera vCamOverShoulderGuest;
    [SerializeField] private CinemachineCamera vCamGroupShotActiveGuests;

    private Dictionary<VCamType, CinemachineCamera> vCamMap;

    void Awake()
    {
        if (cinemachineBrain == null)
        {
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("CinemachineBrain not found on Main Camera. Please add it.");
            }
        }

        vCamMap = new Dictionary<VCamType, CinemachineCamera>
        {
            { VCamType.StaticOpening, vCamStaticOpening },
            { VCamType.Panoramic, vCamPanoramic },
            { VCamType.PlayerFocus, vCamPlayerFocus },
            { VCamType.GuestFocus, vCamGuestFocus },
            { VCamType.OverShoulderPlayer, vCamOverShoulderPlayer },
            { VCamType.OverShoulderGuest, vCamOverShoulderGuest },
            { VCamType.GroupShotActiveGuests, vCamGroupShotActiveGuests }
        };

        // Ensure all VCams start with low priority, except the initial static one
        SetAllVCamPriorities(0);
        if (vCamStaticOpening != null)
        {
            vCamStaticOpening.Priority = 10; // Make the static opening camera active initially
        }
    }

    /// <summary>
    /// Activates a specific Virtual Camera by setting its priority.
    /// </summary>
    /// <param name="cameraType">The type of Virtual Camera to activate.</param>
    /// <param name="targetTransform">Optional: The transform to set as Follow/LookAt target for dynamic cameras (e.g., GuestFocus).</param>
    public void ActivateCamera(VCamType cameraType, Transform targetTransform = null)
    {
        if (!vCamMap.TryGetValue(cameraType, out CinemachineCamera targetVCam) || targetVCam == null)
        {
            Debug.LogWarning($"Virtual Camera of type {cameraType} is not assigned or found.");
            return;
        }

        // Set all other VCams to a lower priority
        SetAllVCamPriorities(0);

        // Set the target VCam to a higher priority to make it active
        targetVCam.Priority = 10;

        // Handle dynamic targets for specific camera types
        if (targetTransform != null)
        {
            if (cameraType == VCamType.PlayerFocus || cameraType == VCamType.GuestFocus)
            {
                targetVCam.Follow = targetTransform;
                targetVCam.LookAt = targetTransform;
            }
            else if (cameraType == VCamType.OverShoulderPlayer || cameraType == VCamType.OverShoulderGuest)
            {
                targetVCam.Follow = targetTransform;
                // For OverShoulder cameras, LookAt is expected to be set by their dedicated SetOverShoulder...Targets methods.
            }
        }
        else
        {
            Debug.Log($"Target transform is null for {cameraType} camera. Camera might not behave as expected.");
        }

        Debug.Log($"Activated camera: {cameraType}");
    }

    private void SetAllVCamPriorities(int priority)
    {
        foreach (var kvp in vCamMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Priority = priority;
            }
        }
    }

    /// <summary>
    /// Sets the target for the GuestFocus camera.
    /// </summary>
    /// <param name="guestTransform">The transform of the guest to focus on.</param>
    public void SetGuestFocusTarget(Transform guestTransform)
    {
        if (vCamGuestFocus != null)
        {
            vCamGuestFocus.Follow = guestTransform;
            vCamGuestFocus.LookAt = guestTransform;
        }
        else
        {
            Debug.LogWarning("vCamGuestFocus is not assigned in DynamicCameraController.");
        }
    }

    /// <summary>
    /// Sets the target for the PlayerFocus camera.
    /// </summary>
    /// <param name="playerTransform">The transform of the player to focus on.</param>
    public void SetPlayerFocusTarget(Transform playerTransform)
    {
        if (vCamPlayerFocus != null)
        {
            vCamPlayerFocus.Follow = playerTransform;
            vCamPlayerFocus.LookAt = playerTransform;
        }
        else
        {
            Debug.LogWarning("vCamPlayerFocus is not assigned in DynamicCameraController.");
        }
    }

    /// <summary>
    /// Sets up the OverShoulderPlayer camera to look at a target.
    /// </summary>
    /// <param name="playerTransform">The player's transform (for positioning the camera behind).</param>
    /// <param name="targetTransform">The transform of the object the player is looking at.</param>
    public void SetOverShoulderPlayerTargets(Transform playerTransform, Transform targetTransform)
    {
        if (vCamOverShoulderPlayer != null)
        {
            // The camera will be positioned relative to the player, looking at the target
            vCamOverShoulderPlayer.Follow = playerTransform; // Or a specific shoulder target
            vCamOverShoulderPlayer.LookAt = targetTransform;
        }
        else
        {
            Debug.LogWarning("vCamOverShoulderPlayer is not assigned in DynamicCameraController.");
        }
    }

    /// <summary>
    /// Sets up the OverShoulderGuest camera to look at a target.
    /// </summary>
    /// <param name="guestTransform">The guest's transform (for positioning the camera behind).</param>
    /// <param name="targetTransform">The transform of the object the guest is looking at (likely the player).</param>
    public void SetOverShoulderGuestTargets(Transform guestTransform, Transform targetTransform)
    {
        if (vCamOverShoulderGuest != null)
        {
            // The camera will be positioned relative to the guest, looking at the target
            vCamOverShoulderGuest.Follow = guestTransform; // Or a specific shoulder target
            vCamOverShoulderGuest.LookAt = targetTransform;
        }
        else
        {
            Debug.LogWarning("vCamOverShoulderGuest is not assigned in DynamicCameraController.");
        }
    }
}
