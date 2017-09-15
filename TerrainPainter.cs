using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // used for Sum of array

[ExecuteInEditMode]
public class TerrainPainter : MonoBehaviour {

    public List<TerrainPaintRule> PaintingRules;
    void Update()
    {
        foreach (var item in PaintingRules)
        {
            if (item.HasChanges())
            {
                if (item.RuleBonds != null)
                {
                    foreach (var bond in item.RuleBonds)
                    {
                        if (PaintingRules[bond.RuleIndex] != null)
                        {
                            if (bond.TieHeightLow)
                            {
                                PaintingRules[bond.RuleIndex].MinimumHeight = item.MaximumHeight;
                            }
                            if (bond.TieHeightHigh)
                            {
                                PaintingRules[bond.RuleIndex].MaximumHeight = item.MinimumHeight;
                            }
                            if (bond.TieSteepLow)
                            {
                                PaintingRules[bond.RuleIndex].MinimumSteepness = item.MaximumSteepness;
                            }
                            if (bond.TieSteepHigh)
                            {
                                PaintingRules[bond.RuleIndex].MaximumSteepness = item.MinimumSteepness;
                            }
                        }
                    }
                }
            }
        }
    }


    public void Paint()
    {

        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        float[, ,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
        
        int[, ,] detailMap = new int[terrainData.detailPrototypes.Length, terrainData.detailWidth, terrainData.detailHeight];

        int detailAlphaRatio = terrainData.detailResolution / terrainData.alphamapResolution;

        float maxHeight = FindMaxHeightOfTerrain(terrainData);
        TerrainPaintRule rule;

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                float steepness = Map(terrainData.GetSteepness(y_01, x_01),0.0f, 90.0f, 0.0f, 1.0f);

                float[] splatWeights = new float[terrainData.alphamapLayers];

                float heightNormalized = (height / maxHeight);


                
                for (int i = 0; i < PaintingRules.Count; i++)
                {
                    rule = PaintingRules[i];
                    //Validation
                    if (rule.MaximumSteepness < rule.MinimumSteepness) throw new UnityException("Higher steepness bound cannot be smaller than lower steepness bound");
                    if (rule.MinimumSteepness + rule.SteepnessGradientLow > rule.MaximumSteepness) throw new UnityException("Steepness Gradient out of range");
                    if (rule.MaximumSteepness - rule.SteepnessGradientHigh < rule.MinimumSteepness) throw new UnityException("Steepness Gradient out of range");
                    if (rule.MaximumHeight < rule.MinimumHeight) throw new UnityException("Higher Height bound cannot be smaller than lower Height bound");
                    if (rule.MinimumHeight + rule.HeightGradientLow > rule.MaximumHeight) throw new UnityException("Height Gradient out of range");
                    if (rule.MaximumHeight - rule.HeightGradientHigh < rule.MinimumHeight) throw new UnityException("Height Gradient out of range");
                    

                    if (i > splatWeights.Length - 1) throw new UnityException("TextureIndex out of range");

                    splatWeights[rule.TextureIndex] = CalculateWeight(heightNormalized, steepness, rule.TextureOverallWeight, rule.MinimumSteepness, rule.MaximumSteepness,
                        rule.SteepnessGradientLow, rule.SteepnessGradientHigh, rule.MinimumHeight, rule.MaximumHeight, rule.HeightGradientLow, rule.HeightGradientHigh);
                
                
                    // Details (grass, etc):
                    if (splatWeights[rule.TextureIndex] > 0.08f)
                    {
                        foreach (var layer in rule.DetailLayers)
                        {
                            if (x % layer.spreadness == 0 && y % layer.spreadness == 0 &&
                                steepness >= layer.minSteep && steepness <= layer.maxSteep &&
                                heightNormalized >= layer.minHeight && heightNormalized <= layer.maxHeight)
                            {
                                detailMap[layer.detailLayer, x * detailAlphaRatio, y * detailAlphaRatio] = layer.strength;
                            }
                            else
                            {
                                detailMap[layer.detailLayer, x * detailAlphaRatio, y * detailAlphaRatio] = 0;
                            }
                        }
                    }
                }




                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);


        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] details = new int[terrainData.detailWidth, terrainData.detailHeight];
            for (int x = 0; x < terrainData.detailWidth; x++)
            {
                for (int y = 0; y < terrainData.detailHeight; y++)
                {
                    details[x, y] = detailMap[i, x , y];
                }
            }
            terrainData.SetDetailLayer(0, 0, i, details);
        }
    }

    public static float CalculateWeight(float height, float steepness, float weightFactor, float steepMin, float steepMax, float steepGradLow, float steepGradHigh, float heightMin, float heightMax, float heightGradMin, float heightGradHigh)
    {
        if (height < heightMin || height > heightMax ||steepness < steepMin ||steepness > steepMax) weightFactor = 0.0f;

       if(heightGradMin > 0.0f) weightFactor *= Map(Mathf.Clamp(height, heightMin, heightMin + heightGradMin), heightMin, heightMin + heightGradMin, 0.0f, 1.0f);
       if (heightGradHigh > 0.0f) weightFactor *= (1.0f - Map(Mathf.Clamp(height, heightMax - heightGradHigh, heightMax), heightMax - heightGradHigh, heightMax, 0.0f, 1.0f));

       if (steepGradLow > 0.0f) weightFactor *= Map(Mathf.Clamp(steepness, steepMin, steepMin + steepGradLow), steepMin, steepMin + steepGradLow, 0.0f, 1.0f);
       if (steepGradHigh > 0.0f) weightFactor *= (1.0f - Map(Mathf.Clamp(steepness, steepMax - steepGradHigh, steepMax), steepMax - steepGradHigh, steepMax, 0.0f, 1.0f));

       return weightFactor;
    }

    public static float FindMaxHeightOfTerrain(TerrainData terrainData)
    {
        float height = 0.0f;
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                float tempHeight = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));
                if (tempHeight > height) height = tempHeight;
            }
        }

        return height;
    }


    public static float ClampZero(float value, float minValue, float maxValue)
    {
        if (value < minValue) return 0.0f;
        if (value > maxValue) return 0.0f;

        return value;
    }

    public static float ClampZeroKeepHigh(float value, float minValue, float maxValue)
    {
        if (value < minValue) return 0.0f;
        if (value > maxValue) return maxValue;

        return value;
    }

    public static float ClampZeroKeepLow(float value, float minValue, float maxValue)
    {
        if (value < minValue) return minValue;
        if (value > maxValue) return 0.0f;

        return value;
    }

    public static float Map(float valueIn, float baseMin, float baseMax, float limitMin, float limitMax)
    {
        return ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
    }
}


