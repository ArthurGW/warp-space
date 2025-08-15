using System;
using System.Linq;
using Layout;
using static Layout.LayoutUtils;
using UnityEngine;

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

        private void Awake()
        {
            _switchDetector = GetComponent<BoxCollider>();
            _pulsers = GameObject.FindGameObjectsWithTag("WarpBlue")
                .Where(go => go.transform.IsChildOf(transform))
                .Select(go=>go.GetComponent<MeshRenderer>())
                .ToArray();
            foreach (var pulser in _pulsers)
            {
                pulser.material = new Material(firstMaterial);
            }
        }

        private void Update()
        {
            foreach (var pulser in _pulsers)
            {
                pulser.material.Lerp(firstMaterial, secondMaterial, pulseAmount);
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            // if (!other.gameObject.CompareTag("Player")) return;
            //
            // // Player has touched control
            // _switchDetector.enabled = false;  // This is a one-time operation, no need to keep detecting
        }
    }
}
