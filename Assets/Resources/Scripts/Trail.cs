using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;

public class Trail
{
    private int chunkSize;
    private int metersPerPixel;
    private int heightMapSize;
    private int generalSize;

    public Trail(List<Vector2> controlPoints, int generalSize, int numChunks, int metersPerPixel, float amplitude, int heightMapSize, int indexTexture, int contributionSize, int uvTextSize, int primarySteps, int typeOfTrail) //Control points are in worldSpace coords
    {
        this.metersPerPixel = metersPerPixel;
        this.chunkSize = heightMapSize / numChunks;
        this.heightMapSize = heightMapSize;
        this.generalSize = generalSize;

        amplitude = amplitude * (heightMapSize * metersPerPixel) / generalSize;
        CreateSpline(controlPoints, amplitude, numChunks, indexTexture, contributionSize, uvTextSize, primarySteps, typeOfTrail);
    }

    private void CreateSpline(List<Vector2> controlPoints, float amplitude, int numChunks, int indexTexture, int contributionSize, int uvTextSize, int primarySteps, int typeOfTrail)
    {
        for (int i = 0, numBezier = 0; i < controlPoints.Count; i+=3, numBezier++)
        {
            if (i + 3 < controlPoints.Count)
            {
                if (Utils.PointIsInBounds(controlPoints[i], generalSize) && Utils.PointIsInBounds(controlPoints[i+1], generalSize) && Utils.PointIsInBounds(controlPoints[i + 2], generalSize) && Utils.PointIsInBounds(controlPoints[i + 3], generalSize))
                {
                    List<Vector2> points = new List<Vector2> { controlPoints[i], controlPoints[i+1], controlPoints[i+2], controlPoints[i+3] };
                    CreateCubicBezier(points, amplitude, numChunks, indexTexture, contributionSize, uvTextSize, primarySteps, typeOfTrail);
                }
            }
        }
    }

    private void CreateCubicBezier(List<Vector2> points, float amplitude, int numChunks, int indexTexture, int contributionSize, int uvTextSize, int primarySteps, int typeOfTrail)
    {
        float scalePointText = contributionSize / ((float)generalSize / numChunks);
        float worldScaleFactor = heightMapSize * metersPerPixel / (float) generalSize;
        float totalDistanceBetweenPoints = Utils.TotalDistanceBetweenPoints(points);
        float resolution = CalculateResolution(totalDistanceBetweenPoints, scalePointText);

        if (typeOfTrail == 2) resolution = (float)1/primarySteps;

        float actualT = 0;
        while (actualT <= 1.0) {
            Vector2 p0 = Vector2.Lerp(points[0], points[1], actualT);
            Vector2 p1 = Vector2.Lerp(points[1], points[2], actualT);
            Vector2 p2 = Vector2.Lerp(points[2], points[3], actualT);
            Vector2 p3 = Vector2.Lerp(p0, p1, actualT);
            Vector2 p4 = Vector2.Lerp(p1, p2, actualT);
            Vector2 point = Vector2.Lerp(p3, p4, actualT);

            Vector2 tangent = (p4-p3).normalized; //Direcció de la velocitat del punt (primera derivada)
            Vector2 normal = new Vector2(tangent.y, -tangent.x); 

            //Agafem els 2 punts dels extrems
            Vector2 ampPoint = point + normal * amplitude / 2;
            Vector2 negAmpPoint = point - normal * amplitude / 2;

            Vector2 ampPointText = ampPoint * scalePointText;
            Vector2 negAmpPointText = negAmpPoint * scalePointText;

            Vector2 dir = (ampPointText - negAmpPointText).normalized;
            int numIts = Mathf.RoundToInt(amplitude * scalePointText);

            for (int i = 0; i < numIts; ++i)
            {
                Vector2 pix = negAmpPointText + dir * i;
                Vector2Int chunks = Utils.PixelToChunkCoord(pix, contributionSize);
                if (Utils.ChunkIsInBounds(chunks.x, chunks.y, numChunks))
                {
                    Texture2D cont = Utils.GetPathContribution(indexTexture, chunks.x, chunks.y, typeOfTrail);

                    pix.x = pix.x % contributionSize;
                    pix.y = pix.y % contributionSize;

                    Utils.SetWhitePixelFadedGivenAVertex(cont, pix, (i + 1) / (float)(numIts + 1));
                }
            }

            //Another way of doing it
            //if(typeOfTrail == 2)
            //{
            //    float scalePointText2 = uvTextSize / ((float)generalSize / numChunks);
            //    ampPointText = ampPoint * scalePointText2;
            //    negAmpPointText = negAmpPoint * scalePointText2;

            //    dir = (ampPointText - negAmpPointText).normalized;
            //    numIts = Mathf.RoundToInt(amplitude * scalePointText2);

            //    for (int i = 0; i < numIts; ++i)
            //    {
            //        Vector2 pix = negAmpPointText + dir * i;
            //        Vector2Int chunks = Utils.PixelToChunkCoord(pix, uvTextSize);
            //        if (Utils.ChunkIsInBounds(chunks.x, chunks.y, numChunks))
            //        {
            //            Texture2D primaryUVS = Utils.GetPrimaryUVsTexture(chunks.x, chunks.y);

            //            pix.x = pix.x % uvTextSize;
            //            pix.y = pix.y % uvTextSize;

            //            Utils.SetUVSToTextureGivenAVertex(primaryUVS, pix, i / (float)(numIts - 1), (actualT * totalDistanceBetweenPoints / 20) % 1);
            //        }
            //    }
            //}

            //Interpolem els 2 punts dels extrems per trobar tots els punts de l'amplitud          
            if (typeOfTrail == 2)
            {
                float scalePointText2 = uvTextSize / ((float)generalSize / numChunks);
                int numIterations = Mathf.RoundToInt(100 * scalePointText2);
                for (int i = 0; i <= numIterations; i++)
                {
                    float actualT2 = i / (float)numIterations;
                    Vector2 p = Vector2.Lerp(ampPoint, negAmpPoint, actualT2);
                    Vector2Int chunks = Utils.WorldToChunkCoord(p * worldScaleFactor, chunkSize, metersPerPixel);
                    if (Utils.ChunkIsInBounds(chunks, numChunks))
                    {
                        Vector2 pointText = new Vector2((p.x * scalePointText2) % (uvTextSize), (p.y * scalePointText2) % (uvTextSize));
                        Texture2D primaryUVS = Utils.GetPrimaryUVsTexture(chunks.x, chunks.y);
                        Utils.SetUVSToTextureGivenAVertex(primaryUVS, pointText, actualT2, (actualT * totalDistanceBetweenPoints / 20) % 1);
                    }
                }
            }
            actualT += resolution;
        }
    }

    private float CalculateResolution(float distance, float relativeContributionSize)
    {
        return 1f/(distance * relativeContributionSize * 1.5f);
    }
}
