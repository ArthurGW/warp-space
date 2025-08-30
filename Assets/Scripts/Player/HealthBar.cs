using System.Collections.Generic;
using System.Linq;
using Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    [RequireComponent(typeof(Image))]
    public class HealthBar : MonoBehaviour
    {
        private List<HealthEntry> _healthEntries;
        private Image _background;
        
        [SerializeField] private PlayerController playerController;

        private uint _lastHealth;
    
        private void Awake()
        {
            _healthEntries = GetComponentsInChildren<HealthEntry>().ToList();
            _lastHealth = playerController.maxHealth + 1;  // i.e. not a possible value
            _background = GetComponent<Image>();
        }

        private void Update()
        {
            var newHealth = playerController.Health;
            if (newHealth == _lastHealth) return;

            foreach (var healthEntry in _healthEntries)
            {
                healthEntry.EntryImage.enabled = newHealth >= healthEntry.minHealth;
            }
            _background.color = _healthEntries.Any(e => e.EntryImage.enabled) ? Color.white : Constants.EnemyColor;
            
            _lastHealth = newHealth;
        }
    }
}
