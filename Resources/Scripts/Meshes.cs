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

    public GameObject GetMesh(int i, int j)
    {
        return meshes[i][j];
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


    public void Dispose()
    {
        if (meshes != null)
        {
            foreach (var list in meshes)
            {
                foreach (var gameObject in list)
                {
                    if (gameObject != null)
                    {
                        GameObject.Destroy(gameObject);
                    }
                }
                list.Clear();
            }
            meshes.Clear();
            meshes = null;
        }
    }


}
