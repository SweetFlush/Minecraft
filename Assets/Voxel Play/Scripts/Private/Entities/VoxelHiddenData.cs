using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	/// <summary>
	/// 이 구조체는 희소 복셀에 존재하는 추가 데이터를 나타냅니다.이것이 바로 이러한 필드가 Voxel 엔터티에 존재하지 않는 이유입니다.
	/// </summary>
	public struct VoxelHiddenData {
		public bool hidden;
		public ushort hiddenTypeIndex;
		public byte hiddenOpaque, hiddenLight;
		public HideStyle hiddenStyle;

		public void Clear()
        {
			hidden = false;
        }
	}
}