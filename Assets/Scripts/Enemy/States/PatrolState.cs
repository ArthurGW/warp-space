using System;
using Layout;
using LevelGenerator;
using MapObjects;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Enemy.States
{
    /// <summary>
    /// In this state the enemy patrols its current room
    /// </summary>
    public class PatrolState : EnemyState
    {
        private Vector3[] _patrolPoints;
        private int _currentPatrolPoint;
        private RoomData _patrolRoom;
        private readonly GameMapController _gameMapController;
        
        public PatrolState(EnemyController enemy, Transform player) : base(enemy, player)
        {
            _gameMapController = Object.FindAnyObjectByType<GameMapController>();
            _patrolPoints = Array.Empty<Vector3>();
        }
        
        protected override void Enter()
        {
            var roomData = RoomController.GetRoomDataForPosition(EnemyTransform.position);
            if (roomData is not { Type: RoomType.Room })
            {
                Debug.LogWarning("Patrol state should be in a room");
                return;
            }
            SetRoom(roomData.Value);
            UpdateDestination();
        }

        private void SetRoom(RoomData roomData)
        {
            _patrolRoom = roomData;
            _patrolPoints = FindPatrolRoute();
            
            // Find the closest corner, then in UpdateDestination() go to the *next* corner in the sequence
            // This avoids doubling back
            _currentPatrolPoint = FindClosestPoint(_patrolPoints);
        }

        private Vector3[] FindPatrolRoute()
        {
            // Get a shuffled patrol pattern
            var points = _patrolRoom.Corners.Clone() as Vector3[];
            if (points == null) return new Vector3[]{};
            
            for (var i = 0; i < points.Length; ++i)
            {
                var j = Random.Range(0, points.Length);
                (points[i], points[j]) = (points[j], points[i]);
            }
            return points;
        }

        private void UpdateDestination()
        {
            if (_patrolPoints.Length == 0) return;
            
            // Add a bit of noise to the destination
            var offset = Random.insideUnitCircle * 4f;
            ++_currentPatrolPoint;
            _currentPatrolPoint %= _patrolPoints.Length;
            var destination = _patrolPoints[_currentPatrolPoint] + new Vector3(offset.x, 0, offset.y);
            EnemyAgent.SetDestination(destination);
        }

        protected override EnemyState DoIteration()
        {
            if (CanDetectPlayer) return new PursueState(Enemy, PlayerTransform);

            // If we failed to set a route, move to another room
            if (_patrolPoints.Length == 0) return new ChangeRoomState(Enemy, PlayerTransform, true);
            
            if (!IsAtDestination) return null;
            
            if (_gameMapController.GetConnectedRooms(_patrolRoom.Id).Count > 0 && Random.value < 0.25f)
                // Change to another random connected room
                return new ChangeRoomState(Enemy, PlayerTransform, false);
                
            // Patrol next point
            UpdateDestination();
            return null;
        }

        protected override void Exit()
        {
        }
    }
}