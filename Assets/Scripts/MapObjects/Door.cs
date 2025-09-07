using System;
using Layout;
using LevelGenerator;

namespace MapObjects
{
    public readonly struct Door : IEquatable<Door>
    {
        public readonly uint InternalX;
        public readonly uint InternalY;
        public readonly RoomType ConnectsTo;
        public readonly CardinalDirection Direction;

        public Door(uint internalX, uint internalY, RoomType connectsTo, CardinalDirection direction)
        {
            InternalX = internalX;
            InternalY = internalY;
            ConnectsTo = connectsTo;
            Direction = direction;
        }

        public override bool Equals(object obj)
        {
            return obj is Door other && Equals(other);
        }

        public bool Equals(Door other)
        {
            return Direction == other.Direction
                   && InternalX == other.InternalX 
                   && InternalY == other.InternalY
                   && ConnectsTo == other.ConnectsTo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(InternalX, InternalY, (byte)ConnectsTo, (byte)Direction);
        }

        public static bool operator ==(Door left, Door right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Door left, Door right)
        {
            return !left.Equals(right);
        }
    }
}
