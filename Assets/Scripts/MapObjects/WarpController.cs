using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MapObjects
{
    [RequireComponent(typeof(BoxCollider))]
    public class WarpController : MonoBehaviour
    {
        private BoxCollider _switchDetector;
        private MeshRenderer[]  _pulsers;
        
        [SerializeField]
        private Material firstMaterial;
        
        [SerializeField]
        private Material secondMaterial;

        public float pulseAmount = 0f;
        private float _lastPulseAmount = 0f;
        
        public UnityEvent onWarp;

        private void Awake()
        {
            onWarp ??= new UnityEvent();
            _switchDetector = GetComponent<BoxCollider>();
            _pulsers = GameObject.FindGameObjectsWithTag("WarpBlue")
                .Where(go => go.transform.IsChildOf(transform))
                .Select(go=>go.GetComponent<MeshRenderer>())
                .ToArray();
            foreach (var pulser in _pulsers)
            {
                // Create a new material which we will use to animate the pulser
                pulser.material = new Material(firstMaterial);
            }

            _lastPulseAmount = pulseAmount;
        }

        private void Update()
        {
            if (Mathf.Approximately(pulseAmount, _lastPulseAmount)) return;
            foreach (var pulser in _pulsers)
            {
                pulser.material.Lerp(firstMaterial, secondMaterial, pulseAmount);
            }
            _lastPulseAmount = pulseAmount;
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
