using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Layout
{
    public record MapResult(List<MapSquareData> Squares, List<RoomData> Rooms, Dictionary<ulong, HashSet<ulong>> Adjacencies, ulong StartRoomId, ulong FinishRoomId);
    
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
            foreach (var adj in result.Adjacencies)
            {
                Debug.Log($"AdjacencyData({adj.Key}: {string.Join(',', adj.Value)})");
            }
            Debug.Log($"Start Room: {result.StartRoomId}");
            Debug.Log($"Finish Room: {result.FinishRoomId}");
        }
        
        private static readonly (int offX, int offY)[] Adjacent =
        {
            (-1, -1),
            (0, -1),
            (1, -1),
            (-1, 0),
            (1, 0),
            (-1, 1),
            (0, 1),
            (1, 1)
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

        public static (int, int)[] ValidAdjacentSquareOffsets((uint X, uint Y) position, uint mapWidth, uint mapHeight)
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
                .ToArray();
        }
    }
}
