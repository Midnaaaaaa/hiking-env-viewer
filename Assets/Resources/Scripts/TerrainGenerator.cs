using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.AI.Navigation;
using System.Linq;
using System.IO;
using System;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] GameObject character;

    private Texture2D heightMap;
    private int numOfChunks = 4;

    private Texture2DArray textures;
    private Texture2DArray[][] landCovers;
    private Texture2DArray[][] trailLandCovers;
    private Texture2DArray normalMaps;

    public int numLODS = 3;
    public float heightScale = 500;
    private int chunkSize;
    private Vector2Int actualTile;
    private List<Meshes> LODSMeshes;
    private List<Vector3Int> visibleTiles; //We use Vector3Int to store the LOD of the visible chunks in the z coord
    private int meterPerPixel = 2;
    public int generalSize = 8192;
    public ComputeShader terrainComputeShader;
    private RenderTexture heightMapRT;
    private Color fogColor;
    private float fogDensity;


    private void Start()
    {
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        heightMap = ttm.heightMap;
        numOfChunks = ttm.GetNumChunks();
        chunkSize = heightMap.width / numOfChunks;
        meterPerPixel = ttm.GetMetersPerPixel();
        fogColor = character.GetComponentInChildren<PostProcessing>().GetFogColor();
        fogDensity = character.GetComponentInChildren<PostProcessing>().GetFogDensity();


        InitializeRenderTexture();
        InitializeLODSMeshes();
        InitializeTextureArrays();

        visibleTiles = new List<Vector3Int>();
        SetStartingPosition();
        CreateAllChunks();
    }

    private void Update()
    {
        actualTile = WorldToGridPosition(character.transform.position);

        foreach (var chunk in visibleTiles) //Desactivar CHUNKS antiguos
        {
            int xCoord = chunk.x;
            int yCoord = chunk.y;
            for (int LOD = 0; LOD < numLODS; LOD++)
            {
                if (Utils.ChunkIsInBounds(xCoord, yCoord, numOfChunks))
                {
                    LODSMeshes[LOD].SetMeshStatus(xCoord, yCoord, false);
                }
            }
        }

        ObtainVisibleChunks();

        foreach (var chunk in visibleTiles) //Activamos los CHUNKS nuevos
        {
            int LOD = chunk.z;
            int xCoord = chunk.x;
            int yCoord = chunk.y;
            if (Utils.ChunkIsInBounds(xCoord, yCoord, numOfChunks))
            {
                LODSMeshes[LOD].SetMeshStatus(xCoord, yCoord, true);
            }
        }
    }

    public void AddTexturesToTextureArrays(Texture2D[] newTextures, Texture2D[] newNormals, List<List<List<Texture2D>>> newContributions)
    {
        //Destruim els buffers
        if (textures != null)
        {
            Destroy(textures);
            textures = null;
        }
        if (trailLandCovers != null)
        {
            for (int chunkX = 0; chunkX < trailLandCovers.Length; ++chunkX)
            {
                for (int chunkY = 0; chunkY < trailLandCovers.Length; ++chunkY)
                {
                    Destroy(trailLandCovers[chunkX][chunkY]);
                    trailLandCovers[chunkX][chunkY] = null;
                }
            }
            trailLandCovers = null;
        }
        if (normalMaps != null)
        {
            Destroy(normalMaps);
            normalMaps = null;
        }


        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        TrailTextureManager ptm = GetComponent<TrailTextureManager>();
        Texture2D[] texturesArray = ttm.GetTexturesArray();
        Texture2D[] normalMapsArray = ttm.GetNormalMapsArray();

        AddPathLandCovers(ptm.GetTrailContributionChunkSize(), newContributions);
        textures = new Texture2DArray(ttm.GetTextureSize(), ttm.GetTextureSize(), texturesArray.Length + newTextures.Length, TextureFormat.RGB24, true);
        normalMaps = new Texture2DArray(ttm.GetNormalMapSize(), ttm.GetNormalMapSize(), normalMapsArray.Length + newNormals.Length, TextureFormat.RGBA32, true);

        int i = 0;
        for (; i < texturesArray.Length; i++)
        {
            Graphics.CopyTexture(texturesArray[i], 0, textures, i);
        }
        for (; i < texturesArray.Length + newTextures.Length; i++)
        {
            Graphics.CopyTexture(newTextures[i - texturesArray.Length], 0, textures, i);
        }

        int k = 0;
        for (; k < normalMapsArray.Length; k++)
        {
            Graphics.CopyTexture(normalMapsArray[k], 0, normalMaps, k);
        }
        for (; k < normalMapsArray.Length + newNormals.Length; k++)
        {
            Graphics.CopyTexture(newNormals[k - normalMapsArray.Length], 0, normalMaps, k);
        }

        textures.Apply();
        normalMaps.Apply();
    }

    private void AddPathLandCovers(int landCoverSize, List<List<List<Texture2D>>> newContributions)
    {
        trailLandCovers = new Texture2DArray[numOfChunks][];
        for (int i = 0; i < numOfChunks; ++i)
        {
            trailLandCovers[i] = new Texture2DArray[numOfChunks];
            for (int j = 0; j < numOfChunks; ++j)
            {
                trailLandCovers[i][j] = new Texture2DArray(landCoverSize, landCoverSize, newContributions[i][j].Count, TextureFormat.R8, false);
                for (int k = 0; k < newContributions[i][j].Count; k++)
                {
                    Graphics.CopyTexture(newContributions[i][j][k], 0, trailLandCovers[i][j], k);
                }
                trailLandCovers[i][j].filterMode = FilterMode.Bilinear;
                trailLandCovers[i][j].wrapMode = TextureWrapMode.Clamp;
                trailLandCovers[i][j].Apply();
            }
        }
    }

    public void AddTagsToTagsArray(float[] newTags)
    {
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        float[] tagArray = ttm.GetTagArray();

        float[] tags = new float[tagArray.Length];

        tagArray.CopyTo(tags, 0);

        int index = 0;
        //We get the index of the first position of the tag array that is not being used
        for (int j = 0; j < tagArray.Length; j++) {
            if (tagArray[j] == -1)
            {
                index = j; break;
            }
        }

        newTags.CopyTo(tags, index);

        ttm.SetTagArray(tags);
    }

    public void AddNormalFlagsToHasNormalsArray(float[] newHasNormals)
    {
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        float[] hasNormals = ttm.GetHasNormalsArray();

        float[] norms = new float[hasNormals.Length];

        hasNormals.CopyTo(norms, 0);

        int index = 0;
        //We get the index of the first position of the hasNormals array that is not being used
        for (int j = 0; j < hasNormals.Length; j++)
        {
            if (hasNormals[j] == -1)
            {
                index = j; break;
            }
        }

        newHasNormals.CopyTo(norms, index);

        ttm.SetHasNormalsArray(norms);
    }

    public void UpdateMaterials()
    {
        Material skybox = RenderSettings.skybox;
        Cubemap skyboxCube = skybox.GetTexture("_Tex") as Cubemap;
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        for (int LOD = 0; LOD < LODSMeshes.Count; LOD++) {
            for (int i = 0; i < numOfChunks; ++i)
            {
                for (int j = 0; j < numOfChunks; j++)
                {
                    Material mat = LODSMeshes[LOD].GetMesh(i, j).GetComponent<MeshRenderer>().material;

                    int numOfLandCovers = landCovers[i][j].depth;
                    int numOfPathLandCovers = trailLandCovers[i][j].depth;

                    mat.SetTexture("_MainTex", landCovers[i][j]); //LandCoverArray
                    mat.SetTexture("_PathLandCovers", trailLandCovers[i][j]); //PathsLandCoverArray

                    mat.SetTexture("_TexturesArray", textures); //TexturesArray
                    mat.SetTexture("_NormalMaps", normalMaps); //NormalMapsArray
                    mat.SetFloatArray("_Tags", ttm.GetTagArray());
                    mat.SetFloatArray("hasNormals", ttm.GetHasNormalsArray());

                    mat.SetInt("_NumOfLandCovers", numOfLandCovers);
                    mat.SetInt("_NumOfPathLandCovers", numOfPathLandCovers);

                    mat.SetTexture("primaryUVs", TrailTextureManager.GetPrimaryUVsAtCoords(i, j));
                    mat.SetTexture("SkyBox", skyboxCube);
                    mat.SetInt("typeOfWaterAnimation", ttm.GetTypeOfWaterAnimation());
                    mat.SetVector("windDir", ttm.GetWindDirection());
                    mat.SetFloat("windStrength", ttm.GetWindStrength());
                    mat.SetVector("FogColor", fogColor);
                    mat.SetFloat("FogDensity", fogDensity);

                }
            }
        }
    }

    private void InitializeTextureArrays()
    {
        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        Texture2D[] texturesArray = ttm.GetTexturesArray();
        Texture2D[] normalMapsArray = ttm.GetNormalMapsArray();

        InitializeLandCoverMatrix(ttm.GetLandCoverSize(), ttm.GetLandCoversMatrix());
        textures = new Texture2DArray(ttm.GetTextureSize(), ttm.GetTextureSize(), texturesArray.Length, TextureFormat.RGB24, true);
        normalMaps = new Texture2DArray(ttm.GetNormalMapSize(), ttm.GetNormalMapSize(), normalMapsArray.Length, TextureFormat.RGBA32, true);
        
        
        for (int i = 0; i < texturesArray.Length; i++) //Copiamos texturas al texture2darray de la GPU
        {
            Graphics.CopyTexture(texturesArray[i], 0, textures, i);
        }

        for (int i = 0; i < normalMapsArray.Length; i++)
        {
            Graphics.CopyTexture(normalMapsArray[i], 0, normalMaps, i);
        }

        textures.Apply();
        normalMaps.Apply();
    }

    private void InitializeLandCoverMatrix(int landCoverSize, Texture2D[][][] landCoverMatrix)
    {
        landCovers = new Texture2DArray[numOfChunks][];
        for (int i = 0; i < numOfChunks; ++i)
        {
            landCovers[i] = new Texture2DArray[numOfChunks];
            for(int j = 0; j < numOfChunks; ++j)
            {
                landCovers[i][j] = new Texture2DArray(landCoverSize, landCoverSize, landCoverMatrix[i][j].Length, TextureFormat.R8, false);
                for (int k = 0; k < landCoverMatrix[i][j].Length; k++)
                {
                    Graphics.CopyTexture(landCoverMatrix[i][j][k], 0, landCovers[i][j], k);
                }
                landCovers[i][j].filterMode = FilterMode.Bilinear;
                landCovers[i][j].wrapMode = TextureWrapMode.Clamp;

                landCovers[i][j].Apply();
            }
        }
    }

    private void InitializeLODSMeshes()
    {
        LODSMeshes = new List<Meshes>();
        for (int i = 0; i < numLODS; i++)
        {
            LODSMeshes.Add(new Meshes(numOfChunks));
        }
    }

    private Vector2Int WorldToGridPosition(Vector3 position)
    {
        Vector2Int gridPosition = Utils.WorldToChunkCoord(new Vector2(position.x, position.z), chunkSize, meterPerPixel);
        if (!Utils.ChunkIsInBounds(gridPosition.x, gridPosition.y, numOfChunks))
        {
            return actualTile;
        }
        return gridPosition;
    }

    private void SetStartingPosition()
    {
        //character.transform.position = new Vector3(436.492f, 1283.289f, 513.3663f);
        //character.transform.position = new Vector3(transform.position.x + 4, transform.position.y + heightScale, transform.position.z + 4);
        character.transform.position = new Vector3(7200, transform.position.y + heightScale, 7200);
        actualTile = WorldToGridPosition(character.transform.position);
    }

    private void CreateAllChunks()
    {

        for (int i = 0; i < numOfChunks; i++)
        {
            for (int j = 0; j < numOfChunks; j++)
            {
                for (int k = 0; k < numLODS; k++)
                {
                    CreateChunkComputeShader(i, j, k);
                }
            }
        }
    }

    private int AssignLOD(int i, int j)
    {
        return Mathf.Max(Mathf.Min(Mathf.Max(Mathf.Abs(i - actualTile.x), Mathf.Abs(j - actualTile.y)) - 1, numLODS - 1), 0); // ------------ LOD POR NIVELES ------------
    }

    private void ObtainVisibleChunks()
    {
        Camera cam = Camera.main;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        Meshes meshToCheck = LODSMeshes[numLODS - 1];

        visibleTiles.Clear();

        for (int i = 0; i < numOfChunks; i++) //Iterem cada CHUNK del LOD més baix
        {
            for (int j = 0; j < numOfChunks; j++)
            {
                Bounds bounds = meshToCheck.GetMesh(i, j).GetComponent<Renderer>().bounds;
                if (GeometryUtility.TestPlanesAABB(planes, bounds)) //Si el CHUNK està al frustum, calculem LOD corresponent i afegim a chunks visibles
                {
                    visibleTiles.Add(new Vector3Int(i, j, AssignLOD(i, j)));
                }
            }
        }
    }
    private void CreateChunkComputeShader(int chunkCoordX, int chunkCoordY, int LOD)
    {
        int chunkSizeLOD = chunkSize / (int)(Mathf.Pow(2, LOD)) + 1;

        Vector4 origin = new Vector4((chunkSize * chunkCoordX) * meterPerPixel, 0, (chunkSize * chunkCoordY) * meterPerPixel, 1); //Origen del CHUNK
        float offsetX = Mathf.Pow(2, LOD); //Distancia entre 2 vertices en funcion del numero de vertices total del CHUNK
        float offsetZ = offsetX;

        ComputeBuffer vertexs = new ComputeBuffer(chunkSizeLOD * chunkSizeLOD, sizeof(float) * 3);
        ComputeBuffer triangles = new ComputeBuffer((chunkSizeLOD-1) * (chunkSizeLOD-1) * 6, sizeof(int));
        ComputeBuffer uv = new ComputeBuffer(chunkSizeLOD * chunkSizeLOD, sizeof(float) * 2);

        //Set parameters
        terrainComputeShader.SetBuffer(0, "vertexs", vertexs);
        terrainComputeShader.SetBuffer(0, "triangles", triangles);
        terrainComputeShader.SetBuffer(0, "uv", uv);

        terrainComputeShader.SetInt("chunkCoordX", chunkCoordX);
        terrainComputeShader.SetInt("chunkCoordY", chunkCoordY);
        terrainComputeShader.SetInt("LOD", LOD);
        terrainComputeShader.SetFloat("offsetX", offsetX);
        terrainComputeShader.SetFloat("offsetZ", offsetZ);
        terrainComputeShader.SetInt("meterPerPixel", meterPerPixel);
        terrainComputeShader.SetInt("chunkSizeLOD", chunkSizeLOD);
        terrainComputeShader.SetVector("origin", origin);
        terrainComputeShader.SetFloat("heightScale", heightScale);
        terrainComputeShader.SetInt("heightMapSize", heightMap.width/(LOD+1));
        terrainComputeShader.SetTexture(0, "heightMap", heightMapRT, LOD);

        //Dispatch shader
        terrainComputeShader.Dispatch(0, chunkSizeLOD / 8 + 1, chunkSizeLOD / 8 + 1, 1);

        //Retrieve data
        Vector3[] finalVertexs = new Vector3[chunkSizeLOD * chunkSizeLOD];
        int[] finalTris = new int[(chunkSizeLOD - 1) * (chunkSizeLOD - 1) * 6];
        Vector2[] finalUVS = new Vector2[chunkSizeLOD * chunkSizeLOD];

        vertexs.GetData(finalVertexs);
        triangles.GetData(finalTris);
        uv.GetData(finalUVS);

        InstantiateChunk(chunkCoordX, chunkCoordY, LOD, finalVertexs, finalTris, finalUVS);
        LODSMeshes[LOD].SetMeshStatus(chunkCoordX, chunkCoordY, false);

        vertexs.Release();
        triangles.Release();
        uv.Release();
    }

    private void InitializeRenderTexture()
    {
        heightMapRT = new RenderTexture(heightMap.width, heightMap.width, 0, RenderTextureFormat.R16);
        heightMapRT.useMipMap = true;
        heightMapRT.autoGenerateMips = false;
        heightMapRT.enableRandomWrite = true;
        RenderTexture.active = heightMapRT;

        Graphics.Blit(heightMap, heightMapRT);
        heightMapRT.GenerateMips();
        heightMapRT.Create();
    }

    private void InstantiateChunk(int i, int j, int LOD, Vector3[] vertexs, int[] triangles, Vector2[] uvs)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertexs;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        GameObject meshObject = new GameObject("Mesh_" + i.ToString() + ", " + j.ToString() + ", LOD: " + LOD);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        Material mat = new Material(Shader.Find("TextureShader"));

        TerrainTextureManager ttm = GetComponent<TerrainTextureManager>();
        meshObject.isStatic = true;

        int numOfLandCovers = ttm.GetTexturesArray().Length;

        mat.SetTexture("_MainTex", landCovers[i][j]); //LandCoverArray
        mat.SetTexture("_TexturesArray", textures); //TexturesArray
        mat.SetTexture("_NormalMaps", normalMaps); //NormalMapsArray
        mat.SetFloatArray("_Tags", ttm.GetTagArray());
        mat.SetFloatArray("hasNormals", ttm.GetHasNormalsArray());


        mat.SetInt("_NumOfLandCovers", numOfLandCovers);
        mat.SetInt("_NumOfChunks", numOfChunks);
        mat.SetInt("_ChunkXCoord", i);
        mat.SetInt("_ChunkYCoord", j);
        mat.SetVector("FogColor", fogColor);
        mat.SetFloat("FogDensity", fogDensity);
        mat.SetInt("_NumOfPathLandCovers", 0);

        Texture2D distText = ttm.GetDistortionTexture();
        if(distText != null)
        {
            mat.SetTexture("distortionTex", distText);
            mat.SetFloat("distortionRegions", ttm.GetNumberOfDifferentTextureRotations());
        }
        else 
        {
            mat.SetFloat("distortionRegions", 0);
        }


        Material skybox = RenderSettings.skybox;
        Cubemap skyboxCube = skybox.GetTexture("_Tex") as Cubemap;
        mat.SetTexture("SkyBox", skyboxCube);

        mat.SetInt("typeOfWaterAnimation", ttm.GetTypeOfWaterAnimation());
        mat.SetVector("windDir", ttm.GetWindDirection());
        mat.SetFloat("windStrength", ttm.GetWindStrength());

        meshRenderer.material = mat;

        LODSMeshes[LOD].AddMesh(i, j, meshObject);
    }


    public int GetMapSize()
    {
        return heightMap.width;
    }

    public int GetChunkSize()
    {
        return chunkSize;
    }

    public int GetNumOfChunks()
    {
        return numOfChunks;
    }

    public int GetMetersPerPixel()
    {
        return meterPerPixel;
    }

    public List<Meshes> GetMeshes()
    {
        return this.LODSMeshes;
    }

    public Vector3 GetPlayerPos()
    {
        return character.transform.position;
    }

    public Vector2Int GetPlayerTile()
    {
        return actualTile;
    }

    public List<Vector3Int> GetVisibleTiles()
    {
        return visibleTiles;
    }

    public int GetGeneralSize()
    {
        return generalSize;
    }

    public float GetHeightScale()
    {
        return heightScale;
    }

    public Texture2D GetHeightMap()
    {
        return heightMap;
    }

    public Texture2DArray GetLandCoverAtChunk(int chunkX, int chunkY)
    {
        return landCovers[chunkX][chunkY];
    }

    public Texture2DArray GetTrailLandCoversAtChunk(int chunkX, int chunkY)
    {
        return trailLandCovers[chunkX][chunkY];
    }

    public RenderTexture GetHeightMapRT()
    {
        return heightMapRT;
    }

    public int GetNumOfContributions()
    {
        return landCovers[0][0].depth;
    }

    public float GetFogDensity()
    {
        return fogDensity;
    }

    public Color GetFogColor()
    {
        return fogColor;
    }


    private void OnDestroy()
    {
        if (LODSMeshes != null)
        {
            foreach (var lodMesh in LODSMeshes)
            {
                lodMesh.Dispose();
            }
            LODSMeshes.Clear();
            LODSMeshes = null;
        }

        if (visibleTiles != null)
        {
            visibleTiles.Clear();
            visibleTiles = null;
        }

        if (textures != null)
        {
            Destroy(textures);
            textures = null;
        }

        if (landCovers != null)
        {
            for (int chunkX = 0; chunkX < landCovers.Length; ++chunkX)
            {
                for (int chunkY = 0; chunkY < landCovers.Length; ++chunkY)
                {
                    Destroy(landCovers[chunkX][chunkY]);
                    landCovers[chunkX][chunkY] = null;
                }
            }
            landCovers = null;
        }

        if (trailLandCovers != null)
        {
            for (int chunkX = 0; chunkX < trailLandCovers.Length; ++chunkX)
            {
                for (int chunkY = 0; chunkY < trailLandCovers.Length; ++chunkY)
                {
                    Destroy(trailLandCovers[chunkX][chunkY]);
                    trailLandCovers[chunkX][chunkY] = null;
                }
            }
            trailLandCovers = null;
        }

        if (normalMaps != null)
        {
            Destroy(normalMaps);
            normalMaps = null;
        }
    }

}