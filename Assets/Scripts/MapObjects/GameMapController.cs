using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;
using static MapObjects.ObjectUtils;

namespace MapObjects
{
    /// <summary>
    /// Class to convert an abstract generated map into a collection of managed GameObjects
    /// </summary>
    public class GameMapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject shipSquarePrefab;
        
        private HullFactory _hullFactory;
        private RoomFactory _roomFactory;

        [SerializeField] private Transform shipSquareContainer;

        private void Awake()
        {
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
        }

        public void OnMapGenerationFailed()
        {
            Debug.Log("GameMapController.OnMapGenerationFailed");
            DestroyMap();
            Debug.Log("GameMapController.OnMapGenerationFailed Done");
        }

        private void DestroyMap()
        {
#if UNITY_EDITOR
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
#endif
            _hullFactory.DestroyHull();
            _roomFactory.DestroyRooms();
            DestroyAllChildren(shipSquareContainer);
        }

        public void OnMapGenerated(MapResult result)
        {
            DestroyMap();

            foreach (var room in result.Rooms.Where(rm => rm.Type == RoomType.AlienBreach))
            {
                Debug.Log(room);
            }
            
            Debug.Log("GameMapController.OnMapGenerated");
            var roomsById = result.Rooms.ToDictionary(rm => rm.Id);
            Debug.Log("GameMapController.OnMapGenerated Room Dict");
            // var usedLocations = new HashSet<(uint, uint)>();

            _hullFactory.ConstructHull(result.Squares);
            
            Debug.Log("GameMapController.OnMapGenerated Parsing Rooms");
            
            _roomFactory.ConstructRooms(result.Rooms,
                roomsById,
                result.Adjacencies);

            Debug.Log("GameMapController.OnMapGenerated Parsing ship squares");
            // Finally, process ship squares to fill in the gaps
            // We could work out the gaps here, but we have Ship squares so we may as well use them
            foreach (var square in result.Squares
                         .Where(sq => sq.Type == SquareType.Ship)
                     )
            {
                var obj = Instantiate(shipSquarePrefab,
                    shipSquareContainer,
                    false);
                obj.transform.localPosition = square.ToPosition();
            }
            Debug.Log("GameMapController.OnMapGenerated Done");
        }
    }
}