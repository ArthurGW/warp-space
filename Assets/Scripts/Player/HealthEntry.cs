using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    [RequireComponent(typeof(Image))]
    public class HealthEntry : MonoBehaviour
    {
        /// <summary>
        /// The minimum health below which to stop showing this health entry
        /// </summary>
        public uint minHealth;

        private void Awake()
        {
            EntryImage  = GetComponent<Image>();
        }

        public Image EntryImage { get; private set; }
    }
}
