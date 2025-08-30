using System.Collections.Generic;
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
    /// In this state the enemy moves to an available new room to patrol
    /// </summary>
    public class ChangeRoomState : EnemyState
    {
        private readonly GameMapController _gameMapController;
        private bool _foundDestination;
        private RoomData _startRoom;

        private readonly bool _chooseClosestRoom;
        
        public ChangeRoomState(Transform enemy, NavMeshAgent enemyAgent, Transform player, bool chooseClosestRoom) 
            : base(enemy, enemyAgent, player)
        {
            _gameMapController = Object.FindAnyObjectByType<GameMapController>();
            _chooseClosestRoom = chooseClosestRoom;
        }

        private RoomData GetClosestRoom(List<ulong> choices) 
        {
            var closestDist = float.MaxValue;
            var closestRoomId = ulong.MaxValue;
            foreach (var rid in choices)
            {
                var sqrDist = _gameMapController.RoomsById[rid].ToWorldBounds().SqrDistance(Enemy.position);
                if (!(sqrDist < closestDist)) continue;
                            
                closestDist  = sqrDist;
                closestRoomId = rid;
            }

            return _gameMapController.RoomsById[closestRoomId];
        }
        
        protected override void Enter()
        {
            // Move to a room connected to the current location
            var roomData = RoomController.GetRoomDataForPosition(Enemy.position) ??
                           CorridorController.GetRoomDataForPosition(Enemy.position);
            if (!roomData.HasValue)
            {
                Debug.LogError("ChangeRoomState couldn't find room data");
                return;
            }
            _startRoom = roomData.Value;
            
            var choices = _gameMapController.GetConnectedRooms(_startRoom.Id);
            if (choices.Count == 0)
            {
                Debug.LogError("ChangeRoomState couldn't find any connected rooms");
                return;
            }

            // Choose either the closest room or a random one
            var newRoomData = _chooseClosestRoom
                ? GetClosestRoom(choices)
                : _gameMapController.RoomsById[choices[Random.Range(0, choices.Count)]];
                    
            var corners = newRoomData.Corners.ToArray();
            var targetInd = FindClosestPoint(corners);
            EnemyAgent.SetDestination(corners[targetInd]);
            _foundDestination = true;
        }

        protected override EnemyState DoIteration()
        {
            if (CanDetectPlayer) return new PursueState(Enemy, EnemyAgent, Player);
            
            if (!_foundDestination)
            {
                if (_startRoom.Type == RoomType.Room)
                    return new PatrolState(Enemy, EnemyAgent, Player);
            }   
            else if (IsAtDestination)
                return new PatrolState(Enemy, EnemyAgent, Player);
            return null;
        }

        protected override void Exit()
        {
        }
    }
}