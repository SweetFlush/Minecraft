# Classic Hand 컨트롤러 추적 문제 해결 계획

## 기본 정보
- **목적**: VR_MinecraftPlayer에서 Classic Hand가 컨트롤러 위치/회전을 따라오지 않고 제자리에 고정되는 버그 수정
- **처리 에이전트**: antigravity
- **작성 일시**: 2026-09-07 23:08
- **대상 브랜치**: feature/antigravity-classic-hand-fix
- **관련 문서**: [VR_MinecraftPlayer.prefab](file:///c:/SSDUnityLibrary/Minecraft/Assets/Prefabs/VR_MinecraftPlayer.prefab), [VR_CharacterController.cs](file:///c:/SSDUnityLibrary/Minecraft/Assets/Scripts/VR_CharacterController.cs)

## 태스크
- [x] **T1**: `VR_MinecraftPlayer.prefab`에서 Classic Hand (L/R)의 `enableMovement`를 `1`로 수정하고 구버전/외부 프리팹 참조인 `followPosition`, `followRotation` 오버라이드 제거 — 대상: `Assets/Prefabs/VR_MinecraftPlayer.prefab` — 완료 기준: 프리팹 내 Classic Hand 컴포넌트의 enableMovement가 1로 설정되고 손상된 외부 레퍼런스 제거
- [x] **T2**: `VR_CharacterController.cs`에 런타임 손 이동 방어 코드 추가 — 대상: `Assets/Scripts/VR_CharacterController.cs` — 완료 기준: `EnsureHandFists()` 등에서 `hand.enableMovement = true` 및 `hand.follow` 유효성 검사 수행
- [x] **T3**: Unity 에디터 컴파일 및 콘솔 로그 확인, 동작 검증 — 대상: Unity 에디터/콘솔 — 완료 기준: 컴파일 에러 없음 확인 및 정상 작동 검증
