using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new();
    List<int> triangles = new();
    List<Vector2> uvs = new();

    byte[,,] voxelMap = new byte[VoxelData.CHUNK_WIDTH, VoxelData.CHUNK_HEIGHT, VoxelData.CHUNK_WIDTH];

    World world;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        PopulateVoxelMap();
        CreateChunkMeshData();

        CreateMesh();
    }

    private void AddVoxelDataToChunk(Vector3 offset)
    {
        for (int point = 0; point < 6; ++point)
        {
            //다른 복셀과 맞닿아있지 않은 면만 렌더링
            if (!CheckVoxel(offset + VoxelData.FACE_CHECKS[point]))
            {
                vertices.Add(offset + VoxelData.VOXEL_VERTS[VoxelData.VOXEL_TRIS[point, 0]]);
                vertices.Add(offset + VoxelData.VOXEL_VERTS[VoxelData.VOXEL_TRIS[point, 1]]);
                vertices.Add(offset + VoxelData.VOXEL_VERTS[VoxelData.VOXEL_TRIS[point, 2]]);
                vertices.Add(offset + VoxelData.VOXEL_VERTS[VoxelData.VOXEL_TRIS[point, 3]]);

                uvs.Add(VoxelData.VOXEL_UVs[0]);
                uvs.Add(VoxelData.VOXEL_UVs[1]);
                uvs.Add(VoxelData.VOXEL_UVs[2]);
                uvs.Add(VoxelData.VOXEL_UVs[3]);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }
    }

    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; ++x)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; ++z)
                {
                    voxelMap[x, y, z] = 0;
                }
            }
        }
    }

    private bool CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        //청크 안 배열의 index out of bound 예외 처리
        if(x < 0 || x > VoxelData.CHUNK_WIDTH -1 || y < 0 || y > VoxelData.CHUNK_HEIGHT - 1 || z < 0 || z > VoxelData.CHUNK_WIDTH - 1)
        {
            return false;
        }

        return World.Instance.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    private void CreateChunkMeshData()
    {
        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; ++x)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; ++z)
                {
                    AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TEXTURE_ATLAS_SIZE_IN_BLOCKS;
        float x = textureID - (y * VoxelData.TEXTURE_ATLAS_SIZE_IN_BLOCKS);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y));
    }
}
