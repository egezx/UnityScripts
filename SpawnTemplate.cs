using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnTemplate
{
    [Range(0.0f, 1.0f)]
    public float SpawnProbability;
    public GameObject ObjectToSpawn;
    [Range(0.0f, 90.0f)]
    public float maxSteepness;
    [Range(0.0f, 1.0f)]
    public float minHeight;
    [Range(0.0f, 1.0f)]
    public float maxHeight;

    public bool terrainNormal;
}
