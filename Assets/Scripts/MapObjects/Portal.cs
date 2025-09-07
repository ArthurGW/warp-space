using System;
using Layout;
using LevelGenerator;

namespace MapObjects
{
    public readonly struct Portal : IEquatable<Portal>
    {
        public readonly uint InternalX;
        public readonly uint InternalY;
        public readonly RoomType ConnectsTo;
        public readonly CardinalDirection Direction;

        public Portal(uint internalX, uint internalY, RoomType connectsTo, CardinalDirection direction)
        {
            InternalX = internalX;
            InternalY = internalY;
            ConnectsTo = connectsTo;
            Direction = direction;
        }

        public override bool Equals(object obj)
        {
            return obj is Portal other && Equals(other);
        }

        public bool Equals(Portal other)
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

        public static bool operator ==(Portal left, Portal right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Portal left, Portal right)
        {
            return !left.Equals(right);
        }
    }
}
