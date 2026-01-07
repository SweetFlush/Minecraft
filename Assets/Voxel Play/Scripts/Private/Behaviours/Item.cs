using System;
using UnityEngine;

namespace VoxelPlay
{

    /// <summary>
    /// 이 동작은 플레이어가 복구하거나 손상시킬 수 있는 장면의 모든 개체에 연결되어야 합니다.
    /// </summary>
    public class Item : MonoBehaviour
    {

        /// <summary>
        /// 이 객체가 나타내는 항목 유형입니다.
        /// </summary>
        public ItemDefinition itemDefinition;

        public float quantity = 1f;

        public bool autoRotate = true;

        /// <summary>
        /// true로 설정하면 이 항목이 청크 항목 목록에 추가됩니다.항목이 이동하면(즉, 아래로 떨어지면) 자동으로 청크가 전환됩니다.
        /// </summary>
        public bool persistentItem;

        /// <summary>
        /// true인 경우 개체를 집어들 때 장면에서 개체가 파괴될 수 있습니다.
        /// </summary>
        public bool canBeDestroyed;

        /// <summary>
        /// 이 항목에 남은 저항 포인트입니다.현장에서 손상될 수 있는 항목에 사용됩니다(복셀 아님).
        /// </summary>
        [NonSerialized]
        public byte resistancePointsLeft;

        /// <summary>
        /// 이 객체가 선택할 수 있는 항목을 나타내는 경우
        /// </summary>
        public bool canPickOnApproach;

        [NonSerialized]
        public float creationTime;

        const float PICK_UP_START_DISTANCE_SQR = 6.5f;
        const float PICK_UP_END_DISTANCE_SQR = 0.81f;
        const float ROTATION_SPEED = 40f;

        [NonSerialized, HideInInspector]
        public Rigidbody rb;

        [NonSerialized]
        public bool pickingUp;

        [NonSerialized]
        public IVoxelPlayPlayer pickingPlayer;

        VoxelPlayEnvironment env;
        Material mat;
        Vector3d lastPosition;

        /// <summary>
        /// 이 항목과 관련된 복셀을 저장합니다.
        /// </summary>
        public VoxelChunk itemChunk;
        public int itemVoxelIndex = -1;

        void Start ()
        {
            if (rb == null) {
                rb = GetComponent<Rigidbody> ();
            }
            env = VoxelPlayEnvironment.instance;
            if (persistentItem) {
                // Clone material to support voxel lighting
                Renderer renderer = GetComponent<Renderer> ();
                if (renderer != null) {
                    mat = renderer.sharedMaterial;
                    if (mat != null) {
                        mat = Instantiate(mat);
                        renderer.sharedMaterial = mat;
                    }
                }
                ManageItem ();
            }
        }

        void Update ()
        {
            if (this == null) return;

            if (transform.position.y < -11000) {
                // safety check in case the object drops to infinite
                Destroy (gameObject);
                return;
            }

            if (!canPickOnApproach || itemDefinition == null) {
                if (!pickingUp) {
                    return;
                }
            }

            if (autoRotate && rb != null) {
                rb.rotation = Quaternion.Euler (Misc.vector3up * ((Time.time * ROTATION_SPEED) % 360));
            }

            if (persistentItem) {
                ManageItem ();
            }

            if (!pickingUp) {
                if (Time.frameCount % 10 != 0)
                    return;
            }

            // Check if player is near
            Vector3 playerPosition = pickingPlayer != null ? pickingPlayer.GetTransform().position : env.currentAnchorPosWS;
            Vector3 pos = transform.position;

            float dx = playerPosition.x - pos.x;
            float dy = playerPosition.y - pos.y;
            float dz = playerPosition.z - pos.z;

            if (pickingUp) {
                pos.x += dx * 0.25f;
                pos.y += dy * 0.25f;
                pos.z += dz * 0.25f;
                transform.position = pos;
            }

            if (Time.time - creationTime > 1f) {
                float dist = dx * dx + dy * dy + dz * dz;
                if (dist < PICK_UP_END_DISTANCE_SQR) {
                    IVoxelPlayPlayer player = pickingPlayer;
                    if (player == null) {
                        player = VoxelPlayPlayer.instance;
                    }
                    if (player != null) {
                        player.PickUpItem (itemDefinition, quantity);
                    }
                    VoxelPlayUI ui = VoxelPlayUI.instance;
                    if (ui != null) {
                        ui.RefreshInventoryContents ();
                    }
                    if (persistentItem || canBeDestroyed) {
                        Destroy (gameObject);
                    } else {
                        gameObject.SetActive (false);
                    }
                } else if (dist < PICK_UP_START_DISTANCE_SQR) {
                    pickingUp = true;
                }
            }
        }

        public void PickItem (IVoxelPlayPlayer pickingPlayer)
        {
            if (itemDefinition == null) return;
            if (!itemDefinition.canBePicked) return;
            this.pickingPlayer = pickingPlayer;
            pickingUp = true;
        }


        void ManageItem ()
        {
            if (env == null)
                return;

            Vector3d currentPosition = transform.position;

            if (itemChunk != null && itemChunk.isRendered) {
                if (currentPosition == lastPosition) {
                    return;
                }
                lastPosition = currentPosition;
            }

            // Update lighting
            if (mat != null) {
                float light = env.GetVoxelLightPacked (currentPosition);
                mat.SetFloat (ShaderParams.VoxelLight, light);
            }

            // Update owner chunk
            if (!env.GetVoxelIndex (currentPosition, out VoxelChunk currentChunk, out int currentVoxelIndex, true))
                return;
            if (currentVoxelIndex != itemVoxelIndex || currentChunk != itemChunk) {
                if (itemChunk != null) {
                    itemChunk.RemoveItem (this);
                    env.RegisterChunkChanges (itemChunk);
                }
                currentChunk.AddItem (this);
                env.RegisterChunkChanges (currentChunk);
                itemChunk = currentChunk;
                itemVoxelIndex = currentVoxelIndex;
            }

            if (currentChunk.isRendered && rb != null && !rb.useGravity) {
                rb.useGravity = true;
            }
        }

        void OnDestroy ()
        {
            if (itemChunk != null) {
                itemChunk.RemoveItem (this);
            }
        }


    }

}