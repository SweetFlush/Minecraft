# Project Status Snapshot

- **활성 작업**: Classic Hand 컨트롤러 위치 추적 버그 해결 (enableMovement 활성화)
- **현재 Phase**: Phase 1 (VR Player Locomotion & Voxel Interaction Bridge)
- **테스트 통과 수**: 프리팹 손 교체, 컨트롤러 물리 이동 활성화, 자동 Fist 바인딩 완료
- **Blocker**: 없음

## Recent Changes
- [antigravity] VR 마인크래프트 게임 기획서(`docs/VR_Minecraft_GDD.md`) 작성 및 `GEMINI.md` SSoT 동기화 반영
- [antigravity] `VR_MinecraftPlayer.prefab`의 Classic Hand (L/R)에서 비활성화(0)되어 있던 `enableMovement`를 `1`로 활성화하고 손상된 외부 프리팹 참조(`followPosition`, `followRotation`) 제거
- [antigravity] `VR_MinecraftPlayer.prefab`의 Classic Hand (L/R) `enableIK`를 `1`로 활성화
- [antigravity] `VR_CharacterController.cs`의 `EnsureHandFists()`에 런타임 `hand.enableMovement = true` 방어 로직 추가
- [antigravity] `VR_MinecraftPlayer.prefab`의 VoxelHand(L/R)를 비활성화하고 Classic Hand(L/R)를 활성화 및 AutoHandPlayer에 연결
- [antigravity] `Classic Hand (R)`의 자식인 `TeleporterPointer` 연동 유지
