using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const string VOXEL_HIGHLIGHT_NAME = "VoxelHighlight";

		[NonSerialized]
		public VoxelHitInfo lastHitInfo, lastHighlightInfo;

		[NonSerialized]
		public Material voxelHighlightMaterial;

		readonly List<VoxelHighlight> highlightedVoxels = new List<VoxelHighlight>();

		readonly Stack<VoxelHighlight> highlightPool = new Stack<VoxelHighlight>();

		/// <summary>
		/// 복셀의 강조 상자를 강제로 새로 고칩니다.
		/// </summary>
		public void RefreshVoxelHighlight () {
			lastHighlightInfo.voxelCenter.y = float.MinValue;
		}

		/// <summary>
		/// 복셀 그룹에 하이라이트 효과를 추가합니다.
		/// </summary>
		/// <returns><c>진실</c>, if highlight was executed, <c>거짓</c> otherwise.</returns>
		/// <param name="hitInfo">강조 표시된 복셀의 위치에 대한 정보가 포함된 voxelHitInfo 구조체입니다.</param>
		/// <param name="fadeAmplitude">펄스 효과의 범위</param>
		public bool VoxelHighlight (List<VoxelIndex> voxelIndices, Color color, float edgeWidth = 0f, float fadeAmplitude = 1f) {
			int viCount = voxelIndices.Count;

			internal_ReleaseHighlightedVoxels();

			for (int k = 0; k < viCount; k++) {
				Vector3d pos = GetVoxelPosition(voxelIndices[k]);
				if (BuildVoxelHitInfo(out VoxelHitInfo hitInfo, pos, pos)) {
					internal_VoxelHighlight(ref hitInfo, color, edgeWidth, microVoxelSize: 0, fadeAmplitude);
				}
			}

			return highlightedVoxels.Count > 0;
		}


		/// <summary>
		/// 특정 위치의 복셀에 하이라이트 효과를 추가합니다.해당 위치에 복셀이 없으면 이 메서드는 false를 반환합니다.
		/// </summary>
		/// <returns><c>진실</c>, if highlight was executed, <c>거짓</c> otherwise.</returns>
		/// <param name="hitInfo">강조 표시된 복셀의 위치에 대한 정보가 포함된 voxelHitInfo 구조체입니다.</param>
		/// <param name="microVoxel">마이크로 복셀을 강조 표시합니다.값은 선택 항목의 크기와 같습니다.</param>
		/// <param name="fadeAmplitude">펄스 효과의 범위</param>
		public bool VoxelHighlight (ref VoxelHitInfo hitInfo, Color color, float edgeWidth = 0f, int microVoxelSize = 0, float fadeAmplitude = 1f) {
			if (hitInfo.voxelCenter == lastHighlightInfo.voxelCenter) return true;
			lastHighlightInfo = hitInfo;
			internal_ReleaseHighlightedVoxels();
			return internal_VoxelHighlight(ref hitInfo, color, edgeWidth, microVoxelSize, fadeAmplitude);
		}

		void internal_ReleaseHighlightedVoxels () {
			foreach (var v in highlightedVoxels) {
				if (v != null) {
					v.SetActive(false);
					highlightPool.Push(v);
				}
			}
			highlightedVoxels.Clear();
		}

		void VoxelHighlightDispose () {
			foreach (var h in highlightPool) {
				if (h != null) DestroyImmediate(h.gameObject);
			}
			highlightPool.Clear();
			foreach (var h in highlightedVoxels) {
				if (h != null) DestroyImmediate(h.gameObject);
			}
			highlightedVoxels.Clear();
			// find any residual object
			VoxelHighlight[] hh = Misc.FindObjectsOfType<VoxelHighlight>(true);
			foreach (var h in hh) {
				DestroyImmediate(h.gameObject);
			}
			if (voxelHighlightMaterial != null) {
				DestroyImmediate(voxelHighlightMaterial);
			}
		}

		public Boundsd GetHighlightBounds (ref VoxelHitInfo hitInfo, int microVoxelSize) {
			if (microVoxelSize > 0) {
				return GetMicroVoxelBounds(ref hitInfo, microVoxelSize, 0.005f);
			}
			return new Boundsd(hitInfo.center, Vector3d.one);
		}

		bool internal_VoxelHighlight (ref VoxelHitInfo hitInfo, Color color, float edgeWidth = 0f, int microVoxelSize = 0, float fadeAmplitude = 1f) {

			VoxelHighlight voxelHighlight = GetHighlightFromPool();
			if (voxelHighlight == null) return false;

			GameObject voxelHighlightGO = voxelHighlight.gameObject;

			voxelHighlightMaterial.color = color;
			if (edgeWidth > 0f) {
				voxelHighlightMaterial.SetFloat(ShaderParams.Width, 1f / edgeWidth);
			}
			voxelHighlightMaterial.SetFloat(ShaderParams.FadeAmplitude, fadeAmplitude);

			Transform ht = voxelHighlightGO.transform;
			VoxelDefinition vd = voxelDefinitions[hitInfo.voxel.typeIndex];

			if (hitInfo.placeholder != null) {
				voxelHighlight.SetTarget(hitInfo.placeholder.transform);
				ht.SetParent(hitInfo.placeholder.transform, false);
				ht.localScale = hitInfo.placeholder.bounds.size;
				if (hitInfo.placeholder.modelMeshRenderers != null && hitInfo.placeholder.modelMeshRenderers.Length > 0 && hitInfo.placeholder.modelMeshRenderers[0] != null) {
					ht.position = hitInfo.placeholder.modelMeshRenderers[0].bounds.center;
				} else {
					ht.localPosition = hitInfo.placeholder.bounds.center;
				}
				if (hitInfo.placeholder.modelInstance != null) {
					ht.localRotation = hitInfo.placeholder.modelInstance.transform.localRotation;
				} else {
					ht.localRotation = Misc.quaternionZero;
				}
			} else if (hitInfo.item != null) {
				Transform itemTransform = hitInfo.item.transform;
				voxelHighlight.SetTarget(itemTransform);
				if (itemTransform.TryGetComponent(out BoxCollider bc)) {
					ht.SetParent(itemTransform, false);
					ht.localScale = bc.size;
					ht.localPosition = bc.center;
				}
			} else {
				voxelHighlight.SetTarget(null);
				ht.SetParent(null);
				Bounds htBounds = GetHighlightBounds(ref hitInfo, vd != null && vd.supportsMicroVoxels ? microVoxelSize : 0);
				ht.position = htBounds.center;
				ht.localScale = htBounds.size;
				ht.localRotation = Misc.quaternionZero;

				// Adapt box highlight to voxel contents
				if ((object)hitInfo.chunk != null && hitInfo.voxel.typeIndex > 0) {
					// water?
					int waterLevel = hitInfo.voxel.GetWaterLevel();
					if (waterLevel > 0) {
						// adapt to water level
						float ly = waterLevel / 15f;
						ht.localScale = new Vector3(1, ly, 1);
						Vector3d pos = new Vector3d(hitInfo.voxelCenter.x, hitInfo.voxelCenter.y - 0.5 + ly * 0.5, hitInfo.voxelCenter.z);
						ht.position = pos;
					} else {
						if (vd.gpuInstancing && vd.renderType == RenderType.Custom) {
							// instanced mesh ?
							Bounds bounds = vd.mesh.bounds;
							Quaternion rotation = vd.GetRotation(hitInfo.voxelCenter);
							// User rotation
							float rot = hitInfo.chunk.voxels[hitInfo.voxelIndex].GetTextureRotationDegrees();
							if (rot != 0) {
								rotation *= Quaternion.Euler(0, rot, 0);
							}
							// Custom position
							Vector3d localPos = hitInfo.voxelCenter + rotation * (bounds.center + vd.GetOffset(hitInfo.voxelCenter));
							ht.position = localPos;
							Vector3 size = bounds.size;
							FastVector.Multiply(ref size, ref vd.scale);
							voxelHighlightGO.transform.localScale = size;
							voxelHighlightGO.transform.localRotation = rotation;
						} else if (vd.isVegetation) {
							// grass?
							Vector3d pos = hitInfo.voxelCenter - hitInfo.chunk.position;
							Vector3d aux = pos;
							float random = WorldRand.GetValue(pos.x, pos.z);
							pos.x += random * 0.5 - 0.25;
							aux.x += 1;
							random = WorldRand.GetValue(aux);
							pos.z += random * 0.5 - 0.25;
							float offsetY = random * 0.1f;
							pos.y -= offsetY * 0.5 + 0.5 - vd.scale.y * 0.5;
							ht.position = (hitInfo.chunk.position + pos);
							Vector3 adjustedScale = vd.scale;
							adjustedScale.y -= offsetY;
							voxelHighlightGO.transform.localScale = adjustedScale;
						}
					}
				}
			}
			if (vd != null) {
				ht.localPosition += vd.highlightOffset;
			}

			highlightedVoxels.Add(voxelHighlight);
			voxelHighlight.SetActive(true);
			return true;
		}


		VoxelHighlight GetHighlightFromPool () {
			VoxelHighlight vh;
			while (highlightPool.TryPop(out vh) && vh == null) { }
			
			if (vh != null) return vh;

			GameObject voxelHighlightGO = Instantiate(Resources.Load<GameObject>("VoxelPlay/Prefabs/VoxelHighlightEdges"));
			voxelHighlightGO.hideFlags = HideFlags.HideInHierarchy;
			voxelHighlightGO.name = VOXEL_HIGHLIGHT_NAME;
			Renderer renderer = voxelHighlightGO.GetComponent<Renderer>();
			if (voxelHighlightMaterial == null) {
				voxelHighlightMaterial = Instantiate(renderer.sharedMaterial); // instantiates material to avoid changing resource
			}
			renderer.sharedMaterial = voxelHighlightMaterial;
			if (!voxelHighlightGO.TryGetComponent(out vh)) {
				vh = voxelHighlightGO.AddComponent<VoxelHighlight>();
			}
			return vh;
		}

	}
}
