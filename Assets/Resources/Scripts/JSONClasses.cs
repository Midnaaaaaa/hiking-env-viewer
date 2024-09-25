using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
using static JSONClasses;

public class JSONClasses
{
    public class Trail
    {
        [JsonProperty("properties")]
        public Dictionary<string, string> properties { get; set; }

        [JsonProperty("geometry")]
        public Geometry geometry { get; set; }
    }

    public class Geometry
    {
        [JsonProperty("coordinates")]
        private List<List<List<float>>> rawCoordinates { get; set; }

        [JsonIgnore]
        public List<Vector2> coordinates { get; private set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            coordinates = new List<Vector2>();
            foreach (var coordinate in rawCoordinates[0])
            {
                coordinates.Add(new Vector2(coordinate[0], coordinate[1]));
            }
        }
    }

    public class Trails
    {
        [JsonProperty("features")]
        public List<Trail> trails { get; set; }
    }


    public class Plant
    {
        public int age { get; set; }
        public Vector3 pos { get; set; }
        public float height { get; set; }
        public float dbh { get; set; }
        public float canopy { get; set; }
        public int pft { get; set; }
    }

    public class Pft
    {
        public bool isTree { get; set; }
        public bool isConifer { get; set; }
    }

    public class Root
    {
        public List<Pft> pfts { get; set; }
        public List<Plant> plants { get; set; }
    }
}
