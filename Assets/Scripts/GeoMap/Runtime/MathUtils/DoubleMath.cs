using System;

namespace GeoMap.MathUtils
{
    public struct DoubleMath
    {
        public static double Acos(double d)
        {
            return Math.Acos(d);
        }

        public static double Sqrt(double d)
        {
            return Math.Sqrt(d);
        }

        public static double Abs(double d)
        {
            return Math.Abs(d);
        }

        public static double Min(double a, double b)
        {
            if (a < b)
                return a;
            return b;
        }

        public static double Max(double a, double b)
        {
            return a > b ? a : b;
        }

        public static double Floor(double d)
        {
            return Math.Floor(d);
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static double Clamp01(double value)
        {
            return value switch
            {
                < 0.0 => 0.0d,
                > 1.0 => 1d,
                _ => value
            };
        }

        public static double Repeat(double t, double length)
        {
            return t - Floor(t / length) * length;
        }

        public static double DeltaAngle(double current, double target)
        {
            double num = Repeat(target - current, 360d);
            if (num > 180.0d)
                num -= 360d;
            return num;
        }
    }
}