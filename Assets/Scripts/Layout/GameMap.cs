using System;
using System.Collections.Generic;
using System.Linq;
using LevelGenerator;
using LevelGenerator.Extensions;
using UnityEngine;

namespace Layout
{
    public class GameMap : MonoBehaviour
    {
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
    
        private List<MapSquare> _squares;
        private List<Room> _rooms;
        private Dictionary<ulong, HashSet<ulong>> _adjacencies;
        
        // Flag for testing to notify when generation has finised
        protected bool isGenerated { private set; get; }
    
        #endregion

        private void Awake()
        {
            isGenerated = false;
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

    private async void Start()
        {
            try
            {
                if (_program == null)
                {
                    Debug.LogError("GameMap.Start finished early due to null program");
                    return;  // Solving won't be possible
                }
            
                // Run the slow level generation stage in a background thread
                await Awaitable.BackgroundThreadAsync();
                GenerateNewLevel();
                await Awaitable.MainThreadAsync();
                isGenerated = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnDestroy()
        {
            ClearArrays();
        }

        private void ClearArrays()
        {
            if (_squares != null)
            {
                _squares.ForEach(x => x.Dispose());
                _squares = null;        
            }
            if (_rooms != null)
            {
                _rooms.ForEach(x => x.Dispose());
                _rooms = null;
            }
            // No need to dispose of the adjacencies, they are primitive values here
            _adjacencies = null;
        }

        public void GenerateNewLevel()
        {
            ClearArrays();  // Clear arrays and ensure contents are disposed of
            _squares = new List<MapSquare>();
            _rooms = new List<Room>();
            _adjacencies = new Dictionary<ulong, HashSet<ulong>>();
        
            if (_program == null)
            {
                return;
            }
        
            using var gen = new LevelGenerator.LevelGenerator(
                maxNumLevels, width, height, minRooms, maxRooms, seed, _program, solverThreads
            );
            _solution = gen.SolveSafe();
            using var level = gen.BestLevel();

            // Retrieve all the data into local copies, so the generator can be destroyed safely
            using var squares = level.MapSquares();
            _squares = squares.GetEnumerable().ToList();

            using var rooms = level.Rooms();
            _rooms = rooms.GetEnumerable().ToList();
        
            using var adjacencies = level.Adjacencies();
            foreach (var adjacency in adjacencies)
            {
                if (!_adjacencies.TryGetValue(adjacency.FirstId, out var adjacentTo))
                {
                    adjacentTo = new HashSet<ulong>();
                    _adjacencies.Add(adjacency.FirstId, adjacentTo);
                }
                adjacentTo.Add(adjacency.SecondId);
                adjacency.Dispose();
            }
        }

        private void PrintArrays()
        {
            _squares.ForEach(sq => Debug.Log(sq.AsString()));
            _rooms.ForEach(rm => Debug.Log(rm.AsString()));
            foreach (var adj in _adjacencies)
            {
                Debug.Log($"Adjacency({adj.Key}: {String.Join(',', adj.Value)})");
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
