using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements the sweep line algorithm for Delaunay triangulation.
    /// </summary>
    /// <remarks>
    /// The SweepLine class implements Fortune's sweep line algorithm for constructing
    /// a Delaunay triangulation of a set of points in the plane. The algorithm uses
    /// a horizontal sweep line that moves from top to bottom, processing events as it
    /// encounters them.
    ///
    /// The algorithm maintains two key data structures:
    /// 1. A priority queue of events (vertex events and circle events)
    /// 2. A balanced binary tree (implemented as a splay tree) representing the beach line
    ///
    /// This implementation is optimized for robustness and efficiency, using exact
    /// geometric predicates for numerical stability.
    /// </remarks>
    [System.Serializable]
    internal class SweepLine
    {
        /// <summary>
        /// Seed for the random number generator.
        /// </summary>
        private static int randomseed = 1;

        /// <summary>
        /// Rate at which to sample triangles for the splay tree.
        /// </summary>
        private static int SAMPLERATE = 10;

        /// <summary>
        /// Reference to the mesh being triangulated.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// Extreme x-coordinate used for circle events.
        /// </summary>
        private double xminextreme;

        /// <summary>
        /// List of nodes in the splay tree representing the beach line.
        /// </summary>
        private List<SweepLine.SplayNode> splaynodes;

        /// <summary>
        /// Generates a random integer between 0 and choices-1.
        /// </summary>
        /// <param name="choices">The upper bound (exclusive) for the random number.</param>
        /// <returns>A random integer between 0 and choices-1.</returns>
        /// <remarks>
        /// This method implements a simple linear congruential generator for
        /// deterministic random number generation. It's used for randomly sampling
        /// triangles to add to the splay tree.
        /// </remarks>
        private int randomnation(int choices)
        {
            SweepLine.randomseed = (SweepLine.randomseed * 1366 + 150889) % 714025;
            return SweepLine.randomseed / (714025 / choices + 1);
        }

        /// <summary>
        /// Inserts a new event into the event priority queue.
        /// </summary>
        /// <param name="heap">The event priority queue.</param>
        /// <param name="heapsize">The current size of the heap.</param>
        /// <param name="newevent">The new event to insert.</param>
        /// <remarks>
        /// This method maintains the heap property of the priority queue, ensuring that
        /// events are processed in the correct order (by y-coordinate, then by x-coordinate).
        /// </remarks>
        private void HeapInsert(SweepLine.SweepEvent[] heap, int heapsize, SweepLine.SweepEvent newevent)
        {
            double xkey = newevent.xkey;
            double ykey = newevent.ykey;
            int index1 = heapsize;
            bool flag = index1 > 0;
            while (flag)
            {
                int index2 = index1 - 1 >> 1;
                if (heap[index2].ykey < ykey || heap[index2].ykey == ykey && heap[index2].xkey <= xkey)
                {
                    flag = false;
                }
                else
                {
                    heap[index1] = heap[index2];
                    heap[index1].heapposition = index1;
                    index1 = index2;
                    flag = index1 > 0;
                }
            }

            heap[index1] = newevent;
            newevent.heapposition = index1;
        }

        /// <summary>
        /// Restores the heap property after a node has been modified.
        /// </summary>
        /// <param name="heap">The event priority queue.</param>
        /// <param name="heapsize">The current size of the heap.</param>
        /// <param name="eventnum">The index of the node to heapify.</param>
        /// <remarks>
        /// This method is used to maintain the heap property after a node has been
        /// modified or after the root has been removed. It ensures that the smallest
        /// event (by y-coordinate, then by x-coordinate) is at the root of the heap.
        /// </remarks>
        private void Heapify(SweepLine.SweepEvent[] heap, int heapsize, int eventnum)
        {
            SweepLine.SweepEvent sweepEvent = heap[eventnum];
            double xkey = sweepEvent.xkey;
            double ykey = sweepEvent.ykey;
            int index1 = 2 * eventnum + 1;
            bool flag = index1 < heapsize;
            while (flag)
            {
                int index2 = heap[index1].ykey < ykey || heap[index1].ykey == ykey && heap[index1].xkey < xkey
                    ? index1
                    : eventnum;
                int index3 = index1 + 1;
                if (index3 < heapsize && (heap[index3].ykey < heap[index2].ykey ||
                                          heap[index3].ykey == heap[index2].ykey &&
                                          heap[index3].xkey < heap[index2].xkey))
                    index2 = index3;
                if (index2 == eventnum)
                {
                    flag = false;
                }
                else
                {
                    heap[eventnum] = heap[index2];
                    heap[eventnum].heapposition = eventnum;
                    heap[index2] = sweepEvent;
                    sweepEvent.heapposition = index2;
                    eventnum = index2;
                    index1 = 2 * eventnum + 1;
                    flag = index1 < heapsize;
                }
            }
        }

        /// <summary>
        /// Deletes an event from the event priority queue.
        /// </summary>
        /// <param name="heap">The event priority queue.</param>
        /// <param name="heapsize">The current size of the heap.</param>
        /// <param name="eventnum">The index of the event to delete.</param>
        /// <remarks>
        /// This method removes an event from the priority queue and restores the heap property.
        /// It replaces the deleted event with the last event in the heap and then
        /// either bubbles it up or down as needed to maintain the heap property.
        /// </remarks>
        private void HeapDelete(SweepLine.SweepEvent[] heap, int heapsize, int eventnum)
        {
            SweepLine.SweepEvent sweepEvent = heap[heapsize - 1];
            if (eventnum > 0)
            {
                double xkey = sweepEvent.xkey;
                double ykey = sweepEvent.ykey;
                bool flag;
                do
                {
                    int index = eventnum - 1 >> 1;
                    if (heap[index].ykey < ykey || heap[index].ykey == ykey && heap[index].xkey <= xkey)
                    {
                        flag = false;
                    }
                    else
                    {
                        heap[eventnum] = heap[index];
                        heap[eventnum].heapposition = eventnum;
                        eventnum = index;
                        flag = eventnum > 0;
                    }
                } while (flag);
            }

            heap[eventnum] = sweepEvent;
            sweepEvent.heapposition = eventnum;
            this.Heapify(heap, heapsize - 1, eventnum);
        }

        /// <summary>
        /// Creates the initial event priority queue from the input vertices.
        /// </summary>
        /// <param name="eventheap">When this method returns, contains the initialized event priority queue.</param>
        /// <remarks>
        /// This method initializes the event priority queue with vertex events for all
        /// input vertices. Each vertex event is placed in the queue according to its
        /// y-coordinate (primary key) and x-coordinate (secondary key).
        /// </remarks>
        private void CreateHeap(out SweepLine.SweepEvent[] eventheap)
        {
            int length = 3 * this._triangularMesh.invertices / 2;
            eventheap = new SweepLine.SweepEvent[length];
            int num = 0;
            foreach (Vertex vertex in this._triangularMesh.vertices.Values)
                this.HeapInsert(eventheap, num++, new SweepLine.SweepEvent()
                {
                    vertexEvent = vertex,
                    xkey = vertex.x,
                    ykey = vertex.y
                });
        }

        /// <summary>
        /// Splays the tree so that the node closest to the search point becomes the root.
        /// </summary>
        /// <param name="splaytree">The root of the splay tree.</param>
        /// <param name="searchpoint">The point to search for.</param>
        /// <param name="searchtri">When this method returns, contains the triangle closest to the search point.</param>
        /// <returns>The new root of the splay tree after the splay operation.</returns>
        /// <remarks>
        /// This method implements the splay operation, which restructures the tree to bring
        /// the node closest to the search point to the root. This improves the efficiency
        /// of subsequent operations on the same or nearby points.
        ///
        /// The splay operation is a key component of the splay tree data structure, which
        /// is used to represent the beach line in Fortune's algorithm.
        /// </remarks>
        private SweepLine.SplayNode Splay(
            SweepLine.SplayNode splaytree,
            Point searchpoint,
            ref Otri searchtri)
        {
            if (splaytree == null)
                return (SweepLine.SplayNode)null;
            if ((Point)splaytree.keyedge.Dest() == (Point)splaytree.keydest)
            {
                bool flag1 = this.RightOfHyperbola(ref splaytree.keyedge, searchpoint);
                SweepLine.SplayNode splaytree1;
                if (flag1)
                {
                    splaytree.keyedge.Copy(ref searchtri);
                    splaytree1 = splaytree.rchild;
                }
                else
                    splaytree1 = splaytree.lchild;

                if (splaytree1 == null)
                    return splaytree;
                if ((Point)splaytree1.keyedge.Dest() != (Point)splaytree1.keydest)
                {
                    splaytree1 = this.Splay(splaytree1, searchpoint, ref searchtri);
                    if (splaytree1 == null)
                    {
                        if (flag1)
                            splaytree.rchild = (SweepLine.SplayNode)null;
                        else
                            splaytree.lchild = (SweepLine.SplayNode)null;
                        return splaytree;
                    }
                }

                bool flag2 = this.RightOfHyperbola(ref splaytree1.keyedge, searchpoint);
                SweepLine.SplayNode splayNode;
                if (flag2)
                {
                    splaytree1.keyedge.Copy(ref searchtri);
                    splayNode = this.Splay(splaytree1.rchild, searchpoint, ref searchtri);
                    splaytree1.rchild = splayNode;
                }
                else
                {
                    splayNode = this.Splay(splaytree1.lchild, searchpoint, ref searchtri);
                    splaytree1.lchild = splayNode;
                }

                if (splayNode == null)
                {
                    if (flag1)
                    {
                        splaytree.rchild = splaytree1.lchild;
                        splaytree1.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = splaytree1.rchild;
                        splaytree1.rchild = splaytree;
                    }

                    return splaytree1;
                }

                if (flag2)
                {
                    if (flag1)
                    {
                        splaytree.rchild = splaytree1.lchild;
                        splaytree1.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = splayNode.rchild;
                        splayNode.rchild = splaytree;
                    }

                    splaytree1.rchild = splayNode.lchild;
                    splayNode.lchild = splaytree1;
                }
                else
                {
                    if (flag1)
                    {
                        splaytree.rchild = splayNode.lchild;
                        splayNode.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = splaytree1.rchild;
                        splaytree1.rchild = splaytree;
                    }

                    splaytree1.lchild = splayNode.rchild;
                    splayNode.rchild = splaytree1;
                }

                return splayNode;
            }

            SweepLine.SplayNode splayNode1 = this.Splay(splaytree.lchild, searchpoint, ref searchtri);
            SweepLine.SplayNode splayNode2 = this.Splay(splaytree.rchild, searchpoint, ref searchtri);
            this.splaynodes.Remove(splaytree);
            if (splayNode1 == null)
                return splayNode2;
            if (splayNode2 == null)
                return splayNode1;
            if (splayNode1.rchild == null)
            {
                splayNode1.rchild = splayNode2.lchild;
                splayNode2.lchild = splayNode1;
                return splayNode2;
            }

            if (splayNode2.lchild == null)
            {
                splayNode2.lchild = splayNode1.rchild;
                splayNode1.rchild = splayNode2;
                return splayNode1;
            }

            SweepLine.SplayNode rchild = splayNode1.rchild;
            while (rchild.rchild != null)
                rchild = rchild.rchild;
            rchild.rchild = splayNode2;
            return splayNode1;
        }

        /// <summary>
        /// Inserts a new node into the splay tree.
        /// </summary>
        /// <param name="splayroot">The root of the splay tree.</param>
        /// <param name="newkey">The triangle to insert.</param>
        /// <param name="searchpoint">The point used to determine the insertion position.</param>
        /// <returns>The new root of the splay tree after the insertion.</returns>
        /// <remarks>
        /// This method inserts a new node into the splay tree, which represents the beach line
        /// in Fortune's algorithm. The insertion position is determined by the search point,
        /// which is typically the site being processed.
        /// </remarks>
        private SweepLine.SplayNode SplayInsert(
            SweepLine.SplayNode splayroot,
            Otri newkey,
            Point searchpoint)
        {
            SweepLine.SplayNode splayNode = new SweepLine.SplayNode();
            this.splaynodes.Add(splayNode);
            newkey.Copy(ref splayNode.keyedge);
            splayNode.keydest = newkey.Dest();
            if (splayroot == null)
            {
                splayNode.lchild = (SweepLine.SplayNode)null;
                splayNode.rchild = (SweepLine.SplayNode)null;
            }
            else if (this.RightOfHyperbola(ref splayroot.keyedge, searchpoint))
            {
                splayNode.lchild = splayroot;
                splayNode.rchild = splayroot.rchild;
                splayroot.rchild = (SweepLine.SplayNode)null;
            }
            else
            {
                splayNode.lchild = splayroot.lchild;
                splayNode.rchild = splayroot;
                splayroot.lchild = (SweepLine.SplayNode)null;
            }

            return splayNode;
        }

        /// <summary>
        /// Inserts a new node into the splay tree based on a circle event.
        /// </summary>
        /// <param name="splayroot">The root of the splay tree.</param>
        /// <param name="newkey">The triangle to insert.</param>
        /// <param name="pa">The first vertex of the circle.</param>
        /// <param name="pb">The second vertex of the circle.</param>
        /// <param name="pc">The third vertex of the circle.</param>
        /// <param name="topy">The y-coordinate of the top of the circle.</param>
        /// <returns>The new root of the splay tree after the insertion.</returns>
        /// <remarks>
        /// This method is used to insert a new node into the splay tree when a circle event
        /// is processed. The insertion position is determined by the center of the circle
        /// formed by the three vertices.
        /// </remarks>
        private SweepLine.SplayNode CircleTopInsert(
            SweepLine.SplayNode splayroot,
            Otri newkey,
            Vertex pa,
            Vertex pb,
            Vertex pc,
            double topy)
        {
            Point searchpoint = new Point();
            Otri searchtri = new Otri();
            double num1 = Primitives.CounterClockwise((Point)pa, (Point)pb, (Point)pc);
            double num2 = pa.x - pc.x;
            double num3 = pa.y - pc.y;
            double num4 = pb.x - pc.x;
            double num5 = pb.y - pc.y;
            double num6 = num2 * num2 + num3 * num3;
            double num7 = num4 * num4 + num5 * num5;
            searchpoint.x = pc.x - (num3 * num7 - num5 * num6) / (2.0 * num1);
            searchpoint.y = topy;
            return this.SplayInsert(this.Splay(splayroot, searchpoint, ref searchtri), newkey, searchpoint);
        }

        /// <summary>
        /// Determines whether a point is to the right of a parabola in the beach line.
        /// </summary>
        /// <param name="fronttri">The triangle representing a parabola in the beach line.</param>
        /// <param name="newsite">The point to test.</param>
        /// <returns>True if the point is to the right of the parabola; otherwise, false.</returns>
        /// <remarks>
        /// This method implements a key geometric predicate in Fortune's algorithm. It determines
        /// whether a new site is to the right of a parabola in the beach line, which is used
        /// to locate the correct position for inserting the new site.
        ///
        /// The test is based on the relative positions of the site defining the parabola and
        /// the new site, taking into account the current position of the sweep line.
        /// </remarks>
        private bool RightOfHyperbola(ref Otri fronttri, Point newsite)
        {
            ++Statistic.HyperbolaCount;
            Vertex vertex1 = fronttri.Dest();
            Vertex vertex2 = fronttri.Apex();
            if (vertex1.y < vertex2.y || vertex1.y == vertex2.y && vertex1.x < vertex2.x)
            {
                if (newsite.x >= vertex2.x)
                    return true;
            }
            else if (newsite.x <= vertex1.x)
                return false;

            double num1 = vertex1.x - newsite.x;
            double num2 = vertex1.y - newsite.y;
            double num3 = vertex2.x - newsite.x;
            double num4 = vertex2.y - newsite.y;
            return num2 * (num3 * num3 + num4 * num4) > num4 * (num1 * num1 + num2 * num2);
        }

        /// <summary>
        /// Calculates the y-coordinate of the top of the circle passing through three vertices.
        /// </summary>
        /// <param name="pa">The first vertex of the circle.</param>
        /// <param name="pb">The second vertex of the circle.</param>
        /// <param name="pc">The third vertex of the circle.</param>
        /// <param name="ccwabc">The signed area of the triangle formed by the three vertices.</param>
        /// <returns>The y-coordinate of the top of the circle.</returns>
        /// <remarks>
        /// This method is used to determine when a circle event will occur during the sweep.
        /// The top of the circle is the point where the sweep line will first intersect the circle,
        /// which is when the circle event should be processed.
        /// </remarks>
        private double CircleTop(Vertex pa, Vertex pb, Vertex pc, double ccwabc)
        {
            ++Statistic.CircleTopCount;
            double num1 = pa.x - pc.x;
            double num2 = pa.y - pc.y;
            double num3 = pb.x - pc.x;
            double num4 = pb.y - pc.y;
            double num5 = pa.x - pb.x;
            double num6 = pa.y - pb.y;
            double num7 = num1 * num1 + num2 * num2;
            double num8 = num3 * num3 + num4 * num4;
            double num9 = num5 * num5 + num6 * num6;
            return pc.y + (num1 * num8 - num3 * num7 + Math.Sqrt(num7 * num8 * num9)) / (2.0 * ccwabc);
        }

        /// <summary>
        /// Checks if a triangle contains a dead event and removes it from the event queue.
        /// </summary>
        /// <param name="checktri">The triangle to check.</param>
        /// <param name="eventheap">The event priority queue.</param>
        /// <param name="heapsize">The current size of the heap, which is updated if an event is removed.</param>
        /// <remarks>
        /// This method is used to check if a triangle contains a circle event that has become
        /// invalid (dead) due to changes in the triangulation. If such an event is found,
        /// it is removed from the event queue to prevent it from being processed.
        /// </remarks>
        private void Check4DeadEvent(
            ref Otri checktri,
            SweepLine.SweepEvent[] eventheap,
            ref int heapsize)
        {
            SweepLine.SweepEventVertex sweepEventVertex = checktri.Org() as SweepLine.SweepEventVertex;
            if (!((Point)sweepEventVertex != (Point)null))
                return;
            int heapposition = sweepEventVertex.evt.heapposition;
            this.HeapDelete(eventheap, heapsize, heapposition);
            --heapsize;
            checktri.SetOrg((Vertex)null);
        }

        /// <summary>
        /// Locates the position in the beach line where a new site should be inserted.
        /// </summary>
        /// <param name="splayroot">The root of the splay tree representing the beach line.</param>
        /// <param name="bottommost">The bottommost triangle in the triangulation.</param>
        /// <param name="searchvertex">The new site to insert.</param>
        /// <param name="searchtri">When this method returns, contains the triangle where the new site should be inserted.</param>
        /// <param name="farright">When this method returns, indicates whether the insertion point is at the far right of the beach line.</param>
        /// <returns>The new root of the splay tree after the location operation.</returns>
        /// <remarks>
        /// This method is used to locate the position in the beach line where a new site should be inserted.
        /// It first splays the tree to bring the node closest to the search vertex to the root,
        /// then walks along the beach line to find the exact insertion position.
        /// </remarks>
        private SweepLine.SplayNode FrontLocate(
            SweepLine.SplayNode splayroot,
            Otri bottommost,
            Vertex searchvertex,
            ref Otri searchtri,
            ref bool farright)
        {
            bottommost.Copy(ref searchtri);
            splayroot = this.Splay(splayroot, (Point)searchvertex, ref searchtri);
            bool flag;
            for (flag = false;
                 !flag && this.RightOfHyperbola(ref searchtri, (Point)searchvertex);
                 flag = searchtri.Equal(bottommost))
                searchtri.OnextSelf();
            farright = flag;
            return splayroot;
        }

        /// <summary>
        /// Removes ghost triangles from the triangulation.
        /// </summary>
        /// <param name="startghost">The starting ghost triangle.</param>
        /// <returns>The number of ghost triangles removed.</returns>
        /// <remarks>
        /// This method is used to clean up the triangulation after the sweep is complete.
        /// It removes the ghost triangles that were added during the algorithm to handle
        /// the convex hull and other special cases.
        /// </remarks>
        private int RemoveGhosts(ref Otri startghost)
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Otri o2_3 = new Otri();
            bool flag = !this._triangularMesh.behavior.Poly;
            startghost.Lprev(ref o2_1);
            o2_1.SymSelf();
            TriangularMesh.dummytri.neighbors[0] = o2_1;
            startghost.Copy(ref o2_2);
            int num = 0;
            do
            {
                ++num;
                o2_2.Lnext(ref o2_3);
                o2_2.LprevSelf();
                o2_2.SymSelf();
                if (flag && o2_2.triangle != TriangularMesh.dummytri)
                {
                    Vertex vertex = o2_2.Org();
                    if (vertex.mark == 0)
                        vertex.mark = 1;
                }

                o2_2.Dissolve();
                o2_3.Sym(ref o2_2);
                this._triangularMesh.TriangleDealloc(o2_3.triangle);
            } while (!o2_2.Equal(startghost));

            return num;
        }

        /// <summary>
        /// Triangulates a mesh using the sweep line algorithm.
        /// </summary>
        /// <param name="triangularMesh">The mesh to triangulate.</param>
        /// <returns>The number of triangles on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// This is the main entry point for the sweep line triangulation algorithm. It implements
        /// Fortune's algorithm, which uses a horizontal sweep line that moves from top to bottom,
        /// processing events as it encounters them.
        ///
        /// The algorithm maintains two key data structures:
        /// 1. A priority queue of events (vertex events and circle events)
        /// 2. A balanced binary tree (implemented as a splay tree) representing the beach line
        ///
        /// The algorithm processes events in order of decreasing y-coordinate. Vertex events
        /// correspond to input sites and result in the insertion of new parabolas into the beach line.
        /// Circle events occur when three consecutive parabolas in the beach line converge to a point,
        /// resulting in the creation of a new Delaunay triangle.
        ///
        /// After all events have been processed, the algorithm removes ghost triangles and
        /// returns the number of triangles on the convex hull.
        /// </remarks>
        public int Triangulate(TriangularMesh triangularMesh)
        {
            this._triangularMesh = triangularMesh;
            this.xminextreme = 10.0 * triangularMesh.bounds.Xmin - 9.0 * triangularMesh.bounds.Xmax;
            Otri otri1 = new Otri();
            Otri otri2 = new Otri();
            Otri newkey = new Otri();
            Otri otri3 = new Otri();
            Otri otri4 = new Otri();
            Otri otri5 = new Otri();
            Otri o2 = new Otri();
            bool farright = false;
            this.splaynodes = new List<SweepLine.SplayNode>();
            SweepLine.SplayNode splayroot = (SweepLine.SplayNode)null;
            SweepLine.SweepEvent[] eventheap;
            this.CreateHeap(out eventheap);
            int invertices = triangularMesh.invertices;
            triangularMesh.MakeTriangle(ref newkey);
            triangularMesh.MakeTriangle(ref otri3);
            newkey.Bond(ref otri3);
            newkey.LnextSelf();
            otri3.LprevSelf();
            newkey.Bond(ref otri3);
            newkey.LnextSelf();
            otri3.LprevSelf();
            newkey.Bond(ref otri3);
            Vertex vertexEvent1 = eventheap[0].vertexEvent;
            this.HeapDelete(eventheap, invertices, 0);
            int heapsize = invertices - 1;
            while (heapsize != 0)
            {
                Vertex vertexEvent2 = eventheap[0].vertexEvent;
                this.HeapDelete(eventheap, heapsize, 0);
                --heapsize;
                if (vertexEvent1.x == vertexEvent2.x && vertexEvent1.y == vertexEvent2.y)
                {
                    vertexEvent2.type = VertexType.UndeadVertex;
                    ++triangularMesh.undeads;
                }

                if (vertexEvent1.x != vertexEvent2.x || vertexEvent1.y != vertexEvent2.y)
                {
                    newkey.SetOrg(vertexEvent1);
                    newkey.SetDest(vertexEvent2);
                    otri3.SetOrg(vertexEvent2);
                    otri3.SetDest(vertexEvent1);
                    newkey.Lprev(ref otri1);
                    Vertex vertex = vertexEvent2;
                    while (heapsize > 0)
                    {
                        SweepLine.SweepEvent sweepEvent1 = eventheap[0];
                        this.HeapDelete(eventheap, heapsize, 0);
                        --heapsize;
                        bool flag = true;
                        if (sweepEvent1.xkey < triangularMesh.bounds.Xmin)
                        {
                            Otri otriEvent = sweepEvent1.otriEvent;
                            otriEvent.Oprev(ref otri4);
                            this.Check4DeadEvent(ref otri4, eventheap, ref heapsize);
                            otriEvent.Onext(ref otri5);
                            this.Check4DeadEvent(ref otri5, eventheap, ref heapsize);
                            if (otri4.Equal(otri1))
                                otriEvent.Lprev(ref otri1);
                            triangularMesh.Flip(ref otriEvent);
                            otriEvent.SetApex((Vertex)null);
                            otriEvent.Lprev(ref newkey);
                            otriEvent.Lnext(ref otri3);
                            newkey.Sym(ref otri4);
                            if (this.randomnation(SweepLine.SAMPLERATE) == 0)
                            {
                                otriEvent.SymSelf();
                                Vertex pa = otriEvent.Dest();
                                Vertex pb = otriEvent.Apex();
                                Vertex pc = otriEvent.Org();
                                splayroot = this.CircleTopInsert(splayroot, newkey, pa, pb, pc, sweepEvent1.ykey);
                            }
                        }
                        else
                        {
                            Vertex vertexEvent3 = sweepEvent1.vertexEvent;
                            if (vertexEvent3.x == vertex.x && vertexEvent3.y == vertex.y)
                            {
                                vertexEvent3.type = VertexType.UndeadVertex;
                                ++triangularMesh.undeads;
                                flag = false;
                            }
                            else
                            {
                                vertex = vertexEvent3;
                                splayroot = this.FrontLocate(splayroot, otri1, vertexEvent3, ref otri2, ref farright);
                                otri1.Copy(ref otri2);
                                for (farright = false;
                                     !farright && this.RightOfHyperbola(ref otri2, (Point)vertexEvent3);
                                     farright = otri2.Equal(otri1))
                                    otri2.OnextSelf();
                                this.Check4DeadEvent(ref otri2, eventheap, ref heapsize);
                                otri2.Copy(ref otri5);
                                otri2.Sym(ref otri4);
                                triangularMesh.MakeTriangle(ref newkey);
                                triangularMesh.MakeTriangle(ref otri3);
                                Vertex ptr = otri5.Dest();
                                newkey.SetOrg(ptr);
                                newkey.SetDest(vertexEvent3);
                                otri3.SetOrg(vertexEvent3);
                                otri3.SetDest(ptr);
                                newkey.Bond(ref otri3);
                                newkey.LnextSelf();
                                otri3.LprevSelf();
                                newkey.Bond(ref otri3);
                                newkey.LnextSelf();
                                otri3.LprevSelf();
                                newkey.Bond(ref otri4);
                                otri3.Bond(ref otri5);
                                if (!farright && otri5.Equal(otri1))
                                    newkey.Copy(ref otri1);
                                if (this.randomnation(SweepLine.SAMPLERATE) == 0)
                                    splayroot = this.SplayInsert(splayroot, newkey, (Point)vertexEvent3);
                                else if (this.randomnation(SweepLine.SAMPLERATE) == 0)
                                {
                                    otri3.Lnext(ref o2);
                                    splayroot = this.SplayInsert(splayroot, o2, (Point)vertexEvent3);
                                }
                            }
                        }

                        if (flag)
                        {
                            Vertex pa1 = otri4.Apex();
                            Vertex pb1 = newkey.Dest();
                            Vertex pc1 = newkey.Apex();
                            double ccwabc1 = Primitives.CounterClockwise((Point)pa1, (Point)pb1, (Point)pc1);
                            if (ccwabc1 > 0.0)
                            {
                                SweepLine.SweepEvent sweepEvent2 = new SweepLine.SweepEvent();
                                sweepEvent2.xkey = this.xminextreme;
                                sweepEvent2.ykey = this.CircleTop(pa1, pb1, pc1, ccwabc1);
                                sweepEvent2.otriEvent = newkey;
                                this.HeapInsert(eventheap, heapsize, sweepEvent2);
                                ++heapsize;
                                newkey.SetOrg((Vertex)new SweepLine.SweepEventVertex(sweepEvent2));
                            }

                            Vertex pa2 = otri3.Apex();
                            Vertex pb2 = otri3.Org();
                            Vertex pc2 = otri5.Apex();
                            double ccwabc2 = Primitives.CounterClockwise((Point)pa2, (Point)pb2, (Point)pc2);
                            if (ccwabc2 > 0.0)
                            {
                                SweepLine.SweepEvent sweepEvent3 = new SweepLine.SweepEvent();
                                sweepEvent3.xkey = this.xminextreme;
                                sweepEvent3.ykey = this.CircleTop(pa2, pb2, pc2, ccwabc2);
                                sweepEvent3.otriEvent = otri5;
                                this.HeapInsert(eventheap, heapsize, sweepEvent3);
                                ++heapsize;
                                otri5.SetOrg((Vertex)new SweepLine.SweepEventVertex(sweepEvent3));
                            }
                        }
                    }

                    this.splaynodes.Clear();
                    otri1.LprevSelf();
                    return this.RemoveGhosts(ref otri1);
                }
            }

            throw new Exception("Input vertices are all identical.");
        }

        /// <summary>
        /// Represents an event in the sweep line algorithm.
        /// </summary>
        /// <remarks>
        /// Events are either vertex events (corresponding to input sites) or circle events
        /// (corresponding to points where three parabolas in the beach line converge).
        /// Events are processed in order of decreasing y-coordinate.
        /// </remarks>
        private class SweepEvent
        {
            /// <summary>
            /// The x-coordinate of the event.
            /// </summary>
            public double xkey;

            /// <summary>
            /// The y-coordinate of the event.
            /// </summary>
            public double ykey;

            /// <summary>
            /// The vertex associated with the event (for vertex events).
            /// </summary>
            public Vertex vertexEvent;

            /// <summary>
            /// The triangle associated with the event (for circle events).
            /// </summary>
            public Otri otriEvent;

            /// <summary>
            /// The position of the event in the heap.
            /// </summary>
            public int heapposition;
        }

        /// <summary>
        /// Represents a vertex associated with a circle event.
        /// </summary>
        /// <remarks>
        /// This class is used to associate a circle event with a vertex in the triangulation.
        /// When a circle event is created, a SweepEventVertex is created and added to the
        /// triangulation to represent the center of the circle.
        /// </remarks>
        private class SweepEventVertex : Vertex
        {
            /// <summary>
            /// The circle event associated with this vertex.
            /// </summary>
            public SweepLine.SweepEvent evt;

            /// <summary>
            /// Initializes a new instance of the <see cref="SweepEventVertex"/> class.
            /// </summary>
            /// <param name="e">The circle event associated with this vertex.</param>
            public SweepEventVertex(SweepLine.SweepEvent e) => this.evt = e;
        }

        /// <summary>
        /// Represents a node in the splay tree that maintains the beach line.
        /// </summary>
        /// <remarks>
        /// Each node in the splay tree corresponds to a parabola in the beach line.
        /// The tree is structured so that the parabolas are ordered from left to right
        /// along the beach line.
        /// </remarks>
        private class SplayNode
        {
            /// <summary>
            /// The triangle associated with this node.
            /// </summary>
            public Otri keyedge;

            /// <summary>
            /// The destination vertex of the triangle's edge.
            /// </summary>
            public Vertex keydest;

            /// <summary>
            /// The left child of this node in the splay tree.
            /// </summary>
            public SweepLine.SplayNode lchild;

            /// <summary>
            /// The right child of this node in the splay tree.
            /// </summary>
            public SweepLine.SplayNode rchild;
        }
    }
}