using System;

namespace Layout
{
    /// <summary>
    /// Choice of direction, for objects that should have exactly one value
    /// </summary>
    public enum Direction : ushort
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
    public enum Directions : ushort
    {
        North = 1,
        East = 2,
        South = 4,
        West = 8,
    }
}