using UnityEngine;

namespace MapObjects
{
    public class DoorController : MonoBehaviour
    {
        private Animator _animator;

        private bool _opened;  // Doors remain open, this flag indicates that state

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player") || _opened) return;
            
            _opened = true;
            _animator.SetTrigger("Open");
        }
    }
}
