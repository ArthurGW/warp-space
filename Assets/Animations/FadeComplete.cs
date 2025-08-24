using System;
using UnityEngine;
using UnityEngine.Events;

namespace Animations
{
    public class FadeComplete : MonoBehaviour
    {
        public UnityEvent onComplete;

        private void Awake()
        {
            onComplete ??= new UnityEvent();
        }
    }
}
