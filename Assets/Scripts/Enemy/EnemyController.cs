using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    [RequireComponent(typeof(AudioSource), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        private GameObject _player;
        private NavMeshAgent _agent;
        
        private AudioSource _audioSource;
        [SerializeField] private AudioClip shockSound;

        private void Awake()
        {
            _player = GameObject.FindWithTag("Player");
            _agent = GetComponent<NavMeshAgent>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            _agent.SetDestination(_player.transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != _player) return;
            
            _audioSource.PlayOneShot(shockSound);
        }
    }
}
