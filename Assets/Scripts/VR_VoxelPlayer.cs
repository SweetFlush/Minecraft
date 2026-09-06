using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using Autohand;

namespace VR_Minecraft
{
    public class VR_VoxelPlayer : VoxelPlayPlayer
    {
        [Header("VR Specific Settings")]
        [SerializeField] private AutoHandPlayer _autoHandPlayer;
        public AutoHandPlayer AutoHandPlayer
        {
            get
            {
                if (_autoHandPlayer == null)
                {
                    _autoHandPlayer = AutoHandPlayer.Instance;
                }
                return _autoHandPlayer;
            }
            set => _autoHandPlayer = value;
        }

        [Header("Haptic Feedback on Damage")]
        [SerializeField] private bool enableDamageHaptics = true;
        [SerializeField] private float damageHapticDuration = 0.2f;
        [SerializeField] private float damageHapticAmplitude = 0.7f;

        protected void Awake()
        {
            //base.Awake();
            if (_autoHandPlayer == null)
            {
                _autoHandPlayer = GetComponentInParent<AutoHandPlayer>();
            }
        }

        public override void DamageToPlayer(int damagePoints)
        {
            base.DamageToPlayer(damagePoints);

            if (enableDamageHaptics && damagePoints > 0)
            {
                TriggerDamageHaptics();
            }
        }

        /// <summary>
        /// 피격 시 양손 컨트롤러에 햅틱 진동 발생
        /// </summary>
        public virtual void TriggerDamageHaptics()
        {
            if (AutoHandPlayer == null) return;

            if (AutoHandPlayer.handRight != null)
            {
                AutoHandPlayer.handRight.PlayHapticVibration(damageHapticDuration, damageHapticAmplitude);
            }
            if (AutoHandPlayer.handLeft != null)
            {
                AutoHandPlayer.handLeft.PlayHapticVibration(damageHapticDuration, damageHapticAmplitude);
            }
        }
    }
}
