using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DiamondSquareGenerator
{
    //Maximum amount of values in one dimension of array.
    private int baseValue;

    //Current length of one step. Halved after each diamond-square routine.
    private int half;

    //Log2 of basevalue = amount of divisions.
    private int divisionCount;

    //Maximum range for random number generation.
    private float randomLimit;

    //How much randomLimit will be decreased (must be less than 1.0) after each diamond-square routine.
    private float randomLimitDivider = 0.25f;

    //Array to be filled.
    private float[,] heightMap;

    //One dimension length of heightMap.
    private int heightMapLen;

    //When true, corner values are pre-defined by user.
    private bool staticCorners = false;

    //Corner initialization value when staticCorners is set true.
    private float staticCornerValue = 0.0f;

    //When true, all edges will be same height as corners.
    private bool staticEdges = false;

    //Parameters to define initial shape for the heightMap before actual diamond-square procedure.
    private Vector2[] preInitializedPoints;
    private float[] preInitializedValues;



    private Vector2 position = new Vector2();




    //Public methods:
    public DiamondSquareGenerator(int baseVal)
    {
        SetInitialValues(baseVal);
    }

    public DiamondSquareGenerator(int seed, int baseVal)
    {
        Rand.SetSeed(seed);
        SetInitialValues(baseVal);
    }

    public void SetSeed(int seed)
    {
        Rand.SetSeed(seed);
    }

    public void SetRoughness(float roughness)
    {
        randomLimitDivider = HelperMethods.Clamp(roughness, 0.0001f, 1.0f);
    }

    public void SetBaseValue(int value)
    {
        SetInitialValues(value);
    }

    public void UseStaticCorners(bool use)
    {
        staticCorners = use;
        SetInitialValues(baseValue);
    }

    public void UseStaticEdges(bool use)
    {
        staticEdges = use;
    }

    public void SetStaticCornerValue(float value)
    {
        staticCornerValue = value;
    }

    public void SetPreinitializedPoints(int[]pointsX, int[]pointsY, float[] values)
    {
        preInitializedPoints = new Vector2[pointsX.Length];
        for (int i = 0; i < pointsX.Length; i++)
        {
            preInitializedPoints[i] = new Vector2(pointsX[i], pointsY[i]);
        }
        preInitializedValues = values;
    }

    public void ClearPreinintializedPoint()
    {
        preInitializedPoints = null;
        preInitializedValues = null;
    }

    public float[,] GenerateHeightMap()
    {
        SetInitialValues(baseValue);

        float currentDiv = 1;

        for (int d = 0; d < divisionCount; d++)
        {

            //Diamond routine:
            position.SetPos(half, half);

            for (int y = 0; y < currentDiv; y++)
            {
                for (int x = 0; x < currentDiv; x++)
                {
                    CalculateDiamond();
                    position.x += half * 2;
                }

                position.x = half;
                position.y += half * 2;
            }

            //Square routine:
            position.SetPos(0, 0);


            for (int y = 0; y < currentDiv * 2 + 1; y++)
            {
                float xCount = currentDiv;
                if (y % 2 != 0)
                {
                    position.x = 0;
                    xCount++;
                }
                else
                {
                    position.x = half;
                }

                for (int x = 0; x < xCount; x++)
                {
                    CalculateSquare();
                    position.x += half * 2;
                }

                position.y += half;
            }

            randomLimit *= randomLimitDivider;
            half /= 2;
            currentDiv *= 2;
        }


        return heightMap;
    }

    public float[,] GenerateMapped01()
    {
        float[,] unclampedArray = GenerateHeightMap();
        float[,] clampedArray = new float[heightMapLen, heightMapLen];

        float max = 0.0f;

        foreach (var val in unclampedArray)
        {
            float abs = Math.Abs(val);
            if (abs > max) max = abs;
        }

        for (int i = 0; i < heightMapLen; i++)
        {
            for (int j = 0; j < heightMapLen; j++)
            {
                clampedArray[j, i] = HelperMethods.Map(unclampedArray[j, i], -max, max, 0.0f, 1.0f);
            }
        }

        return clampedArray;
    }


    //Private methods:
    private void SetInitialValues(int baseVal)
    {
        if (!((baseVal & (baseVal - 1)) == 0)) throw new Exception("Number is not power of two");


        this.baseValue = baseVal;
        this.divisionCount = (int)Math.Log(baseVal, 2);
        this.half = baseVal / 2;
        this.randomLimit = 1f;

        this.heightMap = new float[baseVal + 1, baseVal + 1]; //Length of single dimension must be odd.
        this.heightMapLen = baseVal + 1;

        if (!staticCorners)
        {
            this.heightMap[0, 0] = Rand.Range(-randomLimit, randomLimit);
            this.heightMap[0, heightMapLen - 1] = Rand.Range(-randomLimit, randomLimit);
            this.heightMap[heightMapLen - 1, 0] = Rand.Range(-randomLimit, randomLimit);
            this.heightMap[heightMapLen - 1, heightMapLen - 1] = Rand.Range(-randomLimit, randomLimit);
        }
        else
        {
            this.heightMap[0, 0] = staticCornerValue;
            this.heightMap[0, heightMapLen - 1] = staticCornerValue;
            this.heightMap[heightMapLen - 1, 0] = staticCornerValue;
            this.heightMap[heightMapLen - 1, heightMapLen - 1] = staticCornerValue;
        }

        UsePreInitializedPoints();
    }


    private void UsePreInitializedPoints()
    {
        if (preInitializedPoints == null || preInitializedValues == null) return;

        for (int i = 0; i < preInitializedPoints.Length; i++)
        {
            heightMap[preInitializedPoints[i].x, preInitializedPoints[i].y] = preInitializedValues[i];
        }
    }
  


    private void CalculateDiamond()
    {
        if (preInitializedPoints != null && preInitializedValues != null)
        {
            for (int i = 0; i < preInitializedPoints.Length; i++)
            {
                if (preInitializedPoints[i].x == position.x && preInitializedPoints[i].y == position.y) return;
            }
        }

        if (staticEdges)
        {
            if (position.x == 0 || position.y == 0 || position.x == heightMapLen - 1 || position.y == heightMapLen - 1)
            {
                heightMap[position.x, position.y] = staticCornerValue;
                return;
            }
        }

        heightMap[position.x, position.y] = (heightMap[position.x - half, position.y - half] + heightMap[position.x - half, position.y + half] +
            heightMap[position.x + half, position.y - half] + heightMap[position.x + half, position.y + half]) * 0.25f + Rand.Range(-randomLimit, randomLimit);
    }

    private void CalculateSquare()
    {
        if (preInitializedPoints != null && preInitializedValues != null)
        {
            for (int i = 0; i < preInitializedPoints.Length; i++)
            {
                if (preInitializedPoints[i].x == position.x && preInitializedPoints[i].y == position.y) return;
            }
        }

        if (staticEdges)
        {
            if (position.x == 0 || position.y == 0 || position.x == heightMapLen - 1 || position.y == heightMapLen - 1)
            {
                heightMap[position.x, position.y] = staticCornerValue;
                return;
            }
        }

        Vector2 topPos = new Vector2(position.x, position.y - half);
        Vector2 bottomPos = new Vector2(position.x, position.y + half);
        Vector2 leftPos = new Vector2(position.x - half, position.y);
        Vector2 rightPos = new Vector2(position.x + half, position.y);




        if (topPos.y < 0)
        {
            heightMap[position.x, position.y] = (heightMap[bottomPos.x, bottomPos.y] +
            heightMap[leftPos.x, leftPos.y] + heightMap[rightPos.x, rightPos.y]) / 3 + Rand.Range(-randomLimit, randomLimit);
        }

        else if (bottomPos.y > heightMapLen - 1)
        {
            heightMap[position.x, position.y] = (heightMap[topPos.x, topPos.y] +
             heightMap[leftPos.x, leftPos.y] + heightMap[rightPos.x, rightPos.y]) / 3 + Rand.Range(-randomLimit, randomLimit);
        }

        else if (leftPos.x < 0)
        {
            heightMap[position.x, position.y] = (heightMap[topPos.x, topPos.y] + heightMap[bottomPos.x, bottomPos.y] +
            heightMap[rightPos.x, rightPos.y]) / 3 + Rand.Range(-randomLimit, randomLimit);
        }

        else if (rightPos.x > heightMapLen - 1)
        {
            heightMap[position.x, position.y] = (heightMap[topPos.x, topPos.y] + heightMap[bottomPos.x, bottomPos.y] +
            heightMap[leftPos.x, leftPos.y]) / 3 + Rand.Range(-randomLimit, randomLimit);
        }

        else
        {
            heightMap[position.x, position.y] = (heightMap[topPos.x, topPos.y] + heightMap[bottomPos.x, bottomPos.y] +
                heightMap[leftPos.x, leftPos.y] + heightMap[rightPos.x, rightPos.y]) * 0.25f + Rand.Range(-randomLimit, randomLimit);
        }

    }

    class Vector2
    {
        public int x, y;

        public Vector2() { }

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void SetPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return string.Format("x: {0}, y: {1}", x, y);
        }
    }

}

public static class HelperMethods
{
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) value = min;
        if (value > max) value = max;

        return value;
    }

    public static float Map(float valueIn, float baseMin, float baseMax, float limitMin, float limitMax)
    {
        return ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
    }

    public static bool IsPowerOfTwo(int baseVal)
    {
        return ((baseVal & (baseVal - 1)) == 0);
    }

}



public class Rand
{
    private static Random rnd = new Random();

    public static void SetSeed(int seed)
    {
        rnd = new Random(seed);
    }

    public static float Range(float min, float max)
    {
        return (float)rnd.NextDouble() * (max - min) + min;
    }

    public static int Range(int min, int max)
    {
        return rnd.Next(min, max + 1);
    }
        

}


