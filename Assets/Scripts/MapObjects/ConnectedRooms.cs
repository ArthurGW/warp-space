using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;

namespace MapObjects
{
    /// <summary>
    /// A structure managing connected rooms
    ///
    /// Since we don't care about *how* rooms are connected, we just store connections as sets of connected room IDs,
    /// rather than a full graph tree/forest structure.
    /// </summary>
    public class ConnectedRooms
    {
        // Groups of connected room IDs
        private List<HashSet<ulong>> _groups = new();
        private int _numValidSets = 0;
        private HashSet<ulong> _realRooms = new();

        public void AddRoom(RoomData room)
        {
            if (room.Type == RoomType.Room) _realRooms.Add(room.Id);
            
            _groups.Add(new HashSet<ulong> { room.Id });
            ++_numValidSets;
        }
        
        public void AddConnection((ulong first, ulong second) connection) => AddConnection(connection.first, connection.second);

        public void AddConnection(ulong first, ulong second)
        {
            var firstInd = -1;
            var secondInd = -1;
            for (var i = 0; i < _numValidSets; ++i)
            {
                if (firstInd == -1 && _groups[i].Contains(first)) firstInd = i;
                if (secondInd == -1 && _groups[i].Contains(second)) secondInd = i;
                if (firstInd != -1 && secondInd != -1) break;
            }

            if (firstInd == -1 || secondInd == -1)
            {
                Debug.LogError("Invalid room ID for connection");
                return;
            }

            if (firstInd == secondInd) return;  // Already connected
            
            var low = firstInd <= secondInd ? firstInd : secondInd;
            var high = firstInd <= secondInd ? secondInd : firstInd;
            
            // Merge the two groups, and place the high group out of the valid range
            _groups[low].UnionWith(_groups[high]);
            (_groups[high], _groups[_numValidSets - 1]) = (_groups[_numValidSets - 1], _groups[high]);
            --_numValidSets;
        }

        public List<ulong> GetConnectedRooms(ulong roomId)
        {
            var gid = Enumerable.Range(0, _numValidSets)
                .First(gid => _groups[gid].Contains(roomId));
            return _groups[gid].Where(rid => rid != roomId && _realRooms.Contains(rid)).ToList();
        }
    }
}
