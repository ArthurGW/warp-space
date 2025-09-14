using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MapObjects
{
    [RequireComponent(typeof(BoxCollider))]
    public class WarpController : MonoBehaviour
    {
        private BoxCollider _switchDetector;
        
        public UnityEvent onWarp;

        private void Awake()
        {
            onWarp ??= new UnityEvent();
            _switchDetector = GetComponent<BoxCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            // Player has touched control
            _switchDetector.enabled = false;  // This is a one-time operation, no need to keep detecting
            onWarp?.Invoke();
        }
    }
}
