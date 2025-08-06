using System.Collections.Generic;
using System.Linq;
using Layout;
using static Layout.LayoutUtils;
using UnityEngine;

namespace MapObjects
{
    public class RoomController : MonoBehaviour
    {
        [SerializeField]
        private GameObject floorPrefab;
        
        [SerializeField]
        private GameObject wallPrefab;
        
        [SerializeField]
        private GameObject doorPrefab;
        
        private RoomData _roomData;
        private HashSet<(ulong RoomId, uint InternalX, uint InternalY, CardinalDirection direction)> _doors;

        public void SetData(RoomData data, HashSet<(ulong RoomId, uint InternalX, uint InternalY, CardinalDirection direction)> doors)
        {
            _roomData = data;
            _doors = doors;
            foreach (var x in Enumerable.Range(0, (int)data.Width))
            {
                foreach (var y in Enumerable.Range(0, (int)data.Height))
                {
                    InstantiateSquare(((uint)x, (uint)y));
                }
            }
        }

        private void InstantiateSquare((uint X, uint Y) pos)
        {
            var localPosition = GridToPosition(pos);
            
            // Floor
            var floor = Instantiate(floorPrefab, transform, false);
            floor.transform.localPosition = localPosition;
            
            // Walls and doors
            var walls = new List<CardinalDirection>();
            if (pos.X == 0U) walls.Add(CardinalDirection.West);
            if (pos.Y == 0U) walls.Add(CardinalDirection.North);
            if (pos.X == _roomData.Width - 1)  walls.Add(CardinalDirection.East);
            if (pos.Y == _roomData.Height - 1)  walls.Add(CardinalDirection.South);
            foreach (var wallDir in walls)
            {
                var prefab = _doors.Contains((_roomData.Id, pos.X, pos.Y, wallDir)) ? doorPrefab : wallPrefab;
                var wall = Instantiate(prefab, transform, false);
                wall.transform.SetLocalPositionAndRotation(localPosition, wallDir.ToRotation());
            }
        }
    }
}
