using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

public class VoxelPlayPointer : MonoBehaviour
{
    public Transform rayOrigin;

    VoxelPlayEnvironment env;
    VoxelHitInfo hitInfo;

    void Start()
    {
        env = VoxelPlayEnvironment.instance;

        if(rayOrigin == null) rayOrigin = transform;
    }

    void Update()
    {
        if (env == null)
            return;
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (env.RayCast(ray, out hitInfo, 100))
        {
            Debug.DrawLine(ray.origin, hitInfo.point, Color.red, 1f);
            // Additional logic when a voxel is hit can be added here

            Voxel voxel = hitInfo.GetVoxelNow();
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * 100, Color.green, 1f);
        }
    }
}
