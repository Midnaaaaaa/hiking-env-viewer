using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class PropPositionManager : MonoBehaviour
{
    [SerializeField] float[] propProbabilities;
    List<List<Vector3[]>> meshPropertiesGrass;
    List<List<Vector3[]>> meshPropertiesTrees;
    [SerializeField] float density;
    public Texture2D slopeAngleMap;
    private Texture2DArray slopeArray;
    [SerializeField] ComputeShader computeShader;
    private List<List<ComputeBuffer>> meshPropertiesBufferTrees;
    private List<List<ComputeBuffer>> meshPropertiesBufferGrass;
    private List<List<int>> grassCount;
    private List<List<int>> treesCount;
    private List<List<List<float>>> treesHeight;
    private List<List<List<float>>> grassHeight;
    private List<List<List<float>>> treesCanopy;
    private List<List<List<float>>> grassCanopy;

    [SerializeField] TextAsset plantsJson;

    void Start()
    {
        int numOfChunks = GetComponent<TerrainGenerator>().GetNumOfChunks();

        InitializeHeightsAndCanopies(numOfChunks);
        if(plantsJson == null)
        {
            InitializeTextureArrayForSlope();
            InitializeBuffers(numOfChunks);
            InitializeCounters(numOfChunks);
            InitializeMeshPropertiesComputeShader(numOfChunks);
        }
        else
        {
            InitializeMeshPropertiesPlantsJSON(numOfChunks);
        }
    }

    private void InitializeMeshPropertiesPlantsJSON(int numOfChunks)
    {
        TerrainGenerator tr = GetComponent<TerrainGenerator>();
        int metersPerPixel = tr.GetMetersPerPixel();
        int generalSize = tr.GetGeneralSize();
        int heightMapSize = tr.GetHeightMap().width;
        meshPropertiesTrees = new List<List<Vector3[]>>();
        meshPropertiesGrass = new List<List<Vector3[]>>();
        new PlantJSONParser(plantsJson, meshPropertiesTrees, meshPropertiesGrass, treesHeight, grassHeight, treesCanopy, grassCanopy, metersPerPixel, heightMapSize, generalSize, numOfChunks);
    }

    private void InitializeTextureArrayForSlope()
    {
        slopeArray = new Texture2DArray(slopeAngleMap.width, slopeAngleMap.height, 1, TextureFormat.R8, false);
        Graphics.CopyTexture(slopeAngleMap, 0, slopeArray, 0);
    }

    private void InitializeBuffers(int numOfChunks)
    {
        meshPropertiesBufferGrass = new List<List<ComputeBuffer>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesBufferGrass.Add(new List<ComputeBuffer>());
        }

        meshPropertiesBufferTrees = new List<List<ComputeBuffer>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesBufferTrees.Add(new List<ComputeBuffer>());
        }
    }

    private void InitializeHeightsAndCanopies(int numOfChunks)
    {
        grassHeight = new List<List<List<float>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            List<List<float>> list = new List<List<float>>();
            for (int j = 0; j < numOfChunks; j++)
            {
                list.Add(new List<float>());
            }
            grassHeight.Add(list);
        }

        treesHeight = new List<List<List<float>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            List<List<float>> list = new List<List<float>>();
            for (int j = 0; j < numOfChunks; j++)
            {
                list.Add(new List<float>());
            }
            treesHeight.Add(list);
        }

        grassCanopy = new List<List<List<float>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            List<List<float>> list = new List<List<float>>();
            for (int j = 0; j < numOfChunks; j++)
            {
                list.Add(new List<float>());
            }
            grassCanopy.Add(list);
        }

        treesCanopy = new List<List<List<float>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            List<List<float>> list = new List<List<float>>();
            for (int j = 0; j < numOfChunks; j++)
            {
                list.Add(new List<float>());
            }
            treesCanopy.Add(list);
        }

    }

    private void InitializeCounters(int numOfChunks)
    {
        grassCount = new List<List<int>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            grassCount.Add(new List<int>());
        }

        treesCount = new List<List<int>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            treesCount.Add(new List<int>());
        }
    }

    private void InitializeMeshPropertiesComputeShader(int numOfChunks)
    {
        TerrainGenerator tr = GetComponent<TerrainGenerator>();
        TrailTextureManager ptm = GetComponent<TrailTextureManager>();
        bool hasPaths = ptm.isActiveAndEnabled;
        int metersPerPixel = tr.GetMetersPerPixel();
        int chunkSizeVertexs = tr.GetChunkSize();
        int length = chunkSizeVertexs * metersPerPixel;
        int generalSize = tr.GetGeneralSize();
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        meshPropertiesGrass = new List<List<Vector3[]>>();
        meshPropertiesTrees = new List<List<Vector3[]>>();
        RenderTexture rt = tr.GetHeightMapRT();
        float heightScale = tr.GetHeightScale();
        int heightMapSize = tr.GetMapSize();

        ComputeBuffer probs = new ComputeBuffer(propProbabilities.Length, sizeof(float));
        probs.SetData(propProbabilities);
        int numOfTexts;
        int contributionSize;
        contributionSize = ptm.GetTrailContributionChunkSize();
        if (!hasPaths) contributionSize = 0;

        for (int i = 0; i < numOfChunks; i++)
        {
            List<Vector3[]> listPropsGrass = new List<Vector3[]>();
            List<Vector3[]> listPropsTrees = new List<Vector3[]>();
            for (int j = 0; j < numOfChunks; j++)
            {
                if(density > 0)
                {
                    meshPropertiesBufferGrass[i].Add(new ComputeBuffer(Mathf.RoundToInt(chunkSizeVertexs * chunkSizeVertexs * density * density), sizeof(float) * 3, ComputeBufferType.Append));
                    meshPropertiesBufferTrees[i].Add(new ComputeBuffer(Mathf.RoundToInt(chunkSizeVertexs * chunkSizeVertexs * density * density), sizeof(float) * 3, ComputeBufferType.Append));
                    meshPropertiesBufferGrass[i][j].SetCounterValue(0);
                    meshPropertiesBufferTrees[i][j].SetCounterValue(0);
                    ComputeBuffer argBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
                    Vector4 origin = new Vector4(length * i, 0, length * j, 1); //Origen del CHUNK
                    Texture2DArray t;
                    if (hasPaths)
                    {
                        t = tr.GetTrailLandCoversAtChunk(i, j);
                        numOfTexts = t.depth;
                    }
                    else
                    {
                        t = new Texture2DArray(1,1,1,TextureFormat.R8, true);
                        numOfTexts = 0;
                    }

                    computeShader.SetInt("chunkX", i);
                    computeShader.SetInt("chunkY", j);
                    computeShader.SetInt("slopeAngleSize", slopeAngleMap.width);
                    computeShader.SetFloat("density", density);
                    computeShader.SetInt("numOfChunks", numOfChunks);
                    computeShader.SetInt("indexWherePathBegins", ttm.GetTerrainContributionTextures(i, j).Length);
                    computeShader.SetInt("metersPerPixel", metersPerPixel);
                    computeShader.SetInt("length", length);
                    computeShader.SetVector("origin", origin);

                    computeShader.SetBuffer(0, "grassMeshProperties", meshPropertiesBufferGrass[i][j]);
                    computeShader.SetBuffer(0, "treesMeshProperties", meshPropertiesBufferTrees[i][j]);
                    computeShader.SetTexture(0, "slopeAngleMap", slopeArray);
                    computeShader.SetTexture(0, "contTexts", t);
                    computeShader.SetBuffer(0, "probs", probs);
                    computeShader.SetInt("probsLength", propProbabilities.Length);
                    computeShader.SetInt("generalSize", generalSize);
                    computeShader.SetTexture(0, "heightMap", rt, 0);
                    computeShader.SetInt("heightMapSize", heightMapSize);
                    computeShader.SetFloat("heightScale", heightScale);
                    computeShader.SetInt("numOfTexts", numOfTexts);
                    computeShader.SetInt("pathContributionSize", contributionSize);

                    computeShader.Dispatch(0, Mathf.RoundToInt(chunkSizeVertexs * density) / 8 + 1, Mathf.RoundToInt(chunkSizeVertexs * density) / 8 + 1, 1);


                    ComputeBuffer.CopyCount(meshPropertiesBufferGrass[i][j], argBuffer, 0);
                    int[] args = new int[1] { 0 };
                    argBuffer.GetData(args);
                    grassCount[i].Add(args[0]);
                    int sizeGrass = grassCount[i][j];

                    Vector3[] grassProps = new Vector3[sizeGrass];
                    meshPropertiesBufferGrass[i][j].GetData(grassProps);

                    ComputeBuffer.CopyCount(meshPropertiesBufferTrees[i][j], argBuffer, 0);
                    args = new int[1] { 0 };
                    argBuffer.GetData(args);
                    treesCount[i].Add(args[0]);

                    int sizeTrees = treesCount[i][j];
                    Vector3[] treesProps = new Vector3[sizeTrees];
                    meshPropertiesBufferTrees[i][j].GetData(treesProps);

                    listPropsGrass.Add(grassProps);
                    listPropsTrees.Add(treesProps);

                    argBuffer.Release();
                    meshPropertiesBufferGrass[i][j].Release();
                    meshPropertiesBufferTrees[i][j].Release();
                }
                else
                {
                    Vector3[] grassProps = new Vector3[0];
                    Vector3[] treesProps = new Vector3[0];
                    listPropsGrass.Add(grassProps);
                    listPropsTrees.Add(treesProps);
                }
            }
            meshPropertiesGrass.Add(listPropsGrass);
            meshPropertiesTrees.Add(listPropsTrees);
        }
        probs.Release();
    }

    public float GetDensity()
    {
        return density;
    }

    public bool HasPlantsJson()
    {
        return plantsJson != null;
    }
    public List<List<Vector3[]>> GetGrassMeshProperties()
    {
        return meshPropertiesGrass;
    }

    public List<List<Vector3[]>> GetTreesMeshProperties()
    {
        return meshPropertiesTrees;
    }

    public float[] GetTreesHeightAtChunk(int chunkX, int chunkY)
    {
        return treesHeight[chunkX][chunkY].ToArray();
    }

    public float[] GetGrassHeightAtChunk(int chunkX, int chunkY)
    {
        return grassHeight[chunkX][chunkY].ToArray();
    }

    public float[] GetTreesCanopyAtChunk(int chunkX, int chunkY)
    {
        return treesCanopy[chunkX][chunkY].ToArray();
    }

    public float[] GetGrassCanopyAtChunk(int chunkX, int chunkY)
    {
        return grassCanopy[chunkX][chunkY].ToArray();
    }



}
