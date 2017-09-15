
#if (UNITY_EDITOR) 
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ObjectPlacer : EditorWindow
{

    private  GameObject objectPrefab;
    private GameObject parentObject;

    private bool allowPlacing;
    private bool randomizeRotation;
    private bool useTerrainNormal;
    private bool usePlacer;

    //Mass spawning properties
    private GameObject massSpawnObject;
    private int spawnRounds;
    private float spawnRadius;
    private bool useMassSpawning;
    private float spawnHeightMin;
    private float spawnHeightMax;


    [MenuItem("Window/ObjectPlacer")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ObjectPlacer));
    }

    void OnGUI()
    {

        GUILayout.Label("ObjectPlacer", EditorStyles.boldLabel);

        usePlacer = EditorGUILayout.Toggle("Enabled", usePlacer);

        EditorGUI.BeginDisabledGroup(!usePlacer);
            objectPrefab = (GameObject)EditorGUILayout.ObjectField("Object to spawn", objectPrefab, typeof(GameObject), true);
            parentObject = (GameObject)EditorGUILayout.ObjectField("Parent object (optional)", parentObject, typeof(GameObject), true);

            allowPlacing = EditorGUILayout.Toggle("Allow placing", allowPlacing);
            randomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", randomizeRotation);
            useTerrainNormal = EditorGUILayout.Toggle("Use Terrain Normal", useTerrainNormal);

            GUILayout.Label("Mass Spawning", EditorStyles.boldLabel);
            useMassSpawning = EditorGUILayout.Toggle("Use Mass Spawning", useMassSpawning);
            massSpawnObject = (GameObject)EditorGUILayout.ObjectField("Mass spawn object", massSpawnObject, typeof(GameObject), true);
            spawnRounds = EditorGUILayout.IntField("Spawn Rounds", spawnRounds);
            spawnRadius = EditorGUILayout.FloatField("Spawn Radius", spawnRadius);

            EditorGUILayout.MinMaxSlider("SpawnLevel",ref spawnHeightMin, ref spawnHeightMax, 0.0f, 1.0f);
            spawnHeightMin = EditorGUILayout.FloatField("Spawn Height Min", spawnHeightMin);
            spawnHeightMax = EditorGUILayout.FloatField("Spawn Height Max", spawnHeightMax);

        EditorGUI.EndDisabledGroup();
    }

    void DrawCicle()
    {
        if (usePlacer)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(worldRay, out hitInfo))
            {
                Handles.color = new Color(0.0f, 0.5f, 0.8f, 0.1f);
                Handles.DrawSolidDisc(hitInfo.point, Vector3.up, spawnRadius);
            }
        }
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += SceneGUI;
    }

    void SceneGUI(SceneView sceneView)
    {
        if (Event.current.type == EventType.MouseMove)  sceneView.Repaint();
        if (Event.current.type == EventType.MouseDown && usePlacer)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (Event.current.button == 0 && allowPlacing)
            {
                Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hitInfo;

                if (Physics.Raycast(worldRay, out hitInfo))
                {
                    if (useMassSpawning)
                    {
                        MassSpawn(massSpawnObject, hitInfo.point.x, hitInfo.point.z);
                    }
                    else
                    {
                        SpawnOnTerrain(hitInfo.point.x, hitInfo.point.z, objectPrefab, 90.0f, 0.0f, 1.0f);
                    }
                }

                Event.current.Use();
            }

            if (Event.current.button == 2)
            {
                spawnHeightMin = GetHeightAtCursorPos();
                Repaint();
            }
            if (Event.current.button == 1)
            {
                spawnHeightMax = GetHeightAtCursorPos();

                Repaint();
            }

            GUIUtility.hotControl = controlId;
        }

         DrawCicle();

    }

    float GetHeightAtCursorPos()
    {
        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(worldRay, out hitInfo))
        {
            if (hitInfo.transform.name.Contains("Terrain"))
            {
                return hitInfo.point.y / hitInfo.transform.GetComponent<Terrain>().terrainData.size.y;
            }
        }
        return 0f;
    }

    void MassSpawn(GameObject obj, float X, float Y)
    {
        List<SpawnTemplate> objectsToSpawn = obj.GetComponent<MassSpawn>().objects;
        for (int i = 0; i < spawnRounds; i++)
        {
            foreach (var item in objectsToSpawn)
            {
                float rnd = Random.Range(0.00f, 1.00f);
                if (rnd == 0) continue;

                if (rnd > item.SpawnProbability) continue;

                float a = Random.Range(0.0f, 1.0f);
                float b = Random.Range(0.0f, 1.0f);

                float A = a, B = b;
                if (b < a)
                {
                    A = b;
                    B = a;
                }

                float x, y;

                x = B * spawnRadius * Mathf.Cos(2 * Mathf.PI * a / b);
                y = B * spawnRadius * Mathf.Sin(2 * Mathf.PI * a / b);

                x += X;
                y += Y;

                SpawnOnTerrain(x, y, item.ObjectToSpawn, item.maxSteepness, item.minHeight, item.maxHeight, item.terrainNormal);
            }
        }
    }

    void SpawnOnTerrain(float x, float z, GameObject obj, float maxSteepness, float minheight, float maxheight, bool terrainNormal = false)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 20000, z), -Vector3.up, out hit))
        {
            if (hit.transform.name.Contains("Terrain"))
            {
                if (hit.point.y <= hit.transform.GetComponent<Terrain>().terrainData.size.y * spawnHeightMax && 
                    hit.point.y >= hit.transform.GetComponent<Terrain>().terrainData.size.y * spawnHeightMin &&
                    hit.point.y <= hit.transform.GetComponent<Terrain>().terrainData.size.y * maxheight &&
                    hit.point.y >= hit.transform.GetComponent<Terrain>().terrainData.size.y * minheight)
                {
                    if (maxSteepness >= hit.transform.GetComponent<Terrain>().terrainData.GetSteepness(hit.point.x, hit.point.z) )
                    {
                        GameObject newObj = Instantiate(obj, hit.point, Quaternion.identity);
                        if (randomizeRotation) newObj.transform.transform.Rotate(0, Random.Range(0.0f, 360.0f), 0);

                        if (useTerrainNormal || terrainNormal)
                        {
                            newObj.transform.up = hit.normal;
                        }

                        if (parentObject != null) newObj.transform.SetParent(parentObject.transform);
                    }
                }
            }
        }
    }
}

#endif