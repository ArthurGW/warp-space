using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GameMapGenerator : MonoBehaviour
    {
        public UnityEvent<MapResult> onMapGenerated;
        public UnityEvent onMapGenerationFailed;

        #region Level Generator Params

        public uint width = 10;
        public uint height = 8;
        public uint minRooms = 2;
        public uint maxRooms = 6;
        public uint numBreaches = 1;
        public uint maxNumLevels = 1;

        [Range(1, 16)] public uint solverThreads = 1;

        [Tooltip("seed=0 means use a random seed rather than a fixed value")]
        public uint seed = 0;

        #endregion

        public bool IsGenerating { get; private set; }

        private void Awake()
        {
            IsGenerating = false;
            onMapGenerated ??= new UnityEvent<MapResult>();
            onMapGenerationFailed ??= new UnityEvent();
        }

        public async Awaitable<MapResult> GenerateNewLevel()
        {
            await Awaitable.MainThreadAsync();
            IsGenerating = true;

            // Run the slow level generation stage in a background thread
            await Awaitable.BackgroundThreadAsync();
            using var gen = new LevelGenerator.LevelGenerator(
                maxNumLevels, width, height, minRooms, maxRooms, numBreaches, seed, false, solverThreads
            );
            gen.SolveSafe();
            using var level = gen.BestLevel();
            if (level == null)
            {
                await Awaitable.MainThreadAsync();
                IsGenerating = false;
                onMapGenerationFailed.Invoke();
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

            IsGenerating = false;
            var mapResult = new MapResult(newSquares, newRooms, newAdjacencies, startRoomId, finishRoomId);
            onMapGenerated.Invoke(mapResult);
            return mapResult;
        }
    }
}
