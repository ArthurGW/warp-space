using Layout;
using UnityEngine;

namespace MapObjects
{
    [RequireComponent(typeof(LightController))]
    public class CorridorController : MonoBehaviour
    {
        [EnumButtons]
        public CardinalDirections openDirections;
        
        private LightController _lightController;
        
        private void Awake()
        {
            _lightController = GetComponent<LightController>();
            UpdateCorridor();
        }

        public void UpdateCorridor()
        {
            _lightController ??= GetComponent<LightController>();
            _lightController.SetUpLights(null);
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(openDirections.HasFlag((CardinalDirections)child.direction));
            }
        }
    }
}
