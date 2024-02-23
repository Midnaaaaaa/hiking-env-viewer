using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

    [SerializeField] Texture2D heightMap;
    [SerializeField] Texture2D landCover;
    [SerializeField] int meterPerPixel = 2;
    [SerializeField] GameObject Map;
    [SerializeField] int heightScale = 500;
    [SerializeField] GameObject grass;
    //private int lastMeterPerPixel;

    private Vector3[] vertexs;
    private Vector2[] uv;
    private int[] triangles;
    private Color[] vertexColors;

    private ComputeBuffer grassPositionsBuffer;

    public ComputeShader grassRenderer;



    private Mesh mesh;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        Map.GetComponent<MeshFilter>().mesh = mesh;
        //lastMeterPerPixel = meterPerPixel;


        grassPositionsBuffer = new ComputeBuffer(heightMap.width * heightMap.height, sizeof(float) * 3);

        grassRenderer.SetInt("_MeterPerPixel", meterPerPixel);
        grassRenderer.SetBuffer(0, "_GrassPositionsBuffer", grassPositionsBuffer);
        grassRenderer.SetTexture(0, "_HeightMap", heightMap);
        grassRenderer.Dispatch(0, Mathf.CeilToInt(heightMap.width / 8.0f), Mathf.CeilToInt(heightMap.height / 8.0f), 1);
    }

    void AdaptMesh()
    {
        Debug.Log(vertexs.Length);
        mesh.vertices = vertexs;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.colors = vertexColors;
    }
    
    void GeneratePlane()
    {
        int width = heightMap.width;
        int height = heightMap.height;
        Debug.Log(width);
        Debug.Log(height);

        vertexs = new Vector3[(width) * (height)];
        uv = new Vector2[(width) * (height)];
        triangles = new int[(width - 1) * (height-1) * 6];
        vertexColors = new Color[width * height];

        Color[] pixelData = heightMap.GetPixels();
        Debug.Log(pixelData.Length);

        for (int y = 0, i = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++, i++)
            {
                vertexColors[i] = landCover.GetPixel(x, y);
                vertexs[i] = new Vector3(x * meterPerPixel, pixelData[i].r * heightScale, y * meterPerPixel);
                if (i == 0) Debug.Log(vertexs[i]);
                uv[i] = new Vector2((float)x / (width), (float)y / (height));

                if(y != height - 1 && x != width - 1)
                {
                    int offset = i;
                    int triangleOffset = 6 * (y * (width-1) + x);
                    triangles[triangleOffset + 0] = offset;
                    triangles[triangleOffset + 1] = offset + width;
                    triangles[triangleOffset + 2] = offset + width + 1;

                    triangles[triangleOffset + 3] = offset;
                    triangles[triangleOffset + 4] = offset + width + 1;
                    triangles[triangleOffset + 5] = offset + 1;

                }
            }
        }
    }

    private void Start()
    {
        GeneratePlane();
        AdaptMesh();
        //AddGrass();
        //CreateGPUBuffers();
    }

    void AddGrass()
    {
        Mesh grassMesh = grass.GetComponent<MeshFilter>().sharedMesh;
        Material grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
        grassMaterial.SetBuffer("positionBuffer", grassPositionsBuffer);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, grassMesh.bounds, grassPositionsBuffer.count);
    }



    void Update()
    {
        /*
        if(meterPerPixel != lastMeterPerPixel)
        {
            GeneratePlane();
            AdaptMesh();
            lastMeterPerPixel = meterPerPixel;
        }
        */

        //AddGrass();
    }

}
