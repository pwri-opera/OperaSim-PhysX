using UnityEngine;

/// <summary>
/// Keeps this camera at the ZX200 operator's eye position.
/// </summary>
public class ZX200OperatorCamera : MonoBehaviour
{
    [SerializeField] private string machineName = "zx200";
    [SerializeField] private string targetLinkName = "body_link";
    [SerializeField] private Vector3 localPosition = new Vector3(-0.65f, 2.05f, 0.35f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;

    private Transform targetLink;

    private void Awake()
    {
        UpdateCameraPose();
    }

    private void OnEnable()
    {
        UpdateCameraPose();
    }

    private void LateUpdate()
    {
        UpdateCameraPose();
    }

    private void UpdateCameraPose()
    {
        if (targetLink == null && !TryFindTarget())
            return;

        transform.SetPositionAndRotation(
            targetLink.TransformPoint(localPosition),
            targetLink.rotation * Quaternion.Euler(localEulerAngles));
    }

    private bool TryFindTarget()
    {
        GameObject machine = GameObject.Find(machineName);
        if (machine == null)
            return false;

        targetLink = FindDescendant(machine.transform, targetLinkName);
        if (targetLink == null)
            Debug.LogWarning($"ZX200 operator camera could not find '{targetLinkName}'.", this);

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
