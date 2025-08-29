using System;
using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using static Layout.LayoutUtils;
using UnityEngine;

namespace MapObjects
{
    [RequireComponent(typeof(LightController))]
    public class RoomController : MonoBehaviour
    {
        [SerializeField]
        private GameObject floorPrefab;
        
        [SerializeField]
        private GameObject wallPrefab;
        
        [SerializeField]
        private GameObject breachWallPrefab;
        
        [SerializeField]
        private GameObject doorPrefab;
        
        public RoomData RoomData;
        
        private LightController _lightController;

        public static RoomData? GetRoomDataForPosition(Vector3 position)
        {
            var rc = FindObjectsByType<RoomController>(FindObjectsSortMode.None)
                .FirstOrDefault(rc => rc.RoomData.Contains(position));
            return !rc ? null : rc.RoomData;
        }

        private void Awake()
        {
            _lightController = GetComponent<LightController>();
        }

        public void SetData(RoomData data, ILookup<ulong, Door> doorsByRoomId)
        {
            RoomData = data;
            transform.localPosition = data.ToPosition();
            
            foreach (var x in Enumerable.Range(1, (int)data.Width))
            {
                foreach (var y in Enumerable.Range(1, (int)data.Height))
                {
                    InstantiateSquare(((uint)x, (uint)y), doorsByRoomId);
                }
            }
            
#if UNITY_EDITOR
            _lightController = GetComponent<LightController>();
#endif
            _lightController.SetUpLights(data);
        }

        private void InstantiateSquare((uint X, uint Y) pos, ILookup<ulong, Door> doorsByRoomId)
        {
            // Don't offset the 1,1 square (locally)
            var localPosition = GridToPosition((pos.X, pos.Y));
            
            // Floor
            var floor = Instantiate(floorPrefab, transform, false);
            floor.transform.localPosition = localPosition;
            
            // Walls and doors
            var walls = new List<CardinalDirection>();
            if (pos.X == 1U) walls.Add(CardinalDirection.West);
            if (pos.Y == 1U) walls.Add(CardinalDirection.North);
            if (pos.X == RoomData.Width) walls.Add(CardinalDirection.East);
            if (pos.Y == RoomData.Height) walls.Add(CardinalDirection.South);
            foreach (var wallDir in walls)
            {
                var doors = doorsByRoomId[RoomData.Id].Where(d => d.InternalX == pos.X && d.InternalY == pos.Y && d.Direction == wallDir).ToArray();
                GameObject prefab;
                if (doors.Length == 0)
                {
                    prefab = wallPrefab;
                }
                else
                {
                    prefab = doors.First().ConnectsTo switch
                    {
                        RoomType.Room or RoomType.Corridor => doorPrefab,
                        RoomType.AlienBreach => breachWallPrefab,
                        _ => throw new ArgumentException()
                    };
                }

                var wall = Instantiate(prefab, transform, false);
                wall.transform.SetLocalPositionAndRotation(localPosition, wallDir.ToRotation());
            }
        }
    }
}
