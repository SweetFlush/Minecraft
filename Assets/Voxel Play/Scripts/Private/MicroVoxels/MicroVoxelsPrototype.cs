using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public partial class MicroVoxelsPrototype {

        public struct SideVertexData {
            public List<Vector3> vertices;
            public List<Vector2> uvs;

            public void Init () {
                vertices = new List<Vector3>();
                uvs = new List<Vector2>();
            }
        }

        public SideVertexData[] sidesVertexData;

        public MicroVoxelsPrototype () {
            sidesVertexData = new SideVertexData[6];
            for (int k = 0; k < 6; k++) {
                sidesVertexData[k].Init();
            }
        }
    }
}
