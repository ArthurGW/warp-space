using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Allows using record classes - see https://stackoverflow.com/a/64749403/8280782
namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

namespace Layout
{
    public record MapResult(
		List<MapSquareData> Squares,
		List<RoomData> Rooms,
		Dictionary<ulong, HashSet<ulong>> Doors,
		Dictionary<ulong, HashSet<ulong>> Portals,
		ulong StartRoomId,
		ulong FinishRoomId,
		ulong NumLevelsGenerated
	);
    
    public static class SquareSize
    {
        public const float X = 10f;
        public const float Y = 10f;
        public const float Height = 5f;
    }

    public static class LayoutUtils
    {
        public static void PrintResult(this MapResult result)
        {
            result.Squares.ForEach(sq => Debug.Log(sq));
            result.Rooms.ForEach(rm => Debug.Log(rm));
            foreach (var door in result.Doors)
            {
                Debug.Log($"Doors({door.Key}: {string.Join(',', door.Value)})");
            }
            foreach (var portal in result.Portals)
            {
                Debug.Log($"Portals({portal.Key}: {string.Join(',', portal.Value)})");
            }
            Debug.Log($"Start Room: {result.StartRoomId}");
            Debug.Log($"Finish Room: {result.FinishRoomId}");
            Debug.Log($"Num Levels Generated: {result.NumLevelsGenerated}");
        }
        
        private static readonly (int offX, int offY)[] Adjacent =
        {
            (0, -1),
            (-1, 0),
            (1, 0),
            (0, 1),
        };
        
        public static Vector3 GridToPosition((uint X, uint Y) pos)
        {
            // Note - we are choosing -z in map coordinates as +y in grid coordinates
            // This is so that +x in map coordinates is +x in grid coordinates
            // Also note grid positions are 1-indexed, but we start the grid at (0,0) in world space
            return new Vector3(SquareSize.X * (pos.X - 1), 0f, -SquareSize.Y * (pos.Y - 1));
        }
        
        public static Vector3 GridToSize((uint X, uint Y) size)
        {
            return new Vector3(SquareSize.X * size.X, SquareSize.Height, SquareSize.Y * size.Y);
        }

        public static (uint X, uint Y) Offset(this (uint X, uint Y) pos, (int offX, int offY) offset) =>
            pos.Offset(offset.offX, offset.offY);

        public static (uint, uint) Offset(this (uint X, uint Y) pos, int offX, int offY)
        {
            var first = (int)pos.X + offX;
            var second = (int)pos.Y + offY;
            if (first < 0)
                throw new ArgumentOutOfRangeException(nameof(offX), offX, "x offset makes position negative");
            if (second < 0)
                throw new ArgumentOutOfRangeException(nameof(offY), offY, "y offset makes position negative");
            return ((uint)first, (uint)second);
        }

        public static (uint, uint) SquareToGrid(MapSquareData square)
        {
            return (square.X, square.Y);
        }

        public static List<(int, int)> ValidAdjacentSquareOffsets((uint X, uint Y) position, uint mapWidth, uint mapHeight)
        {
            // Select in-bounds adjacent squares, and return the offset that leads to them
            return Adjacent
                .Select(adj => (X: adj.offX + (int)position.X, Y: adj.offY + (int)position.Y, adj))
                .Where(posAdj =>
                    posAdj.X > 0
                    && posAdj.X <= mapWidth
                    && posAdj.Y > 0
                    && posAdj.Y <= mapHeight)
                .Select(posAdj => posAdj.adj)
                .ToList();
        }
    }
}
