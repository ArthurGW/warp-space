using LevelGenerator;
using UnityEngine;

namespace Layout
{
    public readonly struct MapSquareData
    {
        private MapSquareData(uint x, uint y, SquareType squareType)
        {
            X = x;
            Y = y;
            Type = squareType;
        }

        public static explicit operator MapSquareData(MapSquare sq)
        {
            return new MapSquareData(sq.X, sq.Y, sq.Type);
        }
        
        public static explicit operator MapSquareData(RoomData rm) 
            => new(rm.X, rm.Y, rm.Type switch
            {
                RoomType.Corridor => SquareType.Corridor,
                RoomType.AlienBreach => SquareType.AlienBreach,
                _ => SquareType.Room
            });
        
        public override string ToString() => $"MapSquareData({X},{Y},{Type})";
        
        public Vector3 ToPosition() => LayoutUtils.GridToPosition((X, Y));

        public uint X { get; }
        public uint Y { get; }
        public SquareType Type { get; }
    }
}
