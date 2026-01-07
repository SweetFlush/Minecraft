using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

public static class VoxelData 
{
    public static readonly int CHUNK_WIDTH = 20;
    public static readonly int CHUNK_HEIGHT = 20;

    public static readonly int TEXTURE_ATLAS_SIZE_IN_BLOCKS = 4;
    public static float NormalizedBlockTextureSize
    {
        get
        {
            return 1f / (float)TEXTURE_ATLAS_SIZE_IN_BLOCKS;
        }
        set
        {

        }
    }

    public static readonly Vector3[] VOXEL_VERTS = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f)
    };

    public static readonly Vector3[] FACE_CHECKS = new Vector3[6]
    {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)
    };

    public static readonly int[,] VOXEL_TRIS = new int[6, 4]
    {
        {0, 3, 1, 2}, //Back
        {5, 6, 4, 7}, //Front
        {3, 7, 2, 6}, //Top
        {1, 5, 0, 4}, //Bottom
        {4, 7, 0, 3}, //Left
        {1, 2, 5, 6} //Right
    };

    /// <summary>
    /// LookUp table for voxel UVs
    /// </summary>
    public static readonly Vector2[] VOXEL_UVs = new Vector2[4]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
    };
}
