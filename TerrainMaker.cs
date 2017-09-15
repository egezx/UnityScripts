using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    public int seed;
    public bool useRandomSeed;

    public Vector3 terrainSize;
    public int resolution;

    public float roughness;
    public bool useStaticCorners;
    public float staticCornerValue;
    public bool useStaticEdges;

    public PreInitPoint[] preInitializedPoints;


    private DiamondSquareGenerator terrainGenerator;
    private TerrainData terrainData;

    void Start()
    {
        terrainData = GetComponent<Terrain>().terrainData;
        terrainGenerator = new DiamondSquareGenerator(resolution);

    }

    public void GenerateTerrain()
    {
        Start();

        if (!useRandomSeed)
            terrainGenerator.SetSeed(seed);

        terrainGenerator.SetRoughness(roughness);
        terrainGenerator.SetStaticCornerValue(staticCornerValue);
        terrainGenerator.UseStaticCorners(useStaticCorners);
        terrainGenerator.UseStaticEdges(useStaticEdges);
        PreinitializedPoints();

        terrainData.heightmapResolution = resolution + 1;
        terrainData.SetHeights(0, 0, terrainGenerator.GenerateMapped01());
        terrainData.size = new Vector3(terrainSize.x, terrainSize.y, terrainSize.z);
    }

    private void PreinitializedPoints()
    {
        if (preInitializedPoints == null)
            terrainGenerator.ClearPreinintializedPoint();
        else if (preInitializedPoints.Length <= 0)
            terrainGenerator.ClearPreinintializedPoint();

        else
        {
            int[] x = new int[preInitializedPoints.Length];
            int[] y = new int[preInitializedPoints.Length]; ;
            float[] val = new float[preInitializedPoints.Length]; ;

            for (int i = 0; i < preInitializedPoints.Length; i++)
            {
                x[i] = preInitializedPoints[i].x;
                y[i] = preInitializedPoints[i].y;
                val[i] = Mathf.Clamp(preInitializedPoints[i].height, -1f, 1f);
            }

            terrainGenerator.SetPreinitializedPoints(x, y, val);
        }
    }
}

[System.Serializable]
public class PreInitPoint
{
    public int x, y;
    public float height;
}