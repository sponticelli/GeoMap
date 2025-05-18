using System.Collections.Generic;
using UnityEngine;

namespace GeoMap.Utils
{
    public class MeshData
    {
        public MeshData()
        {
            Vertices = new List<Vector3>();
            Indices = new List<int>();
            UV = new List<Vector2>();
        }

        public List<Vector3> Vertices { get; set; }
        public List<int> Indices { get; set; }
        public List<Vector2> UV { get; set; }
    }
}