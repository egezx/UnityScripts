#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(TerrainMaker))]
public class TerrainRendererEditor : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Generate Terrain"))
        {
            ((TerrainMaker)target).GenerateTerrain();
        }
    }
}

#endif
