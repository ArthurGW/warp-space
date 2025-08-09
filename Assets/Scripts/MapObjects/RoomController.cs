using System.Collections.Generic;
using System.Linq;
using Layout;
using static Layout.LayoutUtils;
using UnityEngine;

namespace MapObjects
{
    [RequireComponent(typeof(BoxCollider))]
    public class RoomController : MonoBehaviour
    {
        [SerializeField]
        private GameObject floorPrefab;
        
        [SerializeField]
        private GameObject wallPrefab;
        
        [SerializeField]
        private GameObject doorPrefab;
        
        private RoomData _roomData;
        private BoxCollider _entryDetector;
        private Light[]  _lights;

        private void Awake()
        {
            _entryDetector = GetComponent<BoxCollider>();
        }

        public void SetData(RoomData data, ILookup<ulong, Door> doorsByRoomId)
        {
            _entryDetector ??= GetComponent<BoxCollider>();
            
            _roomData = data;
            foreach (var x in Enumerable.Range(1, (int)data.Width))
            {
                foreach (var y in Enumerable.Range(1, (int)data.Height))
                {
                    InstantiateSquare(((uint)x, (uint)y), doorsByRoomId);
                }
            }
            
            var roomSize = GridToSize((_roomData.Width, _roomData.Height));
            roomSize.y = 5f;
            _entryDetector.size = roomSize;
            var roomCenter = GridToPosition(((_roomData.Width - 1), (_roomData.Height - 1))) / 2f;
            roomCenter.y = 2.5f;
            _entryDetector.center = roomCenter;
            
            // Turn off the lights - the entry detector will turn them back on
            _lights = GetComponentsInChildren<Light>();
            foreach (var child in _lights)
            {
                child.enabled = false;
            }
        }

        private void InstantiateSquare((uint X, uint Y) pos, ILookup<ulong, Door> doorsByRoomId)
        {
            // Don't offset the 1,1 square (locally)
            var localPosition = GridToPosition((pos.X - 1, pos.Y - 1));
            
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

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            // Player has entered room, turn on the lights
            Destroy(_entryDetector);  // This is a one-time operation, no need to keep detecting
            _entryDetector = null;
            foreach (var child in _lights)
            {
                child.enabled = true;
            }
        }
    }
}
