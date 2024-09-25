using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class TrailJSONParser
{
    private JSONClasses.Trails trails { get; set; }

    public TrailJSONParser(TextAsset json)
    {
        trails = JsonConvert.DeserializeObject<JSONClasses.Trails>(json.text);
        foreach (var path in trails.trails)
        {
            List<Vector2> coordinates = path.geometry.coordinates;
        }
    }

    public JSONClasses.Trails GetTrails()
    {
        return trails;
    }    
}
