
using System;
using System.Runtime.CompilerServices;

namespace GeoMap.MathUtils
{
    public struct Vector2d
    {
        public const double KEpsilon = 1E-05d;
        public double X;
        public double Y;

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2d index!");
                }
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
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2d index!");
                }
            }
        }

        public double Magnitude => DoubleMath.Sqrt(X * X + Y * Y);
        public double SqrMagnitude => X * X + Y * Y;



        public static Vector2d Zero => new(0.0d, 0.0d);


        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vector2d(Vector3d v)
        {
            return new Vector2d(v.x, v.y);
        }

        public static implicit operator Vector3d(Vector2d v)
        {
            return new Vector3d(v.X, v.Y, 0.0d);
        }

        public static Vector2d operator +(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2d operator -(Vector2d a, Vector2d b)
        {
            return new Vector2d(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2d operator -(Vector2d a)
        {
            return new Vector2d(-a.X, -a.Y);
        }

        public static Vector2d operator *(Vector2d a, double d)
        {
            return new Vector2d(a.X * d, a.Y * d);
        }

        public static Vector2d operator *(float d, Vector2d a)
        {
            return new Vector2d(a.X * d, a.Y * d);
        }

        public static Vector2d operator /(Vector2d a, double d)
        {
            return new Vector2d(a.X / d, a.Y / d);
        }

        public static bool operator ==(Vector2d lhs, Vector2d rhs)
        {
            
            
            return (lhs - rhs).SqrMagnitude < 0.0 / 1.0;
        }

        public static bool operator !=(Vector2d lhs, Vector2d rhs)
        {
            return (lhs - rhs).SqrMagnitude >= 0.0 / 1.0;
        }

        public void Set(double newX, double newY)
        {
            X = newX;
            Y = newY;
        }

        public void Normalize()
        {
            double magnitude = Magnitude;
            if (magnitude > 9.99999974737875E-06)
                this /= magnitude;
            else
                this = Zero;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() << 2;
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector2d))
                return false;
            Vector2d vector2d = (Vector2d)other;
            return X.Equals(vector2d.X) && Y.Equals(vector2d.Y);
        }

        
    }
}