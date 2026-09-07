using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;
using VoxelPlay;

namespace VR_Minecraft
{
    [RequireComponent(typeof(AudioSource))]
    public class VR_CharacterController : VoxelPlayCharacterControllerBase
    {
        public static VR_CharacterController Instance
        {
            get
            {
                if (VoxelPlayEnvironment.instance != null)
                {
                    return VoxelPlayEnvironment.instance.characterController as VR_CharacterController;
                }
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                return FindFirstObjectByType<VR_CharacterController>();
#else
                return FindObjectOfType<VR_CharacterController>();
#endif
            }
        }

        [Header("AutoHand Integration")]
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

        public override bool isReady => _isReady;
        private bool _isReady = false;

        [Header("Hand Settings")]
        [Tooltip("오른손을 메인 인터랙션/크로스헤어 손으로 사용할지 여부 (false면 왼손)")]
        public bool isRightHandMain = true;

        [Tooltip("오른손 레이캐스트 기준점 (미지정 시 handRight 사용)")]
        public Transform rayOriginRight;

        [Tooltip("왼손 레이캐스트 기준점 (미지정 시 handLeft 사용)")]
        public Transform rayOriginLeft;

        [Header("VR Crosshair 3D")]
        [SerializeField] private VR_Crosshair crosshair3D;
        [SerializeField] private GameObject crosshairPrefab;

        [Header("Voxel Step Climb (1-Block Step)")]
        [Tooltip("1m(1블록) 높이의 복셀을 점프 없이 자연스럽게 걸어 올라갈 수 있도록 설정")]
        public bool enableVoxelStepClimb = true;
        public float voxelStepHeight = 1.05f;

        [Header("Fall & Landing Damage")]
        [Tooltip("낙하 데미지를 입기 시작하는 최소 낙하 높이(미터)")]
        public float minFallDamageHeight = 4.0f;
        public float fallDamageMultiplier = 2.0f;

        [Header("Water & Swimming Settings")]
        [Tooltip("물속에서의 리지드바디 저항")]
        public float waterLinearDrag = 3.0f;
        [Tooltip("물속에서의 자연 부력 상승 가속도")]
        public float waterBuoyancy = 2.5f;
        [Tooltip("수영 시 이동 속도 배율")]
        public float waterMoveSpeedMultiplier = 0.65f;
        [Tooltip("최대 산소량 (초)")]
        public float maxOxygen = 20.0f;
        [Tooltip("수면 위에서 초당 산소 회복량")]
        public float oxygenRecoveryRate = 6.0f;
        [Tooltip("물속 질식 데미지 간격 (초)")]
        public float suffocationInterval = 1.0f;
        [Tooltip("질식 시 틱당 데미지")]
        public int suffocationDamage = 1;

        public float currentOxygen { get; private set; }

        // 히트 정보
        protected VoxelHitInfo _leftHandCrosshairHitInfo;
        public VoxelHitInfo LeftHandCrosshairHitInfo => _leftHandCrosshairHitInfo;

        protected VoxelHitInfo _rightHandCrosshairHitInfo;
        public VoxelHitInfo RightHandCrosshairHitInfo => _rightHandCrosshairHitInfo;

        /// <summary>
        /// 현재 메인 손의 크로스헤어 히트 정보 반환
        /// </summary>
        public override VoxelHitInfo crosshairHitInfo
        {
            get
            {
                return isRightHandMain ? _rightHandCrosshairHitInfo : _leftHandCrosshairHitInfo;
            }
        }

        /// <summary>
        /// 서브 손의 크로스헤어 히트 정보 반환
        /// </summary>
        public VoxelHitInfo SubHandCrosshairInfo
        {
            get
            {
                return !isRightHandMain ? _rightHandCrosshairHitInfo : _leftHandCrosshairHitInfo;
            }
        }

        // 내부 추적 변수
        private bool lastGroundedState = false;
        private float fallStartAltitude = 0f;
        private bool wasInWater = false;
        private bool wasUnderwater = false;
        private float originalAutoHandMoveSpeed = 2.3f;
        private float originalAutoHandStepHeight = 0.3f;
        private float nextSuffocationTime = 0f;
        private float lastStepSoundTime = 0f;
        private Vector3 lastGroundedFootPos;

        #region Unity LifeCycle

        protected virtual void Awake()
        {
            currentOxygen = maxOxygen;
        }

