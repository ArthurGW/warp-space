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
        
        public void ConstructCorridors(IEnumerable<RoomData> corridors, Dictionary<ulong, RoomData> roomsById, Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            Debug.Log("CorridorFactory.ConstructCorridors");
            foreach (var corridor in corridors)
            {
                var obj = Instantiate(corridorPrefab.gameObject, transform, false);
                var asSquare = (MapSquareData)corridor;
                obj.transform.localPosition = SquareToPosition(asSquare);
                var corridorController = obj.GetComponent<CorridorController>();
                corridorController.openDirections = GetOpenings(corridor, roomsById, adjacencies);
                corridorController.UpdateEntrances();
            }
            
            Debug.Log("CorridorFactory.ConstructCorridors Done");
        }

        private static CardinalDirections GetOpenings(RoomData corridor, Dictionary<ulong, RoomData> roomsById,
            Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            return adjacencies[corridor.Id]
                .Select(adj => roomsById[adj])
                .Select(room =>
                {
                    if (room.X == corridor.X + 1)
                    {
                        return CardinalDirections.East;
                    }
                    if (room.X + room.Width == corridor.X)
                    {
                        return CardinalDirections.West;
                    }
                    return room.Y == corridor.Y + 1 ? CardinalDirections.South : CardinalDirections.North;
                })
                .Aggregate((CardinalDirections)0, (total, add) => total | add);
        }
    }
}
