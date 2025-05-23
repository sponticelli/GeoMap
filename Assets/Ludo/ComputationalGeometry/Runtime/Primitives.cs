using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides geometric primitive operations with exact arithmetic for robust computational geometry.
    /// </summary>
    /// <remarks>
    /// The Primitives class implements fundamental geometric predicates and constructions
    /// that form the basis of the triangulation algorithms. It uses adaptive exact arithmetic
    /// techniques to ensure numerical robustness even in degenerate cases.
    ///
    /// The key operations include orientation tests (CounterClockwise), in-circle tests (InCircle),
    /// and geometric constructions like finding the circumcenter of a triangle.
    /// </remarks>
    [Serializable]
    public static class Primitives
    {
        /// <summary>
        /// A value used in the adaptive exact arithmetic algorithm to split floating-point numbers.
        /// </summary>
        private static double _splitter;

        /// <summary>
        /// The machine epsilon value, representing the smallest value such that 1.0 + epsilon != 1.0.
        /// </summary>
        private static double _epsilon;

        /// <summary>
        /// Error bound for the CounterClockwise predicate.
        /// </summary>
        private static double _ccwErrBoundA;

        /// <summary>
        /// Error bound for the InCircle predicate.
        /// </summary>
        private static double _iccErrBoundA;

        /// <summary>
        /// Initializes the exact arithmetic system by computing machine-dependent constants.
        /// </summary>
        /// <remarks>
        /// This method computes the machine epsilon and related constants needed for the
        /// adaptive exact arithmetic algorithms. It should be called once before using
        /// any of the geometric predicates.
        ///
        /// The algorithm determines the smallest value of epsilon such that 1.0 + epsilon != 1.0
        /// in the floating-point representation, and then uses this to compute error bounds
        /// for the geometric predicates.
        /// </remarks>
        public static void ExactInit()
        {
            bool flag = true;
            double num1 = 0.5;
            _epsilon = 1.0;
            _splitter = 1.0;
            double num2 = 1.0;
            double num3;
            do
            {
                num3 = num2;
                _epsilon *= num1;
                if (flag)
                    _splitter *= 2.0;
                flag = !flag;
                num2 = 1.0 + _epsilon;
            } while (num2 != 1.0 && num2 != num3);

            ++_splitter;
            _ccwErrBoundA = (3.0 + 16.0 * _epsilon) * _epsilon;
            _iccErrBoundA = (10.0 + 96.0 * _epsilon) * _epsilon;
        }

        /// <summary>
        /// Determines whether three points form a counterclockwise turn.
        /// </summary>
        /// <param name="pa">The first point.</param>
        /// <param name="pb">The second point.</param>
        /// <param name="pc">The third point.</param>
        /// <returns>
        /// A positive value if the points form a counterclockwise turn,
        /// a negative value if they form a clockwise turn,
        /// and zero if they are collinear.
        /// </returns>
        /// <remarks>
        /// This is a fundamental geometric predicate that determines the orientation of three points.
        /// It computes the signed area of the triangle formed by the three points.
        ///
        /// The implementation uses adaptive exact arithmetic to ensure correct results even in
        /// nearly degenerate cases. It first attempts a fast floating-point calculation, and
        /// if the result is uncertain (close to zero), it switches to exact arithmetic using
        /// the Decimal type.
        /// </remarks>
        public static double CounterClockwise(Point pa, Point pb, Point pc)
        {
            ++Statistic.CounterClockwiseCount;
            double num1 = (pa.x - pc.x) * (pb.y - pc.y);
            double num2 = (pa.y - pc.y) * (pb.x - pc.x);
            double num3 = num1 - num2;
            if (TriangulationSettings.NoExact)
                return num3;
            double num4;
            if (num1 > 0.0)
            {
                if (num2 <= 0.0)
                    return num3;
                num4 = num1 + num2;
            }
            else
            {
                if (num1 >= 0.0 || num2 >= 0.0)
                    return num3;
                num4 = -num1 - num2;
            }

            double num5 = _ccwErrBoundA * num4;
            return num3 >= num5 || -num3 >= num5 ? num3 : (double)CounterClockwiseDecimal(pa, pb, pc);
        }

        /// <summary>
        /// Performs the CounterClockwise test using Decimal arithmetic for exact results.
        /// </summary>
        /// <param name="pa">The first point.</param>
        /// <param name="pb">The second point.</param>
        /// <param name="pc">The third point.</param>
        /// <returns>
        /// A Decimal value representing the signed area of the triangle formed by the three points.
        /// </returns>
        /// <remarks>
        /// This method is called by CounterClockwise when the floating-point calculation
        /// is deemed unreliable. It uses the Decimal type which provides higher precision
        /// than double, allowing for exact results in cases where floating-point arithmetic
        /// would produce errors.
        /// </remarks>
        private static Decimal CounterClockwiseDecimal(Point pa, Point pb, Point pc)
        {
            ++Statistic.CounterClockwiseCountDecimal;
            Decimal num1 = ((Decimal)pa.x - (Decimal)pc.x) * ((Decimal)pb.y - (Decimal)pc.y);
            Decimal num2 = ((Decimal)pa.y - (Decimal)pc.y) * ((Decimal)pb.x - (Decimal)pc.x);
            Decimal num3 = num1 - num2;
            if (num1 > 0.0M)
            {
                if (num2 <= 0.0M)
                    return num3;
                Decimal num4 = num1 + num2;
            }
            else if (num1 < 0.0M && !(num2 >= 0.0M))
            {
                Decimal num5 = -num1 - num2;
            }

            return num3;
        }

        /// <summary>
        /// Determines whether a point lies inside, outside, or on the circumcircle of three points.
        /// </summary>
        /// <param name="pa">The first point of the triangle.</param>
        /// <param name="pb">The second point of the triangle.</param>
        /// <param name="pc">The third point of the triangle.</param>
        /// <param name="pd">The point to test.</param>
        /// <returns>
        /// A positive value if pd lies inside the circumcircle,
        /// a negative value if pd lies outside the circumcircle,
        /// and zero if pd lies exactly on the circumcircle.
        /// </returns>
        /// <remarks>
        /// This predicate is crucial for Delaunay triangulation algorithms. It determines whether
        /// a point lies inside the circumcircle of a triangle, which is the defining property
        /// of Delaunay triangulations.
        ///
        /// Like CounterClockwise, this implementation uses adaptive exact arithmetic to ensure
        /// correct results even in nearly degenerate cases. It first attempts a fast floating-point
        /// calculation, and if the result is uncertain, it switches to exact arithmetic using
        /// the Decimal type.
        /// </remarks>
        public static double InCircle(Point pa, Point pb, Point pc, Point pd)
        {
            ++Statistic.InCircleCount;
            double num1 = pa.x - pd.x;
            double num2 = pb.x - pd.x;
            double num3 = pc.x - pd.x;
            double num4 = pa.y - pd.y;
            double num5 = pb.y - pd.y;
            double num6 = pc.y - pd.y;
            double num7 = num2 * num6;
            double num8 = num3 * num5;
            double num9 = num1 * num1 + num4 * num4;
            double num10 = num3 * num4;
            double num11 = num1 * num6;
            double num12 = num2 * num2 + num5 * num5;
            double num13 = num1 * num5;
            double num14 = num2 * num4;
            double num15 = num3 * num3 + num6 * num6;
            double num16 = num9 * (num7 - num8) + num12 * (num10 - num11) + num15 * (num13 - num14);
            if (TriangulationSettings.NoExact)
                return num16;
            double num17 = (Math.Abs(num7) + Math.Abs(num8)) * num9 + (Math.Abs(num10) + Math.Abs(num11)) * num12 +
                           (Math.Abs(num13) + Math.Abs(num14)) * num15;
            double num18 = _iccErrBoundA * num17;
            return num16 > num18 || -num16 > num18 ? num16 : (double)InCircleDecimal(pa, pb, pc, pd);
        }

        /// <summary>
        /// Performs the InCircle test using Decimal arithmetic for exact results.
        /// </summary>
        /// <param name="pa">The first point of the triangle.</param>
        /// <param name="pb">The second point of the triangle.</param>
        /// <param name="pc">The third point of the triangle.</param>
        /// <param name="pd">The point to test.</param>
        /// <returns>
        /// A Decimal value indicating whether pd is inside (positive), outside (negative),
        /// or on (zero) the circumcircle of the triangle formed by pa, pb, and pc.
        /// </returns>
        /// <remarks>
        /// This method is called by InCircle when the floating-point calculation
        /// is deemed unreliable. It uses the Decimal type which provides higher precision
        /// than double, allowing for exact results in cases where floating-point arithmetic
        /// would produce errors.
        /// </remarks>
        private static Decimal InCircleDecimal(Point pa, Point pb, Point pc, Point pd)
        {
            ++Statistic.InCircleCountDecimal;
            Decimal num1 = (Decimal)pa.x - (Decimal)pd.x;
            Decimal num2 = (Decimal)pb.x - (Decimal)pd.x;
            Decimal num3 = (Decimal)pc.x - (Decimal)pd.x;
            Decimal num4 = (Decimal)pa.y - (Decimal)pd.y;
            Decimal num5 = (Decimal)pb.y - (Decimal)pd.y;
            Decimal num6 = (Decimal)pc.y - (Decimal)pd.y;
            Decimal num7 = num2 * num6;
            Decimal num8 = num3 * num5;
            Decimal num9 = num1 * num1 + num4 * num4;
            Decimal num10 = num3 * num4;
            Decimal num11 = num1 * num6;
            Decimal num12 = num2 * num2 + num5 * num5;
            Decimal num13 = num1 * num5;
            Decimal num14 = num2 * num4;
            Decimal num15 = num3 * num3 + num6 * num6;
            return num9 * (num7 - num8) + num12 * (num10 - num11) + num15 * (num13 - num14);
        }

        /// <summary>
        /// Determines whether a point lies inside, outside, or on the circumcircle of three points.
        /// </summary>
        /// <param name="pa">The first point of the triangle.</param>
        /// <param name="pb">The second point of the triangle.</param>
        /// <param name="pc">The third point of the triangle.</param>
        /// <param name="pd">The point to test.</param>
        /// <returns>
        /// A positive value if pd lies inside the circumcircle,
        /// a negative value if pd lies outside the circumcircle,
        /// and zero if pd lies exactly on the circumcircle.
        /// </returns>
        /// <remarks>
        /// This is an alias for the InCircle method, provided for compatibility or clarity
        /// in certain contexts. It performs the same test as InCircle.
        /// </remarks>
        public static double NonRegular(Point pa, Point pb, Point pc, Point pd)
        {
            return InCircle(pa, pb, pc, pd);
        }

        /// <summary>
        /// Finds the circumcenter of a triangle with an optional off-center adjustment.
        /// </summary>
        /// <param name="torg">The origin vertex of the triangle.</param>
        /// <param name="tdest">The destination vertex of the triangle.</param>
        /// <param name="tapex">The apex vertex of the triangle.</param>
        /// <param name="xi">When this method returns, contains the barycentric coordinate xi of the circumcenter.</param>
        /// <param name="eta">When this method returns, contains the barycentric coordinate eta of the circumcenter.</param>
        /// <param name="offconstant">A constant that controls how far the point can be moved off-center.</param>
        /// <returns>A point representing the circumcenter of the triangle, possibly adjusted off-center.</returns>
        /// <remarks>
        /// This method computes the circumcenter of the triangle formed by the three input points.
        /// The circumcenter is the center of the circle that passes through all three vertices of the triangle.
        ///
        /// If offconstant is greater than zero, the method may adjust the circumcenter to be slightly
        /// off-center, which can be useful in certain mesh generation algorithms to improve triangle quality.
        ///
        /// The method also computes the barycentric coordinates of the circumcenter, which are returned
        /// through the xi and eta parameters.
        /// </remarks>
        public static Point FindCircumcenter(
            Point torg,
            Point tdest,
            Point tapex,
            ref double xi,
            ref double eta,
            double offconstant)
        {
            ++Statistic.CircumcenterCount;
            double num1 = tdest.x - torg.x;
            double num2 = tdest.y - torg.y;
            double num3 = tapex.x - torg.x;
            double num4 = tapex.y - torg.y;
            double num5 = num1 * num1 + num2 * num2;
            double num6 = num3 * num3 + num4 * num4;
            double num7 = (tdest.x - tapex.x) * (tdest.x - tapex.x) + (tdest.y - tapex.y) * (tdest.y - tapex.y);
            double num8;
            if (TriangulationSettings.NoExact)
            {
                num8 = 0.5 / (num1 * num4 - num3 * num2);
            }
            else
            {
                num8 = 0.5 / CounterClockwise(tdest, tapex, torg);
                --Statistic.CounterClockwiseCount;
            }

            double num9 = (num4 * num5 - num2 * num6) * num8;
            double num10 = (num1 * num6 - num3 * num5) * num8;
            if (num5 < num6 && num5 < num7)
            {
                if (offconstant > 0.0)
                {
                    double num11 = 0.5 * num1 - offconstant * num2;
                    double num12 = 0.5 * num2 + offconstant * num1;
                    if (num11 * num11 + num12 * num12 < num9 * num9 + num10 * num10)
                    {
                        num9 = num11;
                        num10 = num12;
                    }
                }
            }
            else if (num6 < num7)
            {
                if (offconstant > 0.0)
                {
                    double num13 = 0.5 * num3 + offconstant * num4;
                    double num14 = 0.5 * num4 - offconstant * num3;
                    if (num13 * num13 + num14 * num14 < num9 * num9 + num10 * num10)
                    {
                        num9 = num13;
                        num10 = num14;
                    }
                }
            }
            else if (offconstant > 0.0)
            {
                double num15 = 0.5 * (tapex.x - tdest.x) - offconstant * (tapex.y - tdest.y);
                double num16 = 0.5 * (tapex.y - tdest.y) + offconstant * (tapex.x - tdest.x);
                if (num15 * num15 + num16 * num16 < (num9 - num1) * (num9 - num1) + (num10 - num2) * (num10 - num2))
                {
                    num9 = num1 + num15;
                    num10 = num2 + num16;
                }
            }

            xi = (num4 * num9 - num3 * num10) * (2.0 * num8);
            eta = (num1 * num10 - num2 * num9) * (2.0 * num8);
            return new Point(torg.x + num9, torg.y + num10);
        }

        /// <summary>
        /// Finds the circumcenter of a triangle.
        /// </summary>
        /// <param name="torg">The origin vertex of the triangle.</param>
        /// <param name="tdest">The destination vertex of the triangle.</param>
        /// <param name="tapex">The apex vertex of the triangle.</param>
        /// <param name="xi">When this method returns, contains the barycentric coordinate xi of the circumcenter.</param>
        /// <param name="eta">When this method returns, contains the barycentric coordinate eta of the circumcenter.</param>
        /// <returns>A point representing the circumcenter of the triangle.</returns>
        /// <remarks>
        /// This method computes the circumcenter of the triangle formed by the three input points.
        /// The circumcenter is the center of the circle that passes through all three vertices of the triangle.
        ///
        /// The method also computes the barycentric coordinates of the circumcenter, which are returned
        /// through the xi and eta parameters.
        ///
        /// This is a simpler version of the FindCircumcenter method that does not perform any off-center adjustment.
        /// </remarks>
        public static Point FindCircumcenter(
            Point torg,
            Point tdest,
            Point tapex,
            ref double xi,
            ref double eta)
        {
            ++Statistic.CircumcenterCount;
            double num1 = tdest.x - torg.x;
            double num2 = tdest.y - torg.y;
            double num3 = tapex.x - torg.x;
            double num4 = tapex.y - torg.y;
            double num5 = num1 * num1 + num2 * num2;
            double num6 = num3 * num3 + num4 * num4;
            double num7;
            if (TriangulationSettings.NoExact)
            {
                num7 = 0.5 / (num1 * num4 - num3 * num2);
            }
            else
            {
                num7 = 0.5 / CounterClockwise(tdest, tapex, torg);
                --Statistic.CounterClockwiseCount;
            }

            double num8 = (num4 * num5 - num2 * num6) * num7;
            double num9 = (num1 * num6 - num3 * num5) * num7;
            xi = (num4 * num8 - num3 * num9) * (2.0 * num7);
            eta = (num1 * num9 - num2 * num8) * (2.0 * num7);
            return new Point(torg.x + num8, torg.y + num9);
        }
    }
}