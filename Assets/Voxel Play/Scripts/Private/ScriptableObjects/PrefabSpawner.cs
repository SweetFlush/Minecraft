using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VoxelPlay {

    [CreateAssetMenu(menuName = "Voxel Play/Detail Generators/Prefab Spawner", fileName = "PrefabSpawner", order = 103)]
    public class PrefabSpawner : VoxelPlayDetailGenerator {

        public float seed = 1;

        [Range(0, 1f)]
        public float spawnProbability = 0.02f;
        [Tooltip("배치 위치에 오프셋을 추가합니다.배치 위치는 지형 표면 바로 위에 있습니다.예를 들어 이 오프셋은 지형 위에 프리팹을 배치할 수 있습니다.")]
        public Vector3 spawnPositionOffset;
        public BiomeDefinition[] allowedBiomes;
        [Tooltip("물 위에 프리팹을 배치할 수 있도록 활성화합니다.")]
        public bool allowSpawnOnWater;
        [Tooltip("프리팹을 생성하기 전에 청크 충돌체가 나타날 때까지 기다리도록 활성화합니다.")]
        public bool requireCollider;
        [Tooltip("생성된 프리팹이 NavMesh Agent를 사용하는 경우 활성화합니다.이는 프리팹을 인스턴스화하기 전에 청크 navmesh를 사용할 수 있을 때까지 기다립니다.")]
        public bool requireNavMesh;
        public GameObject[] prefabs;

        public bool optimizeMaterial = true;

        VoxelPlayEnvironment env;
        Shader vpShader;

        /// <summary>
        /// 초기화 방법.시작 시 Voxel Play에 의해 호출됩니다.
        /// </summary>
        public override void Init() {
            vpShader = Shader.Find("Voxel Play/Models/Texture/Opaque");
            env = VoxelPlayEnvironment.instance;
            if (requireCollider && !env.enableColliders) {
                Debug.LogWarning($"PrefabSpawner {name} requires colliders but Voxel Play Environment collider option is disabled.");
            }
            if (requireNavMesh && !env.enableNavMesh) {
                Debug.LogWarning($"PrefabSpawner {name} requires NavMesh but Voxel Play Environment NavMesh option is disabled.");
            }
        }


        /// <summary>
        /// 주어진 청크를 세부사항으로 채웁니다.채워진 복셀은 지형 생성기로 대체되지 않습니다.
        /// Voxel.Empty를 사용하여 공백을 채웁니다.
        /// </summary>
        /// <param name="chunk">큰 덩어리.</param>
        public override void AddDetail(VoxelChunk chunk) {

            if (prefabs == null || prefabs.Length == 0) return;
            Vector3d position = chunk.position;
            Vector3d rndPos = position;
            rndPos.x += seed;
            if (WorldRand.GetValue(rndPos) > spawnProbability) return;

            BiomeDefinition biome = env.GetBiome(position);
            if (allowedBiomes != null) {
                for (int k = 0; k < allowedBiomes.Length; k++) {
                    if (allowedBiomes[k] == biome) {
                        Vector3 spawnPosition = GetSpawnPosition(position);
                        if (!allowSpawnOnWater && env.IsWaterAtPosition(spawnPosition)) return;
                        if (requireCollider || requireNavMesh) {
                            SpawnPrefabAsync(spawnPosition);
                        } else {
                            SpawnPrefab(spawnPosition);
                        }
                        return;
                    }
                }
            }
        }

        Vector3 GetSpawnPosition(Vector3 position) {
            position.x += WorldRand.Range(0, VoxelPlayEnvironment.CHUNK_SIZE) - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.z += WorldRand.Range(0, VoxelPlayEnvironment.CHUNK_SIZE) - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.y = env.GetTerrainHeight(position);
            position += spawnPositionOffset;
            return position;
        }

        async void SpawnPrefabAsync(Vector3 position) {
            VoxelChunk chunk = null;
            bool canSpawn = true;
            if (requireCollider) {
                canSpawn = false;
                for (int k = 0; k < 10; k++) {
                    if (env.IsTerrainReadyAtPosition(position, false)) {
                        canSpawn = true;
                        break;
                    }
                    env.GetChunk(position, out chunk, true);
                    env.ChunkRedraw(chunk, refreshLightmap: false, refreshMesh: false, ignoreFrustum: true);
                    await Task.Delay(TimeSpan.FromSeconds(0.5f));
                    if (!env.initialized) return;
                }
            }
            if (requireNavMesh && canSpawn) {
                canSpawn = false;
                for (int k = 0; k < 20; k++) {
                    if (env.ChunkHasNavMeshReady(chunk)) {
                        if (NavMesh.SamplePosition(position, out NavMeshHit navMeshHit, 2f, NavMesh.AllAreas)) {
                            position = navMeshHit.position;
                            canSpawn = true;
                            break;
                        }
                    }
                    env.GetChunk(position, out chunk, true);
                    env.ChunkRedraw(chunk, refreshLightmap: false, refreshMesh: false, ignoreFrustum: true);
                    await Task.Delay(TimeSpan.FromSeconds(0.5f));
                    if (!env.initialized) return;
                }
            }
            if (canSpawn) {
                SpawnPrefab(position);
            }
        }

        void SpawnPrefab(Vector3d position) {

            int prefabIndex = WorldRand.Range(0, prefabs.Length);
            GameObject prefab = prefabs[prefabIndex];
            NavMeshAgent agent = null;
            bool isAgentEnabled = false;
            if (requireNavMesh) {
                agent = prefab.GetComponentInChildren<NavMeshAgent>();
                if (agent != null) {
                    isAgentEnabled = agent.enabled;
                }
                if (isAgentEnabled) {
                    agent.enabled = false;
                }
            }
            GameObject o = Instantiate(prefab);

            if (optimizeMaterial) {
                Renderer r = o.GetComponentInChildren<Renderer>();
                if (r != null) {
                    Material oldMat = r.sharedMaterial;
                    if (oldMat != null && !oldMat.shader.name.Contains("Voxel Play/Models")) {
                        if (vpShader != null) {
                            Material newMat = new Material(vpShader);
                            newMat.mainTexture = oldMat.mainTexture;
                            newMat.color = oldMat.color;
                            r.sharedMaterial = newMat;
                        }
                    }
                }
            }
            o.transform.position = position;

            if (agent != null) {
                if (isAgentEnabled) {
                    agent.enabled = true;
                    NavMeshAgent spawnedAgent = o.GetComponentInChildren<NavMeshAgent>();
                    if (spawnedAgent != null) {
                        spawnedAgent.enabled = true;
                    }
                }
            }

            VoxelPlayBehaviour bh = o.GetComponentInChildren<VoxelPlayBehaviour>();
            if (bh == null) {
                o.AddComponent<VoxelPlayBehaviour>();
            }

        }

    }

}
