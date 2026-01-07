using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay{
    public partial class VoxelPlayEnvironment: MonoBehaviour {


        /// <summary>
        /// Creates a recoverable voxel and `s it at given position, direction and strength
        /// </summary>
        /// <param name="position">Position in world space.</param>
        /// <param name="direction">Direction.</param>
        /// <param name="voxelType">Voxel definition.</param>
        /// <param name="voxelType">Voxel definition.</param>
        public GameObject StackVoxelThrow (Vector3d position, Vector3 direction, float velocity, VoxelDefinition voxelType, Color32 color,float quantity) {
            GameObject voxelGO = CreateRecoverableStackVoxel(position, voxelType, color,quantity);
            if (voxelGO == null)
                return null;
            if (!voxelGO.TryGetComponent(out Rigidbody rb)) {
                return null;
            }
            rb.velocity = direction * velocity;
            return voxelGO;
        }
        
        public GameObject StackItemThrow (Vector3d position, Vector3 direction, float velocity, ItemDefinition itemDefinition, float quantity) {
            GameObject itemGO = CreateRecoverableItem(position, itemDefinition,quantity);
            if (itemGO == null)
                return null;
            if (itemGO.TryGetComponent(out Rigidbody rb)) {
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.velocity = direction * velocity;
            }
            return itemGO;
        }

        public GameObject CreateRecoverableStackVoxel (Vector3d position, VoxelDefinition voxelType, Color32 color, float quantity) {

            // Set item info
            ItemDefinition dropItem = voxelType.dropItem;
            if (dropItem == null) {
                dropItem = GetItemDefinition(ItemCategory.Voxel, voxelType);
                if (dropItem == null)
                    return null;
            }

            int ppeIndex = GetParticleFromPool();
            if (ppeIndex < 0)
                return null;

            // Set collider size
            particlePool[ppeIndex].collider.size = new Vector3(2f, 2f, 2f); // make voxel float on top of other voxels
            particlePool[ppeIndex].endScale = 0;

            // Set rigidbody behaviour
            particlePool[ppeIndex].rigidBody.freezeRotation = true;

            // Set position & scale
            Renderer particleRenderer = particlePool[ppeIndex].renderer;
            Vector3d particlePosition = position + Random.insideUnitSphere * 0.25f;
            particleRenderer.transform.position = particlePosition;
            particleRenderer.transform.localScale = new Vector3(voxelType.dropItemScale, voxelType.dropItemScale, voxelType.dropItemScale);

            float now = Time.time;

            particlePool[ppeIndex].item.itemDefinition = dropItem;
            particlePool[ppeIndex].item.canPickOnApproach = true;
            particlePool[ppeIndex].item.rb = particlePool[ppeIndex].rigidBody;
            particlePool[ppeIndex].item.creationTime = now;
            particlePool[ppeIndex].item.quantity = quantity;

            // Set particle texture
            Material instanceMat = particleRenderer.sharedMaterial;
            switch (dropItem.category) {
                case ItemCategory.Voxel:
                    VoxelDefinition dropVoxelType = dropItem.voxelType;
                    if (dropVoxelType == null) {
                        dropVoxelType = voxelType;
                    }
                    SetParticleMaterialTextures(instanceMat, dropVoxelType, color, false);
                    break;
                default:
                    SetRecoverableVoxelMaterialTextures(instanceMat, dropItem.icon);
                    break;
            }
            instanceMat.mainTextureOffset = Misc.vector2zero;
            instanceMat.mainTextureScale = Misc.vector2one;
            instanceMat.SetInt(ShaderParams.VoxelLight, GetVoxelLightPacked(particlePosition));
            instanceMat.SetFloat(ShaderParams.FlashDelay, 5f);

            // Self-destruct
            particlePool[ppeIndex].creationTime = now;
            particlePool[ppeIndex].destructionTime = now + voxelType.dropItemLifeTime;

            return particlePool[ppeIndex].renderer.gameObject;
        }
    }
}