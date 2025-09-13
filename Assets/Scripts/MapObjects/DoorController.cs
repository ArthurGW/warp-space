using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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

        public UnityEvent<(ulong, ulong)> doorOpened;

        private void FindRoomIds(){
            // Calculate this once and cache the result
            if (_roomIds.HasValue) return;
            
            var forwardPos = transform.position + transform.forward * 5;
            var backwardPos = transform.position - transform.forward * 5;
            var first = RoomController.GetRoomDataForPosition(forwardPos) ??
                        CorridorController.GetRoomDataForPosition(forwardPos);
            var second = RoomController.GetRoomDataForPosition(backwardPos) ??
                         CorridorController.GetRoomDataForPosition(backwardPos);
            if (first.HasValue && second.HasValue)
                _roomIds = (first.Value.Id, second.Value.Id);
        }

        private void Awake()
        {
            DoorId = _nextId++;
            _animator = GetComponent<Animator>();
            _triggerId = Animator.StringToHash("Open");
            _tagHandles = new[]{TagHandle.GetExistingTag("Player"), TagHandle.GetExistingTag("Enemy")};
            doorOpened ??= new UnityEvent<(ulong, ulong)>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_opened || !_tagHandles.Any(other.CompareTag)) return;
            
            _opened = true;
            _animator.SetTrigger(_triggerId);
            FindRoomIds();
            
            if (_roomIds.HasValue)
                doorOpened?.Invoke(_roomIds.Value);
        }
    }
}
