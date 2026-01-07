using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    public Material material;
    public BlockType[] blockTypes;

    private void Awake()
    {
        Instance = this;
    }
}

[Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
}
