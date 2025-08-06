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
            => new MapSquareData(rm.X, rm.Y, rm.IsCorridor ? SquareType.Corridor : SquareType.Room);
        public override string ToString() => $"MapSquareData({X},{Y},{Type})";

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

        private string _program;
        private string _solution;
        public bool IsGenerating { get; private set; }
    
        private List<MapSquareData> _squares;
        private List<RoomData> _rooms;
        private Dictionary<ulong, HashSet<ulong>> _adjacencies;
    
        #endregion

        private void Awake()
        {
            Debug.Log("GameMapGenerator Awake");
            IsGenerating = false;
            onMapGenerated ??= new UnityEvent<MapResult>();
            onMapGenerationFailed ??= new UnityEvent();
            LoadProgram();
        }

        private async void Start()
        {
            try
            {
                Debug.Log("GameMapGenerator Start");
                await GenerateNewLevel();
                
            }
            catch (Exception e)
            {
                Debug.Log("GameMapGenerator Start Exception");
                Debug.LogException(e);
            }
        }

        public void LoadProgram()
        {
            Debug.Log("GameMapGenerator LoadProgram");
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
                Debug.Log("GameMapGenerator LoadProgram Exception");
                Debug.LogException(e);
            }
        }

        public async Awaitable GenerateNewLevel()
        {
            Debug.Log("GameMapGenerator GenerateNewLevel");
            await Awaitable.MainThreadAsync();
            Debug.Log("GameMapGenerator GenerateNewLevel First Main");
            IsGenerating = true;
            _squares = new List<MapSquareData>();
            _rooms = new List<RoomData>();
            _adjacencies = new Dictionary<ulong, HashSet<ulong>>();
        
            if (string.IsNullOrEmpty(_program))
            {
                IsGenerating = false;
                onMapGenerationFailed.Invoke();
                Debug.LogError("GameMapGenerator.GenerateNewLevel finished early due to null program");
                return;
            }
            
            // Run the slow level generation stage in a background thread
            await Awaitable.BackgroundThreadAsync();
            Debug.Log("GameMapGenerator.GenerateNewLevel Background Creating Gen");
            using var gen = new LevelGenerator.LevelGenerator(
                maxNumLevels, width, height, minRooms, maxRooms, seed, _program, solverThreads
            );
            var newSolution = gen.SolveSafe();
            Debug.Log("GameMapGenerator.GenerateNewLevel Background Solved");
            using var level = gen.BestLevel();
            if (level == null)
            {
                Debug.Log("GameMapGenerator GenerateNewLevel null level");
                await Awaitable.MainThreadAsync();
                Debug.Log("GameMapGenerator GenerateNewLevel null level Main");
                IsGenerating = false;
                onMapGenerationFailed.Invoke();
                Debug.LogError("GameMapGenerator.GenerateNewLevel finished early due to null level");
                return;
            }
            Debug.LogFormat("GameMapGenerator.GenerateNewLevel Got Level: {0:N0} {1:N0} {2:N0}", level.NumMapSquares, level.NumRooms, level.NumAdjacencies);

            // Retrieve all the data into local copies, so the unmanaged data can be destroyed safely
            Debug.Log("GameMapGenerator.GenerateNewLevel Getting squares");
            using var squares = level.MapSquares();
            Debug.Log("GameMapGenerator.GenerateNewLevel Got squares");
            var newSquares = squares.GetEnumerable().Select(
                sq =>
                {
                    var data = (MapSquareData)sq;
                    sq.Dispose();
                    return data;
                }
            ).ToList();
            Debug.Log("GameMapGenerator.GenerateNewLevel Converted squares");
            Debug.Log("GameMapGenerator.GenerateNewLevel Getting rooms");
            using var rooms = level.Rooms();
            Debug.Log("GameMapGenerator.GenerateNewLevel Got rooms");
            var newRooms = rooms.GetEnumerable().Select(
                rm =>
                {
                    var data = (RoomData)rm;
                    rm.Dispose();
                    return data;
                }
            ).ToList();
            Debug.Log("GameMapGenerator.GenerateNewLevel Converted rooms");
            Debug.Log("GameMapGenerator.GenerateNewLevel Getting adjs");
            using var adjacencies = level.Adjacencies();
            Debug.Log("GameMapGenerator.GenerateNewLevel Got adjs");
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
            Debug.Log("GameMapGenerator.GenerateNewLevel Converted rooms");
            await Awaitable.MainThreadAsync();
            Debug.Log("GameMapGenerator.GenerateNewLevel Last Main");
            _squares  = newSquares;
            _rooms = newRooms;
            _adjacencies = newAdjacencies;
            _solution = newSolution;
            
            IsGenerating = false;
            Debug.Log("GameMapGenerator.GenerateNewLevel Sending Event");
            onMapGenerated.Invoke(new MapResult(_squares, _rooms, _adjacencies, width, height));
            Debug.Log("GameMapGenerator.GenerateNewLevel Done");
        }

        public void PrintArrays()
        {
            Debug.Log("GameMapGenerator.PrintArrays");
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
