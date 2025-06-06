using System;
using UnityEngine;

namespace GeoMap.MathUtils
{
    public struct DoubleVector3
    {
        public double X;
        public double Y;
        public double Z;

        public double this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("Invalid index!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid DoubleVector3 index!");
                }
            }
        }

        public DoubleVector3 Normalized => Normalize(this);

        public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double SqrMagnitude => X * X + Y * Y + Z * Z;

        public static DoubleVector3 Zero => new(0d, 0d, 0d);


        public DoubleVector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public DoubleVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public DoubleVector3(Vector3 v3)
        {
            X = v3.x;
            Y = v3.y;
            Z = v3.z;
        }

        public DoubleVector3(double x, double y)
        {
            X = x;
            Y = y;
            Z = 0d;
        }

        public static DoubleVector3 operator +(DoubleVector3 a, DoubleVector3 b)
        {
            return new DoubleVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static DoubleVector3 operator -(DoubleVector3 a, DoubleVector3 b)
        {
            return new DoubleVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static DoubleVector3 operator -(DoubleVector3 a)
        {
            return new DoubleVector3(-a.X, -a.Y, -a.Z);
        }

        public static DoubleVector3 operator *(DoubleVector3 a, double d)
        {
            return new DoubleVector3(a.X * d, a.Y * d, a.Z * d);
        }

        public static DoubleVector3 operator *(double d, DoubleVector3 a)
        {
            return new DoubleVector3(a.X * d, a.Y * d, a.Z * d);
        }

        public static DoubleVector3 operator /(DoubleVector3 a, double d)
        {
            return new DoubleVector3(a.X / d, a.Y / d, a.Z / d);
        }

        public static bool operator ==(DoubleVector3 lhs, DoubleVector3 rhs)
        {
            return (lhs - rhs).SqrMagnitude < 0.0 / 1.0;
        }

        public static bool operator !=(DoubleVector3 lhs, DoubleVector3 rhs)
        {
            return (lhs - rhs).SqrMagnitude >= 0.0 / 1.0;
        }

        public static explicit operator Vector3(DoubleVector3 doubleVector3)
        {
            return new Vector3((float)doubleVector3.X, (float)doubleVector3.Y, (float)doubleVector3.Z);
        }

        public static DoubleVector3 Lerp(DoubleVector3 from, DoubleVector3 to, double t)
        {
            t = DoubleMath.Clamp01(t);
            return new DoubleVector3(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t,
                from.Z + (to.Z - from.Z) * t);
        }
        
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() << 2 ^ Z.GetHashCode() >> 2;
        }

        public override bool Equals(object other)
        {
            if (!(other is DoubleVector3))
                return false;
            DoubleVector3 doubleVector3 = (DoubleVector3)other;
            if (X.Equals(doubleVector3.X) && Y.Equals(doubleVector3.Y))
                return Z.Equals(doubleVector3.Z);
            return false;
        }
        
        public static DoubleVector3 Normalize(DoubleVector3 value)
        {
            double num = value.Magnitude;
            if (num > 9.99999974737875E-06)
                return value / num;
            return Zero;
        }
        
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
        
    }
}