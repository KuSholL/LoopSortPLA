using UnityEngine;
using UnityEngine.Splines;

public class SplineBuilder : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;

    private void OnValidate()
    {
        splineContainer ??= GetComponent<SplineContainer>();
    }
}
