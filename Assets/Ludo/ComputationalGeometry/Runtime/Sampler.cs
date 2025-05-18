using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides efficient sampling of triangles from a mesh for point location algorithms.
    /// </summary>
    /// <remarks>
    /// The Sampler class implements a stratified sampling strategy to select representative
    /// triangles from a mesh. This is primarily used by the TriangleLocator to improve the
    /// efficiency of point location operations by providing good starting triangles.
    ///
    /// The number of samples is automatically adjusted based on the size of the mesh to
    /// maintain a balance between sampling coverage and computational efficiency.
    /// </remarks>
    [System.Serializable]
    internal class Sampler
    {
        /// <summary>
        /// Random number generator for selecting sample triangles.
        /// </summary>
        private static Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// The number of triangles to sample from the mesh.
        /// </summary>
        private int samples = 1;

        /// <summary>
        /// The total number of triangles in the mesh when last updated.
        /// </summary>
        private int triangleCount;

        /// <summary>
        /// Factor used to determine the appropriate number of samples based on mesh size.
        /// </summary>
        private static int samplefactor = 11;

        /// <summary>
        /// Array of triangle keys (IDs) from the mesh.
        /// </summary>
        private int[] keys;

        /// <summary>
        /// Resets the sampler to its initial state.
        /// </summary>
        /// <remarks>
        /// This method resets the number of samples to 1 and clears the triangle count,
        /// effectively forcing the sampler to recalculate sampling parameters on the next update.
        /// </remarks>
        public void Reset()
        {
            this.samples = 1;
            this.triangleCount = 0;
        }

        /// <summary>
        /// Updates the sampler with the current state of the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to sample from.</param>
        /// <remarks>
        /// This is a convenience method that calls Update with forceUpdate set to false.
        /// </remarks>
        public void Update(TriangularMesh triangularMesh) => this.Update(triangularMesh, false);

        /// <summary>
        /// Updates the sampler with the current state of the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to sample from.</param>
        /// <param name="forceUpdate">If true, forces an update even if the triangle count hasn't changed.</param>
        /// <remarks>
        /// This method updates the sampler's internal state based on the current mesh.
        /// It adjusts the number of samples based on the mesh size and refreshes the list of triangle keys.
        ///
        /// The number of samples is calculated to ensure adequate coverage of the mesh while
        /// maintaining computational efficiency. The formula uses a cubic root relationship
        /// with the total number of triangles, scaled by the samplefactor.
        /// </remarks>
        public void Update(TriangularMesh triangularMesh, bool forceUpdate)
        {
            int count = triangularMesh.triangles.Count;
            if (!(this.triangleCount != count | forceUpdate))
                return;
            this.triangleCount = count;
            while (Sampler.samplefactor * this.samples * this.samples * this.samples < count)
                ++this.samples;
            this.keys = triangularMesh.triangles.Keys.ToArray<int>();
        }

        /// <summary>
        /// Gets an array of triangle IDs sampled from the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to sample from.</param>
        /// <returns>An array of triangle IDs representing the sampled triangles.</returns>
        /// <remarks>
        /// This method implements a stratified sampling strategy to select representative
        /// triangles from the mesh. It divides the range of triangle indices into equal-sized
        /// strata and randomly selects one triangle from each stratum.
        ///
        /// If a selected triangle no longer exists in the mesh (which can happen if the mesh
        /// has been modified), the sampler is updated and the selection is retried.
        ///
        /// The stratified approach ensures good spatial distribution of the samples across
        /// the mesh, which improves the efficiency of point location algorithms.
        /// </remarks>
        public int[] GetSamples(TriangularMesh triangularMesh)
        {
            List<int> intList = new List<int>(this.samples);
            int num = this.triangleCount / this.samples;
            for (int index1 = 0; index1 < this.samples; ++index1)
            {
                int index2 = Sampler.rand.Next(index1 * num, (index1 + 1) * num - 1);
                if (!triangularMesh.triangles.Keys.Contains<int>(this.keys[index2]))
                {
                    this.Update(triangularMesh, true);
                    --index1;
                }
                else
                    intList.Add(this.keys[index2]);
            }
            return intList.ToArray();
        }
    }
}