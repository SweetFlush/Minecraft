using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;
using VoxelPlay;

namespace VR_Minecraft
{
    public class VR_VoxelFist : MonoBehaviour
    {
        public VR_VoxelPlayer player;
        public Hand hand;
        public LayerMask voxelEnvMask = -1;
        public Transform debugTransform;

        public bool enableCollision = true;
        [Tooltip("타격 시 햅틱 진동 활성화")]
        public bool enableHaptics = true;
        [Tooltip("물리 타격 데미지를 인정하는 최소 스윙 속도")]
        public float minImpactVelocity = 0.5f;

        private VoxelPlayEnvironment env;

        private void Start()
        {
            if (player == null)
            {
                player = GetComponentInParent<VR_VoxelPlayer>();
            }

            if (hand == null)
            {
                hand = GetComponentInChildren<Hand>();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!enableCollision) return;

            if (IsInLayerMask(collision.gameObject, voxelEnvMask))
            {
                var magnitude = collision.relativeVelocity.magnitude;
                if (magnitude < minImpactVelocity) return;

                if (hand != null && enableHaptics)
                {
                    hand.PlayHapticVibration(0.1f, Mathf.Clamp01(magnitude / 5f));
                }

                if (env == null)
                {
                    env = VoxelPlayEnvironment.instance;
                }

                if (env != null && collision.contactCount > 0)
                {
                    Vector3 contactPoint = collision.contacts[0].point;
                    if (debugTransform != null)
                    {
                        debugTransform.position = contactPoint;
                    }

                    int damage = player != null ? player.bareHandsHitDamage : 3;
                    env.VoxelDamage(contactPoint, damage);
                }
            }
        }

        public bool IsInLayerMask(GameObject obj, LayerMask mask)
        {
            return (mask.value & (1 << obj.layer)) != 0;
        }
    }
}
