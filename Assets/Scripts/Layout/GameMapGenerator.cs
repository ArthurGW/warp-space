using System;
using System.Collections.Generic;
using System.Linq;
using LevelGenerator;
using LevelGenerator.Extensions;
using UnityEngine;
using UnityEngine.Events;

// Allows the record class below - see https://stackoverflow.com/a/64749403/8280782
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
        {
            if (!rm.IsCorridor)
            {
                throw new InvalidCastException("can only convert corridor rooms to MapSquareData");
            }
            
            return new MapSquareData(rm.X, rm.Y, SquareType.Corridor);
        }
        
        public override string ToString() => $"MapSquareData({X},{Y},{Type})";

        public uint X { get; }
        public uint Y { get; }
        public SquareType Type { get; }
    }
    
    public record MapResult(List<MapSquareData> Squares, List<RoomData> Rooms, Dictionary<ulong, HashSet<ulong>> Adjacencies);
    
    public class GameMapGenerator : MonoBehaviour
    {
        public UnityEvent<MapResult> onMapGenerated;
        
        #region Level Generator Params

        public uint width;
        public uint height;
        public uint minRooms;
        public uint maxRooms;
        public uint maxNumLevels;
    
        [Range(1, 16)]
        public uint solverThreads;

        [Tooltip("seed=0 means use a random seed rather than a fixed value")]
        public uint seed = 0;

        private string _program;
        private string _solution;
    
        private List<MapSquareData> _squares;
        private List<RoomData> _rooms;
        private Dictionary<ulong, HashSet<ulong>> _adjacencies;
    
        #endregion

        private void Awake()
        {
            onMapGenerated ??= new UnityEvent<MapResult>();
            LoadProgram();
        }

        private async void Start()
        {
            try
            {
                if (_program == null)
                {
                    Debug.LogError("GameMapGenerator.Start finished early due to null program");
                    return;  // Solving won't be possible
                }
                await GenerateNewLevel();
                
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LoadProgram()
        {
            _program = null;
            try
            {
                var prog = Resources.Load<TextAsset>("programs/ship") as TextAsset;
                if (prog != null)
                {
                    _program = prog.text;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async Awaitable GenerateNewLevel()
        {
            _squares = new List<MapSquareData>();
            _rooms = new List<RoomData>();
            _adjacencies = new Dictionary<ulong, HashSet<ulong>>();
        
            if (_program == null)
            {
                Debug.LogError("GameMapGenerator.GenerateNewLevel finished early due to null program");
                return;
            }
            
            // Run the slow level generation stage in a background thread
            await Awaitable.BackgroundThreadAsync();
            
            var newSquares = new List<MapSquareData>();
            var newRooms = new List<RoomData>();
            var newAdjacencies = new Dictionary<ulong, HashSet<ulong>>();
        
            using var gen = new LevelGenerator.LevelGenerator(
                maxNumLevels, width, height, minRooms, maxRooms, seed, _program, solverThreads
            );
            var newSolution = gen.SolveSafe();
            using var level = gen.BestLevel();
            if (level == null)
            {
                Debug.LogError("GameMapGenerator.GenerateNewLevel finished early due to null level");
                return;
            }

            // Retrieve all the data into local copies, so the unmanaged data can be destroyed safely
            using var squares = level.MapSquares();
            newSquares = squares.GetEnumerable().Select(
                sq =>
                {
                    var data = (MapSquareData)sq;
                    sq.Dispose();
                    return data;
                }
            ).ToList();

            using var rooms = level.Rooms();
            newRooms = rooms.GetEnumerable().Select(
                rm =>
                {
                    var data = (RoomData)rm;
                    rm.Dispose();
                    return data;
                }
            ).ToList();
        
            using var adjacencies = level.Adjacencies();
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
            _solution = newSolution;
            
            onMapGenerated.Invoke(new MapResult(_squares, _rooms, _adjacencies));
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

        private void PrintLevel(Level lvl)
        {
            if (lvl != null)
            {
                Debug.Log(lvl.NumMapSquares);
                Debug.Log(lvl.NumCorridors);
                Debug.Log(lvl.NumRooms);
                Debug.Log(lvl.NumAdjacencies);
            
                foreach (var sq in lvl.MapSquares())
                {
                    Debug.Log(sq.AsString());
                }
                foreach (var rm in lvl.Rooms())
                {
                    Debug.Log(rm.AsString());
                }
                foreach (var adj in lvl.Adjacencies())
                {
                    Debug.Log(adj.AsString());
                }
            }
            else
            {
                Debug.LogError("Level is NULL!");
            }
        }
    }
}
