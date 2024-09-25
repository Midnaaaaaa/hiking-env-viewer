using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.VisualScripting;
using UnityEngine;

public class TrailCreator : MonoBehaviour
{
    public TextAsset trailsJson;
    private TrailJSONParser jsonParser;
    private JSONClasses.Trails parsedTrails;
    
    void Start()
    {
        TrailTextureManager ptm = GetComponent<TrailTextureManager>();
        int generalSize = GetComponent<TerrainGenerator>().GetGeneralSize();
        int metersPerPixel = GetComponent<TerrainGenerator>().GetMetersPerPixel();
        int numChunks = GetComponent<TerrainGenerator>().GetNumOfChunks();
        int heightMapSize = GetComponent<TerrainGenerator>().GetMapSize();

        jsonParser = new TrailJSONParser(trailsJson);
        parsedTrails = jsonParser.GetTrails();


        foreach (JSONClasses.Trail trail in parsedTrails.trails)
        {
            string typeOfTrail = trail.properties["highway"];
            int indexTexture;
            int sizeOfText;
            int uvTextSize;
            float amplitude;
            int primarySteps = ptm.GetPrimarySteps();

            switch (typeOfTrail)
            {
                case "path":
                    amplitude = GetPathAmplitude(trail.properties["sac_scale"]);
                    indexTexture = Random.Range(0, TrailTextureManager.GetPathTexturesLength() - 1);
                    sizeOfText = ptm.GetTrailContributionChunkSize();
                    uvTextSize = ptm.GetPrimaryUVChunkSize();
                    if(amplitude != 0) new Trail(trail.geometry.coordinates, generalSize, numChunks, metersPerPixel, amplitude, heightMapSize, indexTexture, sizeOfText, uvTextSize, primarySteps, 0);
                    break;

                case "track":
                    amplitude = Random.Range(2.5f, 4);
                    indexTexture = Random.Range(0, TrailTextureManager.GetTrackTexturesLength() - 1);
                    sizeOfText = ptm.GetTrailContributionChunkSize();
                    uvTextSize = ptm.GetPrimaryUVChunkSize();
                    new Trail(trail.geometry.coordinates, generalSize, numChunks, metersPerPixel, amplitude, heightMapSize, indexTexture, sizeOfText, uvTextSize, primarySteps, 1);
                    break;

                case "primary":
                    amplitude = 7.5f;
                    indexTexture = Random.Range(0, TrailTextureManager.GetPrimaryTexturesLength() - 1);
                    sizeOfText = ptm.GetTrailContributionChunkSize();
                    uvTextSize = ptm.GetPrimaryUVChunkSize();
                    new Trail(trail.geometry.coordinates, generalSize, numChunks, metersPerPixel, amplitude, heightMapSize, indexTexture, sizeOfText, uvTextSize, primarySteps, 2);
                    break;

                default:
                    amplitude = GetPathAmplitude(trail.properties["sac_scale"]);
                    indexTexture = Random.Range(0, TrailTextureManager.GetPathTexturesLength() - 1);
                    sizeOfText = ptm.GetTrailContributionChunkSize();
                    uvTextSize = ptm.GetPrimaryUVChunkSize();
                    if (amplitude != 0) new Trail(trail.geometry.coordinates, generalSize, numChunks, metersPerPixel, amplitude, heightMapSize, indexTexture, sizeOfText, uvTextSize, primarySteps, 0);
                    break;
            }      
        }

        for (int i = 0; i < numChunks; i++)
        {
            for (int j = 0; j < numChunks; j++)
            {
                TrailTextureManager.GetPrimaryUVsAtCoords(i, j).Apply();
            }
        }


        //Imprimir contribucions:
        //for (int i = 0; i < numChunks; i++)
        //{
        //    for (int j = 0; j < numChunks; j++)
        //    {

        //        byte[] bytes = TrailTextureManager.GetPrimaryUVsAtCoords(i, j).EncodeToPNG();
        //        var dirPath = Application.dataPath + "/Resources/primary" + i + "_" + j + ".png";
        //        System.IO.File.WriteAllBytes(dirPath, bytes);
        //    }
        //}


        //Imprimir contribucions:
        //for (int i = 0; i < numChunks; i++)
        //{
        //    for (int j = 0; j < numChunks; j++)
        //    {

        //        byte[] bytes = PathTextureManager.GetPathContributionWithIndexIAtCoords(0, i, j).EncodeToPNG();
        //        var dirPath = Application.dataPath + "/Resources/pathContribution" + i + "_" + j + ".png";
        //        System.IO.File.WriteAllBytes(dirPath, bytes);
        //    }
        //}

        //byte[] bytes2 = ptm.GetPathContributionAtI(0).EncodeToPNG();
        //var dirPath2 = Application.dataPath + "/Resources/path.png";
        //System.IO.File.WriteAllBytes(dirPath2, bytes2);

        //byte[] bytes3 = ptm.GetPrimaryContributionAtI(0).EncodeToPNG();
        //var dirPath3 = Application.dataPath + "/Resources/primary.png";
        //System.IO.File.WriteAllBytes(dirPath3, bytes3);
        ptm.UpdateArrays();
        ptm.UpdateTerrainMaterials();
    }
    
    private float GetPathAmplitude(string sac_scale)
    {
        switch (sac_scale)
        {
            case "hiking":
                return Random.Range(3.0f, 4.0f);

            case "mountain_hiking":
                return Random.Range(2f, 3f);

            case "demanding_mountain_hiking":
                return Random.Range(0.5f, 1);

            default:
                return 0;
        }
    }
}
