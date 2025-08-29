using System.Collections.Generic;
using System.Linq;
using Layout;
using UnityEngine;
using static MapObjects.ObjectUtils;

namespace MapObjects
{
    public class CorridorFactory : MonoBehaviour
    {
        [SerializeField]
        private CorridorController corridorPrefab;

        [SerializeField] private Transform corridorContainer;
        
        public void ConstructCorridors(IEnumerable<RoomData> corridors, ILookup<ulong, Door> doorsByRoomId)
        {
            foreach (var corridor in corridors)
            {
                var obj = Instantiate(corridorPrefab, corridorContainer, false);
                obj.SetData(corridor, doorsByRoomId);
            }
        }

        public void DestroyCorridors()
        {
            DestroyAllChildren(corridorContainer);
        }
    }
}
