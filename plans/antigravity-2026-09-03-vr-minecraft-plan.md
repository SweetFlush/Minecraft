# VR 마인크래프트 (Auto Hand x Voxel Play 3) 개발 계획

## 기본 정보
- **목적**: AutoHandPlayer와 VoxelPlay 3를 완전하게 연결하여 일관된 VR 플레이어 로코모션(점프, 수영, 발소리, 지형 스텝업)과 VR 컨트롤러 기반 크로스헤어/포인팅 시스템 구현
- **처리 에이전트**: antigravity
- **작성 일시**: 2026-09-03 20:46
- **대상 브랜치**: feature/antigravity-vr-player-integration
- **관련 문서**: [implementation_plan.md](file:///C:/Users/GunPark/.gemini/antigravity-ide/brain/1afb7bf9-5a22-480f-8b00-09ad5ccbd3bc/implementation_plan.md)

## 태스크
- [x] **T1**: AutoHandPlayer와 VoxelPlay의 물리 이동/점프/발소리/단차/텔레포트 동기화 구현 — 대상: `Assets/Scripts/VR_CharacterController.cs` — 완료 기준: AutoHandPlayer 지상 상태, 점프/착지 사운드, 바닥 복셀 발소리, 1블록 스텝업 및 SetPosition 연동 완료
- [x] **T2**: 복셀 물(Water) 환경 감지 및 수영/부력/언더워터 시스템 연동 — 대상: `Assets/Scripts/VR_CharacterController.cs` — 완료 기준: 물 복셀 진입 시 첨벙 사운드, 수영/부력 상태 제어, 언더워터 상태 갱신 완료
- [x] **T3**: VR 컨트롤러(주 손/보조 손 전환 가능) 기반 3D 크로스헤어 및 레이캐스트 포인팅 시스템 구현 — 대상: `Assets/Scripts/VR_CharacterController.cs`, `Assets/Scripts/VR_Crosshair.cs` — 완료 기준: 주 손(기본 오른손, 왼손 전환 지원) 레이캐스트, 복셀 하이라이트 및 3D 크로스헤어 마커 정상 연동
- [x] **T4**: `VR_VoxelPlayer.cs` 스탯/피격/산소 브릿지 연동 — 대상: `Assets/Scripts/VR_VoxelPlayer.cs` — 완료 기준: 체력, 산소, 데미지 피드백 연결 완료
