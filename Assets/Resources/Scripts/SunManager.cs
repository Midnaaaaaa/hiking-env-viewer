using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunManager : MonoBehaviour
{
    [SerializeField] private GameObject mapGenerator;
    void Start()
    {
        if (mapGenerator != null)
        {
            float pos = mapGenerator.GetComponent<TerrainTextureManager>().GetHeightMapSize() * mapGenerator.GetComponent<TerrainTextureManager>().GetMetersPerPixel();
            transform.position = new Vector3(pos/2, 0, pos/2);
        }
    }
}
