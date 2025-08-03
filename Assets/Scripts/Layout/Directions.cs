using System;
using System.Linq;
using UnityEngine;

namespace Layout
{
    /// <summary>
    /// Choice of direction, for objects that should have exactly one value
    /// </summary>
    public enum CardinalDirection : ushort
    {
        North = 1,
        East = 2,
        South = 4,
        West = 8,
    }

    /// <summary>
    /// Flag holding a combination of directions, or none
    /// </summary>
    [Flags]
    public enum CardinalDirections : ushort
    {
        North = CardinalDirection.North,
        East = CardinalDirection.East,
        South = CardinalDirection.South,
        West = CardinalDirection.West,
    }

    /// <summary>
    /// Enum holding all 8 major compass directions, made from combinations of cardinal directions
    /// </summary>
    public enum CompassDirection : ushort
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
        private static CompassDirection[] _diagonalDirections = {
            CompassDirection.NorthEast, CompassDirection.SouthEast, CompassDirection.SouthWest, CompassDirection
                .NorthWest
        };

        public static bool IsDiagonal(this CompassDirection direction)
        {
            return _diagonalDirections.Contains(direction);
        }
        
        /// <summary>
        /// Convert a direction into a rotation about the y-axis from North as 0 degrees
        /// </summary>
        /// <param name="direction">Direction of rotation</param>
        /// <returns>Rotation from North</returns>
        public static Quaternion ToRotation(this CompassDirection direction)
        {
            if ((ushort)direction == 0)
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
        
        public static Quaternion ToRotation(this CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => Quaternion.AngleAxis(0.0f, Vector3.up),
                CardinalDirection.East => Quaternion.AngleAxis(90.0f, Vector3.up),
                CardinalDirection.South => Quaternion.AngleAxis(180.0f, Vector3.up),
                CardinalDirection.West => Quaternion.AngleAxis(270.0f, Vector3.up),
                _ => Quaternion.identity
            };
        }
    }
    
}