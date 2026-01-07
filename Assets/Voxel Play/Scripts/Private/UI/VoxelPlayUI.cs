using UnityEngine;

namespace VoxelPlay {

    public abstract class VoxelPlayUI : MonoBehaviour, IVoxelPlayUI {

        static VoxelPlayUI _instance;

        public static void Init() {
            if (_instance != null) return;

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            _instance = Misc.FindObjectOfType<VoxelPlayUI>();
            if (_instance != null) return;

            if (env.UICanvasPrefab == null) return;

            GameObject canvas = Instantiate<GameObject>(env.UICanvasPrefab);
            canvas.name = env.UICanvasPrefab.name;
            if (canvas == null) return;

            _instance = canvas.GetComponent<VoxelPlayUI>();
        }

        public static VoxelPlayUI instance {
            get {
                if (_instance == null) {
                    Init();
                }
                return _instance;
            }
        }

        public static void Dispose() {
            if (_instance != null) DestroyImmediate(_instance.gameObject);
        }

        /// <summary>
        /// 새 메시지가 콘솔에 인쇄될 때 트리거됩니다.
        /// </summary>
        public event OnConsoleEvent OnConsoleNewMessage;

        /// <summary>
        /// 사용자가 새 명령을 입력하면 트리거됩니다.
        /// </summary>
        public event OnConsoleEvent OnConsoleNewCommand;

        /// <summary>
        /// 초기화 중에 호출됨
        /// </summary>
        public virtual void InitUI() { }

        /// <summary>
        /// 콘솔이 표시되면 true를 반환합니다.
        /// </summary>
        public virtual bool IsConsoleVisible { get { return false; } }

        /// <summary>
        /// 콘솔 표시/숨기기
        /// </summary>
        /// <param name="state">If set to <c>진실</c> state.</param>
        public virtual void ToggleConsoleVisibility(bool state) { }

        /// <summary>
        /// 콘솔에 사용자 정의 텍스트를 추가합니다.
        /// </summary>
        public virtual void AddConsoleText(string text) { }

        /// <summary>
        /// 상태 표시줄과 콘솔에 사용자 지정 메시지를 추가합니다.
        /// </summary>
        public virtual void AddMessage(string text, float displayTime = 4f, bool flash = true, bool openConsole = false) { }

        /// <summary>
        /// 상태 표시줄을 숨깁니다.
        /// </summary>
        public virtual void HideStatusText() { }

        /// <summary>
        /// 인벤토리가 표시되면 true를 반환합니다.
        /// </summary>
        public virtual bool IsInventoryVisible { get { return false; } }

        /// <summary>
        /// 인벤토리 표시/숨기기
        /// </summary>
        public virtual void ToggleInventoryVisibility(bool state) { }

        /// <summary>
        /// 다음 인벤토리 페이지로 이동합니다.
        /// </summary>
        public virtual void InventoryNextPage() { }

        /// <summary>
        /// 이전 인벤토리 페이지를 표시합니다.
        /// </summary>
        public virtual void InventoryPreviousPage() { }

        /// <summary>
        /// 인벤토리 콘텐츠를 새로 고칩니다.
        /// </summary>
        public virtual void RefreshInventoryContents() { }

        /// <summary>
        /// 화면에서 선택한 항목 표현을 업데이트합니다.
        /// </summary>
        public virtual void ShowSelectedItem(InventoryItem inventoryItem) { }

        /// <summary>
        /// 선택한 항목 그래픽을 숨깁니다.
        /// </summary>
        public virtual void HideSelectedItem() { }

        /// <summary>
        /// 게임/엔진을 로드/시작하는 동안 패널을 표시하거나 숨깁니다.로딩 진행 상황을 표시하기 위해 여러 번 호출할 수 있습니다.
        /// </summary>
        public virtual void ToggleInitializationPanel(bool visible, string text = "", float progress = 0) { }

        /// <summary>
        /// 디버그 창 표시/숨기기
        /// </summary>
        public virtual void ToggleDebugWindow(bool visible) { }


        public void Awake() {
            InitUI();
        }

        //The event-invoking method that derived classes can override.
        protected virtual void ConsoleNewMessage(string s) {
            if (OnConsoleNewMessage != null) {
                OnConsoleNewMessage.Invoke(s);
            }
        }

        //The event-invoking method that derived classes can override.
        protected virtual void ConsoleNewCommand(string s) {
            if (OnConsoleNewCommand != null) {
                OnConsoleNewCommand.Invoke(s);
            }
        }

    }


}
