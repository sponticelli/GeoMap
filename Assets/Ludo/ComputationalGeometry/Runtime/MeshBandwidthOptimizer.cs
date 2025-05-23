using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements the Cuthill-McKee algorithm for reducing the bandwidth of sparse matrices.
    /// </summary>
    /// <remarks>
    /// The Cuthill-McKee algorithm is a graph algorithm used to reduce the bandwidth of sparse matrices
    /// by reordering the vertices of a graph. This implementation is used for mesh optimization
    /// to improve computational efficiency in numerical methods.
    /// </remarks>
    [Serializable]
    public class MeshBandwidthOptimizer
    {
        /// <summary>
        /// The number of nodes in the mesh.
        /// </summary>
        private int _nodeNum;

        /// <summary>
        /// The adjacency _graph representing the mesh connectivity.
        /// </summary>
        private MeshAdjacencyGraph _graph;

        /// <summary>
        /// Renumbers the vertices of a mesh using the Cuthill-McKee algorithm to reduce bandwidth.
        /// </summary>
        /// <param name="triangularMesh">The mesh to be renumbered.</param>
        /// <returns>An array containing the inverse permutation mapping from original to new vertex indices.</returns>
        /// <remarks>
        /// This method first applies a linear numbering to the mesh, then builds an adjacency _graph,
        /// and finally applies the Reverse Cuthill-McKee algorithm to generate an optimized numbering
        /// that reduces the bandwidth of the resulting system matrices.
        /// </remarks>
        public int[] Renumber(TriangularMesh triangularMesh)
        {
            _nodeNum = triangularMesh.VertexDictionary.Count;
            triangularMesh.Renumber(VertexNumbering.Linear);
            _graph = new MeshAdjacencyGraph(triangularMesh);
            int num1 = _graph.Bandwidth();
            int[] rcm = GenerateRcm();
            int[] permInv = PermInverse(_nodeNum, rcm);
            int num2 = PermBandwidth(rcm, permInv);
            return permInv;
        }

        /// <summary>
        /// Calculates the bandwidth of the _graph after applying the permutation.
        /// </summary>
        /// <param name="perm">The permutation array.</param>
        /// <param name="permInv">The inverse permutation array.</param>
        /// <returns>The bandwidth of the permuted _graph.</returns>
        /// <remarks>
        /// The bandwidth is calculated as the maximum distance of any non-zero element from the diagonal,
        /// plus one (for the diagonal itself).
        /// </remarks>
        private int PermBandwidth(int[] perm, int[] permInv)
        {
            int[] adjacencyRow = _graph.AdjacencyRow;
            int[] adjacency = _graph.Adjacency;
            int val11 = 0;
            int val12 = 0;
            for (int index1 = 0; index1 < _nodeNum; ++index1)
            {
                for (int index2 = adjacencyRow[perm[index1]]; index2 <= adjacencyRow[perm[index1] + 1] - 1; ++index2)
                {
                    int num = permInv[adjacency[index2 - 1]];
                    val11 = Math.Max(val11, index1 - num);
                    val12 = Math.Max(val12, num - index1);
                }
            }
            return val11 + 1 + val12;
        }

        /// <summary>
        /// Generates the Reverse Cuthill-McKee ordering for the mesh.
        /// </summary>
        /// <returns>An array containing the RCM ordering of the vertices.</returns>
        /// <remarks>
        /// This method implements the core of the Reverse Cuthill-McKee algorithm.
        /// It processes each connected component of the graph separately, finding
        /// a suitable root vertex for each component and then applying the RCM algorithm.
        /// </remarks>
        private int[] GenerateRcm()
        {
            int[] rcm = new int[_nodeNum];
            int iccsze = 0;
            int levelNum = 0;
            int[] levelRow = new int[_nodeNum + 1];
            int[] mask = new int[_nodeNum];
            for (int index = 0; index < _nodeNum; ++index)
            {
                mask[index] = 1;
            }
            int num = 1;
            for (int index = 0; index < _nodeNum; ++index)
            {
                if (mask[index] == 0) continue;
                int root = index;
                FindRoot(ref root, mask, ref levelNum, levelRow, rcm, num - 1);
                Rcm(root, mask, rcm, num - 1, ref iccsze);
                num += iccsze;
                if (_nodeNum < num)
                {
                    return rcm;
                }
            }
            return rcm;
        }

        /// <summary>
        /// Applies the Reverse Cuthill-McKee algorithm starting from a given root vertex.
        /// </summary>
        /// <param name="root">The root vertex to start the algorithm from.</param>
        /// <param name="mask">Array indicating which vertices have been processed (0) or not (1).</param>
        /// <param name="perm">The permutation array to be filled with the RCM ordering.</param>
        /// <param name="offset">The offset in the permutation array where to start filling.</param>
        /// <param name="iccsze">The size of the connected component being processed.</param>
        /// <remarks>
        /// This method implements a breadth-first traversal of the graph, ordering vertices
        /// by increasing degree (number of connections) at each level. The final step reverses
        /// the ordering to produce the Reverse Cuthill-McKee ordering.
        /// </remarks>
        private void Rcm(int root, int[] mask, int[] perm, int offset, ref int iccsze)
        {
            int[] adjacencyRow = _graph.AdjacencyRow;
            int[] adjacency = _graph.Adjacency;
            int[] deg = new int[_nodeNum];
            Degree(root, mask, deg, ref iccsze, perm, offset);
            mask[root] = 0;
            if (iccsze <= 1)
            {
                return;
            }
            int num1 = 0;
            int num2 = 1;
            while (num1 < num2)
            {
                int num3 = num1 + 1;
                num1 = num2;
                for (int index1 = num3; index1 <= num1; ++index1)
                {
                    int index2 = perm[offset + index1 - 1];
                    int num4 = adjacencyRow[index2];
                    int num5 = adjacencyRow[index2 + 1] - 1;
                    int num6 = num2 + 1;
                    for (int index3 = num4; index3 <= num5; ++index3)
                    {
                        int index4 = adjacency[index3 - 1];
                        if (mask[index4] == 0) continue;
                        ++num2;
                        mask[index4] = 0;
                        perm[offset + num2 - 1] = index4;
                    }
                    if (num2 > num6)
                    {
                        int num7 = num6;
                        while (num7 < num2)
                        {
                            int num8 = num7;
                            ++num7;
                            int num9 = perm[offset + num7 - 1];
                            for (; num6 < num8; --num8)
                            {
                                int num10 = perm[offset + num8 - 1];
                                if (deg[num10 - 1] > deg[num9 - 1])
                                    perm[offset + num8] = num10;
                                else
                                    break;
                            }
                            perm[offset + num8] = num9;
                        }
                    }
                }
            }
            ReverseVector(perm, offset, iccsze);
        }

        /// <summary>
        /// Finds a suitable root vertex for the Cuthill-McKee algorithm.
        /// </summary>
        /// <param name="root">The initial root vertex, which may be updated to a better choice.</param>
        /// <param name="mask">Array indicating which vertices have been processed (0) or not (1).</param>
        /// <param name="levelNum">The number of levels in the level structure.</param>
        /// <param name="levelRow">Array containing the starting indices of each level in the level array.</param>
        /// <param name="level">Array containing the vertices in each level.</param>
        /// <param name="offset">The offset in the level array where to start filling.</param>
        /// <remarks>
        /// This method attempts to find a peripheral vertex (one that is far from other vertices)
        /// as a good starting point for the Cuthill-McKee algorithm. It uses a heuristic based on
        /// building level structures from candidate vertices.
        /// </remarks>
        private void FindRoot(
            ref int root,
            int[] mask,
            ref int levelNum,
            int[] levelRow,
            int[] level,
            int offset)
        {
            int[] adjacencyRow = _graph.AdjacencyRow;
            int[] adjacency = _graph.Adjacency;
            int levelNum1 = 0;
            GetLevelSet(ref root, mask, ref levelNum, levelRow, level, offset);
            int num1 = levelRow[levelNum] - 1;
            if (levelNum == 1 || levelNum == num1)
            {
                return;
            }
            do
            {
                int num2 = num1;
                int num3 = levelRow[levelNum - 1];
                root = level[offset + num3 - 1];
                if (num3 < num1)
                {
                    for (int index1 = num3; index1 <= num1; ++index1)
                    {
                        int index2 = level[offset + index1 - 1];
                        int num4 = 0;
                        int num5 = adjacencyRow[index2 - 1];
                        int num6 = adjacencyRow[index2] - 1;
                        for (int index3 = num5; index3 <= num6; ++index3)
                        {
                            int index4 = adjacency[index3 - 1];
                            if (mask[index4] > 0)
                            {
                                ++num4;
                            }
                        }
                        if (num4 < num2)
                        {
                            root = index2;
                            num2 = num4;
                        }
                    }
                }
                GetLevelSet(ref root, mask, ref levelNum1, levelRow, level, offset);
                if (levelNum1 > levelNum)
                {
                    levelNum = levelNum1;
                }
                else
                {
                    goto label_1;
                }
            }
            while (num1 > levelNum);
            goto label_16;
            label_1:
            return;
            label_16:;
        }

        /// <summary>
        /// Constructs a level structure (breadth-first traversal) starting from a root vertex.
        /// </summary>
        /// <param name="root">The root vertex to start the traversal from.</param>
        /// <param name="mask">Array indicating which vertices have been processed (0) or not (1).</param>
        /// <param name="levelNum">The number of levels in the resulting level structure.</param>
        /// <param name="levelRow">Array containing the starting indices of each level in the level array.</param>
        /// <param name="level">Array containing the vertices in each level.</param>
        /// <param name="offset">The offset in the level array where to start filling.</param>
        /// <remarks>
        /// This method performs a breadth-first traversal of the graph starting from the root vertex,
        /// organizing vertices into levels based on their distance from the root. The mask array is
        /// temporarily modified during the traversal and restored at the end.
        /// </remarks>
        private void GetLevelSet(
            ref int root,
            int[] mask,
            ref int levelNum,
            int[] levelRow,
            int[] level,
            int offset)
        {
            int[] adjacencyRow = _graph.AdjacencyRow;
            int[] adjacency = _graph.Adjacency;
            mask[root] = 0;
            level[offset] = root;
            levelNum = 0;
            int num1 = 0;
            int num2 = 1;
            do
            {
                int num3 = num1 + 1;
                num1 = num2;
                ++levelNum;
                levelRow[levelNum - 1] = num3;
                for (int index1 = num3; index1 <= num1; ++index1)
                {
                    int index2 = level[offset + index1 - 1];
                    int num4 = adjacencyRow[index2];
                    int num5 = adjacencyRow[index2 + 1] - 1;
                    for (int index3 = num4; index3 <= num5; ++index3)
                    {
                        int index4 = adjacency[index3 - 1];
                        if (mask[index4] != 0)
                        {
                            ++num2;
                            level[offset + num2 - 1] = index4;
                            mask[index4] = 0;
                        }
                    }
                }
            }
            while (num2 - num1 > 0);
            levelRow[levelNum] = num1 + 1;
            for (int index = 0; index < num2; ++index)
            {
                mask[level[offset + index]] = 1;
            }
        }

        /// <summary>
        /// Computes the degree (number of connections) for each vertex in a connected component.
        /// </summary>
        /// <param name="root">The root vertex of the connected component.</param>
        /// <param name="mask">Array indicating which vertices have been processed (0) or not (1).</param>
        /// <param name="deg">Array to be filled with the degree of each vertex.</param>
        /// <param name="iccsze">The size of the connected component.</param>
        /// <param name="ls">Array to be filled with the vertices in the connected component.</param>
        /// <param name="offset">The offset in the ls array where to start filling.</param>
        /// <remarks>
        /// This method performs a breadth-first traversal of the connected component starting from
        /// the root vertex, computing the degree of each vertex and identifying all vertices in the
        /// component. The adjacency row array is temporarily modified during the traversal and restored
        /// at the end.
        /// </remarks>
        private void Degree(int root, int[] mask, int[] deg, ref int iccsze, int[] ls, int offset)
        {
            int[] adjacencyRow = _graph.AdjacencyRow;
            int[] adjacency = _graph.Adjacency;
            int num1 = 1;
            ls[offset] = root;
            adjacencyRow[root] = -adjacencyRow[root];
            int num2 = 0;
            iccsze = 1;
            for (; num1 > 0; num1 = iccsze - num2)
            {
                int num3 = num2 + 1;
                num2 = iccsze;
                for (int index1 = num3; index1 <= num2; ++index1)
                {
                    int l = ls[offset + index1 - 1];
                    int num4 = -adjacencyRow[l];
                    int num5 = Math.Abs(adjacencyRow[l + 1]) - 1;
                    int num6 = 0;
                    for (int index2 = num4; index2 <= num5; ++index2)
                    {
                        int index3 = adjacency[index2 - 1];
                        if (mask[index3] == 0) continue;
                        ++num6;
                        if (0 > adjacencyRow[index3]) continue;
                        adjacencyRow[index3] = -adjacencyRow[index3];
                        ++iccsze;
                        ls[offset + iccsze - 1] = index3;
                    }
                    deg[l] = num6;
                }
            }
            for (int index = 0; index < iccsze; ++index)
            {
                int l = ls[offset + index];
                adjacencyRow[l] = -adjacencyRow[l];
            }
        }

        /// <summary>
        /// Computes the inverse of a permutation.
        /// </summary>
        /// <param name="n">The size of the permutation.</param>
        /// <param name="perm">The permutation array.</param>
        /// <returns>The inverse permutation array.</returns>
        /// <remarks>
        /// If perm[i] = j, then the inverse permutation inv_perm[j] = i.
        /// This is used to map between original and new vertex indices.
        /// </remarks>
        private int[] PermInverse(int n, int[] perm)
        {
            int[] numArray = new int[_nodeNum];
            for (int index = 0; index < n; ++index)
            {
                numArray[perm[index]] = index;
            }
            return numArray;
        }

        /// <summary>
        /// Reverses a segment of an array in place.
        /// </summary>
        /// <param name="a">The array to modify.</param>
        /// <param name="offset">The starting index of the segment to reverse.</param>
        /// <param name="size">The size of the segment to reverse.</param>
        /// <remarks>
        /// This is used to convert the Cuthill-McKee ordering to the Reverse Cuthill-McKee ordering,
        /// which typically provides better bandwidth reduction.
        /// </remarks>
        private void ReverseVector(int[] a, int offset, int size)
        {
            for (int index = 0; index < size / 2; ++index)
            {
                (a[offset + index], a[offset + size - 1 - index]) = (a[offset + size - 1 - index], a[offset + index]);
            }
        }
    }
}