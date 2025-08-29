using System.Linq;
using Layout;
using LevelGenerator;
using MapObjects;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemy.States
{
    /// <summary>
    /// In this state the enemy patrols its current room, and may change to patrolling a different room
    /// </summary>
    public class PatrolState : EnemyState
    {
        private Vector3[] _patrolPoints;
        private int _currentPatrolPoint;
        private RoomData _patrolRoom;
        private GameMapController _gameMapController;
        
        public PatrolState(Transform enemy, NavMeshAgent enemyAgent, Transform player) : base(enemy, enemyAgent, player)
        {
            _gameMapController = Object.FindAnyObjectByType<GameMapController>();
        }
        
        protected override void Enter()
        {
            var roomData = RoomController.GetRoomDataForPosition(Enemy.position);
            if (roomData is not { Type: RoomType.Room })
            {
                Debug.LogError("Patrol state should be in a room");
                return;
            }
            _patrolRoom = roomData.Value;
            _patrolPoints = FindPatrolRoute();
            
            // Find the closest corner, then in UpdateDestination() go to the *next* corner in the sequence
            // This avoids doubling back
            _currentPatrolPoint = FindClosestPatrolPoint();

            UpdateDestination();
        }

        private Vector3[] FindPatrolRoute()
        {
            // Get a shuffled patrol pattern
            var points = _patrolRoom.Corners.Clone() as Vector3[];
            for (var i = 0; i < points.Length; ++i)
            {
                var j = Random.Range(0, points.Length);
                (points[i], points[j]) = (points[j], points[i]);
            }
            return points;
        }

        private int FindClosestPatrolPoint()
        {
            return _patrolPoints
                .Select((corner, ind) => (Vector3.Distance(corner, Enemy.position), ind))
                .Aggregate(((float dist, int ind) first, (float dist, int ind) second) => first.dist <= second.dist ? first : second
                ).Item2;
        }

        private void UpdateDestination()
        {
            if (_patrolPoints.Length == 0) return;
            
            // Add a bit of noise to the destination
            var offset = Random.insideUnitCircle * 3f;
            ++_currentPatrolPoint;
            _currentPatrolPoint %= _patrolPoints.Length;
            var destination = _patrolPoints[_currentPatrolPoint] + new Vector3(offset.x, 0, offset.y);
            EnemyAgent.SetDestination(destination);
        }

        protected override EnemyState DoIteration()
        {
            if (IsAtDestination)
            {
                if (Random.value < 0.2f)
                {
                    // Change to another connected room, if possible
                    var choices = _gameMapController.GetConnectedRooms(_patrolRoom.Id);
                    if (choices.Count != 0)
                    {
                        var choice = choices[Random.Range(0, choices.Count)];
                        
                        _patrolRoom = _gameMapController.RoomsById[choice];
                        _patrolPoints = FindPatrolRoute();
                        _currentPatrolPoint = FindClosestPatrolPoint();
                        if (_currentPatrolPoint > 0) --_currentPatrolPoint;
                        else _currentPatrolPoint = _patrolPoints.Length - 1;
                    }
                }
                
                // Patrol next point
                UpdateDestination();
            }
            return null;
        }

        protected override void Exit()
        {
        }
    }
}