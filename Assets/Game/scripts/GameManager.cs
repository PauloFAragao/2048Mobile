using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    //tamanho do campo
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;

    //prefabs
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;

    //lista de tipos de blocos que vão existir no jogo
    [SerializeField] private List<BlockType> _types;

    //velocidade da movimentação dos blocos
    [SerializeField] private float travelTime = 0.2f;

    //listas
    private List<Node> _nodes;
    private List<Block> _blocks;

    //controlador do estado do jogo
    private GameState _state;

    //variável para marcar se é o começo do jogo
    private int _round;

    //variável que indica a necessidade de spawnar novos blocos
    private bool needNewBlock;

    //para setar os valores de acordo com o valor correto
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);

    void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;

        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;

            case GameState.SpawningBlocks:
                if (needNewBlock)
                    SpawnBlocks(_round++ == 0 ? 2 : 1);

                ChangeState(GameState.CheckGameOver);
                break;

            case GameState.WaitingInput:
                break;

            case GameState.Moving:
                break;

            case GameState.CheckGameOver:

                if (CheckGameIsOver())
                    ChangeState(GameState.Lose);
                else
                    ChangeState(GameState.WaitingInput);

                break;

            case GameState.Lose:
                //loseScreen.SetActive(true);
                Debug.Log("GAME OVER");
                break;

            default:
                break;
        }
    }

    void Update()
    {
        if (_state == GameState.WaitingInput) ProcessInputs();
    }

    void ProcessInputs()
    {
        //input no touch do cel
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Moved)
            {
                if (t.deltaPosition.x < -25)//para esquerda
                {
                    //Debug.Log("PARA Esquerda");
                    Shift(Vector2.left);
                }
                else if (t.deltaPosition.x > 25)//para direita
                {
                    //Debug.Log("PARA Direita");
                    Shift(Vector2.right);
                }
                else if (t.deltaPosition.y > 25)//para cima
                {
                    //Debug.Log("PARA Cima");
                    Shift(Vector2.up);
                }
                else if (t.deltaPosition.y < -25)//para baixo
                {
                    //Debug.Log("PARA Baixo");
                    Shift(Vector2.down);
                }
            }
        }
    }
    //esse método vai ser responsável por criar o tabuleiro
    private void GenerateGrid()
    {
        //marcando que é o começo do jogo
        _round = 0;

        //indicando a necessidade de spawn dos blocos
        needNewBlock = true;

        //iniciando as listas
        _nodes = new List<Node>();//lista de nodes
        _blocks = new List<Block>();//lista de blocos

        //criando os nodes 
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _width; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        //fundo
        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        //centralizando a camera
        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        //mudando o estado
        ChangeState(GameState.SpawningBlocks);

    }

    private void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

        foreach (var node in freeNodes.Take(amount))
        {
            SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
        }
    }

    void SpawnBlock(Node node, int value)
    {
        var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);//instanciando o novo bloco
        block.Init(GetBlockTypeByValue(value));//chamando o método dentro do bloco para passar o valor dele
        block.SetBlock(node);//passando o node onde o bloco está
        _blocks.Add(block);//adicionando o bloco a lista
    }

    //método para processar e movimentar os blocos
    void Shift(Vector2 dir)
    {
        needNewBlock = false;//para evitar adicionar um bloco na hora errada

        ChangeState(GameState.Moving);//mudando o estado do jogo para movendo blocos

        //criando uma lista ordenada pela posição dos blocos
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();

        //mudando a direção da lista
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        //loop para varrer os blocos
        foreach (var block in orderedBlocks)
        {
            //pegando a referencia do node onde o bloco está
            var next = block.node;

            do
            {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.Pos + dir);

                if (possibleNode != null)//verifica se há um node possível 
                {
                    //verifica se os blocos podem se juntar
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        needNewBlock = true;
                    }
                    //verifica se proximo espaço está ocupado
                    if (possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                        needNewBlock = true;
                    }
                }

            } while (next != block.node);

            block.transform.DOMove(block.node.Pos, travelTime);

        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.mergingBlock != null ? block.mergingBlock.node.Pos : block.node.Pos;

            sequence.Insert(0, block.transform.DOMove(movePoint, travelTime));
        }

        sequence.OnComplete(() =>
            {
                foreach (var block in orderedBlocks.Where(b => b.mergingBlock != null))
                {
                    MergeBlocks(block.mergingBlock, block);
                }

                ChangeState(GameState.SpawningBlocks);
            });
    }

    void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.node, baseBlock.Value * 2);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    bool CheckGameIsOver()
    {

        if (_blocks.Count() == 16)
        {
            var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();

            if (orderedBlocks[0].Value == orderedBlocks[1].Value) return false;
            else if (orderedBlocks[0].Value == orderedBlocks[4].Value) return false;

            else if (orderedBlocks[1].Value == orderedBlocks[5].Value) return false;

            else if (orderedBlocks[2].Value == orderedBlocks[1].Value) return false;
            else if (orderedBlocks[2].Value == orderedBlocks[3].Value) return false;
            else if (orderedBlocks[2].Value == orderedBlocks[6].Value) return false;

            else if (orderedBlocks[3].Value == orderedBlocks[7].Value) return false;

            else if (orderedBlocks[4].Value == orderedBlocks[5].Value) return false;

            else if (orderedBlocks[6].Value == orderedBlocks[5].Value) return false;
            else if (orderedBlocks[6].Value == orderedBlocks[7].Value) return false;

            else if (orderedBlocks[8].Value == orderedBlocks[4].Value) return false;
            else if (orderedBlocks[8].Value == orderedBlocks[9].Value) return false;
            else if (orderedBlocks[8].Value == orderedBlocks[12].Value) return false;

            else if (orderedBlocks[9].Value == orderedBlocks[5].Value) return false;
            else if (orderedBlocks[9].Value == orderedBlocks[13].Value) return false;

            else if (orderedBlocks[10].Value == orderedBlocks[6].Value) return false;
            else if (orderedBlocks[10].Value == orderedBlocks[9].Value) return false;
            else if (orderedBlocks[10].Value == orderedBlocks[11].Value) return false;
            else if (orderedBlocks[10].Value == orderedBlocks[14].Value) return false;

            else if (orderedBlocks[11].Value == orderedBlocks[7].Value) return false;
            else if (orderedBlocks[11].Value == orderedBlocks[15].Value) return false;

            else if (orderedBlocks[12].Value == orderedBlocks[13].Value) return false;

            else if (orderedBlocks[14].Value == orderedBlocks[13].Value) return false;
            else if (orderedBlocks[14].Value == orderedBlocks[15].Value) return false;

            else return true;
        }

        return false;

    }


}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    CheckGameOver,
    Lose
}