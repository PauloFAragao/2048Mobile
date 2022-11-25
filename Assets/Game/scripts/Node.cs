using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    //posição do node
    public Vector2 Pos => transform.position;

    public Block OccupiedBlock;
}
