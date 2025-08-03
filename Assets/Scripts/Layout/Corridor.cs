using System;
using UnityEngine;

namespace Layout
{
    public class Corridor : MonoBehaviour
    {
        public CardinalDirections selected;
        
        void Awake()
        {
            UpdateEntrances();
        }

        public void UpdateEntrances()
        {
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(selected.HasFlag((CardinalDirections)child.direction));
            }
        }
    }
}
