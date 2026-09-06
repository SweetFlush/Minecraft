using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;
using Autohand;

namespace VR_Minecraft
{
    public class ToolHead : MonoBehaviour
    {
        public LayerMask voxelEnvMask = -1;
        public int damage = 5;
        public float minImpactVelocity = 0.5f;
        public bool enableHaptics = true;

        private VoxelPlayEnvironment env;
        private Grabbable grabbable;

        private void Start()
        {
            grabbable = GetComponentInParent<Grabbable>();
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (IsInLayerMask(collision.gameObject, voxelEnvMask))
            {
                var magnitude = collision.relativeVelocity.magnitude;
                if (magnitude < minImpactVelocity) return;

                // 쥐고 있는 손에 햅틱 피드백 전달
                if (enableHaptics && grabbable != null)
                {
                    var hands = grabbable.GetHeldBy();
                    if (hands != null)
                    {
                        for (int i = 0; i < hands.Count; i++)
                        {
                            if (hands[i] != null)
                            {
                                hands[i].PlayHapticVibration(0.12f, Mathf.Clamp01(magnitude / 5f));
                            }
                        }
                    }
                }

                if (env == null)
                {
                    env = VoxelPlayEnvironment.instance;
                }

                if (env != null && collision.contactCount > 0)
                {
                    Vector3 contactPoint = collision.contacts[0].point;
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
