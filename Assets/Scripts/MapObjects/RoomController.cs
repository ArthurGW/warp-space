using System.Collections.Generic;
using System.Linq;
using Layout;
using static Layout.LayoutUtils;
using UnityEngine;
using static MapObjects.ObjectUtils;

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
        private GameObject doorPrefab;
        
        private RoomData _roomData;
        
        private LightController _lightController;

        private void Awake()
        {
            _lightController = GetComponent<LightController>();
        }

        public void SetData(RoomData data, ILookup<ulong, Door> doorsByRoomId)
        {
            _roomData = data;
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
            if (pos.X == _roomData.Width)  walls.Add(CardinalDirection.East);
            if (pos.Y == _roomData.Height)  walls.Add(CardinalDirection.South);
            foreach (var wallDir in walls)
            {
                var prefab = doorsByRoomId[_roomData.Id].Contains(new Door(pos.X, pos.Y, wallDir)) ? doorPrefab : wallPrefab;
                var wall = Instantiate(prefab, transform, false);
                wall.transform.SetLocalPositionAndRotation(localPosition, wallDir.ToRotation());
            }
        }
    }
}
