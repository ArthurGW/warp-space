using System;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        private Transform _player;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _player = GameObject.FindWithTag("Player")?.transform;
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            _agent.SetDestination(_player.position);
        
        }
    }
}
