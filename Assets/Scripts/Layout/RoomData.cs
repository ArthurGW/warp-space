using LevelGenerator;
using UnityEngine;
using static Layout.LayoutUtils;

namespace Layout
{
    public readonly struct RoomData
    {
        private RoomData(ulong id, uint x, uint y, uint width, uint height, RoomType roomType)
        {
            Id = id;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Type = roomType;
        }

        public static explicit operator RoomData(Room rm)
        {
            return new RoomData(rm.RoomId, rm.X, rm.Y, rm.W, rm.H, rm.Type);
        }
        
        public override string ToString() => $"RoomData({Id}: {X},{Y},{Width},{Height},{Type})";
        
        public Vector3 ToPosition() => GridToPosition((X, Y));

        public Vector3 ToSize()
        {
            var size = GridToSize((Width, Height));
            return size;
        }

        public Vector3 ToLocalCenter()
        {
            var center = GridToSize((Width - 1, Height - 1)) / 2f;
            center.z *= -1f;  // Negative Z, i.e. "South", goes towards the center as the pos is in the top left square
            return center;
        }

        public Vector3 ToWorldCenter() => ToPosition() + ToLocalCenter();

        public ulong Id { get; }
        public uint X { get; }
        public uint Y { get; }
        public uint Width { get; }
        public uint Height { get; }
        public RoomType Type { get; }
    }
}
