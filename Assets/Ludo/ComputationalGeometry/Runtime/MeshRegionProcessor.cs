using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Responsible for carving holes and assigning regions in a triangular mesh.
    /// Used during mesh generation to remove triangles from specified hole locations
    /// and to assign region IDs to triangles based on region pointers.
    /// </summary>
    [Serializable]
    public class MeshRegionProcessor
    {
        private TriangularMesh _triangularMesh;
        private List<Triangle> _triangles;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshRegionProcessor"/> class with the specified mesh.
        /// </summary>
        /// <param name="triangularMesh">The triangular mesh to carve holes in.</param>
        public MeshRegionProcessor(TriangularMesh triangularMesh)
        {
            _triangularMesh = triangularMesh;
            _triangles = new List<Triangle>();
        }

        /// <summary>
        /// Marks triangles on the convex hull of the mesh for removal.
        /// Also marks boundary segments and vertices.
        /// </summary>
        private void InfectHull()
        {
            OrientedTriangle oreintedTriangle1 = new OrientedTriangle();
            OrientedTriangle orientedTriangle2 = new OrientedTriangle();
            OrientedTriangle orientedTriangle3 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();

            // Start at a dummy triangle and move to a real hull edge
            oreintedTriangle1.triangle = TriangularMesh.dummytri;
            oreintedTriangle1.orient = 0;
            oreintedTriangle1.SetSelfAsSymmetricTriangle();
            oreintedTriangle1.Copy(ref orientedTriangle3);

            // Walk around the entire convex hull
            do
            {
                if (!oreintedTriangle1.IsInfected())
                {
                    oreintedTriangle1.SegPivot(ref orientedSubSegment);
                    if (orientedSubSegment.seg == TriangularMesh.dummysub)
                    {
                        // If there's no subsegment, mark the triangle for removal
                        if (!oreintedTriangle1.IsInfected())
                        {
                            oreintedTriangle1.Infect();
                            _triangles.Add(oreintedTriangle1.triangle);
                        }
                    }
                    else if (orientedSubSegment.seg.boundary == 0)
                    {
                        // Mark the segment as a boundary
                        orientedSubSegment.seg.boundary = 1;
                        Vertex vertex1 = oreintedTriangle1.Origin();
                        Vertex vertex2 = oreintedTriangle1.Destination();

                        // Mark the vertices as boundary vertices
                        if (vertex1.mark == 0)
                            vertex1.mark = 1;
                        if (vertex2.mark == 0)
                            vertex2.mark = 1;
                    }
                }

                // Move to the next hull edge
                oreintedTriangle1.LnextSelf();
                oreintedTriangle1.Oprev(ref orientedTriangle2);
                while (orientedTriangle2.triangle != TriangularMesh.dummytri)
                {
                    orientedTriangle2.Copy(ref oreintedTriangle1);
                    oreintedTriangle1.Oprev(ref orientedTriangle2);
                }
            } while (!oreintedTriangle1.Equal(orientedTriangle3));
        }

        /// <summary>
        /// Removes infected triangles from the mesh and updates the mesh topology.
        /// This method is responsible for the actual "carving" of holes in the mesh.
        /// </summary>
        private void Plague()
        {
            OrientedTriangle orientedTriangle1 = new OrientedTriangle();
            OrientedTriangle orientedTriangle2 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();

            // First pass: Process each infected triangle and spread the infection
            for (int index = 0; index < _triangles.Count; ++index)
            {
                orientedTriangle1.triangle = _triangles[index];
                orientedTriangle1.Uninfect();

                // Process each edge of the triangle
                for (orientedTriangle1.orient = 0; orientedTriangle1.orient < 3; ++orientedTriangle1.orient)
                {
                    orientedTriangle1.SetAsSymmetricTriangle(ref orientedTriangle2);  // Get the adjacent triangle
                    orientedTriangle1.SegPivot(ref orientedSubSegment);  // Get the subsegment on this edge

                    if (orientedTriangle2.triangle == TriangularMesh.dummytri || orientedTriangle2.IsInfected())
                    {
                        // If the adjacent triangle is a dummy or already infected
                        if (orientedSubSegment.seg == TriangularMesh.dummysub) continue;
                        // Remove the subsegment
                        _triangularMesh.SubsegDealloc(orientedSubSegment.seg);
                        if (orientedTriangle2.triangle != TriangularMesh.dummytri)
                        {
                            orientedTriangle2.Uninfect();
                            orientedTriangle2.DissolveBindToSegment();
                            orientedTriangle2.Infect();
                        }
                    }
                    else if (orientedSubSegment.seg == TriangularMesh.dummysub)
                    {
                        // If there's no subsegment, infect the adjacent triangle
                        orientedTriangle2.Infect();
                        _triangles.Add(orientedTriangle2.triangle);
                    }
                    else
                    {
                        // If there's a subsegment, mark it as a boundary
                        orientedSubSegment.TriDissolve();
                        if (orientedSubSegment.seg.boundary == 0)
                        {
                            orientedSubSegment.seg.boundary = 1;
                        }

                        // Mark the vertices as boundary vertices
                        Vertex vertex1 = orientedTriangle2.Origin();
                        Vertex vertex2 = orientedTriangle2.Destination();
                        if (vertex1.mark == 0)
                        {
                            vertex1.mark = 1;
                        }

                        if (vertex2.mark == 0)
                        {
                            vertex2.mark = 1;
                        }
                    }
                }

                orientedTriangle1.Infect();
            }

            // Second pass: Clean up vertices and deallocate triangles
            foreach (Triangle triangle in _triangles)
            {
                orientedTriangle1.triangle = triangle;

                // Process each vertex of the triangle
                for (orientedTriangle1.orient = 0; orientedTriangle1.orient < 3; ++orientedTriangle1.orient)
                {
                    Vertex vertex = orientedTriangle1.Origin();
                    if (vertex != null)
                    {
                        bool flag = true;
                        orientedTriangle1.SetOrigin(null);

                        // Check if the vertex is used by any non-infected triangles
                        orientedTriangle1.Onext(ref orientedTriangle2);
                        while (orientedTriangle2.triangle != TriangularMesh.dummytri && !orientedTriangle2.Equal(orientedTriangle1))
                        {
                            if (orientedTriangle2.IsInfected())
                            {
                                orientedTriangle2.SetOrigin(null);
                            }
                            else
                            {
                                flag = false;
                            }
                            orientedTriangle2.OnextSelf();
                        }

                        if (orientedTriangle2.triangle == TriangularMesh.dummytri)
                        {
                            orientedTriangle1.Oprev(ref orientedTriangle2);
                            while (orientedTriangle2.triangle != TriangularMesh.dummytri)
                            {
                                if (orientedTriangle2.IsInfected())
                                    orientedTriangle2.SetOrigin(null);
                                else
                                    flag = false;
                                orientedTriangle2.OprevSelf();
                            }
                        }

                        // If the vertex is only used by infected triangles, mark it as undead
                        if (!flag) continue;
                        vertex.type = VertexType.UndeadVertex;
                        ++_triangularMesh.undeads;
                    }
                }

                // Update the hull size and dissolve connections to adjacent triangles
                for (orientedTriangle1.orient = 0; orientedTriangle1.orient < 3; ++orientedTriangle1.orient)
                {
                    orientedTriangle1.SetAsSymmetricTriangle(ref orientedTriangle2);
                    if (orientedTriangle2.triangle == TriangularMesh.dummytri)
                    {
                        --_triangularMesh.hullsize;
                    }
                    else
                    {
                        orientedTriangle2.Dissolve();
                        ++_triangularMesh.hullsize;
                    }
                }

                // Deallocate the triangle
                _triangularMesh.TriangleDealloc(orientedTriangle1.triangle);
            }

            _triangles.Clear();
        }

        public void CarveHoles()
        {
            OrientedTriangle searchtri = new OrientedTriangle();
            Triangle[] triangleArray = null;
            if (!_triangularMesh.behavior.Convex)
            {
                InfectHull();
            }
            if (!_triangularMesh.behavior.NoHoles)
            {
                foreach (Point hole in _triangularMesh.holes)
                {
                    if (!_triangularMesh.bounds.Contains(hole)) continue;
                    searchtri.triangle = TriangularMesh.dummytri;
                    searchtri.orient = 0;
                    searchtri.SetSelfAsSymmetricTriangle();
                    if (Primitives.CounterClockwise(searchtri.Origin(), searchtri.Destination(), hole) > 0.0 &&
                        _triangularMesh.locator.Locate(hole, ref searchtri) != PointLocationResult.Outside &&
                        !searchtri.IsInfected())
                    {
                        searchtri.Infect();
                        _triangles.Add(searchtri.triangle);
                    }
                }
            }

            if (_triangularMesh.regions.Count > 0)
            {
                int index = 0;
                triangleArray = new Triangle[_triangularMesh.regions.Count];
                foreach (RegionPointer region in _triangularMesh.regions)
                {
                    triangleArray[index] = TriangularMesh.dummytri;
                    if (_triangularMesh.bounds.Contains(region.point))
                    {
                        searchtri.triangle = TriangularMesh.dummytri;
                        searchtri.orient = 0;
                        searchtri.SetSelfAsSymmetricTriangle();
                        if (Primitives.CounterClockwise(searchtri.Origin(), searchtri.Destination(), region.point) >
                            0.0 && _triangularMesh.locator.Locate(region.point, ref searchtri) != PointLocationResult.Outside &&
                            !searchtri.IsInfected())
                        {
                            triangleArray[index] = searchtri.triangle;
                            triangleArray[index].region = region.id;
                        }
                    }

                    ++index;
                }
            }

            if (_triangles.Count > 0)
                Plague();
            if (triangleArray != null)
            {
                RegionIterator regionIterator = new RegionIterator(_triangularMesh);
                for (int index = 0; index < triangleArray.Length; ++index)
                {
                    if (triangleArray[index] != TriangularMesh.dummytri && !OrientedTriangle.IsDead(triangleArray[index]))
                        regionIterator.Process(triangleArray[index]);
                }
            }

            _triangles.Clear();
        }
    }
}