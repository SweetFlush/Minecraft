using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelPlay {

    public abstract partial class VoxelPlayCharacterControllerBase : MonoBehaviour, IVoxelPlayCharacterController {

        [HideInInspector]
        public bool useThirdPartyController;

        [Header("Start Position")]
        [Tooltip("평평한 세계의 임의 위치에 플레이어를 배치합니다.이 옵션이 활성화되지 않으면 현재 게임오브젝트 변환 위치가 사용됩니다.")]
        public bool startOnFlat = true;

        [Tooltip("평평한 위치를 결정하기 위한 지형 검사 횟수입니다.반복 횟수가 많을수록 결과 시작 위치가 낮아집니다.")]
        [Range(1, 100)]
        public int startOnFlatIterations = 50;

        [Header("State Flags (Informative)")]
        [Tooltip("플레이어가 날고 있습니다. E 및 Q 키를 사용하여 위/아래로 이동할 수 있습니다.")]
        public bool isFlying;

        [Tooltip("플레이어가 추진 중 - X 키를 사용하여 수직 추진을 적용할 수 있습니다.")]
        public bool isThrusting;

        [Tooltip("플레이어가 움직이고 있습니다(걷기 또는 달리기)")]
        public bool isMoving;

        [Tooltip("플레이어가 아무 이동 키나 누르고 있습니다.")]
        public bool isPressingMoveKeys;

        [Tooltip("플레이어가 실행 중입니다.")]
        public bool isRunning;

        [Tooltip("플레이어가 물 표면에 있거나 물 속에 있습니다.")]
        public bool isInWater;

        [Tooltip("플레이어가 물 표면에 있습니다.")]
        public bool isSwimming;

        [Tooltip("플레이어가 수면 아래에 있습니다.")]
        public bool isUnderwater;

        [Tooltip("플레이어가 지상에 있습니다.")]
        public bool isGrounded;

        [Tooltip("플레이어가 웅크리고 있습니다.")]
        public bool isCrouched;

        [Tooltip("플레이어는 지하 또는 잠복 상태입니다.")]
        public bool isUnderground;

        [Tooltip("캐릭터 발 아래의 복셀")]
        public VoxelDefinition voxelUnder;

        [Header("Managed Actions By This Controller")]
        [Tooltip("이 캐릭터 컨트롤러가 공격할 수 있도록 허용합니다(기본값은 왼쪽 마우스 버튼 또는 탭).")]
        public bool manageAttack = true;
        [Tooltip("이 캐릭터 컨트롤러가 웅크릴 수 있도록 허용합니다. (기본값은 C 키입니다.)")]
        public bool manageCrouch = true;
        [Tooltip("이 캐릭터 컨트롤러가 점프할 수 있도록 허용합니다(기본값은 스페이스 키)")]
        public bool manageJump = true;
        [Tooltip("이 캐릭터 컨트롤러를 사용하여 복셀을 구축/배치할 수 있습니다(기본값은 마우스 오른쪽 버튼)")]
        public bool manageBuild = true;
        [Tooltip("이 캐릭터 컨트롤러가 복셀을 배치할 수 있도록 허용합니다(기본값은 마우스 오른쪽 버튼)")]
        public bool managePlaceVoxels = true;
        [Tooltip("이 캐릭터 컨트롤러가 세계를 클릭하여 복셀을 선택할 수 있도록 합니다(기본값은 마우스 가운데 버튼)")]
        public bool manageSelectVoxel = true;
        [Tooltip("이 캐릭터 컨트롤러가 날 수 있도록 허용합니다(기본값은 F 키).")]
        public bool manageFly = true;
        [Tooltip("이 캐릭터 컨트롤러가 수직 추진을 활성화할 수 있도록 허용합니다(기본값은 X 키)")]
        public bool manageThrust;
        [Tooltip("이 캐릭터 컨트롤러가 복셀을 회전할 수 있도록 허용합니다(기본값은 R 키).")]
        public bool manageVoxelRotation;

        [Header("Sounds")]
        [SerializeField] AudioSource m_AudioSource;

        // the sound played when character enters water.
        public AudioClip waterSplash;
        // an array of swim stroke sounds that will be randomly selected from.
        public AudioClip[] swimStrokeSounds;
        public float swimStrokeInterval = 8;

        // an array of footstep sounds that will be randomly selected from.
        public AudioClip[] footstepSounds;

        [Range(0f, 1f)] public float runstepLenghten = 0.7f;
        public float footStepInterval = 5;

        // the sound played when character leaves the ground.
        public AudioClip jumpSound;

        // the sound played when character touches back on ground.
        public AudioClip landSound;

        public AudioClip cancelSound;

        [Header("World Limits")]
        public bool limitBoundsEnabled;
        public Bounds limitBounds;
        [Tooltip("충돌을 감지하고 플레이어를 안전한 위치로 뒤로 또는 위로 이동시킵니다.")]
        public bool unstuck = true;

        [Header("Crosshair")]
        [Tooltip("참고: 현재 VR에서는 십자선이 비활성화되어 있습니다.")]
        public bool enableCrosshair = true;
        [Tooltip("마이크로복셀 모드.0=비활성화됨.값은 도구의 기본 크기를 의미합니다.")]
        public int microVoxels;
        [Tooltip("캐릭터에서 선택까지의 최대 거리")]
        public float microVoxelsProb = 1f;
        public float crosshairMaxDistance = 30f;
        public float crosshairScale = 0.1f;
        public float targetAnimationSpeed = 0.75f;
        public float targetAnimationScale = 0.2f;
        public Color crosshairOnTargetColor = Color.yellow;
        public Color crosshairNormalColor = Color.white;
        public LayerMask crosshairHitLayerMask = -1;
        [Tooltip("십자선은 도달 가능한 복셀에 따라 변경됩니다.")]
        public bool changeOnBlock = true;
        [Tooltip("화면에서 십자선 이동 활성화")]
        public bool freeMode;
        [Tooltip("활성화되면 십자선 색상이 배경 색상에 따라 반전되어 가시성이 향상됩니다.이 기능은 모바일에서 비용이 많이 들 수 있는 GrabPass를 사용합니다.")]
        public bool autoInvertColors = true;
        [Tooltip("모델을 배치할 때 미리보기의 높이를 이동하는 데 사용됩니다.")]
        public float wheelSensibility = 10f;

        [Header("Voxel Highlight")]
        public bool voxelHighlight = true;
        public Color voxelHighlightColor = Color.yellow;
        [Range(1f, 100f)]
        public float voxelHighlightEdge = 20f;

        /// <summary>
        /// 해당 복셀 정의에 TriggerEnterEvent = true가 있는 경우 플레이어가 복셀에 들어갈 때 트리거됩니다.
        /// </summary>
        public event VoxelEvent OnVoxelEnter;

        /// <summary>
        /// 해당 복셀 정의에 TriggerWalkEvent = true가 있는 경우 플레이어가 복셀 위를 걸을 때 트리거됩니다.
        /// </summary>
        public event VoxelEvent OnVoxelWalk;


        // internal fields
        int lastPosX, lastPosY, lastPosZ;
        int lastVoxelTypeIndex;
        float nextPlayerDamageTime;
        float lastDiveTime;
        float m_StepCycle;
        float m_NextStep;
        bool modelBuildPreview;
        ModelDefinition modelBuildItem;
        bool modelBuildInProgress;
        Vector3d modelBuildPreviewPosition;
        int buildRotationDegrees;
        GameObject modelBuildPreviewGO;
        float modelBuildPreviewOffset;


        protected VoxelHitInfo _crosshairHitInfo;
        public virtual VoxelHitInfo crosshairHitInfo => _crosshairHitInfo;

        [NonSerialized]
        public bool crosshairOnBlock;
        Vector3 m_LastNonCollidingCharacterPos;
        protected IVoxelPlayPlayer _player;
        [SerializeField, HideInInspector] protected float _characterHeight = 1.8f;
        protected VoxelPlayInputController input;

        int lastPositionX, lastPositionZ;

        public virtual float GetCharacterHeight () {
            return _characterHeight;
        }


        public IVoxelPlayPlayer player {
            get {
                if (_player == null) {
                    _player = transform.root.GetComponentInChildren<IVoxelPlayPlayer>();
                    if (_player == null) {
                        _player = transform.root.gameObject.AddComponent<VoxelPlayPlayer>();
                    }
                }
                return _player;
            }
        }

        [NonSerialized]
        public VoxelPlayEnvironment env;



        protected void Init () {
            m_AudioSource = GetComponent<AudioSource>();
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;

            env = VoxelPlayEnvironment.instance;
            if (env == null) {
                Debug.LogError("Voxel Play Environment must be added first.");
            } else {
                Debug.Log("env character controller registered : " + gameObject.name);
                env.characterController = this;
            }
            m_LastNonCollidingCharacterPos = Misc.vector3max;

            // Check player can collide with voxels
            if (env != null) {
                env.OnInitialized += () => InitDelayed();
            }
        }

        void InitDelayed () {
            input = env.input;
#if UNITY_EDITOR
            if (Physics.GetIgnoreLayerCollision(gameObject.layer, env.layerVoxels)) {
                Debug.LogError("Player currently can't collide with voxels. Please check physics collision matrix in Project settings or change Voxels Layer in VoxelPlayEnvironment component.");
            }
#endif
            SetupPlayerActiveToolMicroVoxels();
            player.OnItemSelectedChanged += (_, _) => SetupPlayerActiveToolMicroVoxels();
        }


        /// <summary>
        /// 항목이 활성화된 경우 마이크로복셀을 활성화합니다.
        /// </summary>
        bool SetupPlayerActiveToolMicroVoxels () {
            InventoryItem activeItem = player.GetSelectedItem();
            ItemDefinition item = activeItem.item;
            int microVoxels = 0;
            float microVoxelsProb = 1f;
            if (item != null) {
                microVoxels = item.GetPropertyValue<int>("microVoxels");
                microVoxelsProb = item.GetPropertyValue<float>("microVoxelsProb", 1f);
            }
            ToggleMicroVoxels(microVoxels, microVoxelsProb);
            return microVoxels > 0;
        }

        /// <summary>
        /// 현재 캐릭터 및 카메라 변환을 기반으로 내부 회전 변수를 업데이트합니다.
        /// </summary>
        public abstract void UpdateLook ();

        public abstract bool isReady { get; }

        private void OnApplicationFocus (bool focus) {
            if (input != null) {
                input.focused = focus;
            }
        }

        /// <summary>
        /// 마이크로복셀 모드를 켜거나 끕니다.
        /// </summary>
        public virtual void ToggleMicroVoxels () {
            // if microvoxels are already enabled, disable them
            if (microVoxels > 0) {
                ToggleMicroVoxels(0, 0f);
            } else {
                // if microvoxels are disabled, enable them if the active tool allows it
                if (!SetupPlayerActiveToolMicroVoxels()) {
                    // if the active tool doesn't allow microvoxels, enable them with default size and probability
                    ToggleMicroVoxels(1, 1f);
                }
            }
        }

        /// <summary>
        /// 마이크로복셀 모드 설정
        /// </summary>
        /// <param name="state"></param>
        public virtual void ToggleMicroVoxels (int size, float probability) {
            microVoxels = size;
            microVoxelsProb = probability;
        }

        /// <summary>
        /// 캐릭터 라이트를 켜거나 끕니다.
        /// </summary>
        public virtual void ToggleCharacterLight () {
            Light light = GetComponentInChildren<Light>();
            if (light != null) {
                ToggleCharacterLight(!light.enabled);
            }
        }

        /// <summary>
        /// 캐릭터 라이트를 켜거나 끕니다.
        /// </summary>
        public virtual void ToggleCharacterLight (bool state) {
            Light light = GetComponentInChildren<Light>();
            if (light != null && light.enabled != state) {
                light.enabled = state;
                if (light.enabled) {
                    env.ShowMessage("<color=green>Player torch <color=yellow>ON</color></color>");
                } else {
                    env.ShowMessage("<color=green>Player torch <color=yellow>OFF</color></color>");
                }
            }
        }

        protected virtual void CheckFootfalls () {
            if (isGrounded) {
                Vector3 curPos = transform.position;
                int x = (int)curPos.x;
                int y = (int)curPos.y;
                int z = (int)curPos.z;
                if (x != lastPosX || y != lastPosY || z != lastPosZ || voxelUnder == null) {
                    lastPosX = x;
                    lastPosY = y;
                    lastPosZ = z;
                    curPos.y = FastMath.FloorToInt(curPos.y) - 0.5f;
                    VoxelIndex index = env.GetVoxelUnderIndex(curPos, true, ColliderTypes.IgnorePlayer);
                    if (index.typeIndex != lastVoxelTypeIndex || voxelUnder == null) {
                        lastVoxelTypeIndex = index.typeIndex;
                        if (lastVoxelTypeIndex != 0) {
                            voxelUnder = index.type;
                            SetFootstepSounds(voxelUnder.footfalls, voxelUnder.landingSound, voxelUnder.jumpSound);
                            if (voxelUnder.triggerWalkEvent && OnVoxelWalk != null) {
                                OnVoxelWalk(index.chunk, index.voxelIndex);
                            }
                        }
                    }
                }
            }
            if (isGrounded || isInWater) {
                CheckDamage(voxelUnder);
            }
        }

        protected virtual void CheckDamage (VoxelDefinition voxelType) {
            if (voxelType == null)
                return;
            int playerDamage = voxelType.playerDamage;
            if (playerDamage > 0 && Time.time > nextPlayerDamageTime) {
                nextPlayerDamageTime = Time.time + voxelType.playerDamageDelay;
                player.DamageToPlayer(playerDamage);
            }
        }

        protected virtual void CheckEnterTrigger (VoxelChunk chunk, int voxelIndex) {
            if (chunk != null && env.voxelDefinitions[chunk.voxels[voxelIndex].typeIndex].triggerEnterEvent && OnVoxelEnter != null) {
                OnVoxelEnter(chunk, voxelIndex);
            }
        }

        public virtual void SetFootstepSounds (AudioClip[] footStepsSounds, AudioClip jumpSound, AudioClip landSound) {
            this.footstepSounds = footStepsSounds;
            this.jumpSound = jumpSound;
            this.landSound = landSound;
        }

        public virtual void PlayLandingSound () {
            if (isInWater || m_AudioSource == null)
                return;
            m_AudioSource.clip = landSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }



        public virtual void PlayJumpSound () {
            if (isInWater || isFlying || m_AudioSource == null)
                return;
            m_AudioSource.clip = jumpSound;
            m_AudioSource.Play();
        }


        public virtual void PlayCancelSound () {
            if (m_AudioSource == null)
                return;
            m_AudioSource.clip = cancelSound;
            m_AudioSource.Play();
        }


        public virtual void PlayWaterSplashSound () {
            if (Time.time - lastDiveTime < 1f)
                return;
            lastDiveTime = Time.time;
            m_NextStep = m_StepCycle + swimStrokeInterval;
            if (waterSplash != null && m_AudioSource != null) {
                m_AudioSource.clip = waterSplash;
                m_AudioSource.Play();
            }
        }

        /// <summary>
        /// 캐릭터 위치에서 소리를 재생합니다
        /// </summary>
        public virtual void PlayCustomSound (AudioClip sound) {
            if (sound != null && m_AudioSource != null) {
                m_AudioSource.clip = sound;
                m_AudioSource.Play();
            }
        }


        protected virtual void ProgressStepCycle (float velocityMagnitude, float speed) {
            if (velocityMagnitude > 0 && isPressingMoveKeys) {
                m_StepCycle += (velocityMagnitude + (speed * (isMoving ? 1f : runstepLenghten))) * Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep)) {
                return;
            }

            m_NextStep = m_StepCycle + footStepInterval;

            PlayFootStepAudio();
        }



        protected virtual void PlayFootStepAudio () {
            if (!isGrounded || m_AudioSource == null) {
                return;
            }
            if (footstepSounds == null || footstepSounds.Length == 0)
                return;
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n;
            if (footstepSounds.Length == 1) {
                n = 0;
            } else {
                n = Random.Range(1, footstepSounds.Length);
            }
            m_AudioSource.clip = footstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            footstepSounds[n] = footstepSounds[0];
            footstepSounds[0] = m_AudioSource.clip;
        }


        protected virtual void ProgressSwimCycle (Vector3 velocity, float speed) {
            if (velocity.sqrMagnitude > 0 && isPressingMoveKeys) {
                m_StepCycle += (velocity.magnitude + speed) * Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep)) {
                return;
            }

            m_NextStep = m_StepCycle + swimStrokeInterval;

            if (!isUnderwater) {
                PlaySwimStrokeAudio();
            }
        }


        protected virtual void PlaySwimStrokeAudio () {
            if (swimStrokeSounds == null || swimStrokeSounds.Length == 0 || m_AudioSource == null)
                return;
            // pick & play a random swim stroke sound from the array,
            // excluding sound at index 0
            int n;
            if (swimStrokeSounds.Length == 1) {
                n = 0;
            } else {
                n = Random.Range(1, swimStrokeSounds.Length);
            }
            m_AudioSource.clip = swimStrokeSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            swimStrokeSounds[n] = swimStrokeSounds[0];
            swimStrokeSounds[0] = m_AudioSource.clip;
        }


        /// <summary>
        /// 캐릭터 컨트롤러를 새 위치로 이동합니다.변환 위치를 변경하는 대신 이 방법을 사용하십시오.
        /// </summary>
        public abstract void MoveTo (Vector3 newPosition);


        /// <summary>
        /// 캐릭터 컨트롤러를 거리만큼 이동시킵니다.변환 위치를 변경하는 대신 이 방법을 사용하십시오.
        /// </summary>
        public virtual void Move (Vector3 newPosition) { }


        protected virtual void ControllerUpdate () {
            if (input.GetButtonDown(InputButtonNames.Rotate)) {
                buildRotationDegrees = (buildRotationDegrees + 90) % 360;
            }
            ModelBuildPreviewUpdate();

            // Check if character has changed X/Z voxel position
            Vector3 pos = transform.position;
            int posX = (int)pos.x;
            int posZ = (int)pos.z;
            if (posX != lastPositionX || posZ != lastPositionZ) {
                lastPositionZ = posZ;
                lastPositionX = posX;
                CharacterChangedXZPosition(pos);
            }
        }

        protected virtual void CharacterChangedXZPosition (Vector3 newPosition) { }


        protected virtual void MoveBackAfterPlacing (Vector3d camPos, Vector3d placePos, float distance) {
            // Moves back character controller if voxel is put just on its position
            const float minDist = 0.5f;
            float distSqr = Vector3.SqrMagnitude(camPos - placePos);
            if (distSqr < minDist * minDist) {
                MoveTo(transform.position + _crosshairHitInfo.normal * distance);
            }
        }

        /// <summary>
        /// 건물 관련 구현
        /// </summary>
        /// <param name="camPos">카메라 위치 또는 3인칭 컨트롤러의 캐릭터 위치</param>">
        protected virtual void DoBuild (Vector3 camPos, Vector3 forward, Vector3d hintedPlacePos) {
            if (player.selectedItemIndex < 0 || player.selectedItemIndex >= player.items.Count)
                return;

            InventoryItem inventoryItem = player.GetSelectedItem();
            ItemDefinition currentItem = inventoryItem.item;

            if (microVoxels > 0) {
                VoxelDefinition placingVoxelType = null;
                const float placingAmount = 1f / MicroVoxels.COUNT_PER_VOXEL;

                // if we're placing a micro voxel on a voxel position which already has microvoxels, add a microvoxel of that type
                if (env.IsMicroVoxelAtPosition(ref _crosshairHitInfo)) {
                    // player needs the resource in non-build mode
                    placingVoxelType = _crosshairHitInfo.voxel.type;
                    if (!env.buildMode) {
                        inventoryItem = player.GetInventoryItem(placingVoxelType);
                        if (inventoryItem == null || inventoryItem.quantity < placingAmount) {
                            PlayCancelSound();
                            return;
                        }
                        player.ConsumeItem(inventoryItem.item, placingAmount);
                    }
                    int rotation = _crosshairHitInfo.voxel.GetTextureRotation();
                    env.MicroVoxelPlace(ref _crosshairHitInfo, microVoxels, placingVoxelType, probability: microVoxelsProb);
                    MoveBackAfterPlacing(camPos, _crosshairHitInfo.voxelCenter, MicroVoxels.SIZE);
                    return;
                }

                // check if current item can place microvoxels
                if (currentItem != null && inventoryItem.quantity >= placingAmount) {
                    placingVoxelType = currentItem.voxelType;
                }

                // if not, check if the rayhit voxel definition supports micro-voxels, then use it
                if (placingVoxelType == null || !placingVoxelType.supportsMicroVoxels) {
                    placingVoxelType = _crosshairHitInfo.voxel.type;
                }

                // ensure that the selected item is compatible with micro-voxels
                if (placingVoxelType == null || !placingVoxelType.supportsMicroVoxels) {
                    PlayCancelSound();
                    return;
                }

                // place a voxel + microvoxel
                inventoryItem = player.GetInventoryItem(placingVoxelType);
                if (!env.buildMode) {
                    player.ConsumeItem(inventoryItem.item, placingAmount);
                }
                int textureRotation = GetTextureRotationForPlacement(placingVoxelType, forward);
                if (env.MicroVoxelPlace(ref _crosshairHitInfo, microVoxels, placingVoxelType, probability: microVoxelsProb, placingVoxelType.tintColor, rotation: textureRotation)) {
                    MoveBackAfterPlacing(camPos, _crosshairHitInfo.voxelCenter, MicroVoxels.SIZE);
                }
                return;
            }

            switch (currentItem.category) {
                case ItemCategory.Voxel:

                    // Basic placement rules
                    bool canPlace = crosshairOnBlock;
                    Voxel existingVoxel = _crosshairHitInfo.voxel;
                    VoxelDefinition existingVoxelType = existingVoxel.type;
                    Vector3d placePos;

                    VoxelDefinition placeVoxelType = currentItem.voxelType;
                    if (placeVoxelType == null) return;

                    if (currentItem.voxelType.renderType == RenderType.Water && !canPlace) {
                        canPlace = true; // water can be poured anywhere
                        placePos = camPos + forward * 3f;
                    } else {
                        placePos = env.GetVoxelPosition(_crosshairHitInfo.voxelCenter);
                        if (existingVoxelType.renderType != RenderType.CutoutCross) {
                            placePos += _crosshairHitInfo.normal;
                        }
                        if (canPlace && _crosshairHitInfo.normal.y > 0.9f) {
                            // Make sure there's a valid voxel under position (ie. do not build a voxel on top of grass)
                            canPlace = existingVoxelType != null && existingVoxelType.renderType != RenderType.CutoutCross && (existingVoxelType.renderType != RenderType.Water || currentItem.voxelType.renderType == RenderType.Water);
                        }
                    }

                    VoxelDefinition existingVoxelOnPlacePos = env.GetVoxel(placePos).type;
                    float distanceFromCenter = (float)(_crosshairHitInfo.point.y - env.GetVoxelPosition(_crosshairHitInfo.voxelCenter).y);
                    bool isOverCenter = false;

                    if (placeVoxelType.allowUpsideDownVoxel && placeVoxelType.upsideDownVoxel != null) {
                        isOverCenter = distanceFromCenter > 0 ? true : false;

                        isOverCenter = _crosshairHitInfo.normal.y > 0.9f ? false : isOverCenter;
                        isOverCenter = _crosshairHitInfo.normal.y < -0.9f ? true : isOverCenter;

                        if (isOverCenter) {
                            placeVoxelType = placeVoxelType.upsideDownVoxel;
                        }
                    }

                    bool isPromoting = false;
                    // Check voxel promotion
                    if (canPlace) {
                        if (existingVoxelType == placeVoxelType || existingVoxelType == placeVoxelType.upsideDownVoxel) {
                            if (existingVoxelType.promotesTo != null) {
                                if (existingVoxelType.isUpsideDown) {
                                    if (_crosshairHitInfo.normal.y < -0.9f) {
                                        placePos = env.GetVoxelPosition(_crosshairHitInfo.voxelCenter);
                                        placeVoxelType = existingVoxelType.promotesTo;
                                        isPromoting = true;
                                    }
                                } else {
                                    if (_crosshairHitInfo.normal.y > 0.9f) {
                                        placePos = env.GetVoxelPosition(_crosshairHitInfo.voxelCenter);
                                        placeVoxelType = existingVoxelType.promotesTo;
                                        isPromoting = true;
                                    }
                                }
                            }
                        }
                        if ((existingVoxelOnPlacePos == placeVoxelType || existingVoxelOnPlacePos == placeVoxelType.upsideDownVoxel) && !isPromoting) {
                            if (existingVoxelOnPlacePos.promotesTo != null) {
                                if (existingVoxelOnPlacePos.isUpsideDown) {
                                    if (distanceFromCenter < 0f || _crosshairHitInfo.normal.y > 0.9f) {
                                        placeVoxelType = existingVoxelOnPlacePos.promotesTo;
                                        isPromoting = true;
                                    }
                                } else {
                                    if (distanceFromCenter > 0f || _crosshairHitInfo.normal.y < -0.9f) {
                                        placeVoxelType = existingVoxelOnPlacePos.promotesTo;
                                        isPromoting = true;
                                    }
                                }
                            }
                        }
                    }

                    // Compute rotation
                    int textureRotation = 0;
                    if (placeVoxelType.placeOnWall) {
                        if (!existingVoxelType.supportsDecorations) {
                            canPlace = false;
                        } else if (_crosshairHitInfo.normal.z > 0) {
                            textureRotation = 2;
                        } else if (_crosshairHitInfo.normal.z < 0) {
                            textureRotation = 0;
                        } else if (_crosshairHitInfo.normal.x > 0) {
                            textureRotation = 3;
                        } else if (_crosshairHitInfo.normal.x < 0) {
                            textureRotation = 1;
                        } else {
                            canPlace = false;
                        }
                        if (canPlace) {
                            if (existingVoxelOnPlacePos.hasContent) {
                                env.VoxelDestroy(placePos);
                            }
                        } else {
                            PlayCancelSound();
                        }
                    } else {
                        textureRotation = GetTextureRotationForPlacement(placeVoxelType, forward);
                    }

                    // Final check, does it overlap existing geometry?
                    if (canPlace && !isPromoting) {
                        Quaternion rotationQ = Quaternion.Euler(0, Voxel.GetTextureRotationDegrees(textureRotation), 0);
                        canPlace = !env.VoxelOverlaps(placePos, placeVoxelType, rotationQ, 1 << env.layerVoxels);
                        if (!canPlace) {
                            PlayCancelSound();
                        }
                    }

#if UNITY_EDITOR
                    else if (env.constructorMode) {
                        placePos = hintedPlacePos;
                        placeVoxelType = currentItem.voxelType;
                        canPlace = true;
                    }
#endif

                    // Finally place the voxel
                    if (canPlace) {
                        // Consume item first
                        if (!env.buildMode) {
                            player.ConsumeItem();
                        }
                        // Place it
                        float amount = inventoryItem.quantity < 1f ? inventoryItem.quantity : 1f;
                        if (env.VoxelPlace(placePos, placeVoxelType, playSound: true, player.selectedItemTintColor, amount, textureRotation)) {
                            MoveBackAfterPlacing(camPos, placePos, 1f);
                        }
                    }
                    break;
                case ItemCategory.Torch:
                    if (crosshairOnBlock) {
                        GameObject torchAttached = env.TorchAttach(_crosshairHitInfo, currentItem);
                        if (!env.buildMode && torchAttached != null) {
                            player.ConsumeItem();
                        }
                    }
                    break;
                case ItemCategory.Model:
                    if (!modelBuildInProgress) {
                        if (modelBuildPreview) {
                            ModelPreviewCancel();
                            // check if building position is in frustum, otherwise cancel building
                            Vector3 viewportPos = env.cameraMain.WorldToViewportPoint(modelBuildPreviewPosition);
                            if (viewportPos.x < 0 || viewportPos.x > 1f || viewportPos.y < 0 || viewportPos.y > 1f || viewportPos.z < 0) {
                                return;
                            }
                            if (currentItem.model.buildDuration > 0) {
                                modelBuildInProgress = true;
                                env.ModelPlace(modelBuildPreviewPosition, currentItem.model, currentItem.model.buildDuration, buildRotationDegrees, 1f, fitTerrain: currentItem.model.fitToTerrain, FinishBuilding);
                            } else {
                                env.ModelPlace(modelBuildPreviewPosition, currentItem.model, buildRotationDegrees, colorBrightness: 1f, fitTerrain: currentItem.model.fitToTerrain);
                            }
                            player.ConsumeItem();
                        } else {
                            modelBuildPreview = true;
                            modelBuildItem = currentItem.model;
                            ModelBuildPreviewUpdate();
                        }
                    }
                    break;
                case ItemCategory.General:
                    ThrowCurrentItem(camPos, forward);
                    break;
            }
        }

        int GetTextureRotationForPlacement (VoxelDefinition vd, Vector3 forward) {
            if (!vd.placeFacingPlayer) return 0;

            // Orient voxel to player
            if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z)) {
                if (forward.x > 0) {
                    return 1;
                } else {
                    return 3;
                }
            } else if (forward.z < 0) {
                return 2;
            }
            return 0;
        }

        protected virtual void ModelBuildPreviewUpdate () {
            if (!modelBuildPreview || !crosshairOnBlock || modelBuildItem == null)
                return;

            modelBuildPreviewPosition = _crosshairHitInfo.voxelCenter;

            modelBuildPreviewOffset += Input.GetAxis("Mouse ScrollWheel") * wheelSensibility;
            if (modelBuildPreviewOffset < 0) modelBuildPreviewOffset = 0;

            modelBuildPreviewPosition.y += (int)modelBuildPreviewOffset;

            modelBuildPreviewGO = env.ModelHighlight(modelBuildItem, modelBuildPreviewPosition, buildRotationDegrees);
        }


        /// <summary>
        /// 플레이어 인벤토리의 현재 항목에서 유닛을 제거하고 장면에 던집니다.
        /// </summary>
        public virtual void ThrowCurrentItem (Vector3 throwPosition, Vector3 direction) {
            InventoryItem inventoryItem = player.ConsumeItem();
            if (inventoryItem == InventoryItem.Null)
                return;

            if (inventoryItem.item.category == ItemCategory.Voxel) {
                env.VoxelThrow(throwPosition, direction, 15f, inventoryItem.item.voxelType, Misc.color32White);
            } else if (inventoryItem.item.category == ItemCategory.General) {
                env.ItemThrow(throwPosition, direction, 15f, inventoryItem.item);
            }
        }


        protected virtual void FinishBuilding (ModelDefinition modelDefinition, Vector3d position) {
            modelBuildInProgress = false;
        }

        public virtual bool ModelPreviewCancel () {
            if (modelBuildPreview) {
                modelBuildPreview = false;
                if (modelBuildPreviewGO != null) {
                    modelBuildPreviewGO.SetActive(false);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 플레이어가 지형 위에 있는지 확인합니다.
        /// </summary>
        public virtual void Unstuck (bool toSurface = true) {
#if UNITY_EDITOR
            if (env.constructorMode) return;
#endif

            if (!unstuck) return;

            Vector3 transformPosition = transform.position;
            if (env.CheckCollision(env.cameraMain.transform.position) || env.CheckCollision(transformPosition)) {
                // try to move to last good position
                if (m_LastNonCollidingCharacterPos.y < float.MaxValue && !env.CheckCollision(m_LastNonCollidingCharacterPos)) {
                    MoveTo(m_LastNonCollidingCharacterPos);
                    return;
                }
                // try up or surface
                float minAltitude = Mathf.FloorToInt(transformPosition.y) + 1.1f;
                if (toSurface) {
                    minAltitude = Mathf.Max(env.GetTerrainHeight(transformPosition), minAltitude);
                }
                Vector3 pos = new Vector3(transformPosition.x, minAltitude + GetCharacterHeight() * 0.5f, transformPosition.z);
                MoveTo(pos);
            }
        }

        public virtual void AnnotateNonCollidingPosition (Vector3 position) {
            m_LastNonCollidingCharacterPos = position;
        }
    }
}
