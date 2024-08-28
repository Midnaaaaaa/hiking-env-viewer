using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public static class Utils
{
    public static Vector2Int WorldToChunkCoord(Vector2 position, int chunkSize, int metersPerPixel)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / (chunkSize * metersPerPixel)), Mathf.FloorToInt((position.y / (chunkSize * metersPerPixel))));
    }

    public static Vector2Int PixelToChunkCoord(Vector2 pixel, int chunkSize)
    {
        return new Vector2Int(Mathf.FloorToInt(pixel.x / (chunkSize)), Mathf.FloorToInt((pixel.y / chunkSize)));
    }

    public static Texture2D CreateBlackTexture(int sizeOfText, TextureFormat tf)
    {
        RenderTexture renderTexture = new RenderTexture(sizeOfText, sizeOfText, 0);
        Graphics.Blit(Texture2D.blackTexture, renderTexture);

        Texture2D tex = new Texture2D(sizeOfText, sizeOfText, tf, false);
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, sizeOfText, sizeOfText), 0, 0);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        return tex;
    }

    public static void SetUVSToTextureGivenAVertex(Texture2D texture, Vector2 vertexPosXZ, float s, float t)
    {
        int pixelX = /*(int)(texture.width / (float)(heightMapWidth * metersPerPixel) * (float)*/Mathf.FloorToInt(vertexPosXZ.x);
        int pixelY = /*(int)(texture.width / (float)(heightMapWidth * metersPerPixel) * (float)*/Mathf.FloorToInt(vertexPosXZ.y);

        texture.SetPixel(pixelX, pixelY, new Color(s, t, 1));
    }

    public static void SetWhitePixelFadedGivenAVertex(Texture2D texture, Vector2 vertexPosXZ, float t)
    {
        int pixelX = /*(int)(texture.width / (float)(heightMapWidth * metersPerPixel) * (float)*/Mathf.RoundToInt(vertexPosXZ.x);
        int pixelY = /*(int)(texture.width / (float)(heightMapWidth * metersPerPixel) * (float)*/Mathf.RoundToInt(vertexPosXZ.y);

        float lerpFactor = 1 - Mathf.Pow(Mathf.Abs(2 * t - 1), 1/2f); //(-2 * t) * (-2 * t) + 2 * t + 0.5f;
        float col = Mathf.Lerp(0.1f, 1, lerpFactor);
        texture.SetPixel(pixelX, pixelY, new Color(col,col,col));
    }

    public static bool PointIsInBounds(Vector2 position, int generalSize)
    {
        return position.x >= 0 && position.y >= 0 && position.x < generalSize && position.y < generalSize;
    }


    public static bool ChunkIsInBounds(int chunkCoordX, int chunkCoordY, int numOfChunks)
    {
        return chunkCoordX >= 0 && chunkCoordY >= 0 && chunkCoordX < numOfChunks && chunkCoordY < numOfChunks;
    }

    public static bool ChunkIsInBounds(Vector2Int chunk, int numOfChunks)
    {
        return chunk.x >= 0 && chunk.y >= 0 && chunk.x < numOfChunks && chunk.y < numOfChunks;
    }


    public static float TotalDistanceBetweenPoints(List<Vector2> points)
    {
        float totalDistance = 0;
        for (int i = 0; i < points.Count - 1; ++i)
        {
            totalDistance += Vector2.Distance(points[i], points[i + 1]);
        }
        return totalDistance;
    }

    public static Texture2D GetPathContribution(int index, int chunkX, int chunkY, int typeOfPath)
    {
        switch (typeOfPath)
        {
            case 0:
                return TrailTextureManager.GetPathContributionWithIndexIAtCoords(index, chunkX, chunkY);
            case 1:
                return TrailTextureManager.GetTrackContributionWithIndexIAtCoords(index, chunkX, chunkY);
            case 2:
                return TrailTextureManager.GetPrimaryContributionWithIndexIAtCoords(index, chunkX, chunkY);
        }
        return null;
    }

    public static Texture2D GetPrimaryUVsTexture(int chunkX, int chunkY)
    {
        return TrailTextureManager.GetPrimaryUVsAtCoords(chunkX, chunkY);
    }
}

public struct MeshProperties
{
    public Vector3 position;
    public Vector2 uvs;

    public static int Size()
    {
        return
            sizeof(float) * 3 +
            sizeof(float) * 2;     // position;
    }
}
