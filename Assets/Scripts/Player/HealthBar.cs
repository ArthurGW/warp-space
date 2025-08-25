using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player
{
    public class HealthBar : MonoBehaviour
    {
        private List<HealthEntry> _healthEntries;
        
        [SerializeField]
        private PlayerController playerController;

        private uint _lastHealth;
    
        private void Awake()
        {
            _healthEntries = GetComponentsInChildren<HealthEntry>().ToList();
            _lastHealth = playerController.maxHealth + 1;  // i.e. not a possible value
        }

        void Update()
        {
            var newHealth = playerController.Health;
            if (newHealth == _lastHealth) return;

            foreach (var healthEntry in _healthEntries)
            {
                healthEntry.EntryImage.enabled = newHealth >= healthEntry.minHealth;
            }
            
            _lastHealth = newHealth;
        }
    }
}
