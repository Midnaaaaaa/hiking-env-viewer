using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meshes
{
    private List<List<GameObject>> meshes;

    public Meshes(int numOfChunks)
    {
        meshes = new List<List<GameObject>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            meshes.Add(new List<GameObject>());
            for (int j = 0; j < numOfChunks; j++)
            {
                meshes[i].Add(null);
            }
        }
    }

    public void SetMeshStatus(int i, int j, bool status)
    {
        meshes[i][j].SetActive(status);
    }

    public bool MeshCreated(int i, int j)
    {
        if (meshes[i][j] == null) return false;
        return true;
    }


    public void AddMesh(int i, int j, GameObject mesh)
    {
        meshes[i][j] = mesh;
    }



}
