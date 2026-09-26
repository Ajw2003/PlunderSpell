// Headless shim of the UnityEngine math surface used by Plunderspell.
// Only what the game actually calls is implemented; semantics match Unity's documented behaviour.
using System;
using System.Globalization;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 one => new Vector2(1f, 1f);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public override string ToString() => $"({x}, {y})";
    }

    [Serializable]
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);
        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int down => new Vector2Int(0, -1);
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int right => new Vector2Int(1, 0);
        public static Vector2Int Min(Vector2Int a, Vector2Int b) =>
            new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        public static Vector2Int Max(Vector2Int a, Vector2Int b) =>
            new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        public static float Distance(Vector2Int a, Vector2Int b) => (a - b).magnitude;
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public int sqrMagnitude => x * x + y * y;
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public bool Equals(Vector2Int other) => this == other;
        public override bool Equals(object obj) => obj is Vector2Int v && Equals(v);
        public override int GetHashCode() => unchecked(x * 397 ^ y);
        public override string ToString() => $"({x}, {y})";
    }

    [Serializable]
    public struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) : this(x, y, 0f) { }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);
        public static Vector3 left => new Vector3(-1f, 0f, 0f);
        public static Vector3 right => new Vector3(1f, 0f, 0f);
        public static Vector3 forward => new Vector3(0f, 0f, 1f);
        public static Vector3 back => new Vector3(0f, 0f, -1f);

        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized
        {
            get
            {
                float m = magnitude;
                return m > 1e-9f ? new Vector3(x / m, y / m, z / m) : zero;
            }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.x / s, a.y / s, a.z / s);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(
            a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Normalize(Vector3 v) => v.normalized;
        public static Vector3 Min(Vector3 a, Vector3 b) =>
            new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
        public static Vector3 Max(Vector3 a, Vector3 b) =>
            new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
        public static Vector3 Project(Vector3 v, Vector3 onNormal)
        {
            float d = Dot(onNormal, onNormal);
            return d < 1e-9f ? zero : onNormal * (Dot(v, onNormal) / d);
        }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal) => v - Project(v, planeNormal);
        public static Vector3 ClampMagnitude(Vector3 v, float maxLength)
        {
            float m = v.magnitude;
            return m > maxLength && m > 1e-9f ? v / m * maxLength : v;
        }
        /// <summary>Unsigned angle between two vectors in degrees.</summary>
        public static float Angle(Vector3 from, Vector3 to)
        {
            float denominator = (float)Math.Sqrt(from.sqrMagnitude * (double)to.sqrMagnitude);
            if (denominator < 1e-15f) return 0f;
            float cos = Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f);
            return (float)Math.Acos(cos) * Mathf.Rad2Deg;
        }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsigned = Angle(from, to);
            return unsigned * Mathf.Sign(Dot(axis, Cross(from, to)));
        }
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
        {
            Vector3 d = target - current;
            float m = d.magnitude;
            if (m <= maxDelta || m < 1e-9f) return target;
            return current + d / m * maxDelta;
        }

        /// <summary>Unity's instance Normalize() mutates in place (unlike the static overload).</summary>
        public void Normalize() { Vector3 n = normalized; x = n.x; y = n.y; z = n.z; }
        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }

        public bool Equals(Vector3 other) => this == other;
        public override bool Equals(object obj) => obj is Vector3 v && Equals(v);
        public override int GetHashCode() => unchecked(x.GetHashCode() * 397 ^ y.GetHashCode() * 31 ^ z.GetHashCode());
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0}, {2:0.0})", x, y, z);
    }

    /// <summary>Quaternion backed by real xyzw maths so rotated sockets resolve correctly headlessly.</summary>
    [Serializable]
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static Quaternion identity => new Quaternion(0f, 0f, 0f, 1f);

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            // v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v)
            var u = new Vector3(q.x, q.y, q.z);
            Vector3 t = Vector3.Cross(u, v) + v * q.w;
            return v + Vector3.Cross(u, t) * 2f;
        }

        public static bool operator ==(Quaternion a, Quaternion b) =>
            Math.Abs(a.x - b.x) < 1e-5f && Math.Abs(a.y - b.y) < 1e-5f &&
            Math.Abs(a.z - b.z) < 1e-5f && Math.Abs(a.w - b.w) < 1e-5f;
        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);
        public override bool Equals(object obj) => obj is Quaternion q && this == q;
        public override int GetHashCode() => unchecked(x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode());

        public Quaternion normalized
        {
            get
            {
                float m = (float)Math.Sqrt(x * x + y * y + z * z + w * w);
                return m > 1e-9f ? new Quaternion(x / m, y / m, z / m, w / m) : identity;
            }
        }

        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w).normalized;

        public static Quaternion Euler(float pitch, float yaw, float roll)
        {
            float cx = (float)Math.Cos(pitch * Mathf.Deg2Rad * 0.5f), sx = (float)Math.Sin(pitch * Mathf.Deg2Rad * 0.5f);
            float cy = (float)Math.Cos(yaw * Mathf.Deg2Rad * 0.5f), sy = (float)Math.Sin(yaw * Mathf.Deg2Rad * 0.5f);
            float cz = (float)Math.Cos(roll * Mathf.Deg2Rad * 0.5f), sz = (float)Math.Sin(roll * Mathf.Deg2Rad * 0.5f);
            // Unity applies Z, then X, then Y.
            return new Quaternion(
                sx * cy * cz + cx * sy * sz,
                cx * sy * cz - sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz).normalized;
        }

        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            forward = forward.normalized;
            if (forward.sqrMagnitude < 1e-9f) return identity;
            Vector3 right = Vector3.Cross(upwards, forward).normalized;
            if (right.sqrMagnitude < 1e-9f) right = Vector3.Cross(Vector3.forward, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);

            // Build from the rotation matrix columns (right, up, forward).
            float trace = right.x + up.y + forward.z;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                return new Quaternion((up.z - forward.y) / s, (forward.x - right.z) / s, (right.y - up.x) / s, 0.25f * s).normalized;
            }
            if (right.x > up.y && right.x > forward.z)
            {
                float s = (float)Math.Sqrt(1f + right.x - up.y - forward.z) * 2f;
                return new Quaternion(0.25f * s, (up.x + right.y) / s, (forward.x + right.z) / s, (up.z - forward.y) / s).normalized;
            }
            if (up.y > forward.z)
            {
                float s = (float)Math.Sqrt(1f + up.y - right.x - forward.z) * 2f;
                return new Quaternion((up.x + right.y) / s, 0.25f * s, (forward.y + up.z) / s, (forward.x - right.z) / s).normalized;
            }
            {
                float s = (float)Math.Sqrt(1f + forward.z - right.x - up.y) * 2f;
                return new Quaternion((forward.x + right.z) / s, (forward.y + up.z) / s, 0.25f * s, (right.y - up.x) / s).normalized;
            }
        }

        public static Quaternion LookRotation(Vector3 forward) => LookRotation(forward, Vector3.up);

        public static Quaternion AngleAxis(float angleDegrees, Vector3 axis)
        {
            axis = axis.normalized;
            float half = angleDegrees * Mathf.Deg2Rad * 0.5f;
            float s = (float)Math.Sin(half);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, (float)Math.Cos(half));
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
            if (dot < 0f) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); dot = -dot; }
            if (dot > 0.9995f)
                return new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t,
                                      a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t).normalized;
            float theta0 = (float)Math.Acos(dot);
            float theta = theta0 * t;
            float sinTheta = (float)Math.Sin(theta);
            float sinTheta0 = (float)Math.Sin(theta0);
            float s0 = (float)Math.Cos(theta) - dot * sinTheta / sinTheta0;
            float s1 = sinTheta / sinTheta0;
            return new Quaternion(a.x * s0 + b.x * s1, a.y * s0 + b.y * s1,
                                  a.z * s0 + b.z * s1, a.w * s0 + b.w * s1).normalized;
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => Slerp(a, b, t);
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta) => Slerp(from, to, 1f);
        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = Mathf.Clamp(Math.Abs(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w), -1f, 1f);
            return (float)Math.Acos(dot) * 2f * Mathf.Rad2Deg;
        }
        public override string ToString() => $"({x:0.0}, {y:0.0}, {z:0.0}, {w:0.0})";
    }

    public static class Mathf
    {
        public const float PI = 3.14159265358979f;
        public const float Epsilon = 1.1920929E-07f;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;

        public static float Abs(float v) => Math.Abs(v);
        public static int Abs(int v) => Math.Abs(v);
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Round(float v) => (float)Math.Round(v, MidpointRounding.ToEven);
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.ToEven);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Floor(float v) => (float)Math.Floor(v);
        public static float Ceil(float v) => (float)Math.Ceiling(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float v) =>
            Math.Abs(b - a) < 1e-9f ? 0f : Clamp01((v - a) / (b - a));
        public static float MoveTowards(float current, float target, float maxDelta) =>
            Math.Abs(target - current) <= maxDelta ? target : current + Math.Sign(target - current) * maxDelta;
        public static bool Approximately(float a, float b) =>
            Math.Abs(b - a) < Max(1E-06f * Max(Math.Abs(a), Math.Abs(b)), Epsilon * 8f);
        public static float Sign(float v) => v >= 0f ? 1f : -1f;
    }

    [Serializable]
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color red => new Color(1f, 0f, 0f);
        public static Color green => new Color(0f, 1f, 0f);
        public static Color blue => new Color(0f, 0f, 1f);
        public static Color white => new Color(1f, 1f, 1f);
        public static Color black => new Color(0f, 0f, 0f);
        public static Color yellow => new Color(1f, 0.92f, 0.016f);
        public static Color cyan => new Color(0f, 1f, 1f);
        public static Color magenta => new Color(1f, 0f, 1f);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color clear => new Color(0f, 0f, 0f, 0f);
    }

    public struct Bounds
    {
        public Vector3 center;
        public Vector3 extents;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.extents = size * 0.5f; }
        public Vector3 size { get => extents * 2f; set => extents = value * 0.5f; }
        public Vector3 min => center - extents;
        public Vector3 max => center + extents;
        public bool Contains(Vector3 p)
        {
            Vector3 lo = min, hi = max;
            return p.x >= lo.x && p.x <= hi.x && p.y >= lo.y && p.y <= hi.y && p.z >= lo.z && p.z <= hi.z;
        }
        /// <summary>Squared distance from a point to this box (0 when inside).</summary>
        public float SqrDistance(Vector3 p)
        {
            Vector3 lo = min, hi = max;
            float dx = p.x < lo.x ? lo.x - p.x : (p.x > hi.x ? p.x - hi.x : 0f);
            float dy = p.y < lo.y ? lo.y - p.y : (p.y > hi.y ? p.y - hi.y : 0f);
            float dz = p.z < lo.z ? lo.z - p.z : (p.z > hi.z ? p.z - hi.z : 0f);
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
