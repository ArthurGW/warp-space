using Enemy.States;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    [RequireComponent(typeof(AudioSource), typeof(NavMeshAgent), typeof(ParticleSystem))]
    public class EnemyController : MonoBehaviour
    {
        private Transform _player;

        public AudioClip shockSound;
        public AudioClip lockOnSound;

        private EnemyState _state;

        public float coolDownTime = 3f;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _state = new InitialState(this, _player);
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
        }
        
        private void OnTriggerEnter(Collider other)
        {
            _state.OnTriggerEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            _state.OnTriggerStay(other);
        }
    }
}
