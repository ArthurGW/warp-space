using System.Collections.Generic;
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

        private ConnectedRooms _connectedRooms;
        private List<DoorController> _doorControllers;

        public Dictionary<ulong, RoomData> RoomsById;

        private void Awake()
        {
            mapComplete ??= new UnityEvent();
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
            _playerController = FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(controller => controller.CompareTag("Player"));
            _navMeshSurface = GetComponent<NavMeshSurface>();
        }
        
        public List<ulong> GetConnectedRooms(ulong roomId) => _connectedRooms.GetConnectedRooms(roomId);

        private void Start()
        {
            GetComponentInChildren<WarpController>()?.onWarp?.AddListener(OnWarp);
        }

        public void OnMapGenerationFailed()
        {
            DestroyMap();
        }

        public void DoorOpened(object doorId)
        {
            if (doorId is not uint)
            {
                Debug.LogError("invalid door id");
                return;
            }
            
            _connectedRooms.AddConnection(_doorControllers.First(dc => dc.DoorId == (uint)doorId).RoomIds);
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
            
            _connectedRooms = new ConnectedRooms();
            _doorControllers = new List<DoorController>();
            RoomsById = result.Rooms.ToDictionary(rm => rm.Id);

            // Create hull
            _hullFactory.ConstructHull(result.Squares);

            // Generate rooms and corridors
            _roomFactory.ConstructRooms(result.Rooms, RoomsById,
                result.Adjacencies, result.StartRoomId);
            _doorControllers = GetComponentsInChildren<DoorController>().ToList();
            
            // Initialise connected rooms - corridors are connected to corridors next to them, rooms are connected to
            // nothing at present as all doors start closed
            foreach (var rc in GetComponentsInChildren<RoomController>())
            {
                _connectedRooms.AddRoom(rc.RoomData);
            }
            foreach (var cc in GetComponentsInChildren<CorridorController>())
            {
                _connectedRooms.AddRoom(cc.RoomData);
            }
            var corridorsByGrid = result.Rooms
                .Where(rm => rm.Type == RoomType.Corridor)
                .ToDictionary(rm => (rm.X, rm.Y));
            foreach (var cc in GetComponentsInChildren<CorridorController>())
            {
                // Check just right and down, the other direction will be checked by squares above and left
                var corridorGrid = (cc.RoomData.X, cc.RoomData.Y);
                if (corridorsByGrid.TryGetValue(corridorGrid.Offset(1, 0), out var corridor))
                {
                    _connectedRooms.AddConnection(cc.RoomData.Id, corridor.Id);
                }
                if (corridorsByGrid.TryGetValue(corridorGrid.Offset(0, 1), out corridor))
                {
                    _connectedRooms.AddConnection(cc.RoomData.Id, corridor.Id);
                }
            }

            // Set up the start and finish rooms
            if (!RoomsById.TryGetValue(result.StartRoomId, out var startRoom))
            {
                startRoom = result.Rooms.First(rm => rm.Type == RoomType.Room);
            }

            if (!RoomsById.TryGetValue(result.FinishRoomId, out var finishRoom))
            {
                finishRoom = result.Rooms.Last(rm => rm.Type == RoomType.Room);
            }

            // Player goes in the start room
            var playerPos = startRoom.ToWorldCenter();
            playerPos.y = _playerController.height / 2;
            _playerController.transform.position = playerPos;
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