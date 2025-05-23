using System.Collections.Generic;
using UnityEngine;

namespace GeoMap.Utils
{
    public class MeshData
    {
        public List<Vector3> Vertices { get; set; } = new();
        public List<int> Indices { get; set; } = new();
        public List<Vector2> UV { get; set; } = new();
    }
}