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

        [SerializeField] private Transform shipSquareContainer;

        [SerializeField]
        private CharacterController playerPrefab;
        
        [SerializeField]
        private WarpController warpControlPrefab;

        private HullFactory _hullFactory;
        private RoomFactory _roomFactory;

        private void Awake()
        {
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
        }

        public void OnMapGenerationFailed()
        {
            DestroyMap();
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
            
            var roomsById = result.Rooms.ToDictionary(rm => rm.Id);

            _hullFactory.ConstructHull(result.Squares);
            
            // Generate rooms and corridors
            _roomFactory.ConstructRooms(result.Rooms, roomsById,
                result.Adjacencies);
            
            // Set up the start and finish rooms
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
#if UNITY_EDITOR 
                DestroyImmediate(player);
#else
                Destroy(player);
#endif
            }
            var warp = GameObject.FindGameObjectWithTag("WarpControl");
            if (warp != null)
            {
#if UNITY_EDITOR 
                DestroyImmediate(warp);
#else
                Destroy(warp);
#endif
            }

            if (!roomsById.TryGetValue(result.StartRoomId, out var startRoom))
            {
                startRoom = result.Rooms.First(rm => rm.Type == RoomType.Room);
            }
            if (!roomsById.TryGetValue(result.FinishRoomId, out var finishRoom))
            {
                finishRoom = result.Rooms.Last(rm => rm.Type == RoomType.Room);
            }
            
            // Player goes in the start room
            var playerPos = startRoom.ToWorldCenter();
            playerPos.y = playerPrefab.height / 2;
            Instantiate(playerPrefab, playerPos, Quaternion.identity);
            
            // Warp control goes in the finish room
            Instantiate(warpControlPrefab, finishRoom.ToWorldCenter() , Quaternion.identity);

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