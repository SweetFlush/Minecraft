using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelDefinition : ScriptableObject {

        /// <summary>
        /// 마이크로복셀 모양을 나타내는 선택적 데이터입니다.이는 VoxelPlace 메서드를 호출할 때만 사용됩니다.
        /// </summary>
        public MicroVoxels microVoxels;

        public bool usesMicroVoxels => supportsMicroVoxels && microVoxels != null && !microVoxels.isEmpty;

        // If the voxel definition supports microvoxels
        public bool supportsMicroVoxels => !placeOnWall && renderType.supportsMicroVoxels();

#if UNITY_EDITOR
        [NonSerialized]
        public Mesh microVoxelsPreviewMesh;

        [NonSerialized]
        public GameObject microVoxelsPreviewGO;

        ulong microVoxelsPreviewHash;

        public Mesh GetMicroVoxelsPreviewMesh () {

            ulong currentHash = microVoxels.GetGridHashCode();
            if (microVoxelsPreviewMesh != null) {
                if (currentHash == microVoxelsPreviewHash) return microVoxelsPreviewMesh;
            }

            microVoxelsPreviewHash = currentHash;

            if (microVoxelsPreviewMesh != null) {
                microVoxelsPreviewMesh.Clear();
            } else {
                microVoxelsPreviewMesh = new Mesh();
            }

            MeshingThreadMicroVoxels mesher = new MeshingThreadMicroVoxels();
            mesher.UpdateMeshData(microVoxels);
            MicroVoxelsPrototype proto = microVoxels.prototype;
            if (proto == null) return null;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            int vertexOffset = 0;

            for (int side = 0; side < 6; side++) {
                vertices.AddRange(proto.sidesVertexData[side].vertices);
                uvs.AddRange(proto.sidesVertexData[side].uvs);
                int sideVertexCount = proto.sidesVertexData[side].vertices.Count;
                for (int i = 0; i < sideVertexCount; i += 4) {
                    triangles.Add(vertexOffset);
                    triangles.Add(vertexOffset + 1);
                    triangles.Add(vertexOffset + 3);
                    triangles.Add(vertexOffset + 3);
                    triangles.Add(vertexOffset + 2);
                    triangles.Add(vertexOffset);
                    vertexOffset += 4;
                }
            }

            microVoxelsPreviewMesh.SetVertices(vertices);
            microVoxelsPreviewMesh.SetUVs(0, uvs);
            microVoxelsPreviewMesh.SetTriangles(triangles, 0);
            microVoxelsPreviewMesh.RecalculateNormals();
            microVoxelsPreviewMesh.name = name + " MicroVoxels";

            return microVoxelsPreviewMesh;
        }

#endif

    }

}