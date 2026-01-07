using Autohand;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using VoxelPlay;

[RequireComponent(typeof(AudioSource))]
public class VR_CharacterController : VoxelPlayCharacterControllerBase
{
    public static VR_CharacterController Instance 
    {
        get
        {
            return VoxelPlayEnvironment.instance.characterController as VR_CharacterController;
        }
    }

    public AutoHandPlayer AutoHandPlayer => AutoHandPlayer.Instance;

    public override bool isReady => true;

    public bool isRightHandMain = true;

    protected VoxelHitInfo _leftHandCrosshairHitInfo;
    public VoxelHitInfo LeftHandCrosshairHitInfo => _leftHandCrosshairHitInfo;

    protected VoxelHitInfo _rightHandCrosshairHitInfo;
    public VoxelHitInfo RightHandCrosshairHitInfo => _rightHandCrosshairHitInfo;
    /// <summary>
    /// 메인 손 크로스헤어 리턴
    /// </summary>
    public override VoxelHitInfo crosshairHitInfo
    {
        get
        {
            if(isRightHandMain) return _rightHandCrosshairHitInfo;
            else return _leftHandCrosshairHitInfo;
        }
    }
    /// <summary>
    /// 서브 손 크로스헤어 리턴
    /// </summary>
    public VoxelHitInfo subHandCrosshairInfo
    {
        get
        {
            if (!isRightHandMain) return _rightHandCrosshairHitInfo;
            else return _leftHandCrosshairHitInfo;
        }
    }

    public override void MoveTo(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public override void UpdateLook()
    {
        //Do Nothing
    }

    protected virtual void LateInit()
    {
        WaitForCurrentChunk();
    }

    /// <summary>
    /// Disables character controller until chunk is ready
    /// </summary>
    public virtual void WaitForCurrentChunk()
    {
        //ToggleCharacterController(false);
        StartCoroutine(WaitForCurrentChunkCoroutine());
    }

    /// <summary>
    /// Ensures player chunk is finished before allow player movement / interaction with colliders
    /// </summary>
    IEnumerator WaitForCurrentChunkCoroutine()
    {
        // Wait until current player chunk is rendered
        WaitForSeconds w = new WaitForSeconds(0.2f);
        for (int k = 0; k < 20; k++)
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

    /// <summary>
    /// 메인 손 리턴
    /// </summary>
    /// <returns></returns>
    public Hand GetMainHand()
    {
        if(AutoHandPlayer.Instance == null)
        {
            Debug.LogError("AutoHandPlayer is null");
            return null;
        }

        if (isRightHandMain) return AutoHandPlayer.Instance.handRight;
        else return AutoHandPlayer.Instance.handLeft;
    }
    public Hand GetSubHand()
    {
        if (AutoHandPlayer.Instance == null)
        {
            Debug.LogError("AutoHandPlayer is null");
            return null;
        }

        if (!isRightHandMain) return AutoHandPlayer.Instance.handRight;
        else return AutoHandPlayer.Instance.handLeft;

    }

    //복셀 선택 구현하기
    //1. 레이캐스팅 타겟 구현하기

    //기본 로코모션 구현
    //1. 이동기능 연결하기
    //2. 점프 기능 구현하기
    //3. 달리기 구현
    //4. 수영 구현

    #region Unity LifeCycle

    private void Awake()
    {

    }

    private void Start()
    {
        Init();

        if (env.initialized)
        {
            LateInit();
        }
        else
        {
            env.OnInitialized += () => LateInit();
        }
    }

    private void LateUpdate()
    {
        if (env != null && env.initialized)
        {
            UpdateHandCrosshair(AutoHandPlayer.Instance.handRight, ref _rightHandCrosshairHitInfo);
            UpdateHandCrosshair(AutoHandPlayer.Instance.handLeft, ref _leftHandCrosshairHitInfo);
        }
    }

    #endregion

    #region Crosshair

    //오른손 및 왼손에 크로스헤어 생성
    protected void InitCrosshair()
    {

    }

    public void ResetCrosshairPosition()
    {

    }

    private void UpdateHandCrosshair(Hand hand, ref VoxelHitInfo hitInfo)
    {
        bool isMainHand = (hand == GetMainHand());
        Transform handTransform = hand.transform;

        float hitRange = player.GetHitRange();
        if (env.buildMode) hitRange = Mathf.Max(crosshairMaxDistance, hitRange);

        Ray handRay = new Ray(handTransform.position, handTransform.forward);

        crosshairOnBlock = env.RayCast(handRay, out hitInfo, hitRange, colliderTypes: ColliderTypes.IgnorePlayer, layerMask: crosshairHitLayerMask, microVoxels: microVoxels > 0) && _crosshairHitInfo.voxelIndex >= 0;

        if(isMainHand)
        {
            if (crosshairOnBlock)
            {
                if (voxelHighlight)
                {
                    env.VoxelHighlight(ref hitInfo, voxelHighlightColor, voxelHighlightEdge, microVoxelSize: microVoxels);
                }
            }
            else
            {
                env.VoxelHighlight(false);
            }
        }
    }
    #endregion
}
