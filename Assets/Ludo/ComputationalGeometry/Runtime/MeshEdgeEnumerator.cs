using System;
using System.Collections;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides an enumerator for iterating through all edges in a mesh.
    /// </summary>
    /// <remarks>
    /// This enumerator traverses all triangles in the mesh and extracts unique edges.
    /// It ensures each edge is enumerated exactly once by comparing triangle IDs and
    /// using the dummy triangle check to handle boundary edges.
    /// </remarks>
    [Serializable]
    public class MeshEdgeEnumerator : IEnumerator<MeshEdge>
    {
        /// <summary>
        /// Enumerator for the triangles in the mesh.
        /// </summary>
        private IEnumerator<Triangle> _triangles;

        /// <summary>
        /// The current oriented triangle being processed.
        /// </summary>
        private OrientedTriangle _orientedTriangle;

        /// <summary>
        /// The neighboring triangle across the current edge.
        /// </summary>
        private OrientedTriangle _neighborTriangle;

        /// <summary>
        /// The subsegment associated with the current edge, if any.
        /// </summary>
        private OrientedSubSegment _orientedSubSegment;

        /// <summary>
        /// The current edge being enumerated.
        /// </summary>
        private MeshEdge _current;

        /// <summary>
        /// The first vertex of the current edge.
        /// </summary>
        private Vertex _p1;

        /// <summary>
        /// The second vertex of the current edge.
        /// </summary>
        private Vertex _p2;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshEdgeEnumerator"/> class for the specified mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh whose edges will be enumerated.</param>
        /// <remarks>
        /// The constructor initializes the triangle enumerator and sets up the first triangle
        /// to begin the edge enumeration process.
        /// </remarks>
        public MeshEdgeEnumerator(TriangularMesh triangularMesh)
        {
            _triangles = triangularMesh.TriangleDictionary.Values.GetEnumerator();
            _triangles.MoveNext();
            _orientedTriangle.triangle = _triangles.Current;
            _orientedTriangle.orient = 0;
        }

        /// <summary>
        /// Gets the current edge in the enumeration.
        /// </summary>
        /// <value>The current edge.</value>
        public MeshEdge Current => _current;

        /// <summary>
        /// Releases all resources used by the <see cref="MeshEdgeEnumerator"/>.
        /// </summary>
        public void Dispose() => _triangles.Dispose();

        /// <summary>
        /// Gets the current edge in the enumeration as an object.
        /// </summary>
        /// <value>The current edge as an object.</value>
        object IEnumerator.Current => _current;

        /// <summary>
        /// Advances the enumerator to the next edge in the mesh.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next edge;
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <remarks>
        /// This method iterates through the triangles and their orientations to find unique edges.
        /// An edge is considered unique if:
        /// 1. The current triangle ID is less than the neighboring triangle ID, or
        /// 2. The neighboring triangle is a dummy triangle (indicating a boundary edge).
        ///
        /// The method handles advancing to the next triangle when all orientations of the current
        /// triangle have been processed.
        /// </remarks>
        public bool MoveNext()
        {
            if (_orientedTriangle.triangle == null)
                return false;
            _current = null;
            while (_current == null)
            {
                if (_orientedTriangle.orient == 3)
                {
                    if (!_triangles.MoveNext())
                        return false;
                    _orientedTriangle.triangle = _triangles.Current;
                    _orientedTriangle.orient = 0;
                }
                _orientedTriangle.SetAsSymmetricTriangle(ref _neighborTriangle);
                if (_orientedTriangle.triangle.id < _neighborTriangle.triangle.id || _neighborTriangle.triangle == TriangularMesh.dummytri)
                {
                    _p1 = _orientedTriangle.Origin();
                    _p2 = _orientedTriangle.Destination();
                    _orientedTriangle.SegPivot(ref _orientedSubSegment);
                    _current = new MeshEdge(_p1.id, _p2.id, _orientedSubSegment.seg.boundary);
                }
                ++_orientedTriangle.orient;
            }
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first edge in the collection.
        /// </summary>
        public void Reset() => _triangles.Reset();
    }
}