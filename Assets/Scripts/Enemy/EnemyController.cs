using Enemy.States;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    [RequireComponent(typeof(AudioSource), typeof(NavMeshAgent), typeof(ParticleSystem))]
    public class EnemyController : MonoBehaviour
    {
        private Transform _player;

        private AudioSource _audioSource;
        [SerializeField] private AudioClip shockSound;

        private ParticleSystem _particleSystem;

        private EnemyState _state;

        public float coolDownTime = 3f;
        private float _coolDown;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _audioSource = GetComponent<AudioSource>();
            _particleSystem = GetComponent<ParticleSystem>();
            _state = new InitialState(transform, GetComponent<NavMeshAgent>(), _player);
        }

        public void SetState(EnemyState state)
        {
            _state.OverrideNextState(state);
        }
        
        private void Update()
        {
           var newState = _state.Update();
           if (newState != null)
               _state = newState;
           if (_coolDown > 0f)
               _coolDown -= Time.deltaTime;
        }

        private void Fire()
        {
            _audioSource.PlayOneShot(shockSound);
            _particleSystem.Play();
            _coolDown = coolDownTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != _player.gameObject) return;

            Fire();
        }

        private void OnTriggerStay(Collider other)
        {
            if (_coolDown > 0f || other.gameObject != _player.gameObject) return;
            
            Fire();
        }
    }
}
