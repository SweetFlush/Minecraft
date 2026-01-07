using System.Collections.Generic;

namespace VoxelPlay {
    
    public class RegionPartitioner {
        private Dictionary<(int, int), Region> regions = new Dictionary<(int, int), Region>();

        public RegionPartitioner (List<VoxelChunk> chunks) {
            
            foreach (var chunk in chunks) {

                if (chunk == null || !chunk.modified)
                    continue;

                int regionX = FastMath.FloorToInt(chunk.position.x / 256);
                int regionZ = FastMath.FloorToInt(chunk.position.z / 256);

                var key = (regionX, regionZ);
                if (!regions.ContainsKey(key)) {
                    regions[key] = new Region(regionX, regionZ);
                }

                regions[key].AddChunk(chunk);
            }
        }

        public int Count => regions.Count;

        public IEnumerable<Region> GetRegions () {
            return regions.Values;
        }
    }

}