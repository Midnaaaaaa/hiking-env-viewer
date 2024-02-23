using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MapGen2 : MonoBehaviour
{
    [SerializeField] Texture2D heightMap;
    [SerializeField] int numOfChunks = 4; //32


    private List<LODMatrix> LODSMatrixList;

    public int numLODS = 3;

    public int heightScale = 500;

    private int chunkSize;
    private Vector2Int actualTile;
    private Vector2Int lastActualTile;

    private List<List<GameObject>> meshes;

    private List<Meshes> LODSMeshes;


    private List<Vector3Int> surroundingTiles;

    public int meterPerPixel = 2;

    [SerializeField] GameObject character;

    private void Awake()
    {
        chunkSize = heightMap.width / numOfChunks;

        InitializeLODSMatrixList();
        InitializeLODSMeshes();

        surroundingTiles = new List<Vector3Int>{ //Z == 1 -> LOD1, Z == 2 -> LOD2 ...

            new Vector3Int(0,  0, 0),  // Centro 
            new Vector3Int(-1, 0, 3),  // Izquierda
            new Vector3Int(1,  0, 3),  // Derecha
            new Vector3Int(0,  1, 3),  // Arriba
            new Vector3Int(0, -1, 3),  // Abajo
            new Vector3Int(-1, 1, 3),  // Arriba izquierda
            new Vector3Int(1,  1, 3),  // Arriba derecha
            new Vector3Int(-1,-1, 3),  // Abajo izquierda
            new Vector3Int(1, -1, 3)   // Abajo derecha
        };
    }

    private void InitializeLODSMeshes()
    {
        LODSMeshes = new List<Meshes>();
        for (int i = 0; i < numLODS; i++)
        {
            LODSMeshes.Add(new Meshes(numOfChunks));
        }
    }


    private void InitializeLODSMatrixList()
    {
        LODSMatrixList = new List<LODMatrix>();
        
        for (int i = 0; i < numLODS; i++)
        {
            LODSMatrixList.Add(new LODMatrix(chunkSize / (int)(Mathf.Pow(2,i)), i == 0 ? heightMap.width : heightMap.width / (int)(Mathf.Pow(2, i))));
        }
    }

    
    private Vector2Int WorldToGridPosition(Vector3 position) {
        Vector2Int gridPosition = new Vector2Int(Mathf.FloorToInt(position.x / (chunkSize * meterPerPixel)), Mathf.FloorToInt((position.z / (chunkSize * meterPerPixel))));
        if (!LODSMatrixList[0].isInside(gridPosition.x, gridPosition.y))
        {
            return lastActualTile;
        }
        return gridPosition;
    }
    


    private void SetStartingPosition()
    {
        character.transform.position = new Vector3(transform.position.x + 4, transform.position.y + 1000, transform.position.z + 4);
        actualTile = WorldToGridPosition(character.transform.position);
        lastActualTile = actualTile;
    }

    private void CreateAllChunks()
    {
        for (int i = 0; i < numOfChunks; i++)
        {
            for (int j = 0; j < numOfChunks; j++)
            {
                for (int k = 0; k < numLODS; k++)
                {
                    CreateChunk(i, j, k);
                }
            }
        }
    }

    private void InstantiateAllChunks()
    {
        for (int i = 0; i < numOfChunks; i++)
        {
            for (int j = 0; j < numOfChunks; j++)
            {
                for (int LOD = 0; LOD < numLODS; LOD++)
                {
                    InstantiateChunk(i, j, LOD);
                    LODSMeshes[LOD].SetMeshStatus(i, j, false);
                }
            }
        }
    }

    private void ActivateFirstChunks()
    {
        foreach (var chunk in surroundingTiles) //Activate first CHUNKS
        {
            int LOD = chunk.z;
            int xCoord = chunk.x + actualTile.x;
            int yCoord = chunk.y + actualTile.y;
            if (LODSMatrixList[LOD].isInside(xCoord, yCoord))
            {
                LODSMeshes[LOD].SetMeshStatus(xCoord, yCoord, true);
            }
        }

    }


    private void Start()
    {
        SetStartingPosition();
        CreateAllChunks();
        InstantiateAllChunks();
        ActivateFirstChunks();


        lastActualTile = actualTile;
    }

    private void Update()
    {
        actualTile = WorldToGridPosition(character.transform.position);

        if(lastActualTile != actualTile) //Ha cambiado de CHUNK
        {
            foreach (var chunk in surroundingTiles) //Desactivar CHUNKS antiguos
            {
                int xCoord = chunk.x + lastActualTile.x;
                int yCoord = chunk.y + lastActualTile.y;
                for (int LOD = 0; LOD < numLODS; LOD++)
                {
                    if (LODSMatrixList[LOD].isInside(xCoord, yCoord))
                    {
                        LODSMeshes[LOD].SetMeshStatus(xCoord, yCoord, false);
                    }
                }
            }

            foreach (var chunk in surroundingTiles) //Activamos los CHUNKS nuevos
            {
                int LOD = chunk.z;
                int xCoord = chunk.x + actualTile.x;
                int yCoord = chunk.y + actualTile.y;
                if (LODSMatrixList[LOD].isInside(xCoord, yCoord))
                {
                    LODSMeshes[LOD].SetMeshStatus(xCoord, yCoord, true);
                }
            }
            lastActualTile = actualTile;
        }
    }

    private void CreateChunk(int chunkCoordX, int chunkCoordY, int LOD)
    {
        List<Vector3> vertexs = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        int numPixels = LOD == 0 ? heightMap.width : heightMap.width / (2 * LOD); //NumPixelsOfHeightMapLOD

        int chunkSizeLOD = LODSMatrixList[LOD].GetChunkSize() + 1;

        Vector3 origin = new Vector3((chunkSize * chunkCoordX) * meterPerPixel, 0, (chunkSize * chunkCoordY) * meterPerPixel); //Origen del CHUNK
        float offsetX = Mathf.Pow(2, LOD); //Distancia entre 2 vertices en funcion del numero de vertices total del CHUNK
        float offsetZ = offsetX; 

        for (int i = 0, index = 0; i < chunkSizeLOD; i++)
        {
            for (int j = 0; j < chunkSizeLOD; j++, ++index)
            {
                int mappedXToTexture = (int)(i + chunkCoordX * (chunkSizeLOD - 1));
                int mappedYToTexture = (int)(j + chunkCoordY * (chunkSizeLOD - 1));

                vertexs.Add(origin + new Vector3((i * offsetX) * meterPerPixel, heightMap.GetPixel(mappedXToTexture, mappedYToTexture, LOD).r * heightScale, (j * offsetZ) * meterPerPixel));
                uv.Add(origin + new Vector3((i * offsetX) / chunkSizeLOD, (j * offsetZ) / chunkSizeLOD));

                if (j != chunkSizeLOD - 1 && i != chunkSizeLOD - 1)
                {
                    int offset = i * chunkSizeLOD + j;

                    triangles.Add(offset);
                    triangles.Add(offset + chunkSizeLOD + 1);
                    triangles.Add(offset + chunkSizeLOD);

                    triangles.Add(offset);
                    triangles.Add(offset + 1);
                    triangles.Add(offset + chunkSizeLOD + 1);
                }
            }
        }

        LODSMatrixList[LOD].AddChunkVertices(vertexs, chunkCoordX, chunkCoordY);
        LODSMatrixList[LOD].AddChunkTriangles(triangles, chunkCoordX, chunkCoordY);
        LODSMatrixList[LOD].AddChunkUV(uv, chunkCoordX, chunkCoordY);
    }

    private void InstantiateChunk(int i, int j, int LOD)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] list = LODSMatrixList[LOD].GetChunkVertices(i, j).ToArray();

        mesh.vertices = LODSMatrixList[LOD].GetChunkVertices(i, j).ToArray();
        mesh.triangles = LODSMatrixList[LOD].GetChunkTriangles(i, j).ToArray();
        mesh.uv = LODSMatrixList[LOD].GetChunkUV(i, j).ToArray();

        GameObject meshObject = new GameObject("Mesh_" + i.ToString() + ", " + j.ToString() + ", LOD: " + LOD);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Standard"));

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








}
