using System.Linq;
using Layout;
using LevelGenerator;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;
using static MapObjects.ObjectUtils;

namespace MapObjects
{
    /// <summary>
    /// Class to convert an abstract generated map into a collection of managed GameObjects
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class GameMapController : MonoBehaviour
    {
        public UnityEvent mapComplete;
        
        [SerializeField] private GameObject shipSquarePrefab;

        [SerializeField] private Transform shipSquareContainer;

        [SerializeField] private WarpController warpControlPrefab;

        private CharacterController _playerController;
        private HullFactory _hullFactory;
        private RoomFactory _roomFactory;
        private NavMeshSurface _navMeshSurface;

        private void Awake()
        {
            mapComplete ??= new UnityEvent();
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
            _playerController = FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(controller => controller.CompareTag("Player"));
            _navMeshSurface = GetComponent<NavMeshSurface>();
        }

        private void Start()
        {
            GetComponentInChildren<WarpController>()?.onWarp?.AddListener(OnWarp);
        }

        public void OnMapGenerationFailed()
        {
            DestroyMap();
        }

        public void DestroyMap()
        {
#if UNITY_EDITOR
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
            _navMeshSurface = GetComponent<NavMeshSurface>();
#endif
            _hullFactory.DestroyHull();
            _roomFactory.DestroyRooms();
            DestroyAllChildren(shipSquareContainer);
            
            var warp = GameObject.FindGameObjectWithTag("WarpControl");
            if (warp != null)
            {
                warp.GetComponent<WarpController>()?.onWarp.RemoveAllListeners();
                DestroyGameObject(warp);
            }
            
            _navMeshSurface.RemoveData();
        }

        public void OnMapGenerated(MapResult result)
        {
#if UNITY_EDITOR
            _playerController = FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(controller => controller.CompareTag("Player"));
#endif
            
            DestroyMap();

            var roomsById = result.Rooms.ToDictionary(rm => rm.Id);

            // Create hull
            _hullFactory.ConstructHull(result.Squares);

            // Generate rooms and corridors
            _roomFactory.ConstructRooms(result.Rooms, roomsById,
                result.Adjacencies, result.StartRoomId);

            // Set up the start and finish rooms
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
            playerPos.y = _playerController.height / 2;
            _playerController.Move(playerPos - _playerController.transform.position);
            _playerController.transform.rotation = Quaternion.identity;

            // Warp control goes in the finish room
            var warpControl = Instantiate(warpControlPrefab, transform);
            warpControl.transform.localPosition = finishRoom.ToWorldCenter();
            warpControl.onWarp.AddListener(OnWarp);

            // Finally, process ship squares to fill in the gaps
            // We could work out the gaps here, but we have Ship squares so we may as well use them
            foreach (var square in result.Squares.Where(sq => sq.Type == SquareType.Ship))
            {
                var obj = Instantiate(shipSquarePrefab, shipSquareContainer, false);
                obj.transform.localPosition = square.ToPosition();
            }
            
            // Rebuild the nav mesh with the new level geometry for enemies to navigate
            _navMeshSurface.BuildNavMesh();
        }

        private void OnWarp()
        {
            mapComplete?.Invoke();
        }
    }
}