        protected virtual void Start()
        {
            Init();

            if (AutoHandPlayer != null)
            {
                originalAutoHandMoveSpeed = AutoHandPlayer.maxMoveSpeed;
                originalAutoHandStepHeight = AutoHandPlayer.maxStepHeight;

                if (enableVoxelStepClimb)
                {
                    AutoHandPlayer.maxStepHeight = Mathf.Max(AutoHandPlayer.maxStepHeight, voxelStepHeight);
                    AutoHandPlayer.useSmoothStep = true;
                }

                EnsureHandFists();
            }

            InitCrosshair3D();

            if (env != null && env.initialized)
            {
                LateInit();
            }
            else if (env != null)
            {
                env.OnInitialized += () => LateInit();
            }
        }

        /// <summary>
        /// 손 모델이 교체되어도 맨손 타격 및 햅틱이 작동하도록 VR_VoxelFist 자동 확보 및 컨트롤러 추적 활성화 보장
        /// </summary>
        private void EnsureHandFists()
        {
            if (AutoHandPlayer == null) return;

            SetupHand(AutoHandPlayer.handRight);
            SetupHand(AutoHandPlayer.handLeft);

            void SetupHand(Hand hand)
            {
                if (hand == null) return;

                // 물리 이동이 비활성화되어 컨트롤러를 따라가지 못하는 현상 방지
                if (!hand.enableMovement)
                {
                    hand.enableMovement = true;
                }

                var fist = hand.GetComponent<VR_VoxelFist>();
                if (fist == null)
                {
                    fist = hand.gameObject.AddComponent<VR_VoxelFist>();
                }
                fist.hand = hand;
                if (fist.player == null && player is VR_VoxelPlayer vrPlayer)
                {
                    fist.player = vrPlayer;
                }
            }
        }

        protected virtual void LateInit()
        {
            _isReady = true;
            WaitForCurrentChunk();
        }

        protected virtual void Update()
        {
            if (env == null || !env.initialized || AutoHandPlayer == null) return;

            UpdateLocomotionAndGrounded();
            UpdateWaterState();
            UpdateOxygenState();
        }

        protected virtual void LateUpdate()
        {
            if (env == null || !env.initialized || AutoHandPlayer == null) return;

            UpdateHandCrosshair(AutoHandPlayer.handRight, rayOriginRight, ref _rightHandCrosshairHitInfo, isRightHandMain);
            UpdateHandCrosshair(AutoHandPlayer.handLeft, rayOriginLeft, ref _leftHandCrosshairHitInfo, !isRightHandMain);
        }

        #endregion

        #region Locomotion & Physics Integration

        public override void MoveTo(Vector3 newPosition)
        {
            if (AutoHandPlayer != null)
            {
                AutoHandPlayer.SetPosition(newPosition);
            }
            else
            {
                transform.position = newPosition;
            }
        }

        public override void Move(Vector3 deltaPosition)
        {
            if (AutoHandPlayer != null)
            {
                AutoHandPlayer.SetPosition(AutoHandPlayer.transform.position + deltaPosition);
            }
            else
            {
                transform.position += deltaPosition;
            }
        }

        public override void UpdateLook()
        {
            // VR HMD의 헤드 트래킹을 사용하므로 수동 Look 업데이트는 생략
        }

        /// <summary>
        /// 플레이어 지상 접지, 점프, 착지 및 발소리/낙하 데미지 갱신
        /// </summary>
        protected virtual void UpdateLocomotionAndGrounded()
        {
            if (AutoHandPlayer == null) return;

            bool currentGrounded = AutoHandPlayer.IsGrounded();
            this.isGrounded = currentGrounded;
            this.isMoving = AutoHandPlayer.body != null && AutoHandPlayer.body.linearVelocity.sqrMagnitude > 0.05f;

            Vector3 playerPos = AutoHandPlayer.transform.position;

            // 1. 착지 감지 (공중 -> 지상)
            if (!lastGroundedState && currentGrounded)
            {
                PlayLandingSound();

                // 낙하 데미지 계산
                float fallDistance = fallStartAltitude - playerPos.y;
                if (fallDistance > minFallDamageHeight && !isInWater)
                {
                    int damage = Mathf.RoundToInt((fallDistance - minFallDamageHeight) * fallDamageMultiplier);
                    if (damage > 0 && player != null)
                    {
                        player.DamageToPlayer(damage);
                    }
                }
                fallStartAltitude = playerPos.y;
            }
            // 2. 점프/낙하 시작 감지 (지상 -> 공중)
            else if (lastGroundedState && !currentGrounded)
            {
                fallStartAltitude = playerPos.y;

                if (AutoHandPlayer.body != null && AutoHandPlayer.body.linearVelocity.y > 0.5f && !isInWater)
                {
                    PlayJumpSound();
                }
            }

            lastGroundedState = currentGrounded;

            // 3. 지상 이동 중 발소리 및 바닥 복셀 체크
            if (currentGrounded)
            {
                CheckFootfalls();

                if (isMoving && Time.time - lastStepSoundTime > (1.0f / (AutoHandPlayer.maxMoveSpeed * 1.5f)))
                {
                    float movedDist = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(lastGroundedFootPos.x, 0, lastGroundedFootPos.z));
                    if (movedDist > 0.4f)
                    {
                        PlayFootStepAudio();
                        lastGroundedFootPos = playerPos;
                        lastStepSoundTime = Time.time;
                    }
                }
            }
        }

