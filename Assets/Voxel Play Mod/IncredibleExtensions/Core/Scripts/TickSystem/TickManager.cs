using UnityEngine;
using System;
using System.Collections;

namespace IncredibleExtensions.VPAddons{
    public class TickManager : MonoBehaviour {
        public static TickManager instance;

        public static event Action OnTick; // Any system can subscribe to this event
        public float tickRate = 0.2f; // How many seconds per tick

        private void Awake() {
            if (instance == null) instance = this;
        }

        private void Start() {
            StartCoroutine(TickLoop());
        }

        private IEnumerator TickLoop() {
            while (true) {
                yield return new WaitForSeconds(tickRate);
                OnTick?.Invoke();
            }
        }
    }
}