using System;
using System.Linq;
using UnityEngine;

namespace Layout
{
    /// <summary>
    /// Choice of direction, for objects that should have exactly one value
    /// </summary>
    ///
    /// These values are defined such that opposite directions are bit-shifted left by 4 from each other (modulo 255).
    /// i.e. ((North | West) << 4) % 0xFF == (South | East), while perpendicular directions are bit shifted by 2 from
    /// each other, i.e. a bit shift by two represents a 90 degree rotation.
    public enum CardinalDirection : byte
    {
        North = 1 << 0,
        East = 1 << 2,
        South = 1 << 4,
        West =  1 << 6,
    }

    /// <summary>
    /// Flag holding a combination of directions, or none
    /// </summary>
    ///
    /// The purpose of this is to indicate something that can have more than one direction. This differs from
    /// CompassDirection (below), which has the same underlying values, but indicates a single thing pointing to a
    /// single direction.
    [Flags]
    public enum CardinalDirections : byte
    {
        North = CardinalDirection.North,
        East = CardinalDirection.East,
        South = CardinalDirection.South,
        West = CardinalDirection.West,
    }

    /// <summary>
    /// Enum holding all 8 major compass directions, made from combinations of cardinal directions
    /// </summary>
    public enum CompassDirection : byte
    {
        North = CardinalDirection.North,
        NorthEast = CardinalDirection.North |  CardinalDirection.East,
        East = CardinalDirection.East,
        SouthEast = CardinalDirection.East  |  CardinalDirection.South,
        South = CardinalDirection.South,
        SouthWest = CardinalDirection.South  |  CardinalDirection.West,
        West = CardinalDirection.West,
        NorthWest = CardinalDirection.West | CardinalDirection.North
    }

    public static class Directions
    {
        public static bool IsDiagonal(this CompassDirection direction) => !Enum.IsDefined(typeof(CardinalDirection), (byte)direction);

        public static bool IsOpposite(this CompassDirection direction, CompassDirection other) =>
            // See definition of CardinalDirection for reasoning
            (byte)direction != 0 && (direction < other
                ? (byte)direction << 4 == (byte)other
                : (byte)other << 4 == (byte)direction
            );

        public static bool IsPerpendicular(this CompassDirection direction, CompassDirection other) =>
            // See definition of CardinalDirection for reasoning
            (byte)direction != 0 && (direction < other
                ? (byte)direction << 2 == (byte)other
                : (byte)other << 2 == (byte)direction
            );
            

        public static CompassDirection FromTuple((int, int) xyTuple)
        {
            CompassDirection first = xyTuple.Item1 switch
            {
                -1 => CompassDirection.West,
                0 => 0,
                1 => CompassDirection.East,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(xyTuple.Item2), xyTuple.Item1, "CompassDirection tuple must have unit offsets"
                )
            };
            CompassDirection second = xyTuple.Item2 switch
            {
                -1 => CompassDirection.North,
                0 => 0,
                1 => CompassDirection.South,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(xyTuple.Item2), xyTuple.Item2, "CompassDirection tuple must have unit offsets"
                )
            };
            var result = (byte)first | (byte)second;
            if (!Enum.IsDefined(typeof(CompassDirection), result))
            {
                throw new ArgumentException("undefined CompassDirection value", nameof(xyTuple));
            }
            return (CompassDirection)result;
        }
        
        /// <summary>
        /// Convert a direction into a rotation about the y-axis from North as 0 degrees
        /// </summary>
        /// <param name="direction">Direction of rotation</param>
        /// <returns>Rotation from North</returns>
        public static Quaternion ToRotation(this CompassDirection direction)
        {
            if ((byte)direction == 0)
            {
                // This is really an error, but return no change, equivalent to North
                return Quaternion.identity;
            }

            var flagDirection = (CardinalDirections)direction;
            var directions = Enum.GetValues(typeof(CardinalDirections)).Cast<CardinalDirections>()
                .Where(d => flagDirection.HasFlag(d))
                .Select(d => ((CardinalDirection)d).ToRotation())
                .ToArray();
            return directions.Length switch
            {
                1 => directions[0],
                2 => Quaternion.Lerp(directions[0], directions[1], 0.5f),  // Average of the two
                _ => Quaternion.identity
            };
        }
        
        public static Quaternion ToRotation(this CardinalDirection direction) => direction switch
        {
            CardinalDirection.North => Quaternion.AngleAxis(0.0f, Vector3.up),
            CardinalDirection.East => Quaternion.AngleAxis(90.0f, Vector3.up),
            CardinalDirection.South => Quaternion.AngleAxis(180.0f, Vector3.up),
            CardinalDirection.West => Quaternion.AngleAxis(270.0f, Vector3.up),
            _ => Quaternion.identity
        };
    }
    
}