        /// <summary>
        /// 캐릭터 발 아래의 복셀을 질의하여 사운드 및 위험 복셀 데미지 처리
        /// </summary>
        protected override void CheckFootfalls()
        {
            if (AutoHandPlayer == null || env == null) return;

            Vector3 footPos = AutoHandPlayer.transform.position;
            footPos.y = FastMath.FloorToInt(footPos.y) - 0.5f;

            VoxelIndex index = env.GetVoxelUnderIndex(footPos, true, ColliderTypes.IgnorePlayer);
            if (index.typeIndex != 0)
            {
                voxelUnder = index.type;
                if (voxelUnder != null)
                {
                    SetFootstepSounds(voxelUnder.footfalls, voxelUnder.landingSound, voxelUnder.jumpSound);
                }
            }

            if (isGrounded || isInWater)
            {
                CheckDamage(voxelUnder);
            }
        }

        #endregion

        #region Water & Swimming System

        /// <summary>
        /// 복셀 물 환경 감지 및 수영/부력/언더워터 상태 동기화
        /// </summary>
        protected virtual void UpdateWaterState()
        {
            if (AutoHandPlayer == null || env == null) return;

            Vector3 bodyPos = AutoHandPlayer.transform.position + Vector3.up * (AutoHandPlayer.bodyCollider != null ? AutoHandPlayer.bodyCollider.height * 0.5f : 0.9f);
            Vector3 headPos = AutoHandPlayer.headCamera != null ? AutoHandPlayer.headCamera.transform.position : bodyPos + Vector3.up * 0.7f;

            // 물 복셀 확인
            Voxel bodyVoxel = env.GetVoxel(bodyPos);
            Voxel headVoxel = env.GetVoxel(headPos);

            bool bodyInWater = bodyVoxel.hasContent && bodyVoxel.type != null && (bodyVoxel.type.renderType == RenderType.Water || bodyVoxel.GetWaterLevel() > 0);
            bool headInWater = headVoxel.hasContent && headVoxel.type != null && (headVoxel.type.renderType == RenderType.Water || headVoxel.GetWaterLevel() > 0);

            this.isInWater = bodyInWater || headInWater;
            this.isUnderwater = headInWater;
            this.isSwimming = isInWater && !isGrounded;

            // 1. 물 진입 / 퇴출 사운드
            if (!wasInWater && isInWater)
            {
                PlayWaterSplashSound();
            }
            wasInWater = isInWater;

            // 2. 언더워터 전환 사운드 및 효과
            if (!wasUnderwater && isUnderwater)
            {
                // 언더워터 진입
            }
            wasUnderwater = isUnderwater;

            // 3. 물속 물리 조정 (부력 및 저항)
            if (AutoHandPlayer.body != null)
            {
                if (isInWater)
                {
                    AutoHandPlayer.maxMoveSpeed = originalAutoHandMoveSpeed * waterMoveSpeedMultiplier;

                    // 부력 적용
                    if (!isGrounded && AutoHandPlayer.body.linearVelocity.y < 1.0f)
                    {
                        AutoHandPlayer.body.AddForce(Vector3.up * waterBuoyancy, ForceMode.Acceleration);
                    }

                    // 물속 저항
                    Vector3 vel = AutoHandPlayer.body.linearVelocity;
                    vel.x *= Mathf.Clamp01(1f - waterLinearDrag * Time.deltaTime);
                    vel.z *= Mathf.Clamp01(1f - waterLinearDrag * Time.deltaTime);
                    AutoHandPlayer.body.linearVelocity = vel;

                    // 수영 사운드 주기적 재생
                    if (isMoving && !isUnderwater)
                    {
                        ProgressSwimCycle(AutoHandPlayer.body.linearVelocity, AutoHandPlayer.maxMoveSpeed);
                    }
                }
                else
                {
                    AutoHandPlayer.maxMoveSpeed = originalAutoHandMoveSpeed;
                }
            }
        }

