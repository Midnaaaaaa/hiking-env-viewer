using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class PlantJSONParser
{
    public PlantJSONParser(TextAsset json, List<List<Vector3[]>> meshPropertiesTrees, List<List<Vector3[]>> meshPropertiesGrass, List<List<List<float>>> treesHeight, List<List<List<float>>> grassHeight, List<List<List<float>>> treesCanopy, List<List<List<float>>> grassCanopy, int metersPerPixel, int heightMapSize, int generalSize, int numOfChunks)
    {
        float scalePoint = heightMapSize * metersPerPixel / (float)generalSize;
        int chunkSize = heightMapSize / numOfChunks;
        List<List<List<Vector3>>> meshPropertiesTreesAux = new List<List<List<Vector3>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesTreesAux.Add(new List<List<Vector3>>());
            for (int j = 0; j < numOfChunks; j++)
            {
                meshPropertiesTreesAux[i].Add(new List<Vector3>());
            }
        }
        List<List<List<Vector3>>> meshPropertiesGrassAux = new List<List<List<Vector3>>>();
        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesGrassAux.Add(new List<List<Vector3>>());
            for (int j = 0; j < numOfChunks; j++)
            {
                meshPropertiesGrassAux[i].Add(new List<Vector3>());
            }
        }


        JSONClasses.Root root = JsonConvert.DeserializeObject<JSONClasses.Root>(json.text);
        foreach (var plant in root.plants)
        {
            Vector2 scaledPoint = new Vector2(plant.pos.x, plant.pos.z) * scalePoint;
            Vector2Int chunks = Utils.WorldToChunkCoord(scaledPoint, chunkSize, metersPerPixel);
            if (Utils.ChunkIsInBounds(chunks, numOfChunks))
            {
                if (root.pfts[plant.pft].isTree) //If prop is tree
                {
                    meshPropertiesTreesAux[chunks.x][chunks.y].Add(new Vector3(scaledPoint.x, plant.pos.y, scaledPoint.y));
                    treesHeight[chunks.x][chunks.y].Add(plant.height);
                    treesCanopy[chunks.x][chunks.y].Add(plant.canopy);
                }
                else 
                { 
                    meshPropertiesGrassAux[chunks.x][chunks.y].Add(new Vector3(scaledPoint.x, plant.pos.y, scaledPoint.y));
                    grassHeight[chunks.x][chunks.y].Add(plant.height);
                    grassCanopy[chunks.x][chunks.y].Add(plant.canopy);
                }
            }

        }

        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesTrees.Add(new List<Vector3[]>());
            for (int j = 0; j < numOfChunks; j++)
            {
                meshPropertiesTrees[i].Add(meshPropertiesTreesAux[i][j].ToArray());
            }
        }

        for (int i = 0; i < numOfChunks; i++)
        {
            meshPropertiesGrass.Add(new List<Vector3[]>());
            for (int j = 0; j < numOfChunks; j++)
            {
                meshPropertiesGrass[i].Add(meshPropertiesGrassAux[i][j].ToArray());
            }
        }
    }
}
