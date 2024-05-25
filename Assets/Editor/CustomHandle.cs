using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HandleOffsets
{
    public float size;
    public float offset;
}

public enum HandleTypes
{
    arrow,
    circle,
    cone, 
    cube,
    dot, 
    rectangle, 
    sphere
}

public class CustomHandle : MonoBehaviour
{
    [SerializeField]
    public HandleOffsets handleOffsets;
    public HandleTypes handleTypes;
}
