namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a priority queue for triangles that violate quality constraints.
    /// Used during mesh quality improvement to efficiently process triangles based on their quality measure.
    /// </summary>
    [System.Serializable]
    public class QualityViolatingTriangleQueue
    {
        /// <summary>
        /// The square root of 2, used in the bucketing algorithm.
        /// </summary>
        private static readonly double SQRT2 = 1.4142135623730951;

        /// <summary>
        /// Array of pointers to the front of each bucket in the queue.
        /// </summary>
        private QualityViolatingTriangle[] queuefront;

        /// <summary>
        /// Array of pointers to the tail of each bucket in the queue.
        /// </summary>
        private QualityViolatingTriangle[] queuetail;

        /// <summary>
        /// Array of indices to the next non-empty bucket in the queue.
        /// </summary>
        private int[] nextnonemptyq;

        /// <summary>
        /// Index of the first non-empty bucket in the queue.
        /// </summary>
        private int firstnonemptyq;

        /// <summary>
        /// The number of triangles in the queue.
        /// </summary>
        private int count;

        /// <summary>
        /// Gets the number of triangles in the queue.
        /// </summary>
        public int Count => this.count;

        /// <summary>
        /// Initializes a new instance of the <see cref="QualityViolatingTriangleQueue"/> class.
        /// </summary>
        public QualityViolatingTriangleQueue()
        {
            this.queuefront = new QualityViolatingTriangle[4096 /*0x1000*/];
            this.queuetail = new QualityViolatingTriangle[4096 /*0x1000*/];
            this.nextnonemptyq = new int[4096 /*0x1000*/];
            this.firstnonemptyq = -1;
            this.count = 0;
        }

        /// <summary>
        /// Adds a bad triangle to the priority queue.
        /// </summary>
        /// <param name="badtri">The bad triangle to add to the queue.</param>
        /// <remarks>
        /// This method uses a bucketing algorithm to efficiently prioritize triangles.
        /// Triangles are placed in buckets based on their quality measure (key),
        /// allowing for fast retrieval of the worst-quality triangles.
        /// </remarks>
        public void Enqueue(QualityViolatingTriangle badtri)
        {
            ++this.count;
            double num1;
            int num2;
            if (badtri.key >= 1.0)
            {
                num1 = badtri.key;
                num2 = 1;
            }
            else
            {
                num1 = 1.0 / badtri.key;
                num2 = 0;
            }
            int num3 = 0;
            double num4;
            for (; num1 > 2.0; num1 *= num4)
            {
                int num5 = 1;
                for (num4 = 0.5; num1 * num4 * num4 > 1.0; num4 *= num4)
                    num5 *= 2;
                num3 += num5;
            }
            int num6 = 2 * num3 + (num1 > QualityViolatingTriangleQueue.SQRT2 ? 1 : 0);
            int index1 = num2 <= 0 ? 2048 /*0x0800*/ + num6 : 2047 /*0x07FF*/ - num6;
            if (this.queuefront[index1] == null)
            {
                if (index1 > this.firstnonemptyq)
                {
                    this.nextnonemptyq[index1] = this.firstnonemptyq;
                    this.firstnonemptyq = index1;
                }
                else
                {
                    int index2 = index1 + 1;
                    while (this.queuefront[index2] == null)
                        ++index2;
                    this.nextnonemptyq[index1] = this.nextnonemptyq[index2];
                    this.nextnonemptyq[index2] = index1;
                }
                this.queuefront[index1] = badtri;
            }
            else
                this.queuetail[index1].nexttriang = badtri;
            this.queuetail[index1] = badtri;
            badtri.nexttriang = (QualityViolatingTriangle) null;
        }

        /// <summary>
        /// Adds a bad triangle to the priority queue by creating a new QualityViolatingTriangle instance.
        /// </summary>
        /// <param name="enqtri">The oriented triangle to add to the queue.</param>
        /// <param name="minedge">The quality measure of the triangle (typically the shortest edge length).</param>
        /// <param name="enqapex">The apex vertex of the triangle.</param>
        /// <param name="enqorg">The origin vertex of the triangle.</param>
        /// <param name="enqdest">The destination vertex of the triangle.</param>
        public void Enqueue(
            ref Otri enqtri,
            double minedge,
            Vertex enqapex,
            Vertex enqorg,
            Vertex enqdest)
        {
            this.Enqueue(new QualityViolatingTriangle()
            {
                poortri = enqtri,
                key = minedge,
                triangapex = enqapex,
                triangorg = enqorg,
                triangdest = enqdest
            });
        }

        /// <summary>
        /// Removes and returns the bad triangle with the highest priority from the queue.
        /// </summary>
        /// <returns>
        /// The bad triangle with the highest priority, or null if the queue is empty.
        /// Triangles with lower quality measures (smaller keys) have higher priority.
        /// </returns>
        public QualityViolatingTriangle Dequeue()
        {
            if (this.firstnonemptyq < 0)
                return (QualityViolatingTriangle) null;
            --this.count;
            QualityViolatingTriangle qualityViolatingTriangle = this.queuefront[this.firstnonemptyq];
            this.queuefront[this.firstnonemptyq] = qualityViolatingTriangle.nexttriang;
            if (qualityViolatingTriangle == this.queuetail[this.firstnonemptyq])
                this.firstnonemptyq = this.nextnonemptyq[this.firstnonemptyq];
            return qualityViolatingTriangle;
        }
    }
}