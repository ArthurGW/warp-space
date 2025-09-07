using System.Linq;
using UnityEngine;

namespace MapObjects
{
    public class DoorController : MonoBehaviour
    {
        private static uint _nextId = 0u;

        public uint DoorId { get; private set; }
        
        private Animator _animator;

        private TagHandle[] _tagHandles;
        private int _triggerId;

        private bool _opened;  // Doors remain open, this flag indicates that state

        private (ulong, ulong)? _roomIds;

        public (ulong, ulong) RoomIds
        {
            get
            {
                if (_roomIds.HasValue) return _roomIds.Value;
                
                var forwardPos = transform.position + transform.forward * 5;
                var backwardPos = transform.position - transform.forward * 5;
                var first = RoomController.GetRoomDataForPosition(forwardPos) ??
                            CorridorController.GetRoomDataForPosition(forwardPos);
                var second = RoomController.GetRoomDataForPosition(backwardPos) ??
                             CorridorController.GetRoomDataForPosition(backwardPos);
                // These must have a value now, as they must be either a room or a corridor
                _roomIds = (first.Value.Id, second.Value.Id);
                return _roomIds.Value;
            }
        }

        private void Awake()
        {
            DoorId = _nextId++;
            _animator = GetComponent<Animator>();
            _triggerId = Animator.StringToHash("Open");
            _tagHandles = new[]{TagHandle.GetExistingTag("Player"), TagHandle.GetExistingTag("Enemy")};
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_opened || !_tagHandles.Any(other.CompareTag)) return;
            
            _opened = true;
            _animator.SetTrigger(_triggerId);
            SendMessageUpwards("DoorOpened", RoomIds);
        }
    }
}
