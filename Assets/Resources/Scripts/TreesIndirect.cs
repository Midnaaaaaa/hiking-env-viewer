using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreesIndirect : MonoBehaviour
{
    private List<Meshes> LODMeshes;
    public Mesh treesMesh;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer textureProbabilitiesBuffer;
    private ComputeBuffer landCoverProbabilitiesBuffer;
    List<List<Vector3[]>> meshPropertiesList;
    private Vector2Int actualPlayerTile;
    private Bounds bounds;
    public ComputeShader computeShader;
    private int numOfChunks;


    public List<ComputeBuffer> meshPropertiesBuffers;
    public List<ComputeBuffer> modelTransformBuffers;
    public List<ComputeBuffer> typeOfTextureBuffers;
    public List<ComputeBuffer> treesHeightBuffers;
    public List<ComputeBuffer> treesCanopyBuffers;

    [SerializeField] int textureSizeWidth = 512;
    [SerializeField] int textureSizeHeight = 1024;

    private Texture2DArray textures;
    private Texture2DArray normalMaps;
    private Texture2DArray landcovers;

    [SerializeField] Texture2D[] texturesArray;
    [SerializeField] Texture2D[] normalMapsArray;
    public float[] textureProbabilities; // Texture probabilities must sum up to 100
    [SerializeField] Texture2D[] landCoversArray;

    [SerializeField] float[] landCoverProbabilities;

    private int metersPerPixel;
    private int chunkSizeVertex;
    private int length;
    private float density;

    private int usingJsonPlants;

    private Shader propShader;
    private PropPositionManager ptm;
    private float modelHighestPoint;
    private float modelWidestPoint;
    private float offset;

    private Color fogColor;
    private float fogDensity;

    void Start()
    {
        LODMeshes = GetComponent<TerrainGenerator>().GetMeshes();
        bounds = new Bounds(transform.position, Vector3.one * 100000);
        numOfChunks = gameObject.GetComponent<TerrainGenerator>().GetNumOfChunks();
        meshPropertiesBuffers = new List<ComputeBuffer>();
        modelTransformBuffers = new List<ComputeBuffer>();
        typeOfTextureBuffers = new List<ComputeBuffer>();
        treesHeightBuffers = new List<ComputeBuffer>();
        treesCanopyBuffers = new List<ComputeBuffer>();
        density = GetComponent<PropPositionManager>().GetDensity();
        ptm = GetComponent<PropPositionManager>();
        modelHighestPoint = treesMesh.bounds.max.y;
        modelWidestPoint = treesMesh.bounds.max.x;

        metersPerPixel = GetComponent<TerrainGenerator>().GetMetersPerPixel();
        chunkSizeVertex = GetComponent<TerrainGenerator>().GetChunkSize();
        length = chunkSizeVertex * metersPerPixel;
        usingJsonPlants = GetComponent<PropPositionManager>().HasPlantsJson() ? 1 : 0;
        offset = GetComponent<TerrainTextureManager>().GetLandCoverSize() / ((float)density * chunkSizeVertex);
        fogColor = GetComponent<TerrainGenerator>().GetFogColor();
        fogDensity = GetComponent<TerrainGenerator>().GetFogDensity();

        //InitializeMeshPropertiesList();
        meshPropertiesList = GetComponent<PropPositionManager>().GetTreesMeshProperties();

        propShader = Shader.Find("TreeShader");

        InitializeArgsBuffer();

        InitializeTextureProbabilitiesBuffer();
        InitializeLandCoverProbabilitiesBuffer();

        InitializeTextureArrays();

    }

    // Update is called once per frame
    void Update()
    {
        foreach (var buffer in meshPropertiesBuffers)
        {
            buffer.Release();
        }
        foreach (var buffer in modelTransformBuffers)
        {
            buffer.Release();
        }
        foreach (var buffer in typeOfTextureBuffers)
        {
            buffer.Release();
        }
        foreach(var buffer in treesHeightBuffers)
        {
            buffer?.Release();
        }
        foreach (var buffer in treesCanopyBuffers)
        {
            buffer?.Release();
        }

        meshPropertiesBuffers.Clear();
        modelTransformBuffers.Clear();
        typeOfTextureBuffers.Clear();
        treesHeightBuffers.Clear();
        treesCanopyBuffers.Clear();


        actualPlayerTile = gameObject.GetComponent<TerrainGenerator>().GetPlayerTile();
        List<Vector2Int> neigbhourTiles = GetNeigbourVisibleTiles(gameObject.GetComponent<TerrainGenerator>().GetVisibleTiles(), actualPlayerTile);
        //List<Vector2Int> neigbhourTiles = GetVisibleTiles(GetComponent<TerrainGenerator>().GetVisibleTiles());


        Material[] mats = new Material[neigbhourTiles.Count];


        for (int i = 0; i < neigbhourTiles.Count; ++i)
        {
            Vector2Int tileActual = neigbhourTiles[i];

            int instanceCount = meshPropertiesList[tileActual.x][tileActual.y].Length;
            if (instanceCount > 0)
            {
                ComputeBuffer meshPropertiesBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 3);
                meshPropertiesBuffer.SetData(meshPropertiesList[tileActual.x][tileActual.y]);
                meshPropertiesBuffers.Add(meshPropertiesBuffer);

                ComputeBuffer modelTransformBuffer = new ComputeBuffer(instanceCount, 4 * 4 * sizeof(float));
                modelTransformBuffers.Add(modelTransformBuffer);

                ComputeBuffer typeOfTextureBuffer = new ComputeBuffer(instanceCount, sizeof(int));
                typeOfTextureBuffers.Add(typeOfTextureBuffer);

                float[] treeHeights = ptm.GetTreesHeightAtChunk(tileActual.x, tileActual.y);
                float[] treeCanopies = ptm.GetTreesCanopyAtChunk(tileActual.x, tileActual.y);
                ComputeBuffer treesHeightBuffer;
                ComputeBuffer treesCanopyBuffer;
                if (treeHeights.Length > 0)
                {
                    treesHeightBuffer = new ComputeBuffer(instanceCount, sizeof(float));
                    treesHeightBuffer.SetData(treeHeights);
                    treesCanopyBuffer = new ComputeBuffer(instanceCount, sizeof(float));
                    treesCanopyBuffer.SetData(treeCanopies);
                }
                else
                {
                    treesHeightBuffer = new ComputeBuffer(1, sizeof(float));
                    treesCanopyBuffer = new ComputeBuffer(1, sizeof(float));
                }
                treesHeightBuffers.Add(treesHeightBuffer);
                treesCanopyBuffers.Add(treesCanopyBuffer);


                computeShader.SetBuffer(0, "modelTransformBuffer", modelTransformBuffers[meshPropertiesBuffers.Count - 1]);
                computeShader.SetBuffer(0, "positionBuffer", meshPropertiesBuffers[meshPropertiesBuffers.Count - 1]);
                computeShader.SetBuffer(0, "typeOfTextureBuffer", typeOfTextureBuffers[meshPropertiesBuffers.Count - 1]);
                computeShader.SetBuffer(0, "plantHeight", treesHeightBuffers[meshPropertiesBuffers.Count - 1]);
                computeShader.SetBuffer(0, "plantCanopy", treesCanopyBuffers[meshPropertiesBuffers.Count - 1]);
                computeShader.SetBuffer(0, "textureProbabilities", textureProbabilitiesBuffer);

                computeShader.SetInt("numOfTextures", texturesArray.Length);
                computeShader.SetInt("instanceCount", instanceCount);
                computeShader.SetInt("plantHeightBufferLength", treeHeights.Length);
                computeShader.SetInt("plantCanopyBufferLength", treeCanopies.Length);
                computeShader.SetFloat("offsetSize", offset);
                computeShader.SetFloat("modelHighestPoint", modelHighestPoint);
                computeShader.SetFloat("modelWidestPoint", modelWidestPoint);

                computeShader.Dispatch(0, instanceCount / 64 + 1, 1, 1);
            }
        }
        int counter = 0;
        for (int i = 0; i < neigbhourTiles.Count; i++)
        {
            Vector2Int tileActual = neigbhourTiles[i];

            int instanceCount = meshPropertiesList[tileActual.x][tileActual.y].Length;

            if (instanceCount > 0)
            {
                Material mat = new Material(propShader);
                mat.SetBuffer("_Properties", meshPropertiesBuffers[counter]);
                mat.SetBuffer("_ModelTransformBuffer", modelTransformBuffers[counter]);
                mat.SetBuffer("_TypeOfTextureBuffer", typeOfTextureBuffers[counter]);
                mat.SetInt("_NumOfChunks", numOfChunks);
                mat.SetInt("_ChunkXCoord", neigbhourTiles[i].x);
                mat.SetInt("_ChunkYCoord", neigbhourTiles[i].y);
                mat.SetTexture("_TexturesArray", textures); //TexturesArray
                mat.SetTexture("_NormalMaps", normalMaps); //NormalMapsArray
                mat.SetTexture("_LandCovers", landcovers); //LandCoversArray
                mat.SetInt("_NumOfLandCovers", landcovers.depth);
                mat.SetInt("length", length);
                mat.SetInt("usingJsonTrees", usingJsonPlants);
                mat.SetBuffer("landCoverProbs", landCoverProbabilitiesBuffer);
                mat.SetFloat("_FogDensity", fogDensity);
                mat.SetVector("_FogColor", fogColor);

                mats[i] = mat;

                Graphics.DrawMeshInstancedIndirect(treesMesh, 0, mats[i], bounds, argsBuffer);
                counter++;
            }

        }
    }

    private void InitializeTextureArrays()
    {
        int landCoverSize = GetComponent<TerrainTextureManager>().GetLandCoverSizeNoChunks();
        textures = new Texture2DArray(textureSizeWidth, textureSizeHeight, texturesArray.Length, TextureFormat.RGBA32, true);
        normalMaps = new Texture2DArray(textureSizeWidth, textureSizeHeight, normalMapsArray.Length, TextureFormat.RGBA32, true);
        landcovers = new Texture2DArray(landCoverSize, landCoverSize, landCoversArray.Length, TextureFormat.R8, false);

        for (int i = 0; i < texturesArray.Length; i++) //Copiamos texturas al texture2darray de la GPU
        {
            Graphics.CopyTexture(texturesArray[i], 0, textures, i);
        }

        for (int i = 0; i < normalMapsArray.Length; i++)
        {
            Graphics.CopyTexture(normalMapsArray[i], 0, normalMaps, i);
        }

        for (int i = 0; i < landCoversArray.Length; i++)
        {
            Graphics.CopyTexture(landCoversArray[i], 0, landcovers, i);
        }

        landcovers.Apply();
        textures.Apply();
        normalMaps.Apply();
    }

    private List<Vector2Int> GetNeigbourVisibleTiles(List<Vector3Int> visibleTiles, Vector2Int actualPlayerTile)
    {
        HashSet<Vector2Int> neighbors = new HashSet<Vector2Int>
        {
            new Vector2Int(0,  0),  // Centro 
            new Vector2Int(-1, 0),  // Izquierda
            new Vector2Int(1,  0),  // Derecha
            new Vector2Int(0,  1),  // Arriba
            new Vector2Int(0, -1),  // Abajo
            new Vector2Int(-1, 1),  // Arriba izquierda
            new Vector2Int(1,  1),  // Arriba derecha
            new Vector2Int(-1,-1),  // Abajo izquierda
            new Vector2Int(1, -1)   // Abajo derecha
        };

        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var tile in visibleTiles)
        {
            if (neighbors.Contains(new Vector2Int(tile.x, tile.y) - actualPlayerTile))
            {
                result.Add(new Vector2Int(tile.x, tile.y));
            }
        }
        return result;
    }

    private List<Vector2Int> GetVisibleTiles(List<Vector3Int> visibleTiles)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int i = 0; i < visibleTiles.Count; i++)
        {
            result.Add(new Vector2Int(visibleTiles[i].x, visibleTiles[i].y));
        }
        return result;
    }


    //Inicialitza una Matriu de MeshProperties on cada element es correspon a un chunk del terreny.
    //L'element (i,j) de meshPropertiesList, té una llista de MeshProperties (posicio, uv) de cada vertex del chunk i,j
    //void InitializeMeshPropertiesList()
    //{
    //    meshPropertiesList = new List<List<MeshProperties[]>>();
    //    for (int i = 0; i < numOfChunks; i++)
    //    {
    //        List<MeshProperties[]> listProps = new List<MeshProperties[]>();
    //        for (int j = 0; j < numOfChunks; j++)
    //        {
    //            List<Vector3> vertexs = LODSMatrixList[0].GetChunkVertices(i, j);
    //            List<Vector2> uvs = LODSMatrixList[0].GetChunkUV(i, j);
    //            MeshProperties[] props = new MeshProperties[vertexs.Count];

    //            for (int k = 0; k < vertexs.Count; k++)
    //            {
    //                if (!Utils.isFromPath(vertexs[k], i, j))
    //                {
    //                    MeshProperties prop = new MeshProperties();
    //                    prop.position = vertexs[k];
    //                    prop.uvs = uvs[k];

    //                    props[k] = prop;
    //                }
    //            }
    //            listProps.Add(props);
    //        }
    //        meshPropertiesList.Add(listProps);
    //    }
    //}

    void InitializeArgsBuffer()
    {
        float density = GetComponent<PropPositionManager>().GetDensity();
        int instances = Mathf.RoundToInt(LODMeshes[0].GetMesh(0, 0).GetComponent<MeshFilter>().mesh.vertexCount * density * density);

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)treesMesh.GetIndexCount(0);
        args[1] = (uint)instances;
        args[2] = (uint)treesMesh.GetIndexStart(0);
        args[3] = (uint)treesMesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    void InitializeLandCoverProbabilitiesBuffer()
    {
        landCoverProbabilitiesBuffer = new ComputeBuffer(landCoverProbabilities.Length, sizeof(float));
        landCoverProbabilitiesBuffer.SetData(landCoverProbabilities);
    }
    void InitializeTextureProbabilitiesBuffer()
    {
        textureProbabilitiesBuffer = new ComputeBuffer(textureProbabilities.Length, sizeof(float));
        textureProbabilitiesBuffer.SetData(textureProbabilities);
    }
    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }

        if (textureProbabilitiesBuffer != null)
        {
            textureProbabilitiesBuffer.Release();
            textureProbabilitiesBuffer = null;
        }

        if (landCoverProbabilitiesBuffer != null)
        {
            landCoverProbabilitiesBuffer.Release();
            landCoverProbabilitiesBuffer = null;
        }

        if (LODMeshes != null)
        {
            foreach (var lodMesh in LODMeshes)
            {
                lodMesh.Dispose();
            }
            LODMeshes.Clear();
            LODMeshes = null;
        }

        if (meshPropertiesList != null)
        {
            meshPropertiesList.Clear();
            meshPropertiesList = null;
        }

        if (meshPropertiesBuffers != null)
        {
            foreach (var buffer in meshPropertiesBuffers)
            {
                buffer.Release();
            }
            meshPropertiesBuffers.Clear();
        }
        if (modelTransformBuffers != null)
        {
            foreach (var buffer in modelTransformBuffers)
            {
                buffer.Release();
            }
            modelTransformBuffers.Clear();
        }
        if (typeOfTextureBuffers != null)
        {
            foreach (var buffer in typeOfTextureBuffers)
            {
                buffer.Release();
            }
            typeOfTextureBuffers.Clear();
        }
        if(treesHeightBuffers != null)
        {
            foreach(var buffer in treesHeightBuffers)
            {
                buffer.Release();
            }
            treesHeightBuffers.Clear();
        }
        if (treesCanopyBuffers != null)
        {
            foreach (var buffer in treesCanopyBuffers)
            {
                buffer.Release();
            }
            treesCanopyBuffers.Clear();
        }
    }
}
