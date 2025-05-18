using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Responsible for carving holes and assigning regions in a triangular mesh.
    /// Used during mesh generation to remove triangles from specified hole locations
    /// and to assign region IDs to triangles based on region pointers.
    /// </summary>
    [System.Serializable]
    public class MeshRegionProcessor
    {
        private TriangularMesh _triangularMesh;
        private List<Triangle> viri;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshRegionProcessor"/> class with the specified mesh.
        /// </summary>
        /// <param name="triangularMesh">The triangular mesh to carve holes in.</param>
        public MeshRegionProcessor(TriangularMesh triangularMesh)
        {
            this._triangularMesh = triangularMesh;
            this.viri = new List<Triangle>();
        }

        /// <summary>
        /// Marks triangles on the convex hull of the mesh for removal.
        /// Also marks boundary segments and vertices.
        /// </summary>
        private void InfectHull()
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Otri o2_3 = new Otri();
            Osub os = new Osub();

            // Start at a dummy triangle and move to a real hull edge
            o2_1.triangle = TriangularMesh.dummytri;
            o2_1.orient = 0;
            o2_1.SymSelf();
            o2_1.Copy(ref o2_3);

            // Walk around the entire convex hull
            do
            {
                if (!o2_1.IsInfected())
                {
                    o2_1.SegPivot(ref os);
                    if (os.seg == TriangularMesh.dummysub)
                    {
                        // If there's no subsegment, mark the triangle for removal
                        if (!o2_1.IsInfected())
                        {
                            o2_1.Infect();
                            this.viri.Add(o2_1.triangle);
                        }
                    }
                    else if (os.seg.boundary == 0)
                    {
                        // Mark the segment as a boundary
                        os.seg.boundary = 1;
                        Vertex vertex1 = o2_1.Org();
                        Vertex vertex2 = o2_1.Dest();

                        // Mark the vertices as boundary vertices
                        if (vertex1.mark == 0)
                            vertex1.mark = 1;
                        if (vertex2.mark == 0)
                            vertex2.mark = 1;
                    }
                }

                // Move to the next hull edge
                o2_1.LnextSelf();
                o2_1.Oprev(ref o2_2);
                while (o2_2.triangle != TriangularMesh.dummytri)
                {
                    o2_2.Copy(ref o2_1);
                    o2_1.Oprev(ref o2_2);
                }
            } while (!o2_1.Equal(o2_3));
        }

        /// <summary>
        /// Removes infected triangles from the mesh and updates the mesh topology.
        /// This method is responsible for the actual "carving" of holes in the mesh.
        /// </summary>
        private void Plague()
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Osub os = new Osub();

            // First pass: Process each infected triangle and spread the infection
            for (int index = 0; index < this.viri.Count; ++index)
            {
                o2_1.triangle = this.viri[index];
                o2_1.Uninfect();

                // Process each edge of the triangle
                for (o2_1.orient = 0; o2_1.orient < 3; ++o2_1.orient)
                {
                    o2_1.Sym(ref o2_2);  // Get the adjacent triangle
                    o2_1.SegPivot(ref os);  // Get the subsegment on this edge

                    if (o2_2.triangle == TriangularMesh.dummytri || o2_2.IsInfected())
                    {
                        // If the adjacent triangle is a dummy or already infected
                        if (os.seg != TriangularMesh.dummysub)
                        {
                            // Remove the subsegment
                            this._triangularMesh.SubsegDealloc(os.seg);
                            if (o2_2.triangle != TriangularMesh.dummytri)
                            {
                                o2_2.Uninfect();
                                o2_2.SegDissolve();
                                o2_2.Infect();
                            }
                        }
                    }
                    else if (os.seg == TriangularMesh.dummysub)
                    {
                        // If there's no subsegment, infect the adjacent triangle
                        o2_2.Infect();
                        this.viri.Add(o2_2.triangle);
                    }
                    else
                    {
                        // If there's a subsegment, mark it as a boundary
                        os.TriDissolve();
                        if (os.seg.boundary == 0)
                            os.seg.boundary = 1;

                        // Mark the vertices as boundary vertices
                        Vertex vertex1 = o2_2.Org();
                        Vertex vertex2 = o2_2.Dest();
                        if (vertex1.mark == 0)
                            vertex1.mark = 1;
                        if (vertex2.mark == 0)
                            vertex2.mark = 1;
                    }
                }

                o2_1.Infect();
            }

            // Second pass: Clean up vertices and deallocate triangles
            foreach (Triangle triangle in this.viri)
            {
                o2_1.triangle = triangle;

                // Process each vertex of the triangle
                for (o2_1.orient = 0; o2_1.orient < 3; ++o2_1.orient)
                {
                    Vertex vertex = o2_1.Org();
                    if ((Point)vertex != (Point)null)
                    {
                        bool flag = true;
                        o2_1.SetOrg((Vertex)null);

                        // Check if the vertex is used by any non-infected triangles
                        o2_1.Onext(ref o2_2);
                        while (o2_2.triangle != TriangularMesh.dummytri && !o2_2.Equal(o2_1))
                        {
                            if (o2_2.IsInfected())
                                o2_2.SetOrg((Vertex)null);
                            else
                                flag = false;
                            o2_2.OnextSelf();
                        }

                        if (o2_2.triangle == TriangularMesh.dummytri)
                        {
                            o2_1.Oprev(ref o2_2);
                            while (o2_2.triangle != TriangularMesh.dummytri)
                            {
                                if (o2_2.IsInfected())
                                    o2_2.SetOrg((Vertex)null);
                                else
                                    flag = false;
                                o2_2.OprevSelf();
                            }
                        }

                        // If the vertex is only used by infected triangles, mark it as undead
                        if (flag)
                        {
                            vertex.type = VertexType.UndeadVertex;
                            ++this._triangularMesh.undeads;
                        }
                    }
                }

                // Update the hull size and dissolve connections to adjacent triangles
                for (o2_1.orient = 0; o2_1.orient < 3; ++o2_1.orient)
                {
                    o2_1.Sym(ref o2_2);
                    if (o2_2.triangle == TriangularMesh.dummytri)
                    {
                        --this._triangularMesh.hullsize;
                    }
                    else
                    {
                        o2_2.Dissolve();
                        ++this._triangularMesh.hullsize;
                    }
                }

                // Deallocate the triangle
                this._triangularMesh.TriangleDealloc(o2_1.triangle);
            }

            this.viri.Clear();
        }

        public void CarveHoles()
        {
            Otri searchtri = new Otri();
            Triangle[] triangleArray = (Triangle[])null;
            if (!this._triangularMesh.behavior.Convex)
                this.InfectHull();
            if (!this._triangularMesh.behavior.NoHoles)
            {
                foreach (Point hole in this._triangularMesh.holes)
                {
                    if (this._triangularMesh.bounds.Contains(hole))
                    {
                        searchtri.triangle = TriangularMesh.dummytri;
                        searchtri.orient = 0;
                        searchtri.SymSelf();
                        if (Primitives.CounterClockwise((Point)searchtri.Org(), (Point)searchtri.Dest(), hole) > 0.0 &&
                            this._triangularMesh.locator.Locate(hole, ref searchtri) != PointLocationResult.Outside &&
                            !searchtri.IsInfected())
                        {
                            searchtri.Infect();
                            this.viri.Add(searchtri.triangle);
                        }
                    }
                }
            }

            if (this._triangularMesh.regions.Count > 0)
            {
                int index = 0;
                triangleArray = new Triangle[this._triangularMesh.regions.Count];
                foreach (RegionPointer region in this._triangularMesh.regions)
                {
                    triangleArray[index] = TriangularMesh.dummytri;
                    if (this._triangularMesh.bounds.Contains(region.point))
                    {
                        searchtri.triangle = TriangularMesh.dummytri;
                        searchtri.orient = 0;
                        searchtri.SymSelf();
                        if (Primitives.CounterClockwise((Point)searchtri.Org(), (Point)searchtri.Dest(), region.point) >
                            0.0 && this._triangularMesh.locator.Locate(region.point, ref searchtri) != PointLocationResult.Outside &&
                            !searchtri.IsInfected())
                        {
                            triangleArray[index] = searchtri.triangle;
                            triangleArray[index].region = region.id;
                        }
                    }

                    ++index;
                }
            }

            if (this.viri.Count > 0)
                this.Plague();
            if (triangleArray != null)
            {
                RegionIterator regionIterator = new RegionIterator(this._triangularMesh);
                for (int index = 0; index < triangleArray.Length; ++index)
                {
                    if (triangleArray[index] != TriangularMesh.dummytri && !Otri.IsDead(triangleArray[index]))
                        regionIterator.Process(triangleArray[index]);
                }
            }

            this.viri.Clear();
        }
    }
}