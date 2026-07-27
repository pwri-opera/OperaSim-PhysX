using UnityEngine;

/// <summary>
/// Keeps the Spatial Reality Display view volume at the ZX200 operator position
/// and enables the regular operator camera when the SRD manager is unavailable.
/// </summary>
public class ZX200SpatialDisplayRig : MonoBehaviour
{
    [SerializeField] private string machineName = "zx200";
    [SerializeField] private string targetLinkName = "body_link";
    [SerializeField] private Vector3 localPosition = new Vector3(-1.0f, 1.5f, 0.6f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private GameObject spatialDisplayManager;
    [SerializeField] private Camera fallbackCamera;

    private Transform targetLink;

    private void Awake()
    {
        UpdateRigPose();
        UpdateCameraState();
    }

    private void LateUpdate()
    {
        UpdateRigPose();
        UpdateCameraState();
    }

    private void UpdateRigPose()
    {
        if (targetLink == null && !TryFindTarget())
            return;

        transform.SetPositionAndRotation(
            targetLink.TransformPoint(localPosition),
            targetLink.rotation * Quaternion.Euler(localEulerAngles));
    }

    private void UpdateCameraState()
    {
        if (fallbackCamera == null)
            return;

        bool isSpatialDisplayActive = spatialDisplayManager != null
            && spatialDisplayManager.activeInHierarchy;
        fallbackCamera.enabled = !isSpatialDisplayActive;
    }

    private bool TryFindTarget()
    {
        GameObject machine = GameObject.Find(machineName);
        if (machine == null)
            return false;

        targetLink = FindDescendant(machine.transform, targetLinkName);
        if (targetLink == null)
            Debug.LogWarning($"ZX200 SRD rig could not find '{targetLinkName}'.", this);

        return targetLink != null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform match = FindDescendant(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }
}
