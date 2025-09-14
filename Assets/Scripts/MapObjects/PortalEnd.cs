using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MapObjects
{
    [RequireComponent(typeof(Collider), typeof(AudioSource))]
    public class PortalEnd : MonoBehaviour
    {
        /// <summary>
        /// Priority of the portal, used for choosing which portals to activate, higher is higher priority
        /// </summary>
        public uint priority;

        public UnityEvent<Vector2, Quaternion> portalActivated;
        
        private Vector2? _spawnPoint;
        private Quaternion? _spawnOrientation;

        private AudioSource _audioSource;

        private Collider _collider;

        private GameObject _visualPortal;
        private Pulser _pulser;
        
        private GameObject _glower;

        public Vector2 SpawnPoint
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

        public Quaternion SpawnOrientation
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
        
        public PortalEnd Destination { get; private set; }

        private void Awake()
        {
            portalActivated ??= new UnityEvent<Vector2, Quaternion>();
            _collider = GetComponent<Collider>();
            _collider.enabled = false;
            _visualPortal = GetComponentInChildren<MeshRenderer>().gameObject;
            _visualPortal.SetActive(false);
            _audioSource = GetComponent<AudioSource>();
            _pulser = _visualPortal.GetComponent<Pulser>();
            _glower = GetComponentInChildren<Light>().gameObject;
            _glower.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !Destination) return;
            _audioSource.Play();
            Destination._audioSource.Play();
            portalActivated.Invoke(Destination.SpawnPoint, Destination.SpawnOrientation);
        }
        
        public bool HasDestinationPortal => Destination != null;

        private void SetDestinationPortal(PortalEnd other)
        {
#if UNITY_EDITOR
            portalActivated ??= new UnityEvent<Vector2, Quaternion>();
            _collider ??= GetComponent<Collider>();
            _visualPortal ??= GetComponentInChildren<MeshRenderer>().gameObject;
            _audioSource ??= GetComponent<AudioSource>();
            _pulser = _visualPortal.GetComponent<Pulser>();
            _glower ??= GetComponentInChildren<Light>().gameObject;
#endif
            Destination = other;
            _collider.enabled = true;
            _visualPortal.SetActive(true);
            _glower.SetActive(true);
        }

        public void ConnectTo(PortalEnd other)
        {
            SetDestinationPortal(other);
            other.SetDestinationPortal(this);
            
            // Randomise display for a bit of visual variety, while keeping paired portals in sync
            var rotationZ = Random.Range(0f, 360f);
            _visualPortal.transform.Rotate(0f, 0f, rotationZ);
            other._visualPortal.transform.Rotate(0f, 0f, rotationZ);

            var startingPhase = Random.Range(0f, Mathf.PI * 2);
            _pulser.startingPhase = startingPhase;
            other._pulser.startingPhase = startingPhase;

            var rate = Random.Range(2.5f, 3.5f);
            _pulser.rate = rate;
            other._pulser.rate = rate;
        }
    }
}