        /// <summary>
        /// 언더워터 상태 시 산소 소모 및 질식 데미지 처리
        /// </summary>
        protected virtual void UpdateOxygenState()
        {
            if (isUnderwater)
            {
                currentOxygen = Mathf.Max(0f, currentOxygen - Time.deltaTime);

                if (currentOxygen <= 0f && Time.time >= nextSuffocationTime)
                {
                    nextSuffocationTime = Time.time + suffocationInterval;
                    if (player != null)
                    {
                        player.DamageToPlayer(suffocationDamage);
                    }
                }
            }
            else
            {
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + oxygenRecoveryRate * Time.deltaTime);
            }
        }

        #endregion

        #region Crosshair & Pointing

        /// <summary>
        /// 주 손 전환 메서드 (true: 오른손 메인, false: 왼손 메인)
        /// </summary>
        public void SetMainHand(bool rightHandMain)
        {
            isRightHandMain = rightHandMain;
        }

        public Hand GetMainHand()
        {
            if (AutoHandPlayer == null) return null;
            return isRightHandMain ? AutoHandPlayer.handRight : AutoHandPlayer.handLeft;
        }

        public Hand GetSubHand()
        {
            if (AutoHandPlayer == null) return null;
            return !isRightHandMain ? AutoHandPlayer.handRight : AutoHandPlayer.handLeft;
        }

        protected virtual void InitCrosshair3D()
        {
            if (crosshair3D == null && crosshairPrefab != null)
            {
                GameObject chGO = Instantiate(crosshairPrefab, transform);
                chGO.name = "VR_Crosshair_Instance";
                crosshair3D = chGO.GetComponent<VR_Crosshair>();
                if (crosshair3D == null)
                {
                    crosshair3D = chGO.AddComponent<VR_Crosshair>();
                }
            }
        }

        private void UpdateHandCrosshair(Hand hand, Transform customOrigin, ref VoxelHitInfo hitInfo, bool isMainHand)
        {
            if (hand == null || env == null) return;

            Transform originTransform = customOrigin != null ? customOrigin : hand.transform;
            Vector3 rayStart = originTransform.position;
            Vector3 rayDir = originTransform.forward;

            float hitRange = player != null ? player.GetHitRange() : 5f;
            if (env.buildMode) hitRange = Mathf.Max(crosshairMaxDistance, hitRange);

            Ray handRay = new Ray(rayStart, rayDir);

            bool hit = env.RayCast(handRay, out hitInfo, hitRange, colliderTypes: ColliderTypes.IgnorePlayer, layerMask: crosshairHitLayerMask, microVoxels: microVoxels > 0) && hitInfo.voxelIndex >= 0;

            if (isMainHand)
            {
                crosshairOnBlock = hit;
                _crosshairHitInfo = hitInfo;

                // 1. Voxel 하이라이트
                if (voxelHighlight)
                {
                    if (crosshairOnBlock)
                    {
                        env.VoxelHighlight(ref hitInfo, voxelHighlightColor, voxelHighlightEdge, microVoxelSize: microVoxels);
                    }
                    else
                    {
                        env.VoxelHighlight(false);
                    }
                }

                // 2. 3D 크로스헤어 마커 위치 갱신
                if (enableCrosshair && crosshair3D != null)
                {
                    if (crosshairOnBlock)
                    {
                        float dist = Vector3.Distance(rayStart, hitInfo.point);
                        crosshair3D.SetTarget(hitInfo.point, hitInfo.normal, true, dist);
                    }
                    else
                    {
                        // 목표물이 없을 때는 전방 적정 거리에 띄움
                        Vector3 freePos = rayStart + rayDir * Mathf.Min(hitRange, 3f);
                        crosshair3D.SetFreePosition(freePos, rayDir, 3f);
                    }
                }
            }
        }

        #endregion

        #region Chunk Synchronization & Safety

        public virtual void WaitForCurrentChunk()
        {
            StartCoroutine(WaitForCurrentChunkCoroutine());
        }

        IEnumerator WaitForCurrentChunkCoroutine()
        {
            WaitForSeconds w = new WaitForSeconds(0.2f);
            for (int k = 0; k < 30; k++)
            {
                VoxelChunk chunk = env.GetCurrentChunk();
                if (chunk != null && chunk.isRendered)
                {
                    break;
                }
                yield return w;
            }
            Unstuck(true);
        }

        public override void Unstuck(bool toSurface = true)
        {
            if (env == null || !unstuck || AutoHandPlayer == null) return;

            Vector3 playerPos = AutoHandPlayer.transform.position;
            if (env.CheckCollision(playerPos) || (AutoHandPlayer.headCamera != null && env.CheckCollision(AutoHandPlayer.headCamera.transform.position)))
            {
                float minAltitude = Mathf.FloorToInt(playerPos.y) + 1.1f;
                if (toSurface)
                {
                    minAltitude = Mathf.Max(env.GetTerrainHeight(playerPos), minAltitude);
                }
                Vector3 safePos = new Vector3(playerPos.x, minAltitude + GetCharacterHeight() * 0.5f, playerPos.z);
                MoveTo(safePos);
            }
        }

        #endregion
    }
}
