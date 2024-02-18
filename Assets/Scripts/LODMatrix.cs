using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODMatrix
{
    private List<List<List<Vector3>>> matrixVertices;
    private List<List<List<int>>> matrixTriangles;
    private List<List<List<Vector2>>> matrixUV;
    private int chunkSize;
    private int heightMapSize;


    public LODMatrix(int chunkSize, int heightMapSize)
    {
        this.chunkSize = chunkSize;
        this.heightMapSize = heightMapSize;


        matrixVertices = new List<List<List<Vector3>>>();
        matrixTriangles = new List<List<List<int>>>();
        matrixUV = new List<List<List<Vector2>>>();

        int matrixSize = heightMapSize / chunkSize;

        for (int i = 0; i < matrixSize; ++i) //Initialize vertex matrix
        {
            matrixVertices.Add(new List<List<Vector3>>());
            for (int j = 0; j < matrixSize; ++j)
            {
                matrixVertices[i].Add(new List<Vector3>());
            }
        }

        for (int i = 0; i < matrixSize; ++i) //Initialize triangle matrix
        {
            matrixTriangles.Add(new List<List<int>>());
            for (int j = 0; j < matrixSize; ++j)
            {
                matrixTriangles[i].Add(new List<int>());
            }
        }

        for (int i = 0; i < matrixSize; ++i) //Initialize uv matrix
        {
            matrixUV.Add(new List<List<Vector2>>());
            for (int j = 0; j < matrixSize; ++j)
            {
                matrixUV[i].Add(new List<Vector2>());
            }
        }
    }


    public int GetChunkSize()
    {
        return chunkSize;
    }



    public void AddChunkVertices(List<Vector3> chunk, int indexX, int indexY)
    {
        matrixVertices[indexX][indexY] = chunk;
    }

    public void AddChunkTriangles(List<int> chunk, int indexX, int indexY)
    {
        matrixTriangles[indexX][indexY] = chunk;
    }

    public void AddChunkUV(List<Vector2> chunk, int indexX, int indexY)
    {
        matrixUV[indexX][indexY] = chunk;
    }


    public List<int> GetChunkTriangles(int indexX, int indexY)
    {
        return matrixTriangles[indexX][indexY];
    }

    public List<Vector2> GetChunkUV(int indexX, int indexY)
    {
        return matrixUV[indexX][indexY];
    }


    public List<Vector3> GetChunkVertices(int indexX, int indexY)
    {
        return matrixVertices[indexX][indexY];
    }

    public bool isCreated(int indexX, int indexY)
    {
        if (matrixVertices[indexX][indexY].Count == 0) return false;
        return true;
    }

    public bool isInside(int indexX, int indexY)
    {
        return indexX < matrixVertices.Count && indexY < matrixVertices.Count && indexX >= 0 && indexY >= 0;
    }
}
