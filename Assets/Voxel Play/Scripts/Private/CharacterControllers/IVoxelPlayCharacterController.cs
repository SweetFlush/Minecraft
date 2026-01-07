using System;
using UnityEngine;

namespace VoxelPlay {

    public partial interface IVoxelPlayCharacterController {

        /// <summary>
        /// 캐릭터 컨트롤러는 현재 강조된 객체에 대한 데이터(있는 경우)를 포함하는 VoxelHitInfo를 반환해야 합니다.
        /// </summary>
        VoxelHitInfo crosshairHitInfo { get; }

        /// <summary>
        /// 이 메서드는 VP에 의해 저장된 게임을 로드하고 캐릭터 컨트롤러의 위치/회전을 업데이트한 직후에 호출됩니다.
        /// </summary>
        void UpdateLook();

        /// <summary>
        /// VP가 컨트롤러 게임오브젝트와 상호작용할 수 있도록 하는 기본적인 보일러플레이트입니다. 예를 들어 위치 가져오기 등이 있습니다.
        /// </summary>
        Transform transform { get; }
        T GetComponentInChildren<T>();
        GameObject gameObject { get; }

        /// <summary>
        /// 캐릭터 컨트롤러가 초기화되고 사용할 준비가 되었는지 여부를 반환합니다.
        /// </summary>
        bool isReady { get; }

        /// <summary>
        /// 캐릭터를 목표 위치로 이동시킵니다.
        /// </summary>
        void MoveTo(Vector3 destination);

    }
}
