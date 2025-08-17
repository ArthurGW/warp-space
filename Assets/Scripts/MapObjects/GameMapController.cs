using System;
using System.Linq;
using Layout;
using LevelGenerator;
using Player;
using UnityEngine;
using UnityEngine.Events;
using static MapObjects.ObjectUtils;
using Object = UnityEngine.Object;

namespace MapObjects
{
    /// <summary>
    /// Class to convert an abstract generated map into a collection of managed GameObjects
    /// </summary>
    public class GameMapController : MonoBehaviour
    {
        public UnityEvent mapComplete;
        
        [SerializeField] private GameObject shipSquarePrefab;

        [SerializeField] private Transform shipSquareContainer;

        [SerializeField] private WarpController warpControlPrefab;

        private PlayerMovement _playerController;
        private HullFactory _hullFactory;
        private RoomFactory _roomFactory;

        private void Awake()
        {
            mapComplete ??= new UnityEvent();
            _hullFactory = GetComponentInChildren<HullFactory>();
            _roomFactory = GetComponentInChildren<RoomFactory>();
            _playerController = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
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
        }

        public void OnMapGenerated(MapResult result)
        {
            DestroyMap();

            var roomsById = result.Rooms.ToDictionary(rm => rm.Id);

            // Create hull
            _hullFactory.ConstructHull(result.Squares);

            // Generate rooms and corridors
            _roomFactory.ConstructRooms(result.Rooms, roomsById,
                result.Adjacencies);

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
            playerPos.y = _playerController.GetComponent<CharacterController>().height / 2;
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
        }

        private void OnWarp()
        {
            mapComplete?.Invoke();
        }
    }
}