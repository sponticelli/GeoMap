using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an adjacency matrix for a mesh, used for analyzing connectivity between vertices.
    /// </summary>
    [Serializable]
    public class MeshAdjacencyGraph
    {
        private int _nodeNum;
        private int _adjNum;
        private int[] _adjRow;
        private int[] _adj;

        /// <summary>
        /// Gets the adjacency row indices array.
        /// </summary>
        /// <remarks>
        /// The adjacency row array contains indices into the adjacency array.
        /// For each node i, the adjacencies are stored in positions from adj_row[i] to adj_row[i+1]-1.
        /// </remarks>
        public int[] AdjacencyRow => _adjRow;

        /// <summary>
        /// Gets the adjacency array containing the node indices of adjacent vertices.
        /// </summary>
        public int[] Adjacency => _adj;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshAdjacencyGraph"/> class from a mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to build the adjacency matrix from.</param>
        public MeshAdjacencyGraph(TriangularMesh triangularMesh)
        {
            _nodeNum = triangularMesh.VertexDictionary.Count;
            _adjRow = AdjacencyCount(triangularMesh);
            _adjNum = _adjRow[_nodeNum] - 1;
            _adj = AdjacencySet(triangularMesh, _adjRow);
        }

        /// <summary>
        /// Calculates the bandwidth of the adjacency matrix.
        /// </summary>
        /// <returns>The bandwidth of the adjacency matrix.</returns>
        /// <remarks>
        /// The bandwidth is a measure of how far matrix entries are from the diagonal.
        /// A smaller bandwidth can lead to more efficient matrix operations.
        /// </remarks>
        public int Bandwidth()
        {
            int lowerBandwidth = 0;
            int upperBandwidth = 0;
            for (int index1 = 0; index1 < _nodeNum; ++index1)
            {
                for (int index2 = _adjRow[index1]; index2 <= _adjRow[index1 + 1] - 1; ++index2)
                {
                    int currentNeighborId = _adj[index2 - 1];
                    lowerBandwidth = Math.Max(lowerBandwidth, index1 - currentNeighborId);
                    upperBandwidth = Math.Max(upperBandwidth, currentNeighborId - index1);
                }
            }
            return lowerBandwidth + 1 + upperBandwidth;
        }

        /// <summary>
        /// Counts the number of adjacencies for each node in the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to analyze.</param>
        /// <returns>An array containing the cumulative count of adjacencies for each node.</returns>
        private int[] AdjacencyCount(TriangularMesh triangularMesh)
        {
            int[] numArray = new int[_nodeNum + 1];
            for (int index = 0; index < _nodeNum; ++index)
            {
                numArray[index] = 1;
            }
            foreach (Triangle triangle in triangularMesh.TriangleDictionary.Values)
            {
                int id1 = triangle.id;
                int id2 = triangle.vertices[0].id;
                int id3 = triangle.vertices[1].id;
                int id4 = triangle.vertices[2].id;
                int id5 = triangle.neighbors[2].triangle.id;
                if (id5 < 0 || id1 < id5)
                {
                    ++numArray[id2];
                    ++numArray[id3];
                }
                int id6 = triangle.neighbors[0].triangle.id;
                if (id6 < 0 || id1 < id6)
                {
                    ++numArray[id3];
                    ++numArray[id4];
                }
                int id7 = triangle.neighbors[1].triangle.id;
                if (id7 < 0 || id1 < id7)
                {
                    ++numArray[id4];
                    ++numArray[id2];
                }
            }

            for (int nodeNum = _nodeNum; 1 <= nodeNum; --nodeNum)
            {
                numArray[nodeNum] = numArray[nodeNum - 1];
            }
            numArray[0] = 1;
            for (int index = 1; index <= _nodeNum; ++index)
            {
                numArray[index] = numArray[index - 1] + numArray[index];
            }
            return numArray;
        }

        /// <summary>
        /// Creates the adjacency set for the mesh based on the adjacency row counts.
        /// </summary>
        /// <param name="triangularMesh">The mesh to analyze.</param>
        /// <param name="rows">The adjacency row counts array.</param>
        /// <returns>An array containing the adjacency information for each node.</returns>
        private int[] AdjacencySet(TriangularMesh triangularMesh, int[] rows)
        {
            int[] destinationArray = new int[_nodeNum];
            Array.Copy(rows, destinationArray, _nodeNum);
            int length = rows[_nodeNum] - 1;
            int[] a = new int[length];
            for (int index = 0; index < length; ++index)
            {
                a[index] = -1;
            }
            for (int index = 0; index < _nodeNum; ++index)
            {
                a[destinationArray[index] - 1] = index;
                ++destinationArray[index];
            }
            foreach (Triangle triangle in triangularMesh.TriangleDictionary.Values)
            {
                int id1 = triangle.id;
                int id2 = triangle.vertices[0].id;
                int id3 = triangle.vertices[1].id;
                int id4 = triangle.vertices[2].id;
                int id5 = triangle.neighbors[2].triangle.id;
                if (id5 < 0 || id1 < id5)
                {
                    a[destinationArray[id2] - 1] = id3;
                    ++destinationArray[id2];
                    a[destinationArray[id3] - 1] = id2;
                    ++destinationArray[id3];
                }
                int id6 = triangle.neighbors[0].triangle.id;
                if (id6 < 0 || id1 < id6)
                {
                    a[destinationArray[id3] - 1] = id4;
                    ++destinationArray[id3];
                    a[destinationArray[id4] - 1] = id3;
                    ++destinationArray[id4];
                }
                int id7 = triangle.neighbors[1].triangle.id;
                if (id7 < 0 || id1 < id7)
                {
                    a[destinationArray[id2] - 1] = id4;
                    ++destinationArray[id2];
                    a[destinationArray[id4] - 1] = id2;
                    ++destinationArray[id4];
                }
            }
            for (int index = 0; index < _nodeNum; ++index)
            {
                int row = rows[index];
                int num = rows[index + 1] - 1;
                HeapSort(a, row - 1, num + 1 - row);
            }
            return a;
        }

        /// <summary>
        /// Creates a max-heap from an array segment.
        /// </summary>
        /// <param name="a">The array to heapify.</param>
        /// <param name="offset">The starting index of the segment to heapify.</param>
        /// <param name="size">The size of the segment to heapify.</param>
        private void CreateHeap(int[] a, int offset, int size)
        {
            for (int index = size / 2 - 1; 0 <= index; --index)
            {
                int num1 = a[offset + index];
                int num2 = index;
                while (true)
                {
                    int num3 = 2 * num2 + 1;
                    if (size > num3)
                    {
                        if (num3 + 1 < size && a[offset + num3] < a[offset + num3 + 1])
                            ++num3;
                        if (num1 < a[offset + num3])
                        {
                            a[offset + num2] = a[offset + num3];
                            num2 = num3;
                        }
                        else
                            break;
                    }
                    else
                        break;
                }
                a[offset + num2] = num1;
            }
        }

        /// <summary>
        /// Sorts an array segment using the heap sort algorithm.
        /// </summary>
        /// <param name="a">The array to sort.</param>
        /// <param name="offset">The starting index of the segment to sort.</param>
        /// <param name="size">The size of the segment to sort.</param>
        private void HeapSort(int[] a, int offset, int size)
        {
            if (size <= 1)
            {
                return;
            }
            CreateHeap(a, offset, size);
            (a[offset], a[offset + size - 1]) = (a[offset + size - 1], a[offset]);
            for (int size1 = size - 1; 2 <= size1; --size1)
            {
                CreateHeap(a, offset, size1);
                (a[offset], a[offset + size1 - 1]) = (a[offset + size1 - 1], a[offset]);
            }
        }
    }
}