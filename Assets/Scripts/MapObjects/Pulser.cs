using System.Linq;
using UnityEngine;

namespace MapObjects
{
    public class Pulser : MonoBehaviour
    {
        private MeshRenderer[]  _pulsers;
        
        [SerializeField]
        private Material firstMaterial;
        
        [SerializeField]
        private Material secondMaterial;

        public float pulseAmount;
        private float _lastPulseAmount;

        public float startingPhase;
        private float _currentPhase;
        public float rate = 3f;

        private void Awake()
        {
            _pulsers = GameObject.FindGameObjectsWithTag("Pulser")
                .Where(go => go.transform.IsChildOf(transform))
                .Select(go=>go.GetComponent<MeshRenderer>())
                .ToArray();
            foreach (var pulser in _pulsers)
            {
                // Create a new material which we will use to animate the pulser
                pulser.material = new Material(firstMaterial);
            }

            _lastPulseAmount = pulseAmount;
            _currentPhase = startingPhase;
        }

        private void Update()
        {
            _currentPhase += rate * Time.deltaTime;
            _currentPhase %= Mathf.PI * 2;
            pulseAmount = (Mathf.Sin(_currentPhase) + 1) / 2;
            
            if (Mathf.Approximately(pulseAmount, _lastPulseAmount)) return;
            foreach (var pulser in _pulsers)
            {
                pulser.material.Lerp(firstMaterial, secondMaterial, pulseAmount);
            }
            _lastPulseAmount = pulseAmount;
        }
    }
}
