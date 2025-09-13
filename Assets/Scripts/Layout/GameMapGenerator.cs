using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LevelGenerator.Extensions;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Layout
{
    public class GameMapGenerator : MonoBehaviour
    {
        #region Level Generator Params

        public uint width = 10;
        public uint height = 8;
        public uint minRooms = 2;
        public uint maxRooms = 6;
        public uint numBreaches = 1;
        public uint numPortals = 0;
        public uint maxNumLevels = 1;

        [Range(1, 16)] public uint solverThreads = 1;

        [Tooltip("seed=0 means use a random seed rather than a fixed value")]
        public uint seed = 0;

        #endregion

        private CancellationTokenSource _disableTokenSource = new();
        private LevelGenerator.LevelGenerator _currentGen;

        private void Awake()
        {
            ResetTokens();
        }

#if UNITY_EDITOR
        private void Start()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) DoCancel();
        }
#endif

        public void ResetTokens()
        {
            _ = destroyCancellationToken;  // Ensure token is initialised
            if (_disableTokenSource is { IsCancellationRequested: false }) return;
            _disableTokenSource?.Dispose();
            _disableTokenSource = new CancellationTokenSource();
        }

        private void OnEnable()
        {
            ResetTokens();
        }

        private void OnDisable()
        {
            DoCancel();
        }

        private void OnDestroy()
        {
            DoCancel();
            _disableTokenSource?.Dispose();
            _disableTokenSource = null;
        }

        public void DoCancel()
        {
            if (_disableTokenSource is { IsCancellationRequested: false }) _disableTokenSource.Cancel(false);
            _currentGen?.Interrupt();
        }

        public void InterruptIfHasLevel()
        {
            if (_currentGen == null) return;
            _currentGen.InterruptIfHasLevel();
        }

        public bool CheckCancel()
        {
            return destroyCancellationToken.IsCancellationRequested ||  _disableTokenSource == null || _disableTokenSource.Token.IsCancellationRequested;
        }

        public async Awaitable<MapResult> GenerateNewLevel()
        {
            return await GenerateNewLevel(maxNumLevels, width, height, minRooms, maxRooms, numBreaches, numPortals, seed, solverThreads);
        }
        
        public async Awaitable<MapResult> GenerateNewLevel(
            uint maxNumberLevels, uint mapWidth, uint mapHeight, uint minRoomCount, uint maxRoomCount, uint numAlienBreaches, uint numPortals, uint mapSeed, uint numSolverThreads
        )
        {
            // Run the slow level generation stage in a background thread
            await Awaitable.BackgroundThreadAsync();
            try
            {
                _currentGen = new LevelGenerator.LevelGenerator(
                    maxNumberLevels, mapWidth, mapHeight, minRoomCount, maxRoomCount, numAlienBreaches, numPortals, mapSeed, false,
                    numSolverThreads
                );
                using (_currentGen)
                {
                    return DoSolve(_currentGen);
                }
            }
            finally
            {
                _currentGen = null;
            }
        }

        private MapResult DoSolve(LevelGenerator.LevelGenerator gen)
        {
            gen.SolveSafe(CheckCancel);
            
            using var level = gen.BestLevel();
            if (level == null)
            {
                return null;
            }

            var startRoomId = level.StartRoom;
            var finishRoomId = level.FinishRoom;

            // Retrieve all the data into local copies, so the unmanaged data can be destroyed safely
            using var squares = level.MapSquares();
            var newSquares = squares.GetEnumerable().Select(sq =>
                {
                    var data = (MapSquareData)sq;
                    sq.Dispose();
                    return data;
                }
            ).ToList();

            using var rooms = level.Rooms();
            var newRooms = rooms.GetEnumerable().Select(rm =>
                {
                    var data = (RoomData)rm;
                    rm.Dispose();
                    return data;
                }
            ).ToList();

            using var doors = level.Doors();
            var newDoors = newRooms.ToDictionary(rm => rm.Id, rm => new HashSet<ulong>());
            foreach (var door in doors)
            {
                if (newDoors.TryGetValue(door.FirstId, out var doorTo)) doorTo.Add(door.SecondId);
                door.Dispose();
            }
            
            using var portals = level.Portals();
            var newPortals = newRooms.ToDictionary(rm => rm.Id, rm => new HashSet<ulong>());
            foreach (var portal in portals)
            {
                if (newPortals.TryGetValue(portal.FirstId, out var portalTo)) portalTo.Add(portal.SecondId);
                portal.Dispose();
            }
            
            return new MapResult(newSquares, newRooms, newDoors, newPortals, startRoomId, finishRoomId, gen.NumLevels);
        }
    }
}
