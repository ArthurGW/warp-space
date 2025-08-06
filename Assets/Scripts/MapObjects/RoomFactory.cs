using System.Collections.Generic;
using Layout;
using UnityEngine;
using static Layout.LayoutUtils;

namespace MapObjects
{
  
    public class RoomFactory : MonoBehaviour
    {
        [SerializeField]
        private RoomController roomPrefab;
        
        public void ConstructRooms(IEnumerable<RoomData> rooms, Dictionary<ulong, RoomData> roomsById, Dictionary<ulong, HashSet<ulong>> adjacencies)
        {
            Debug.Log("RoomFactory.ConstructRooms");
            foreach (var room in rooms)
            {
                var obj = Instantiate(roomPrefab.gameObject, transform, false);
                obj.transform.localPosition = SquareToPosition((MapSquareData)room);
                var roomController = obj.GetComponent<RoomController>();
                roomController.SetData(room, new HashSet<(ulong RoomId, uint InternalX, uint InternalY, CardinalDirection direction)>());
            }
            
            Debug.Log("RoomFactory.ConstructRooms Done");
        }
    }
}
