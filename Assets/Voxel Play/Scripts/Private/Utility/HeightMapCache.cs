namespace VoxelPlay {

    public class HeightMapCache {

        readonly FastHashSet<HeightMapInfo[]> sectorsDict;

        public HeightMapCache (int poolSize) {
            sectorsDict = new FastHashSet<HeightMapInfo[]>(16);
        }

        public void Clear () {
            sectorsDict.Clear();
        }

        public bool TryGetValue (double x, double z, out HeightMapInfo[] heights, out int heightIndex) {
            FastMath.FloorToInt(x / VoxelPlayEnvironment.CHUNK_SIZE, z / VoxelPlayEnvironment.CHUNK_SIZE, out int chunkX, out int chunkZ);
            int key = ((chunkZ + 1024) << 16) + chunkX + 1024;
            chunkX *= VoxelPlayEnvironment.CHUNK_SIZE;
            chunkZ *= VoxelPlayEnvironment.CHUNK_SIZE;
            int px = (int)(x - chunkX);
            int pz = (int)(z - chunkZ);
            heightIndex = pz * VoxelPlayEnvironment.CHUNK_SIZE + px;

            if (sectorsDict.TryGetValue(key, out heights)) return true;

            heights = new HeightMapInfo[VoxelPlayEnvironment.ONE_Y_ROW];
            sectorsDict.Add(key, heights);

            return false;
        }

        public void Remove (double x, double z) {
            FastMath.FloorToInt(x / VoxelPlayEnvironment.CHUNK_SIZE, z / VoxelPlayEnvironment.CHUNK_SIZE, out int chunkX, out int chunkZ);
            int key = ((chunkZ + 1024) << 16) + chunkX + 1024;
            sectorsDict.Remove(key);
        }

    }

}