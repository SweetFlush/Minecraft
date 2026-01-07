using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public static class VoxelPlayCookie {

        static string[] cookies = {
                                                "월드는 여러 바이옴으로 구성되며, 각 바이옴에는 서로 다른 지형, 나무, 식생 복셀이 포함됩니다.",
                                                "'Expand/Collapse World'를 클릭해 지형, 물, 하늘 설정을 구성하세요.",
                                                "바이옴은 고도 범위와 습도 범위로 정의됩니다.",
                                                "하나의 바이옴은 여러 고도와 습도 범위에 연결될 수 있습니다.",
                                                "World Settings에서 물 범람 범위를 설정할 수 있습니다.",
                                                "물은 빌드 모드에서만 제거할 수 있습니다.",
                                                "플레이 모드에서 F1을 누르면 콘솔, Tab을 누르면 인벤토리가 열립니다.",
            "실제 성능은 Unity 에디터가 아닌 빌드에서 측정해야 합니다.",
                                                "'New Chunk Buffer Size'를 늘리면 한 번에 더 많은 청크를 예약하여 메모리 스파이크 발생 빈도를 줄일 수 있습니다.",
                                                "'Render in Editor'는 월드에 추가 정적 콘텐츠를 배치할 때 유용합니다.",
                                                "프로젝트 패널에서 마우스 오른쪽 버튼을 클릭하고 Voxel Play 하위 메뉴에서 옵션을 선택하면 World, Biome 또는 Voxel 타입을 만들 수 있습니다.",
                                                "Fog Distance와 Fog Falloff를 카메라의 Far Clip에 맞추어 환경의 부드러운 페이드 인/아웃을 설정하세요.",
                                                "Voxel Play는 Environment, FPS Controller, Player, Behaviour 네 가지 주요 커스텀 컴포넌트를 사용합니다.",
                                                "Voxel Play Environment를 통해 씬을 설정할 수 있으며, 월드 구성은 World Definition 자산에 저장됩니다.",
                                                "Voxel Play FPS Controller는 조준선, 조작, 발소리 등을 처리하는 맞춤형 FPS 컨트롤러입니다.",
                                                "Voxel Play Player 컴포넌트는 생명력 등 플레이어의 인벤토리와 속성을 저장합니다.",
                                                "Voxel Play Behaviour는 씬의 애니메이션 모델에 대한 복셀 라이팅을 동적으로 업데이트하는 선택적 컴포넌트입니다.",
            "데모용으로 선택형 Third Person Controller가 포함되어 있습니다.",
                                                "/unstuck 명령을 입력하면 캐릭터를 지상 위로 이동시킬 수 있습니다.",
                                                "사용자 지정 Terrain Generator를 작성해 월드에 연결할 수 있습니다.",
                                                "사용자 가이드는 이제 kronnect.freshdesk.com에 온라인으로 제공됩니다. 확인해 보세요!",
                                                "Qubicle을 사용해 모델을 만든 뒤 아래 도구로 Voxel Play Model Definition으로 가져오거나 변환할 수 있습니다.",
                                                "https://kronnect.com/support 지원 포럼에서 유용한 확장 기능을 받아보세요.",
                                                "플레이 모드에서 월드를 편집한 뒤 콘솔에 /save 파일이름을 입력하면 변경사항을 저장할 수 있습니다.",
                                                "커스텀 복셀 정의를 사용하면 일반 큐브 대신 프리팹을 사용할 수 있어 식생, 나무, 오브젝트 등에 유용합니다.",
                                                "Voxel Play Player 컴포넌트는 맨손과 현재 아이템에 대한 타격 및 피해 속성을 제공합니다. 각 아이템 정의는 사거리, 피해량, 공격 속도 등 사용자 지정 타격 속성을 정의할 수 있습니다."
                                };

        public static string GetCookie(int cookieIndex) {
            int c = cookieIndex % cookies.Length;
            return cookies[c];
        }

    }
}
