using System.Linq;
using Layout;
using UnityEngine;

namespace MapObjects
{
    [RequireComponent(typeof(LightController))]
    public class CorridorController : MonoBehaviour
    {
        [EnumButtons]
        public CardinalDirections openDirections;
        
        private LightController _lightController;
        
        public RoomData RoomData;
        
        public static RoomData? GetRoomDataForPosition(Vector3 position)
        {
            var cc = FindObjectsByType<CorridorController>(FindObjectsSortMode.None)
                .FirstOrDefault(cc => cc.RoomData.Contains(position));
            return !cc ? null : cc.RoomData;
        }
        
        private void Awake()
        {
            _lightController = GetComponent<LightController>();
            UpdateCorridor();
        }

        public void SetData(RoomData data, ILookup<ulong, Door> doorsByRoomId)
        {
            RoomData = data;
            var asSquare = (MapSquareData)data;
            transform.position = asSquare.ToPosition();
            openDirections = GetOpenings(data, doorsByRoomId);
            UpdateCorridor();
        }
        
        private static CardinalDirections GetOpenings(RoomData corridor, ILookup<ulong, Door> doorsByRoomId)
        {
            return doorsByRoomId[corridor.Id]
                .Select(door => door.Direction)
                .Aggregate((CardinalDirections)0, (total, add) => total | (CardinalDirections)add);
        }
        
        public void UpdateCorridor()
        {
#if UNITY_EDITOR
            _lightController = GetComponent<LightController>();
#endif
            _lightController.SetUpLights(null);
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(openDirections.HasFlag((CardinalDirections)child.direction));
            }
        }
    }
}
