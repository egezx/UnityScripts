using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class TerrainFromPerlin : MonoBehaviour {

    [Header("Terrain Real Dimensions:")]
    public Vector3 terrainDimensions;

    [Header("Resolution in power of two:")]
    public int terrainHeightmapResolution;

    [Header("Terrain noise settings:")]

    [Range(0.0f, 1.0f)]
    public float amplitude;

    [Range(0.01f, 10.0f)]
    public float redistribution;

    [Header("Terraces:")]
    public int terraceCount;
    public bool useTerraces;


    [Header("Values under cannot be recalculated:")]
    public float frequency;

    [Range(1, 20)]
    public int octaves;

    [Range(1, 20)]
    public float freqIncreasePerOctave;

    [Range(0.0f, 1.0f)]
    public float weightDecreasePerOctave;

    public bool dampenEdges;

    public bool additiveMode;


    private float startPointX;
    private float startPointY;


    private float[,] noise;

    private TerrainData terrainData;



    public void SetDefaults()
    {
        amplitude = 0.9f;
        redistribution = 2.8f;
        frequency = 3f;
        octaves = 5;
        freqIncreasePerOctave = 2.3f;
        weightDecreasePerOctave = 0.7f;
        terrainDimensions = new Vector3(1000f, 500f, 1000f);
        terrainHeightmapResolution = 2048;
        dampenEdges = true;
    }


	public void GenerateTerrain (bool forceRecalculate = false) {

        if (!HelperMethods.IsPowerOfTwo(terrainHeightmapResolution))
        {
            Debug.LogError("Terrain heightmap resolution is not power of two");
            return;
        }

        terrainData = GetComponent<Terrain>().terrainData;


        float freq = frequency;
        float weight = 1.0f;

        if (!forceRecalculate)
        {
            noise = new float[terrainHeightmapResolution, terrainHeightmapResolution];

            for (int i = 0; i < terrainHeightmapResolution; i++)
            {
                for (int j = 0; j < terrainHeightmapResolution; j++)
                {
                    noise[i, j] = 0.0f;
                }
            }

            //Generating terrain
            for (int i = 1; i <= octaves; i++)
            {
                AddOctave(noise, freq, terrainHeightmapResolution, terrainHeightmapResolution, weight);
                freq *= freqIncreasePerOctave;
                weight *= 1.0f - weightDecreasePerOctave;
            }

        }
        else
        {
            if (noise == null)
            {
                LoadTerrain();
                if (noise == null)
                {
                    Debug.LogError("Existing heightmap not found!");
                }
            }
        }

        float[,] finalTerrainArray = new float[terrainHeightmapResolution, terrainHeightmapResolution];

        //Post processing
        for (int i = 0; i < terrainHeightmapResolution; i++)
        {
            for (int j = 0; j < terrainHeightmapResolution; j++)
            {
                finalTerrainArray[i, j] = Mathf.Pow(noise[i, j], redistribution);

                //Damp edges 
                if (dampenEdges)
                {
                    float distanceX = Mathf.Abs((float)terrainHeightmapResolution * 0.5f - (float)j);
                    float distanceY = Mathf.Abs((float)terrainHeightmapResolution * 0.5f - (float)i);
                    float distanceGreater = distanceX;
                    if (distanceY > distanceX) distanceGreater = distanceY;

                    finalTerrainArray[i, j] *= Mathf.Clamp01( 1.0f - Mathf.Pow(10.0f, distanceGreater / (terrainHeightmapResolution *0.5f)) * 0.1f ); 
                }

                if (useTerraces)
                {
                    finalTerrainArray[i, j] = Mathf.Round(finalTerrainArray[i, j] * terraceCount) / terraceCount;
                }
            }
        }


        if (additiveMode)
        {
            var terrainHeightMap = terrainData.GetHeights(0, 0, terrainHeightmapResolution, terrainHeightmapResolution);
         

            for (int i = 0; i < terrainHeightmapResolution; i++)
            {
                for (int j = 0; j < terrainHeightmapResolution; j++)
                {
                    finalTerrainArray[i, j] += terrainHeightMap[i,j];
                }
            }
        }


        //Normalizing
        float max = 0.0f;
        
        foreach (var height in finalTerrainArray)
        {
            if (Mathf.Abs(height) > max) max = height;
        }

        float multiplier = 1.0f / max;

        for (int i = 0; i < terrainHeightmapResolution; i++)
        {
            for (int j = 0; j < terrainHeightmapResolution; j++)
            {
                finalTerrainArray[i, j] *= multiplier * amplitude;
            }
        }


            terrainData.heightmapResolution = terrainHeightmapResolution;
            terrainData.SetHeights(0, 0, finalTerrainArray);
            terrainData.size = new Vector3(terrainDimensions.x, terrainDimensions.y, terrainDimensions.z);
        
	}


    void AddOctave(float[,] arr, float freq, int countX, int countY, float w)
    {
        startPointX = Random.Range(-9000f, 9000f);
        startPointY = Random.Range(-9000f, 9000f);

        float[,] temp = new float[countY, countX];

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                arr[y, x] += Mathf.PerlinNoise(startPointX + x / (float)countX * freq, startPointY + y / (float)countY * freq) * w;
            }
        }
    }

    public void SaveTerrain()
    {
        if (noise == null)
        {
            Debug.LogWarning("Failed to save terrain. HeightMapData is null.");
            return;
        }
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/perlinterrain_" + this.gameObject.GetInstanceID() + ".data");
        bf.Serialize(file, noise);
        file.Close();

        Debug.Log("Terrain Heightmap saved: " + Application.persistentDataPath + "/perlinterrain_" + this.gameObject.GetInstanceID() + ".data");
    }

    public void LoadTerrain()
    {
        if (File.Exists(Application.persistentDataPath + "/perlinterrain_" + this.gameObject.GetInstanceID() + ".data"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/perlinterrain_" + this.gameObject.GetInstanceID() + ".data", FileMode.Open);
            noise = (float[,])bf.Deserialize(file);
            file.Close();
            GenerateTerrain(true);

            Debug.Log("Terrain Heightmap loaded: " + Application.persistentDataPath + "/perlinterrain_" + this.gameObject.GetInstanceID() + ".data");
        }
        else
        {
            Debug.LogWarning("Failed to load terrain. Existing save not found.");
        }
    }
}
