using System;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Axial hex coordinates (q, r). See redblobgames.com/grids/hexagons for the math.
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int q;
        public int r;

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public int S => -q - r;

        public int DistanceTo(HexCoordinates other)
        {
            int dq = Mathf.Abs(q - other.q);
            int dr = Mathf.Abs(r - other.r);
            int ds = Mathf.Abs(S - other.S);
            return Mathf.Max(dq, dr, ds);
        }

        /// <summary>World-space position for a pointy-top hex layout.</summary>
        public Vector3 ToWorldPosition(float hexSize)
        {
            float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
            float z = hexSize * (3f / 2f * r);
            return new Vector3(x, 0f, z);
        }

        public bool Equals(HexCoordinates other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => (q, r).GetHashCode();
        public override string ToString() => $"({q}, {r})";
    }
}