[System.Serializable]
public class TerrainPaintRule
{
    public int TextureIndex = 0;

    [Range(0.0f, 1.0f)]
    public float TextureOverallWeight;

    [Header("Steepness rules:")]
    [Range(0.0f, 1.0f)]
    public float MinimumSteepness = 0.0f;
    [Range(0.0f, 1.0f)]
    public float MaximumSteepness = 1.0f;
    [Range(0.0f, 1.0f)]
    public float SteepnessGradientLow = 0.0f;
    [Range(0.0f, 1.0f)]
    public float SteepnessGradientHigh = 0.0f;

    [Header("Height rules:")]
    [Range(0.0f, 1.0f)]
    public float MinimumHeight = 0.0f;
    [Range(0.0f, 1.0f)]
    public float MaximumHeight = 1.0f;
    [Range(0.0f, 1.0f)]
    public float HeightGradientLow = 0.0f;
    [Range(0.0f, 1.0f)]
    public float HeightGradientHigh = 0.0f;

    public List<PaintDetailRule> DetailLayers;

    //Private fields to keep track of calue changes:
    private float m_minSteep, m_maxSteep, m_minHeight, m_maxHeight;

    public bool HasChanges()
    {
        bool hasChanges = false;
        if (m_minSteep != MinimumSteepness || m_maxSteep != MaximumSteepness || m_minHeight != MinimumHeight || m_maxHeight != MaximumHeight) hasChanges = true;
        m_minSteep = MinimumSteepness;
        m_maxSteep = MaximumSteepness;
        m_minHeight = MinimumHeight;
        m_maxHeight = MaximumHeight;

        return hasChanges;
    }

    public List<PaintRuleBond> RuleBonds;
}

[System.Serializable]
public class PaintRuleBond
{
    public int RuleIndex;
    public bool TieHeightLow, TieHeightHigh;
    public bool TieSteepLow, TieSteepHigh;
}

[System.Serializable]
public class PaintDetailRule
{
    public int detailLayer;
    [Range(1,10)]
    public int spreadness;

    [Range(1,50)]
    public int strength;

    [Range(0.0f,1.0f)]
    public float minSteep, maxSteep, minHeight, maxHeight;
    
}