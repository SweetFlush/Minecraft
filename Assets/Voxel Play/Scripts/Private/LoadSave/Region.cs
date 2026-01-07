
using System.Collections.Generic;
using System.Linq;

namespace VoxelPlay {

    public class Region {
        public int x { get; private set; }
        public int z { get; private set; }
        public List<VoxelChunk> chunks { get; private set; }

        public bool IsModifiedSinceLastSave() {
            return chunks.Any(c => c.modifiedTimestamp > VoxelPlayEnvironment.stage);
        }

        public Region (int x, int z) {
            this.x = x;
            this.z = z;
            chunks = new List<VoxelChunk>();
        }

        public void AddChunk (VoxelChunk chunk) {
            chunks.Add(chunk);
        }
    }

}