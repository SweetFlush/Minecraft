# VR 마인크래프트 (Auto Hand x Voxel Play 3) 프로젝트 지침 (SSoT)

> 이 문서는 VR 마인크래프트 프로젝트의 단일 진입점이자 단일 진실 공급원(SSoT)입니다.
> 현재 프로젝트 상태와 변경 사항은 [handoff/STATUS.md](handoff/STATUS.md)를 참조합니다.

## 1. 프로젝트 개요
- **엔진/환경**: Unity 2022/6 (URP)
- **핵심 패키지**:
  - **Auto Hand**: 물리 기반 VR 핸드 트래킹, 그랩, 로코모션, 햅틱 인터랙션
  - **Voxel Play 3**: 무한 절차적 복셀 월드 생성, 청크 렌더링, 복셀 수정(채굴/설치), 조명, 바이옴
- **목표**: 마인크래프트의 샌드박스 서바이벌/크리에이티브 코어 루프를 VR 환경에서 최상의 물리 체감형 인터랙션(총기 기반 채굴/건설/전투, 백팩 & 퀵슬롯 인벤토리, 물리 드롭 및 월드 오브젝트화)으로 구현
- **상세 게임 기획서 (GDD)**: [docs/VR_Minecraft_GDD.md](docs/VR_Minecraft_GDD.md) 참조

## 2. 코딩 및 아키텍처 규칙
- **하드코딩 금지**: 레이어 마스크, 물리 파라미터, 데미지 계수 등은 ScriptableObject나 인스펙터 직렬화 필드로 관리
- **Auto Hand & Voxel Play 연동 원칙**:
  - Auto Hand의 물리 충돌 및 Grab 이벤트를 Voxel Play의 Voxel 수정 API(`VoxelDamage`, `VoxelPlace` 등)와 브릿지 레이어로 연결
  - Voxel 지형 콜라이더와 AutoHandPlayer 간의 이동/낙하/점프/수영 일관성 유지
- **문서화**: 새로운 기능 추가 시 `plans/` 및 `handoff/`를 통해 진행 관리

