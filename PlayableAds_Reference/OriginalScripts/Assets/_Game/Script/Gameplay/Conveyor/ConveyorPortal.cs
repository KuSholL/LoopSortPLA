using UnityEngine;
using UnityEngine.Splines;

public class ConveyorPortal : MonoBehaviour
{
    private enum PortalRole
    {
        Entry,
        Exit
    }

    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform arrow;
    [SerializeField] private float exitDistance = 0.35f;
    [SerializeField] private float gizmoLength = 1.25f;
    
    private ConveyorPortal _linkedPortal;
    private SplineContainer _splineContainer;
    private bool _isTeleporting;
    private PortalRole _portalRole;
    
    public void Setup(
        SplineContainer targetSpline,
        ConveyorPortal targetPortal,
        bool isEntryPortal)
    {
        _splineContainer = targetSpline;
        _linkedPortal = targetPortal;
        _portalRole = isEntryPortal ? PortalRole.Entry : PortalRole.Exit;
        arrow.localRotation = Quaternion.Euler(90, 0, _portalRole == PortalRole.Entry ? -90f : 90f);
        GetComponent<Collider>().isTrigger = isEntryPortal;
    }

    private void ReceiveCube(CubeMovement cubeMovement, Transform entryPortal)
    {
        if (cubeMovement == null || entryPortal == null)
        {
            return;
        }

        var body = cubeMovement.GetComponent<Rigidbody>();
        var currentVelocity = body != null ? body.velocity : Vector3.zero;
        var localOffset = entryPortal.InverseTransformPoint(cubeMovement.transform.position);
        var targetExit = GetExitPoint();
        var exitForward = GetExitForward();
        var speed = currentVelocity.magnitude;

        var worldPosition = GetExitWorldPosition(targetExit, exitForward, localOffset);
        var worldVelocity = exitForward * speed;
        var worldForward = exitForward;

        _isTeleporting = true;
        cubeMovement.ApplyPortalTransfer(_splineContainer, worldPosition, worldVelocity, worldForward);

        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.ConveyorSpeedBoostConfig != null)
        {
            var config = ConveyorDeliverySystem.Instance.ConveyorSpeedBoostConfig;
            if (config.PortalExtraSpeed > 0f && config.PortalBoostDistance > 0f)
            {
                cubeMovement.ApplyTemporarySpeedBoost(config.PortalExtraSpeed, config.PortalBoostDistance);
            }
        }

        _isTeleporting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_linkedPortal == null || _isTeleporting)
        {
            return;
        }

        if (!CanReceiveTrigger())
        {
            return;
        }

        var cubeMovement = other.GetComponentInParent<CubeMovement>();
        if (cubeMovement == null)
        {
            return;
        }

        _linkedPortal.ReceiveCube(cubeMovement, transform);
    }

    private bool CanReceiveTrigger()
    {
        if (_portalRole == PortalRole.Exit)
        {
            return false;
        }
        return true;
    }

    private Transform GetExitPoint()
    {
        return exitPoint != null ? exitPoint : transform;
    }

    private Vector3 GetExitForward()
    {
        var forward = GetExitPoint().TransformDirection(Vector3.forward);
        return forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
    }

    private Vector3 GetExitWorldPosition(Transform targetExit, Vector3 exitForward, Vector3 localOffset)
    {
        var right = targetExit.right;
        var up = targetExit.up;

        var lateralOffset = right * localOffset.x + up * localOffset.y;
        return targetExit.position + lateralOffset + exitForward * exitDistance;
    }

    private void OnDrawGizmos()
    {
        DrawPortalGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawPortalGizmo(true);
    }

    private void DrawPortalGizmo(bool isSelected)
    {
        var targetExit = GetExitPoint();
        var origin = targetExit.position;
        var forward = GetExitForward();
        var length = Mathf.Max(0.1f, gizmoLength);
        var tip = origin + forward * length;
        var right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude <= 0.001f)
        {
            right = Vector3.right;
        }

        Gizmos.color = isSelected ? Color.yellow : Color.cyan;
        Gizmos.DrawSphere(origin, 0.08f);
        Gizmos.DrawLine(origin, tip);
        Gizmos.DrawLine(tip, tip - forward * 0.25f * length + right * 0.18f * length);
        Gizmos.DrawLine(tip, tip - forward * 0.25f * length - right * 0.18f * length);
    }
}
