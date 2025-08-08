using UnityEngine;

namespace MapObjects
{
    public class DoorController : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                _animator.SetTrigger("Open");
            }
        }
    }
}
