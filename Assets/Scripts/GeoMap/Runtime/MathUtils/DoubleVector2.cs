using System;

namespace GeoMap.MathUtils
{
    public struct DoubleVector2
    {
        public double X;
        public double Y;

        public double this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    _ => throw new IndexOutOfRangeException("Invalid DoubleVector2 index!")
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
                    default:
                        throw new IndexOutOfRangeException("Invalid DoubleVector2 index!");
                }
            }
        }

        public double Magnitude => DoubleMath.Sqrt(X * X + Y * Y);
        public double SqrMagnitude => X * X + Y * Y;



        public static DoubleVector2 Zero => new(0.0d, 0.0d);


        public DoubleVector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator DoubleVector2(DoubleVector3 v)
        {
            return new DoubleVector2(v.X, v.Y);
        }

        public static implicit operator DoubleVector3(DoubleVector2 v)
        {
            return new DoubleVector3(v.X, v.Y, 0.0d);
        }

        public static DoubleVector2 operator +(DoubleVector2 a, DoubleVector2 b)
        {
            return new DoubleVector2(a.X + b.X, a.Y + b.Y);
        }

        public static DoubleVector2 operator -(DoubleVector2 a, DoubleVector2 b)
        {
            return new DoubleVector2(a.X - b.X, a.Y - b.Y);
        }

        public static DoubleVector2 operator -(DoubleVector2 a)
        {
            return new DoubleVector2(-a.X, -a.Y);
        }

        public static DoubleVector2 operator *(DoubleVector2 a, double d)
        {
            return new DoubleVector2(a.X * d, a.Y * d);
        }

        public static DoubleVector2 operator *(float d, DoubleVector2 a)
        {
            return new DoubleVector2(a.X * d, a.Y * d);
        }

        public static DoubleVector2 operator /(DoubleVector2 a, double d)
        {
            return new DoubleVector2(a.X / d, a.Y / d);
        }

        public static bool operator ==(DoubleVector2 lhs, DoubleVector2 rhs)
        {
            
            
            return (lhs - rhs).SqrMagnitude < 0.0 / 1.0;
        }

        public static bool operator !=(DoubleVector2 lhs, DoubleVector2 rhs)
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
            if (!(other is DoubleVector2))
                return false;
            DoubleVector2 doubleVector2 = (DoubleVector2)other;
            return X.Equals(doubleVector2.X) && Y.Equals(doubleVector2.Y);
        }

        
    }
}