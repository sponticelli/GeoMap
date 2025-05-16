using System;
using UnityEngine;

namespace GeoData
{
    /// <summary>
    /// Represents a single polygon with its exterior boundary and any holes.
    /// Unity-serializable wrapper for polygon ring collections.
    /// </summary>
    [Serializable]
    public class CountryPolygon
    {
        /// <summary>
        /// All rings that make up this polygon.
        /// First ring should be the exterior boundary, subsequent rings are holes.
        /// </summary>
        [SerializeField]
        public PolygonRing[] rings;
        
        /// <summary>
        /// Initializes a new country polygon.
        /// </summary>
        /// <param name="rings">Rings forming this polygon.</param>
        public CountryPolygon(PolygonRing[] rings)
        {
            this.rings = rings ?? new PolygonRing[0];
        }
        
        /// <summary>
        /// Default constructor for Unity serialization.
        /// </summary>
        public CountryPolygon()
        {
            rings = new PolygonRing[0];
        }
        
        /// <summary>
        /// Gets the exterior boundary ring of this polygon.
        /// </summary>
        public PolygonRing ExteriorRing => rings != null && rings.Length > 0 ? rings[0] : null;
        
        /// <summary>
        /// Gets all hole rings in this polygon.
        /// </summary>
        public PolygonRing[] HoleRings
        {
            get
            {
                if (rings == null || rings.Length <= 1)
                    return new PolygonRing[0];
                    
                var holes = new PolygonRing[rings.Length - 1];
                Array.Copy(rings, 1, holes, 0, holes.Length);
                return holes;
            }
        }
        
        /// <summary>
        /// Gets the total number of points in all rings of this polygon.
        /// </summary>
        public int TotalPointCount
        {
            get
            {
                if (rings == null) return 0;
                int total = 0;
                foreach (var ring in rings)
                    total += ring?.PointCount ?? 0;
                return total;
            }
        }
        
        /// <summary>
        /// Gets the number of rings in this polygon.
        /// </summary>
        public int RingCount => rings?.Length ?? 0;
        
        /// <summary>
        /// Validates that the polygon has valid data.
        /// </summary>
        /// <returns>True if the polygon is valid, false otherwise.</returns>
        public bool IsValid()
        {
            if (rings == null || rings.Length == 0)
                return false;
                
            // Must have at least an exterior ring
            if (rings[0] == null || !rings[0].IsValid())
                return false;
                
            // Validate all rings
            foreach (var ring in rings)
            {
                if (ring == null || !ring.IsValid())
                    return false;
            }
            
            return true;
        }
    }
}