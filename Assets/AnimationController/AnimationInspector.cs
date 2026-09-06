using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class AnimationInspector : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("애니메이터 컴포넌트가 부착된 대상 게임오브젝트")]
    public GameObject targetObject;

    [Header("UI Controls")]
    public TMP_Dropdown clipDropdown;
    public Slider progressSlider;
    public TMP_InputField frameInputField;

    [Header("Playback Buttons")]
    public Button playBtn;
    public Button pauseBtn;
    public Button rewindBtn;

    [Header("추가 제안 기능 (선택사항)")]
    public Button nextFrameBtn;     // 1프레임 앞으로
    public Button prevFrameBtn;     // 1프레임 뒤로
    public TextMeshProUGUI totalFramesText; // 전체 프레임 표시 텍스트

    private Animator animator;
    private AnimationClip currentClip;
    private List<AnimationClip> clipList = new List<AnimationClip>();

    private enum PlayState { Paused, Playing, Rewinding }
    private PlayState currentState = PlayState.Paused;

    private float currentFrame = 0f;
    private int totalFrames = 1;

    private bool isUpdatingUI = false;
    private bool isDraggingSlider = false;

    void Start()
    {
        if (targetObject == null || !targetObject.TryGetComponent(out animator))
        {
            Debug.LogError("Target GameObject에 Animator가 없거나 할당되지 않았습니다.");
            return;
        }

        // 애니메이터의 자체 재생 속도를 0으로 고정 (스크립트에서 프레임을 수동 제어하기 위함)
        animator.speed = 0f;

        InitDropdown();
        SetupUIEvents();
        SetupSliderEventTriggers();

        // 초기 클립 설정
        if (clipList.Count > 0)
        {
            SetCurrentClip(0);
        }
    }

    void Update()
    {
        if (currentClip == null || isDraggingSlider) return;

        // 상태에 따른 프레임 연산 (시간 기준이 아닌 프레임 기준)
        if (currentState == PlayState.Playing)
        {
            currentFrame += currentClip.frameRate * Time.deltaTime;
        }
        else if (currentState == PlayState.Rewinding)
        {
            currentFrame -= currentClip.frameRate * Time.deltaTime;
        }

        // 루프 처리 (프레임 범위를 벗어나면 순환)
        if (currentState != PlayState.Paused)
        {
            if (currentFrame > totalFrames) currentFrame = 0f;
            if (currentFrame < 0f) currentFrame = totalFrames;

            ApplyFrameToAnimator();
            UpdateUI();
        }
    }

    // ================= UI 초기화 및 이벤트 연결 =================

    private void InitDropdown()
    {
        clipDropdown.ClearOptions();
        clipList.Clear();

        // 애니메이터 컨트롤러에서 모든 클립 가져오기
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null) return;

        List<string> clipNames = new List<string>();
        foreach (AnimationClip clip in controller.animationClips)
        {
            clipList.Add(clip);
            clipNames.Add(clip.name);
        }

        clipDropdown.AddOptions(clipNames);
    }

    private void SetupUIEvents()
    {
        clipDropdown.onValueChanged.AddListener(SetCurrentClip);

        progressSlider.onValueChanged.AddListener(OnSliderValueChanged);
        frameInputField.onEndEdit.AddListener(OnInputFieldSubmitted);

        playBtn.onClick.AddListener(() => currentState = PlayState.Playing);
        pauseBtn.onClick.AddListener(() => currentState = PlayState.Paused);
        rewindBtn.onClick.AddListener(() => currentState = PlayState.Rewinding);

        // 추가 제안: 1프레임씩 이동
        if (nextFrameBtn != null) nextFrameBtn.onClick.AddListener(() => StepFrame(1));
        if (prevFrameBtn != null) prevFrameBtn.onClick.AddListener(() => StepFrame(-1));
    }

    private void SetupSliderEventTriggers()
    {
        // 슬라이더를 드래그하는 동안 애니메이션을 일시정지하기 위한 이벤트 동적 할당
        EventTrigger trigger = progressSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = progressSlider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => {
            isDraggingSlider = true;
            currentState = PlayState.Paused;
        });

        EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => {
            isDraggingSlider = false;
        });

        trigger.triggers.Add(pointerDown);
        trigger.triggers.Add(pointerUp);
    }

    // ================= 핵심 기능 구현 =================

    private void SetCurrentClip(int index)
    {
        if (index < 0 || index >= clipList.Count) return;

        currentClip = clipList[index];

        // 총 프레임 수 계산 (길이 * 초당 프레임)
        totalFrames = Mathf.RoundToInt(currentClip.length * currentClip.frameRate);
        currentFrame = 0f;

        if (totalFramesText != null)
            totalFramesText.text = $"/ {totalFrames}";

        currentState = PlayState.Paused;

        ApplyFrameToAnimator();
        UpdateUI();
    }

    private void OnSliderValueChanged(float value)
    {
        if (isUpdatingUI) return; // 코드로 인한 값 변경 시 무시

        currentFrame = value * totalFrames;
        ApplyFrameToAnimator();
        UpdateInputField();
    }

    private void OnInputFieldSubmitted(string input)
    {
        if (isUpdatingUI) return;

        if (int.TryParse(input, out int parsedFrame))
        {
            // 입력된 값이 유효 범위를 벗어나지 않도록 클램프
            currentFrame = Mathf.Clamp(parsedFrame, 0, totalFrames);
            currentState = PlayState.Paused; // 순간 이동 후 일시정지

            ApplyFrameToAnimator();
            UpdateSlider();
        }
    }

    private void StepFrame(int step)
    {
        currentState = PlayState.Paused;
        currentFrame = Mathf.Clamp(currentFrame + step, 0, totalFrames);
        ApplyFrameToAnimator();
        UpdateUI();
    }

    // ================= 상태 갱신 =================

    private void ApplyFrameToAnimator()
    {
        if (currentClip == null) return;

        // 정규화된 시간(0~1) 계산
        float normalizedTime = totalFrames > 0 ? (currentFrame / totalFrames) : 0f;

        // 애니메이터에 적용 (Layer 0 기준)
        // 주의: Animator State의 이름이 Animation Clip의 이름과 동일해야 정상 동작합니다.
        animator.Play(currentClip.name, 0, normalizedTime);
        animator.Update(0f); // 즉시 갱신
    }

    private void UpdateUI()
    {
        isUpdatingUI = true;
        progressSlider.value = totalFrames > 0 ? (currentFrame / totalFrames) : 0f;
        frameInputField.text = Mathf.RoundToInt(currentFrame).ToString();
        isUpdatingUI = false;
    }

    private void UpdateSlider()
    {
        isUpdatingUI = true;
        progressSlider.value = totalFrames > 0 ? (currentFrame / totalFrames) : 0f;
        isUpdatingUI = false;
    }

    private void UpdateInputField()
    {
        isUpdatingUI = true;
        frameInputField.text = Mathf.RoundToInt(currentFrame).ToString();
        isUpdatingUI = false;
    }

}