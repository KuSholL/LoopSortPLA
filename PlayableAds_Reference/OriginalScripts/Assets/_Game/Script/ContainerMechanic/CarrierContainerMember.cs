using Alchemy.Inspector;
using UnityEngine;

public class CarrierContainerMember : MonoBehaviour
{
    [SerializeField] private CarrierBase carrier;
    [SerializeField, ReadOnly] private ContainerMechanic currentContainer;

    public CarrierBase Carrier => carrier;
    public ContainerMechanic CurrentContainer => currentContainer;
    public bool IsLocked => currentContainer != null && !currentContainer.IsOpen;

    private void OnValidate()
    {
        carrier ??= GetComponent<CarrierBase>();
    }

    private void Awake()
    {
        carrier ??= GetComponent<CarrierBase>();
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
        if (currentContainer != container) return;
        currentContainer = null;
    }

    public void Clear()
    {
        currentContainer = null;
    }
}
