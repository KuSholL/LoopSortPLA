using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CubeConfigSO", menuName = "ScriptableObjects/CubeConfigSO")]
public class CubeConfigSO : ScriptableObject
{
    public Cube CubePrefab;
    public AnimCube AnimCubePrefab;
    public Vector3 CubeDefaultScale = new Vector3(1.3f, 1.3f, 1.3f);
}
