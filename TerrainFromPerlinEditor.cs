#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(TerrainFromPerlin))]
public class TerrainFromPerlinEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Terrain", GUILayout.Height(25), GUILayout.Width(320)))
        {
            ((TerrainFromPerlin)target).GenerateTerrain();
        }

        if (GUILayout.Button("Recalculate", GUILayout.Height(25)))
        {
            ((TerrainFromPerlin)target).GenerateTerrain(true);
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Saved Terrain", GUILayout.Height(25)))
        {
            ((TerrainFromPerlin)target).LoadTerrain();
        }

        if (GUILayout.Button("Save Current Terrain", GUILayout.Height(25)))
        {
            ((TerrainFromPerlin)target).SaveTerrain();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Set Default Values", GUILayout.ExpandWidth(false)))
        {
            ((TerrainFromPerlin)target).SetDefaults();
        }
    }
}
#endif
