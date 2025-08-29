using Enemy.States;
using Player;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    [RequireComponent(typeof(AudioSource), typeof(NavMeshAgent), typeof(ParticleSystem))]
    public class EnemyController : MonoBehaviour
    {
        private PlayerController _player;
        
        private AudioSource _audioSource;
        [SerializeField] private AudioClip shockSound;
        
        private ParticleSystem  _particleSystem;

        private EnemyState _state;

        private void Awake()
        {
            _player = FindAnyObjectByType<PlayerController>();
            _audioSource = GetComponent<AudioSource>();
            _particleSystem = GetComponent<ParticleSystem>();
            _state = new InitialState(transform, GetComponent<NavMeshAgent>(), _player.transform);
        }

        private void Update()
        {
           var newState = _state.Update();
           if (newState != null)
               _state = newState;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != _player.gameObject) return;
            
            _audioSource.PlayOneShot(shockSound);
            _particleSystem.Play();
        }
    }
}
