using UnityEngine;

public sealed class CarrierContainerMember : MonoBehaviour
{
    [SerializeField] private CarrierBase carrier;
    [SerializeField] private ContainerMechanic currentContainer;

    public CarrierBase Carrier => carrier;
    public bool IsLocked => currentContainer != null && !currentContainer.IsOpen;

    private void Awake()
    {
        if (carrier == null) carrier = GetComponent<CarrierBase>();
    }

    public void SetCarrier(CarrierBase targetCarrier)
    {
        carrier = targetCarrier;
    }

    public void Bind(ContainerMechanic container)
    {
        currentContainer = container;
    }

    public void Unbind(ContainerMechanic container)
    {
        if (currentContainer == container) currentContainer = null;
    }
}
