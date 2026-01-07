// Voxel Play 
// Created by Ramiro Oliva (Kronnect)

// Voxel Play Interactive Object - attach this script to any custom voxel or object that you want to react to player interactions

using System;
using UnityEngine;
using System.Collections;

namespace VoxelPlay {
				
	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000049602-interactive-objects")]
	public abstract class VoxelPlayInteractiveObject : MonoBehaviour {

		/// <summary>
		/// 사용자 정의 태그.장면에서 개체를 구별하는 데 유용할 수 있습니다.예를 들어 이중문의 경우 customTag는 왼쪽과 오른쪽을 구별하는 데 도움이 됩니다.
		/// </summary>
		public string customTag;

		/// <summary>
		/// 이 개체의 상호 작용 거리입니다.플레이어가 이 개체에 접근하면 'E'를 눌러 개체와 상호 작용할 수 있습니다.그러면 OnPlayerAction 이벤트가 호출됩니다.
		/// </summary>
		public float interactionDistance = 4f;

		/// <summary>
		/// 이 개체와 상호 작용하면 근처에 있는 다른 개체도 활성화됩니다.이중문과 같은 복합 객체에 유용합니다.
		/// </summary>
		public bool triggerNearbyObjects;

		/// <summary>
		/// 플레이어가 이 개체에 접근하면(상호작용 거리보다 가까움) Voxel Play에서 true로 설정합니다.플레이어가 상호작용 영역을 나갈 때 false로 설정됩니다.OnPlayerApproach/OnPlayerGoesAway도 그에 따라 실행됩니다.
		/// </summary>
		[NonSerialized]
		public bool playerIsNear;

		/// <summary>
		/// 등록 시 이 대화형 개체에 대해 내부적으로 할당된 인덱스입니다.수정하지 마십시오.
		/// </summary>
		[NonSerialized]
		public int registrationIndex;

		/// <summary>
		/// 플레이어가 상호 작용 거리 내에 있을 때 이 상호 작용 개체에 대해 내부적으로 할당된 인덱스입니다.수정하지 마십시오.
		/// </summary>
		[NonSerialized]
		public int nearIndex;

		protected VoxelPlayEnvironment env;

		public virtual void OnStart() {}
		public virtual void OnPlayerApproach() {}
		public virtual void OnPlayerGoesAway() {}
		public virtual void OnPlayerAction() {}

		public void Start() {
			env = VoxelPlayEnvironment.instance;
			if (env != null) {
				VoxelPlayInteractiveObjectsManager.instance.InteractiveObjectRegister (this);
			}
			OnStart ();
		}

		public void OnDestroy() {
			if (env != null) {
				VoxelPlayInteractiveObjectsManager.instance.InteractiveObjectUnRegister (this);
			}
			
		}


	}
}