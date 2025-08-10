using System;
using System.Collections.Generic;
using System.Linq;
using static Layout.LayoutUtils;
using LevelGenerator;
using LevelGenerator.Extensions;
using UnityEngine;
using UnityEngine.Events;

// Allows using record classes - see https://stackoverflow.com/a/64749403/8280782
namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

namespace Layout
{
    public readonly struct RoomData
    {
        private RoomData(ulong id, uint x, uint y, uint width, uint height, bool isCorridor)
        {
            Id = id;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            IsCorridor = isCorridor;
        }

        public static explicit operator RoomData(Room rm)
        {
            return new RoomData(rm.RoomId, rm.X, rm.Y, rm.W, rm.H, rm.IsCorridor);
        }
        
        public override string ToString() => $"RoomData({Id}: {X},{Y},{Width},{Height},{IsCorridor})";
        
        public Vector3 ToPosition() => GridToPosition((X, Y));

        public ulong Id { get; }
        public uint X { get; }
        public uint Y { get; }
        public uint Width { get; }
        public uint Height { get; }
        public bool IsCorridor { get; }
    }
    
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
            => new(rm.X, rm.Y, rm.IsCorridor ? SquareType.Corridor : SquareType.Room);
        
        public override string ToString() => $"MapSquareData({X},{Y},{Type})";
        
        public Vector3 ToPosition() => GridToPosition((X, Y));

        public uint X { get; }
        public uint Y { get; }
        public SquareType Type { get; }
    }
    
    public record MapResult(List<MapSquareData> Squares, List<RoomData> Rooms, Dictionary<ulong, HashSet<ulong>> Adjacencies, uint Width, uint Height);
    
    public class GameMapGenerator : MonoBehaviour
    {
        public UnityEvent<MapResult> onMapGenerated;
        public UnityEvent onMapGenerationFailed;
        
        #region Level Generator Params

        public uint width = 10;
        public uint height = 8;
        public uint minRooms = 2;
        public uint maxRooms = 6;
        public uint maxNumLevels = 1;
    
        [Range(1, 16)]
        public uint solverThreads = 1;

        [Tooltip("seed=0 means use a random seed rather than a fixed value")]
        public uint seed = 0;

        public bool IsGenerating { get; private set; }
    
        private List<MapSquareData> _squares;
        private List<RoomData> _rooms;
        private Dictionary<ulong, HashSet<ulong>> _adjacencies;
    
        #endregion

        private void Awake()
        {
            IsGenerating = false;
            onMapGenerated = new UnityEvent<MapResult>();
            onMapGenerationFailed = new UnityEvent();
        }

        private async void Start()
        {
            try
            {
                await GenerateNewLevel();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async Awaitable GenerateNewLevel()
        {
            await Awaitable.MainThreadAsync();
            IsGenerating = true;
            _squares = new List<MapSquareData>();
            _rooms = new List<RoomData>();
            _adjacencies = new Dictionary<ulong, HashSet<ulong>>();
            
            // Run the slow level generation stage in a background thread
            await Awaitable.BackgroundThreadAsync();
            using var gen = new LevelGenerator.LevelGenerator(
                maxNumLevels, width, height, minRooms, maxRooms, seed, false, solverThreads
            );
            gen.SolveSafe();
            using var level = gen.BestLevel();
            if (level == null)
            {
                await Awaitable.MainThreadAsync();
                IsGenerating = false;
                onMapGenerationFailed.Invoke();
                return;
            }

            // Retrieve all the data into local copies, so the unmanaged data can be destroyed safely
            using var squares = level.MapSquares();
            var newSquares = squares.GetEnumerable().Select(
                sq =>
                {
                    var data = (MapSquareData)sq;
                    sq.Dispose();
                    return data;
                }
            ).ToList();
            
            using var rooms = level.Rooms();
            var newRooms = rooms.GetEnumerable().Select(
                rm =>
                {
                    var data = (RoomData)rm;
                    rm.Dispose();
                    return data;
                }
            ).ToList();

            using var adjacencies = level.Adjacencies();
            var newAdjacencies = new Dictionary<ulong, HashSet<ulong>>();
            foreach (var adjacency in adjacencies)
            {
                if (!newAdjacencies.TryGetValue(adjacency.FirstId, out var adjacentTo))
                {
                    adjacentTo = new HashSet<ulong>();
                    newAdjacencies.Add(adjacency.FirstId, adjacentTo);
                }
                adjacentTo.Add(adjacency.SecondId);
                adjacency.Dispose();
            }
            
            await Awaitable.MainThreadAsync();
            _squares  = newSquares;
            _rooms = newRooms;
            _adjacencies = newAdjacencies;
            
            IsGenerating = false;
            onMapGenerated.Invoke(new MapResult(_squares, _rooms, _adjacencies, width, height));
        }

        public void PrintArrays()
        {
            _squares.ForEach(sq => Debug.Log(sq));
            _rooms.ForEach(rm => Debug.Log(rm));
            foreach (var adj in _adjacencies)
            {
                Debug.Log($"AdjacencyData({adj.Key}: {string.Join(',', adj.Value)})");
            }
        }
    }
}
