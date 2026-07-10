using UnityEngine;

public class SplineBuilder : MonoBehaviour
{
	[SerializeField]
	private ConveyorManager conveyorManager;

	private void OnValidate()
	{
		if (conveyorManager == null)
		{
			conveyorManager = GetComponent<ConveyorManager>();
		}
	}
}
