using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MapObjects
{
    [RequireComponent(typeof(Collider), typeof(MeshRenderer), typeof(AudioSource))]
    public class PortalEnd : MonoBehaviour
    {
        /// <summary>
        /// Priority of the portal, used for choosing which portals to activate, higher is higher priority
        /// </summary>
        public uint priority;

        private Vector2? _spawnPoint;
        
        private AudioSource _audioSource;
        
        private Vector2 SpawnPoint
        {
            get
            {
                // Delay calculating this until required, to ensure the portal is placed in the level first, then cache
                if (_spawnPoint.HasValue) return _spawnPoint.Value;
                
                var spawnPoint = GetComponentsInChildren<Transform>()
                    .First(t => t.CompareTag("Respawn"));
                _spawnPoint = new Vector2(spawnPoint.position.x, spawnPoint.position.z);
                return _spawnPoint.Value;
            }
        }
        
        private Quaternion? _spawnOrientation;
        
        private Quaternion SpawnOrientation
        {
            get
            {
                // Delay calculating this until required, to ensure the portal is placed in the level first, then cache
                if (_spawnOrientation.HasValue) return _spawnOrientation.Value;
                
                var spawnPoint = GetComponentsInChildren<Transform>()
                    .First(t => t.CompareTag("Respawn"));
                _spawnOrientation = spawnPoint.rotation;
                return _spawnOrientation.Value;
            }
        }

        public UnityEvent<Vector2, Quaternion> portalActivated;

        private PortalEnd _destination;

        private Collider _collider;
        private MeshRenderer _visualPortal;
        
        private void Awake()
        {
            portalActivated ??= new UnityEvent<Vector2, Quaternion>();
            _collider = GetComponent<Collider>();
            _collider.enabled = false;
            _visualPortal = GetComponent<MeshRenderer>();
            _visualPortal.enabled = false;
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !_destination) return;
            _audioSource.Play();
            _destination._audioSource.Play();
            portalActivated.Invoke(_destination.SpawnPoint, _destination.SpawnOrientation);
        }
        
        public bool HasDestinationPortal => _destination;

        public void SetDestinationPortal(PortalEnd other)
        {
            _destination = other;
            _collider.enabled = true;
            _visualPortal.enabled = true;
        }
    }
}