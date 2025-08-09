using System;
using System.Collections.Generic;
using System.Linq;
using Layout;
using UnityEngine;
using static MapObjects.ObjectUtils;

namespace MapObjects
{
    public readonly struct Door : IEquatable<Door>
    {
        private readonly uint _internalX;
        private readonly uint _internalY;
        public readonly CardinalDirection Direction;

        public Door(uint internalX, uint internalY, CardinalDirection direction)
        {
            _internalX = internalX;
            _internalY = internalY;
            Direction = direction;
        }

        public override bool Equals(object obj)
        {
            return obj is Door other && Equals(other);
        }

        public bool Equals(Door other)
        {
            return Direction == other.Direction
                   && _internalX == other._internalX 
                   && _internalY == other._internalY;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_internalX, _internalY, (byte)Direction);
        }

        public static bool operator ==(Door left, Door right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Door left, Door right)
        {
            return !left.Equals(right);
        }
    }
    
  
    [RequireComponent(typeof(CorridorFactory))]
    public class RoomFactory : MonoBehaviour
    {
        [SerializeField]
        private RoomController roomPrefab;
        
        private CorridorFactory _corridorFactory;

        [SerializeField] private Transform roomContainer;

        private ILookup<ulong, Door> _doorsByRoomId;

        private System.Random _doorRandom;

        public int doorSeed = 11;

        private void Awake()
        {
            _corridorFactory = GetComponent<CorridorFactory>();
            _doorRandom = new System.Random(doorSeed);
        }

        public void ConstructRooms(List<RoomData> rooms, Dictionary<ulong, RoomData> roomsById, Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            Debug.Log("RoomFactory.ConstructRooms");
            DestroyAllChildren(roomContainer);
            
            InitDoors(rooms, roomsById, adjacencies);
            
#if UNITY_EDITOR
            _corridorFactory = GetComponent<CorridorFactory>();
#endif
            _corridorFactory.ConstructCorridors(rooms.Where(rm => rm.IsCorridor), _doorsByRoomId);
            foreach (var room in rooms.Where(rm => !rm.IsCorridor))
            {
                var obj = Instantiate(roomPrefab.gameObject, roomContainer, false);
                obj.transform.localPosition = room.ToPosition();
                var roomController = obj.GetComponent<RoomController>();
                roomController.SetData(room, _doorsByRoomId);
            }
            
            Debug.Log("RoomFactory.ConstructRooms Done");
        }
        
        private void InitDoors(List<RoomData> rooms, Dictionary<ulong, RoomData> roomsById,
            Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            var seen = new HashSet<(ulong firstId, ulong secondId)>();
            var doors = new List<(ulong roomId, Door door)>();
            
#if UNITY_EDITOR
            _doorRandom = new System.Random(doorSeed);
#endif

            foreach (var room in rooms) 
            {
                foreach (var adjRoom in adjacencies[room.Id])
                {
                    var minId = Math.Min(room.Id, adjRoom);
                    var maxId = Math.Max(room.Id, adjRoom);
                    if (!seen.Add((minId, maxId))) continue;  // Already processed
                    var newDoors = MakeDoor(room, roomsById[adjRoom]);
                    doors.Add((room.Id, newDoors.roomDoor));
                    doors.Add((adjRoom, newDoors.adjRoomDoor));
                }
            }
            
            _doorsByRoomId = doors.ToLookup(entry => entry.roomId,  entry => entry.door);
        }

        private (Door roomDoor, Door adjRoomDoor) MakeDoor(RoomData room, RoomData adjRoom)
        {
            // Find the contacting edge, of which there can only be one as rooms are rectangular
            if (adjRoom.X == room.X + room.Width || adjRoom.X + adjRoom.Width == room.X)
            {
                // To the east or west, contacts a vertical edge
                var choices = Enumerable.Range((int)room.Y, (int)room.Height)
                    .Intersect(Enumerable.Range((int)adjRoom.Y, (int)adjRoom.Height))
                    .ToArray();
                var doorPoint = (uint)_doorRandom.Next(choices.Min(), choices.Max() + 1);
                var directions = room.X < adjRoom.X 
                    ? (rm: CardinalDirection.East, adj: CardinalDirection.West) 
                    : (rm: CardinalDirection.West, adj: CardinalDirection.East);
                var xPos = room.X < adjRoom.X 
                    ? (rm: room.Width, adj: 1U) 
                    : (rm: 1U, adj: adjRoom.Width);
                return (
                    new Door(xPos.rm, doorPoint - room.Y + 1, directions.rm),
                    new Door(xPos.adj, doorPoint - adjRoom.Y + 1, directions.adj)
                );
            }
            {
                // To the south or north, contacts a horizontal edge
                var choices = Enumerable.Range((int)room.X, (int)room.Width)
                    .Intersect(Enumerable.Range((int)adjRoom.X, (int)adjRoom.Width))
                    .ToArray();
                var doorPoint = (uint)_doorRandom.Next(choices.Min(), choices.Max());
                var directions = room.Y < adjRoom.Y
                    ? (rm: CardinalDirection.South, adj: CardinalDirection.North)
                    : (rm: CardinalDirection.North, adj: CardinalDirection.South);
                var yPos = room.Y < adjRoom.Y
                    ? (rm: room.Height, adj: 1U)
                    : (rm: 1U, adj: adjRoom.Height);
                return (
                    new Door(doorPoint - room.X + 1, yPos.rm, directions.rm),
                    new Door(doorPoint - adjRoom.X + 1, yPos.adj, directions.adj)
                );
            }
        }
    }
}
