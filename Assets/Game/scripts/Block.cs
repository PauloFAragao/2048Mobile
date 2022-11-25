using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    //valor do bloco
    public int Value;

    //referencia ao node que está ocupando
    public Node node;

    public Block mergingBlock;

    public bool merging;

    //posição do bloco
    public Vector2 Pos => transform.position;

    //render do objeto
    [SerializeField] private SpriteRenderer _render;

    //referencia para o texto do bloco
    [SerializeField] private TextMeshPro _text;

    //método que vai ser chamado para iniciar o bloco
    public void Init(BlockType type)
    {
        //recebendo o valor do bloco
        Value = type.Value;

        //recebendo a cor do bloco
        _render.color = type.Color;

        //adicionando o valor ao texto do bloco
        _text.text = type.Value.ToString();
    }

    public void SetBlock(Node _node)
    {
        if (node != null) node.OccupiedBlock = null;

        node = _node;

        node.OccupiedBlock = this;
    }

    public void MergeBlock(Block blockToMergeWith)
    {
        //set the block we are merging with
        mergingBlock = blockToMergeWith;

        //set current node as unoccupied to allow blocks to use it.
        node.OccupiedBlock = null;

        //set the base block as merging, so it does not get used twice.
        blockToMergeWith.merging = true;
    }

    public bool CanMerge(int _value) => _value == Value && !merging && mergingBlock == null;

}
