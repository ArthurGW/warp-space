using System;
using UnityEngine;

namespace Layout
{
    public class Corridor : MonoBehaviour
    {
        public CardinalDirections selected;
        
        private void Awake()
        {
            UpdateEntrances();
        }

        private void Start()
        {
            UpdateEntrances();
        }

        private void Reset()
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
