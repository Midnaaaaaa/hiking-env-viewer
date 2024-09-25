using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureManager : MonoBehaviour
{

    [SerializeField] int textureSize;
    [SerializeField] int landCoverSize;
    [SerializeField] int normalMapSize;
    [SerializeField] int numChunks = 16;
    //[SerializeField] ComputeShader textureChunker;
    [SerializeField] int metersPerPixel;

    [SerializeField] public Texture2D heightMap;

    [Range(0,3)]
    [SerializeField] public int typeOfWaterAnimation; //0 normal movement, 1 static, 2 wind, 3 turbulent  
    [SerializeField] public Vector2 windDirection; //Wind direction
    [SerializeField] public float windStrength; //Wind direction
    [Range(0, 50)]
    [SerializeField] public int numberOfDifferentTextureRotations;
    [SerializeField] Texture2D distortionTexture;

    [SerializeField] Texture2D[] urbanTextureArray;
    [SerializeField] Texture2D[] agricultureTextureArray;
    [SerializeField] Texture2D[] forestDenseTextureArray;
    [SerializeField] Texture2D[] forestSparseTextureArray;
    [SerializeField] Texture2D[] bushesTextureArray;
    [SerializeField] Texture2D[] grassTextureArray;
    [SerializeField] Texture2D[] forestRiverTextureArray;
    [SerializeField] Texture2D[] groundTextureArray;
    [SerializeField] Texture2D[] rockTextureArray;
    [SerializeField] Texture2D[] sandTextureArray;
    [SerializeField] Texture2D[] humidTextureArray;
    [SerializeField] Texture2D[] waterTextureArray;

    [SerializeField] Texture2D[] urbanNormalArray;
    [SerializeField] Texture2D[] agricultureNormalArray;
    [SerializeField] Texture2D[] forestDenseNormalArray;
    [SerializeField] Texture2D[] forestSparseNormalArray;
    [SerializeField] Texture2D[] bushesNormalArray;
    [SerializeField] Texture2D[] grassNormalArray;
    [SerializeField] Texture2D[] forestRiverNormalArray;
    [SerializeField] Texture2D[] groundNormalArray;
    [SerializeField] Texture2D[] rockNormalArray;
    [SerializeField] Texture2D[] sandNormalArray;
    [SerializeField] Texture2D[] humidNormalArray;
    [SerializeField] Texture2D[] waterNormalArray;

    [SerializeField] Texture2D[] urbanLandCoverArray;
    [SerializeField] Texture2D[] agricultureLandCoverArray;
    [SerializeField] Texture2D[] forestDenseLandCoverArray;
    [SerializeField] Texture2D[] forestSparseLandCoverArray;
    [SerializeField] Texture2D[] bushesLandCoverArray;
    [SerializeField] Texture2D[] grassLandCoverArray;
    [SerializeField] Texture2D[] forestRiverLandCoverArray;
    [SerializeField] Texture2D[] groundLandCoverArray;
    [SerializeField] Texture2D[] rockLandCoverArray;
    [SerializeField] Texture2D[] sandLandCoverArray;
    [SerializeField] Texture2D[] humidLandCoverArray;
    [SerializeField] Texture2D[] waterLandCoverArray;

    private Texture2D[] texturesArray;
    private Texture2D[] landCoversArray;
    private Texture2D[] normalMapsArray;
    private Texture2D[][][] landCoversMatrix;

    private float[] tagArray; //0 Means default behaviour texture, 1 is for water behaviour, 2 is for path, 3 is for road
    private float[] hasNormals; //0 means no, 1 means yes

    private int numberOfTextures;
    private int numberOfNormals;
    private int chunkSize;

    // Start is called before the first frame update
    void Start()
    {
        chunkSize = landCoverSize / numChunks;
        numberOfTextures = CalculateNumberOfTotalTextures();
        numberOfNormals = CalculateNumberOfTotalNormals();

        landCoversArray = new Texture2D[numberOfTextures];
        texturesArray = new Texture2D[numberOfTextures];
        normalMapsArray = new Texture2D[numberOfNormals];
        tagArray = new float[15]; //We set the maximum number of tags because array size in shader cannot be changed in runtime
        hasNormals = new float[15];

        InitializeArrays(numberOfTextures);
        ChopLandCovers(numberOfTextures, numChunks);
    }

    private void ChopLandCovers(int numberOfTotalTextures, int numChunks)
    {
        landCoversMatrix = new Texture2D[numChunks][][];

        for (int i = 0; i < numChunks; i++)
        {
            landCoversMatrix[i] = new Texture2D[numChunks][];
            for (int j = 0; j < numChunks; j++)
            {
                landCoversMatrix[i][j] = new Texture2D[numberOfTotalTextures];
                for (int k = 0; k < numberOfTotalTextures; k++)
                {
                    Color[] chunkPixels = landCoversArray[k].GetPixels(i * chunkSize, j * chunkSize, chunkSize, chunkSize);
                    landCoversMatrix[i][j][k] = new Texture2D(chunkSize, chunkSize, TextureFormat.R8, false);
                    landCoversMatrix[i][j][k].wrapMode = TextureWrapMode.Clamp;
                    landCoversMatrix[i][j][k].filterMode = FilterMode.Point;
                    landCoversMatrix[i][j][k].SetPixels(chunkPixels);
                    landCoversMatrix[i][j][k].Apply();
                }
            }
        }
    }

    private void InitializeArrays(int numberOfTotalTextures)
    {
        int index = 0;
        int indexNormals = 0;
        for (int i = 0; urbanTextureArray != null && i < urbanTextureArray.Length; i++)
        {
            texturesArray[index] = urbanTextureArray[i];
            landCoversArray[index] = urbanLandCoverArray[i];
            if(i < urbanNormalArray.Length)
            {
                normalMapsArray[indexNormals] = urbanNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Urban
        for (int i = 0; agricultureTextureArray != null && i < agricultureTextureArray.Length; i++) 
        {
            texturesArray[index] = agricultureTextureArray[i];
            landCoversArray[index] = agricultureLandCoverArray[i];
            if (i < agricultureNormalArray.Length)
            {
                normalMapsArray[indexNormals] = agricultureNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Agriculture
        for (int i = 0; forestDenseTextureArray != null && i < forestDenseTextureArray.Length; i++)
        {
            texturesArray[index] = forestDenseTextureArray[i];
            landCoversArray[index] = forestDenseLandCoverArray[i];
            if (i < forestDenseNormalArray.Length)
            {
                normalMapsArray[indexNormals] = forestDenseNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Forest Dense
        for (int i = 0; forestSparseTextureArray != null && i < forestSparseTextureArray.Length; i++) 
        {
            texturesArray[index] = forestSparseTextureArray[i];
            landCoversArray[index] = forestSparseLandCoverArray[i];
            if (i < forestSparseNormalArray.Length)
            {
                normalMapsArray[indexNormals] = forestSparseNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Forest Sparse
        for (int i = 0; bushesTextureArray != null && i < bushesTextureArray.Length; i++)
        {
            texturesArray[index] = bushesTextureArray[i];
            landCoversArray[index] = bushesLandCoverArray[i];
            if (i < bushesNormalArray.Length)
            {
                normalMapsArray[indexNormals] = bushesNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Bushes
        for (int i = 0; grassTextureArray != null && i < grassTextureArray.Length; i++) 
        {
            texturesArray[index] = grassTextureArray[i];
            landCoversArray[index] = grassLandCoverArray[i];
            if (i < grassNormalArray.Length)
            {
                normalMapsArray[indexNormals] = grassNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Grass
        for (int i = 0; forestRiverTextureArray != null && i < forestRiverTextureArray.Length; i++)
        {
            texturesArray[index] = forestRiverTextureArray[i];
            landCoversArray[index] = forestRiverLandCoverArray[i];
            if (i < forestRiverNormalArray.Length)
            {
                normalMapsArray[indexNormals] = forestRiverNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Forest River
        for (int i = 0; groundTextureArray != null && i < groundTextureArray.Length; i++) 
        {
            texturesArray[index] = groundTextureArray[i];
            landCoversArray[index] = groundLandCoverArray[i];
            if (i < groundNormalArray.Length)
            {
                normalMapsArray[indexNormals] = groundNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Ground
        for (int i = 0; rockTextureArray != null && i < rockTextureArray.Length; i++)
        {
            texturesArray[index] = rockTextureArray[i];
            landCoversArray[index] = rockLandCoverArray[i];
            if (i < rockNormalArray.Length)
            {
                normalMapsArray[indexNormals] = rockNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Rock
        for (int i = 0; sandTextureArray != null && i < sandTextureArray.Length; i++) 
        {
            texturesArray[index] = sandTextureArray[i];
            landCoversArray[index] = sandLandCoverArray[i];
            if (i < sandNormalArray.Length)
            {
                normalMapsArray[indexNormals] = sandNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Sand
        for (int i = 0; humidTextureArray != null && i < humidTextureArray.Length; i++)
        {
            texturesArray[index] = humidTextureArray[i];
            landCoversArray[index] = humidLandCoverArray[i];
            if (i < humidNormalArray.Length)
            {
                normalMapsArray[indexNormals] = humidNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 0;
            ++index;
        } //Humid
        for (int i = 0; waterTextureArray != null && i < waterTextureArray.Length; i++) 
        {
            texturesArray[index] = waterTextureArray[i];
            landCoversArray[index] = waterLandCoverArray[i];
            if (i < waterNormalArray.Length)
            {
                normalMapsArray[indexNormals] = waterNormalArray[i];
                indexNormals++;
                hasNormals[index] = 1f;
            }
            else hasNormals[index] = 0f;
            tagArray[index] = 1;
            ++index;
        } //Water
        for(; index < tagArray.Length; ++index) //We fill the rest of the array with -1 to know that those positions are not being used
        {
            hasNormals[index] = -1f;
            tagArray[index] = -1;
        }
    }

    private int CalculateNumberOfTotalTextures()
    {
        int numberOfTotalTextures = 0;

        if (urbanTextureArray != null)
            numberOfTotalTextures += urbanTextureArray.Length;
        if (agricultureTextureArray != null)
            numberOfTotalTextures += agricultureTextureArray.Length;
        if (forestDenseTextureArray != null)
            numberOfTotalTextures += forestDenseTextureArray.Length;
        if (forestSparseTextureArray != null)
            numberOfTotalTextures += forestSparseTextureArray.Length;
        if (bushesTextureArray != null)
            numberOfTotalTextures += bushesTextureArray.Length;
        if (grassTextureArray != null)
            numberOfTotalTextures += grassTextureArray.Length;
        if (forestRiverTextureArray != null)
            numberOfTotalTextures += forestRiverTextureArray.Length;
        if (groundTextureArray != null)
            numberOfTotalTextures += groundTextureArray.Length;
        if (rockTextureArray != null)
            numberOfTotalTextures += rockTextureArray.Length;
        if (sandTextureArray != null)
            numberOfTotalTextures += sandTextureArray.Length;
        if (humidTextureArray != null)
            numberOfTotalTextures += humidTextureArray.Length;
        if (waterTextureArray != null)
            numberOfTotalTextures += waterTextureArray.Length;

        return numberOfTotalTextures;
    }

    private int CalculateNumberOfTotalNormals()
    {
        int numberOfTotalTextures = 0;

        if (urbanNormalArray != null)
            numberOfTotalTextures += urbanNormalArray.Length;
        if (agricultureNormalArray != null)
            numberOfTotalTextures += agricultureNormalArray.Length;
        if (forestDenseNormalArray != null)
            numberOfTotalTextures += forestDenseNormalArray.Length;
        if (forestSparseNormalArray != null)
            numberOfTotalTextures += forestSparseNormalArray.Length;
        if (bushesNormalArray != null)
            numberOfTotalTextures += bushesNormalArray.Length;
        if (grassNormalArray != null)
            numberOfTotalTextures += grassNormalArray.Length;
        if (forestRiverNormalArray != null)
            numberOfTotalTextures += forestRiverNormalArray.Length;
        if (groundNormalArray != null)
            numberOfTotalTextures += groundNormalArray.Length;
        if (rockNormalArray != null)
            numberOfTotalTextures += rockNormalArray.Length;
        if (sandNormalArray != null)
            numberOfTotalTextures += sandNormalArray.Length;
        if (humidNormalArray != null)
            numberOfTotalTextures += humidNormalArray.Length;
        if (waterNormalArray != null)
            numberOfTotalTextures += waterNormalArray.Length;

        return numberOfTotalTextures;
    }
    
    public int GetTextureSize() {  return textureSize; }
    
    public int GetNormalMapSize() { return normalMapSize; }
    
    public int GetLandCoverSize() { return chunkSize; }

    public int GetLandCoverSizeNoChunks() { return landCoverSize; }

    public int GetMetersPerPixel()
    {
        return metersPerPixel;
    }

    public int GetNumChunks()
    {
        return numChunks;
    }

    public Texture2D[] GetTexturesArray()
    {
        return texturesArray;
    }

    public Texture2D[] GetNormalMapsArray()
    {
        return normalMapsArray;
    }

    public Texture2D[][][] GetLandCoversMatrix()
    {
        return landCoversMatrix;
    }

    public Texture2D[] GetTerrainContributionTextures(int chunkX, int chunkY)
    {
        return landCoversMatrix[chunkX][chunkY];
    }

    public float[] GetTagArray()
    {
        return tagArray;
    }
    public float[] GetHasNormalsArray()
    {
        return hasNormals;
    }

    public int GetTypeOfWaterAnimation()
    {
        return typeOfWaterAnimation;
    }

    public Vector2 GetWindDirection()
    {
        return windDirection;
    }

    public float GetWindStrength()
    {
        return windStrength;
    }

    public void SetTagArray(float[] tagArray)
    {
        this.tagArray = tagArray;
    }

    public void SetHasNormalsArray(float[] hasNormalsArray)
    {
        this.hasNormals = hasNormalsArray;
    }

    public float GetNumberOfDifferentTextureRotations()
    {
        return numberOfDifferentTextureRotations;
    }
    public Texture2D GetDistortionTexture()
    {
        return distortionTexture;
    }
    public int GetHeightMapSize()
    {
        return heightMap.width;
    }
}
