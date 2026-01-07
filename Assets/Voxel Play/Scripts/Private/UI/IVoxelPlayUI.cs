
namespace VoxelPlay {

    public delegate void OnConsoleEvent (string text);

    public partial interface IVoxelPlayUI {

        /// <summary>
        /// 새 메시지가 콘솔에 인쇄될 때 트리거됩니다.
        /// </summary>
        event OnConsoleEvent OnConsoleNewMessage;

        /// <summary>
        /// 사용자가 새 명령을 입력하면 트리거됩니다.
        /// </summary>
        event OnConsoleEvent OnConsoleNewCommand;

        /// <summary>
        /// UI를 초기화하는 데 필요한 방법입니다.OnEnable 이벤트와 같습니다.
        /// </summary>
        void InitUI ();

        /// <summary>
        /// 콘솔이 표시되면 true를 반환합니다.
        /// </summary>
        bool IsConsoleVisible { get; }

        /// <summary>
        /// 콘솔 표시/숨기기
        /// </summary>
        void ToggleConsoleVisibility (bool state);

        /// <summary>
        /// 콘솔에 사용자 정의 텍스트를 추가합니다.
        /// </summary>
        void AddConsoleText (string text);

        /// <summary>
        /// 상태 표시줄과 콘솔에 사용자 지정 메시지를 추가합니다.
        /// </summary>
        void AddMessage (string text, float displayTime = 4f, bool flash = true, bool openConsole = false);

        /// <summary>
        /// 상태 표시줄을 숨깁니다.
        /// </summary>
        void HideStatusText ();

        /// <summary>
        /// 인벤토리 표시/숨기기
        /// </summary>
        void ToggleInventoryVisibility (bool state);

        /// <summary>
        /// 인벤토리가 표시되면 true를 반환합니다.
        /// </summary>
        bool IsInventoryVisible { get; }

        /// <summary>
        /// 다음 인벤토리 페이지로 이동합니다.
        /// </summary>
        void InventoryNextPage ();

        /// <summary>
        /// 이전 인벤토리 페이지를 표시합니다.
        /// </summary>
        void InventoryPreviousPage ();

        /// <summary>
        /// 인벤토리 콘텐츠를 새로 고칩니다.
        /// </summary>
        void RefreshInventoryContents ();

        /// <summary>
        /// 화면에서 선택한 항목 표현을 업데이트합니다.
        /// </summary>
        void ShowSelectedItem (InventoryItem inventoryItem);

        /// <summary>
        /// 선택한 항목 그래픽을 숨깁니다.
        /// </summary>
        void HideSelectedItem ();

        /// <summary>
        /// 게임/엔진을 로드/시작하는 동안 패널을 표시하거나 숨깁니다.로딩 진행 상황을 표시하기 위해 여러 번 호출할 수 있습니다.
        /// </summary>
        void ToggleInitializationPanel (bool visible, string text = "", float progress = 0);

        /// <summary>
        /// 디버그 창 표시/숨기기
        /// </summary>
        void ToggleDebugWindow (bool visible);

    }


}
