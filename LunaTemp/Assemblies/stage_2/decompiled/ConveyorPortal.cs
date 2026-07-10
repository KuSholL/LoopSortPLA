using UnityEngine;
using UnityEngine.Splines;

public class ConveyorPortal : MonoBehaviour
{
	private enum PortalRole
	{
		Entry,
		Exit
	}

	[SerializeField]
	private Transform exitPoint;

	[SerializeField]
	private Transform arrow;

	[SerializeField]
	private float exitDistance = 0.35f;

	[SerializeField]
	private float gizmoLength = 1.25f;

	private ConveyorPortal _linkedPortal;

	private SplineContainer _splineContainer;

	private bool _isTeleporting;

	private PortalRole _portalRole;

	public void Setup(SplineContainer targetSpline, ConveyorPortal targetPortal, bool isEntryPortal)
	{
		_splineContainer = targetSpline;
		_linkedPortal = targetPortal;
		_portalRole = ((!isEntryPortal) ? PortalRole.Exit : PortalRole.Entry);
		arrow.localRotation = Quaternion.Euler(90f, 0f, (_portalRole == PortalRole.Entry) ? (-90f) : 90f);
		GetComponent<Collider>().isTrigger = isEntryPortal;
	}

	private void ReceiveCube(CubeMovement cubeMovement, Transform entryPortal)
	{
		if (cubeMovement == null || entryPortal == null)
		{
			return;
		}
		Rigidbody body = cubeMovement.GetComponent<Rigidbody>();
		Vector3 currentVelocity = ((body != null) ? body.velocity : Vector3.zero);
		Vector3 localOffset = entryPortal.InverseTransformPoint(cubeMovement.transform.position);
		Transform targetExit = GetExitPoint();
		Vector3 exitForward = GetExitForward();
		float speed = currentVelocity.magnitude;
		Vector3 worldPosition = GetExitWorldPosition(targetExit, exitForward, localOffset);
		Vector3 worldVelocity = exitForward * speed;
		Vector3 worldForward = exitForward;
		_isTeleporting = true;
		cubeMovement.ApplyPortalTransfer(_splineContainer, worldPosition, worldVelocity, worldForward);
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.ConveyorSpeedBoostConfig != null)
		{
			ConveyorSpeedBoostConfigSO config = MonoSingleton<ConveyorDeliverySystem>.Instance.ConveyorSpeedBoostConfig;
			if (config.PortalExtraSpeed > 0f && config.PortalBoostDistance > 0f)
			{
				cubeMovement.ApplyTemporarySpeedBoost(config.PortalExtraSpeed, config.PortalBoostDistance);
			}
		}
		_isTeleporting = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(_linkedPortal == null) && !_isTeleporting && CanReceiveTrigger())
		{
			CubeMovement cubeMovement = other.GetComponentInParent<CubeMovement>();
			if (!(cubeMovement == null))
			{
				_linkedPortal.ReceiveCube(cubeMovement, base.transform);
			}
		}
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
		return (exitPoint != null) ? exitPoint : base.transform;
	}

	private Vector3 GetExitForward()
	{
		Vector3 forward = GetExitPoint().TransformDirection(Vector3.forward);
		return (forward.sqrMagnitude > 0.001f) ? forward.normalized : base.transform.forward;
	}

	private Vector3 GetExitWorldPosition(Transform targetExit, Vector3 exitForward, Vector3 localOffset)
	{
		Vector3 right = targetExit.right;
		Vector3 up = targetExit.up;
		Vector3 lateralOffset = right * localOffset.x + up * localOffset.y;
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
		Transform targetExit = GetExitPoint();
		Vector3 origin = targetExit.position;
		Vector3 forward = GetExitForward();
		float length = Mathf.Max(0.1f, gizmoLength);
		Vector3 tip = origin + forward * length;
		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		if (right.sqrMagnitude <= 0.001f)
		{
			right = Vector3.right;
		}
		Gizmos.color = (isSelected ? Color.yellow : Color.cyan);
		Gizmos.DrawSphere(origin, 0.08f);
		Gizmos.DrawLine(origin, tip);
		Gizmos.DrawLine(tip, tip - forward * 0.25f * length + right * 0.18f * length);
		Gizmos.DrawLine(tip, tip - forward * 0.25f * length - right * 0.18f * length);
	}
}
