using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrailTextureManager : MonoBehaviour
{

    [SerializeField] private Texture2D[] pathTextures;
    [SerializeField] private Texture2D[] trackTextures;
    [SerializeField] private Texture2D[] primaryTextures;

    [SerializeField] private Texture2D[] pathNormals;
    [SerializeField] private Texture2D[] trackNormals;
    [SerializeField] private Texture2D[] primaryNormals;

    private static Texture2D[][][] pathContributions;
    private static Texture2D[][][] trackContributions;
    private static Texture2D[][][] primaryContributions;
    private static Texture2D[][] primaryUVs;

    private List<Texture2D> textures;
    private List<Texture2D> normalMaps;
    private List<List<List<Texture2D>>> contributions;
    private List<float> tags;
    private List<float> hasNormals;
    private int trailContributionChunkSize;
    [SerializeField] private int relativeTrailResolution;
    private int primaryUVsChunkSize;
    [SerializeField] private int relativeUvsResolution;
    [SerializeField] private int primarySteps;

    private void Start()
    {
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        trailContributionChunkSize = ttm.GetLandCoverSize() * relativeTrailResolution;
        primaryUVsChunkSize = ttm.GetLandCoverSize() * relativeUvsResolution;
        InitializeContributions(ttm.GetNumChunks(), trailContributionChunkSize);
        InitializePrimaryUVs(ttm.GetNumChunks(), primaryUVsChunkSize);
        //pathContributions = new Texture2D[pathTextures.Length];
        //trackContributions = new Texture2D[trackTextures.Length];
        //primaryContributions = new Texture2D[primaryTextures.Length];
        textures = new List<Texture2D>();
        normalMaps = new List<Texture2D>();
        InitializeContributionsList(ttm.GetNumChunks());
        tags = new List<float>();
        hasNormals = new List<float>();
    }


    private void InitializeContributionsList(int numChunks)
    {
        contributions = new List<List<List<Texture2D>>>();
        for (int i = 0; i < numChunks; i++)
        {
            List<List<Texture2D>> secondDimension = new List<List<Texture2D>>();
            for (int j = 0; j < numChunks; j++)
            {
                List<Texture2D> thirdDimension = new List<Texture2D>();
                secondDimension.Add(thirdDimension);
            }
            contributions.Add(secondDimension);
        }
    }
    private void InitializeContributions(int numChunks, int chunkSize)
    {
        pathContributions = new Texture2D[numChunks][][];
        for (int i = 0; i < numChunks; i++) //Path
        {
            pathContributions[i] = new Texture2D[numChunks][];
            for (int j = 0; j < numChunks; j++)
            {
                pathContributions[i][j] = new Texture2D[pathTextures.Length];
                for (int k = 0; k < pathTextures.Length; k++)
                {
                    pathContributions[i][j][k] = Utils.CreateBlackTexture(chunkSize, TextureFormat.R8);
                }
            }
        }

        trackContributions = new Texture2D[numChunks][][];
        for (int i = 0; i < numChunks; i++) //Track
        {
            trackContributions[i] = new Texture2D[numChunks][];
            for (int j = 0; j < numChunks; j++)
            {
                trackContributions[i][j] = new Texture2D[trackTextures.Length];
                for (int k = 0; k < trackTextures.Length; k++)
                {
                    trackContributions[i][j][k] = Utils.CreateBlackTexture(chunkSize, TextureFormat.R8);
                }
            }
        }

        primaryContributions = new Texture2D[numChunks][][];
        for (int i = 0; i < numChunks; i++) //Primary
        {
            primaryContributions[i] = new Texture2D[numChunks][];
            for (int j = 0; j < numChunks; j++)
            {
                primaryContributions[i][j] = new Texture2D[primaryTextures.Length];
                for (int k = 0; k < primaryTextures.Length; k++)
                {
                    primaryContributions[i][j][k] = Utils.CreateBlackTexture(chunkSize, TextureFormat.R8);
                }
            }
        }
    }

    private void InitializePrimaryUVs(int numChunks, int chunkSize)
    {
        primaryUVs = new Texture2D[numChunks][];
        for (int i = 0; i < numChunks; i++) //Path
        {
            primaryUVs[i] = new Texture2D[numChunks];
            for (int j = 0; j < numChunks; j++)
            {
                primaryUVs[i][j] = Utils.CreateBlackTexture(chunkSize, TextureFormat.RGB24);
            }
        }
    }

    public void UpdateArrays()
    {
        for (int i = 0; i < pathTextures.Length; i++)
        {
            textures.Add(pathTextures[i]);
            if (i < pathNormals.Length)
            {
                normalMaps.Add(pathNormals[i]);
                hasNormals.Add(1);
            }
            else hasNormals.Add(0);
            tags.Add(2.0f);
            for (int chunkX = 0; chunkX < pathContributions.Length; chunkX++)
            {
                for (int chunkY = 0; chunkY < pathContributions[chunkX].Length; chunkY++)
                {
                    contributions[chunkX][chunkY].Add(pathContributions[chunkX][chunkY][i]);
                }
            }
        }

        for (int i = 0; i < trackTextures.Length; i++)
        {
            textures.Add(trackTextures[i]);
            if (i < trackNormals.Length)
            {
                normalMaps.Add(trackNormals[i]);
                hasNormals.Add(1);
            }
            else hasNormals.Add(0);
            tags.Add(2.0f);
            for (int chunkX = 0; chunkX < trackContributions.Length; chunkX++)
            {
                for (int chunkY = 0; chunkY < trackContributions[chunkX].Length; chunkY++)
                {
                    contributions[chunkX][chunkY].Add(trackContributions[chunkX][chunkY][i]);
                }
            }
        }

        for (int i = 0; i < primaryTextures.Length; i++)
        {
            textures.Add(primaryTextures[i]);
            if (i < primaryNormals.Length)
            {
                normalMaps.Add(primaryNormals[i]);
                hasNormals.Add(1);
            }
            else hasNormals.Add(0);
            tags.Add(3.0f);
            for (int chunkX = 0; chunkX < primaryContributions.Length; chunkX++)
            {
                for (int chunkY = 0; chunkY < primaryContributions[chunkX].Length; chunkY++)
                {
                    contributions[chunkX][chunkY].Add(primaryContributions[chunkX][chunkY][i]);
                }
            }
        }
    }

    public void UpdateTerrainMaterials()
    {
        TerrainGenerator terrainGen = GetComponent<TerrainGenerator>();
        terrainGen.AddTexturesToTextureArrays(textures.ToArray(), normalMaps.ToArray(), contributions);
        terrainGen.AddTagsToTagsArray(tags.ToArray());
        terrainGen.AddNormalFlagsToHasNormalsArray(hasNormals.ToArray());
        terrainGen.UpdateMaterials();
    }

    public static int GetPathTexturesLength() {
        return pathContributions[0][0].Length;
    }
    public static int GetTrackTexturesLength() { return trackContributions[0][0].Length; }
    public static int GetPrimaryTexturesLength() { return primaryContributions[0][0].Length; }

    public static Texture2D GetPathContributionWithIndexIAtCoords(int index, int chunkX, int chunkY)
    {
        return pathContributions[chunkX][chunkY][index];
    }
    public static Texture2D GetTrackContributionWithIndexIAtCoords(int index, int chunkX, int chunkY)
    {
        return trackContributions[chunkX][chunkY][index];
    }
    public static Texture2D GetPrimaryContributionWithIndexIAtCoords(int index, int chunkX, int chunkY)
    {
        return primaryContributions[chunkX][chunkY][index];
    }
    public static Texture2D GetPrimaryUVsAtCoords(int chunkX, int chunkY)
    {
        return primaryUVs[chunkX][chunkY];
    }
    public int GetRelativeTrailResolution()
    {
        return relativeTrailResolution;
    }
    public int GetTrailContributionChunkSize()
    {
        return trailContributionChunkSize;
    }
    public int GetRelativeUVResolution()
    {
        return relativeUvsResolution;
    }
    public int GetPrimaryUVChunkSize()
    {
        return primaryUVsChunkSize;
    }
    public int GetPrimarySteps()
    {
        return primarySteps;
    }
}
