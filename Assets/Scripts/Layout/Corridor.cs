using System;
using UnityEngine;

namespace Layout
{
    [Flags]
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }
    
    public class Corridor : MonoBehaviour
    {
        public Direction selected;
        
        void Awake()
        {
            UpdateEntrances();
        }

        public void UpdateEntrances()
        {
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(selected.HasFlag(child.direction));
            }
        }
    }
}
