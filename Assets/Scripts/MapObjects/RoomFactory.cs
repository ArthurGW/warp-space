using System;
using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;
using static MapObjects.ObjectUtils;
using Random = UnityEngine.Random;

namespace MapObjects
{
    public readonly struct Door : IEquatable<Door>
    {
        public readonly uint InternalX;
        public readonly uint InternalY;
        public readonly RoomType ConnectsTo;
        public readonly CardinalDirection Direction;

        public Door(uint internalX, uint internalY, RoomType connectsTo, CardinalDirection direction)
        {
            InternalX = internalX;
            InternalY = internalY;
            ConnectsTo = connectsTo;
            Direction = direction;
        }

        public override bool Equals(object obj)
        {
            return obj is Door other && Equals(other);
        }

        public bool Equals(Door other)
        {
            return Direction == other.Direction
                   && InternalX == other.InternalX 
                   && InternalY == other.InternalY
                   && ConnectsTo == other.ConnectsTo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(InternalX, InternalY, (byte)ConnectsTo, (byte)Direction);
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

        private void Awake()
        {
            _corridorFactory = GetComponent<CorridorFactory>();
        }
                
        public void DestroyRooms()
        {
#if UNITY_EDITOR
            _corridorFactory = GetComponent<CorridorFactory>();
#endif
            DestroyAllChildren(roomContainer);
            _corridorFactory.DestroyCorridors();
        }

        public void ConstructRooms(List<RoomData> rooms, Dictionary<ulong, RoomData> roomsById, Dictionary<ulong, HashSet<ulong>> adjacencies, ulong startRoom)
        {
            DestroyRooms();
            
            InitDoors(rooms, roomsById, adjacencies);
            
#if UNITY_EDITOR
            _corridorFactory = GetComponent<CorridorFactory>();
#endif
            _corridorFactory.ConstructCorridors(rooms.Where(rm => rm.Type == RoomType.Corridor), _doorsByRoomId);
            foreach (var room in rooms.Where(rm => rm.Type == RoomType.Room))
            {
                var roomController = Instantiate(roomPrefab, roomContainer, false);
                roomController.SetData(room, _doorsByRoomId);
                if (room.Id == startRoom)
                    roomController.GetComponent<LightController>().TurnOnLights();
            }

            
        }
        
        private void InitDoors(List<RoomData> rooms, Dictionary<ulong, RoomData> roomsById, 
            Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            var seen = new HashSet<(ulong firstId, ulong secondId)>();
            var doors = new List<(ulong roomId, Door door)>();

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

        private static (Door roomDoor, Door adjRoomDoor) MakeDoor(RoomData room, RoomData adjRoom)
        {
            // Find the contacting edge, of which there can only be one as rooms are rectangular
            if (adjRoom.X == room.X + room.Width || adjRoom.X + adjRoom.Width == room.X)
            {
                // To the east or west, contacts a vertical edge
                var choices = Enumerable.Range((int)room.Y, (int)room.Height)
                    .Intersect(Enumerable.Range((int)adjRoom.Y, (int)adjRoom.Height))
                    .ToArray();
                var doorPoint = (uint)Random.Range(choices.Min(), choices.Max() + 1);
                var directions = room.X < adjRoom.X 
                    ? (rm: CardinalDirection.East, adj: CardinalDirection.West) 
                    : (rm: CardinalDirection.West, adj: CardinalDirection.East);
                var xPos = room.X < adjRoom.X 
                    ? (rm: room.Width, adj: 1U) 
                    : (rm: 1U, adj: adjRoom.Width);
                return (
                    new Door(xPos.rm, doorPoint - room.Y + 1, adjRoom.Type, directions.rm),
                    new Door(xPos.adj, doorPoint - adjRoom.Y + 1, room.Type, directions.adj)
                );
            }
            {
                // To the south or north, contacts a horizontal edge
                var choices = Enumerable.Range((int)room.X, (int)room.Width)
                    .Intersect(Enumerable.Range((int)adjRoom.X, (int)adjRoom.Width))
                    .ToArray();
                var doorPoint = (uint)Random.Range(choices.Min(), choices.Max() + 1);
                var directions = room.Y < adjRoom.Y
                    ? (rm: CardinalDirection.South, adj: CardinalDirection.North)
                    : (rm: CardinalDirection.North, adj: CardinalDirection.South);
                var yPos = room.Y < adjRoom.Y
                    ? (rm: room.Height, adj: 1U)
                    : (rm: 1U, adj: adjRoom.Height);
                return (
                    new Door(doorPoint - room.X + 1, yPos.rm, adjRoom.Type, directions.rm),
                    new Door(doorPoint - adjRoom.X + 1, yPos.adj, room.Type, directions.adj)
                );
            }
        }
    }
}
