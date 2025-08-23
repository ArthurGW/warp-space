using System.Linq;
using UnityEngine;

namespace MapObjects
{
    public class DoorController : MonoBehaviour
    {
        private Animator _animator;

        private TagHandle[] _tagHandles;
        private int _triggerId;

        private bool _opened;  // Doors remain open, this flag indicates that state

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _triggerId = Animator.StringToHash("Open");
            _tagHandles = new[]{TagHandle.GetExistingTag("Player"), TagHandle.GetExistingTag("Enemy")};
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_opened || !_tagHandles.Any(other.CompareTag)) return;
            
            _opened = true;
            _animator.SetTrigger(_triggerId);
        }
    }
}
