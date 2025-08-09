using System.Collections.Generic;
using System.Linq;
using Layout;
using UnityEngine;
using static Layout.LayoutUtils;

namespace MapObjects
{
    public class CorridorFactory : MonoBehaviour
    {
        [SerializeField]
        private CorridorController corridorPrefab;
        
        public void ConstructCorridors(IEnumerable<RoomData> corridors, ILookup<ulong, Door> doorsByRoomId)
        {
            Debug.Log("CorridorFactory.ConstructCorridors");
            foreach (var corridor in corridors)
            {
                var obj = Instantiate(corridorPrefab.gameObject, transform, false);
                var asSquare = (MapSquareData)corridor;
                obj.transform.localPosition = asSquare.ToPosition();
                var corridorController = obj.GetComponent<CorridorController>();
                corridorController.openDirections = GetOpenings(corridor, doorsByRoomId);
                corridorController.UpdateCorridor();
            }
            
            Debug.Log("CorridorFactory.ConstructCorridors Done");
        }

        private static CardinalDirections GetOpenings(RoomData corridor, ILookup<ulong, Door> doorsByRoomId)
        {
            return doorsByRoomId[corridor.Id]
                .Select(door => door.Direction)
                .Aggregate((CardinalDirections)0, (total, add) => total | (CardinalDirections)add);
        }
    }
}
