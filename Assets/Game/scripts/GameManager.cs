using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    //tamanho do campo
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;

    //ToastFactory
    [SerializeField] private ToastFactory toastFactory;

    //prefabs
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;

    //lista de tipos de blocos que vão existir no jogo
    [SerializeField] private List<BlockType> _types;

    //velocidade da movimentação dos blocos
    [SerializeField] private float travelTime = 0.2f;

    //referencias da interface
    [SerializeField] private TextMeshProUGUI _pointsText;
    [SerializeField] private TextMeshProUGUI _movesText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private GameObject _gameOverUI;
    [SerializeField] private GameObject _gameMenu;
    [SerializeField] private GameObject _settingsMenu;
    [SerializeField] private GameObject _newGameConfirm;
    [SerializeField] private GameObject _soundOnButton;
    [SerializeField] private GameObject _soundOffButton;
    [SerializeField] private GameObject _saveLoadWindow;

    [SerializeField] private AudioManager audioManager;

    //variável do contador de pontos
    private int _maxValue;
    private int _score;

    //listas
    private List<Node> _nodes;
    private List<Block> _blocks;


    //controlador do estado do jogo
    private GameState _state;

    //pause do jogo
    public bool _gamePause;

    //variável para marcar se é o começo do jogo
    private int _round;

    //variável que indica a necessidade de spawnar novos blocos
    private bool needNewBlock;

    //variáveis de som
    public bool soundIsOn;
    private bool playMergeSound;

    //histórico
    private List<History> _blocksHistory;
    private int _scoreHistory;
    private bool wasUndo;
    private int previousBlocksAmount;

    //para setar os valores de acordo com o valor correto
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);

    void Awake()//tentando resolver o problema da proporção
    {
        if (Screen.height / Screen.width > 2.222f)//proporção de 9:20
        {
            Camera.main.aspect = 9f / 20f;
        }
        else if (Screen.height / Screen.width > 2f)//proporção de 9:18
        {
            Camera.main.aspect = 9f / 18f;
        }
        else//proporção de 9/16 -- 1.777
        {
            Camera.main.aspect = 9f / 16f;
        }
    }

    void Start()
    {
        //ChangeState(GameState.GenerateLevel);
        ChangeState(GameState.LoadData);
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;

        switch (newState)
        {
            case GameState.LoadData:

                if (!PlayerPrefs.HasKey("score") || PlayerPrefs.GetInt("score") == -1) //sem dados
                    ChangeState(GameState.GenerateLevel);

                else
                    LoadData();

                break;

            case GameState.GenerateLevel:
                GenerateGrid();
                break;

            case GameState.SpawningBlocks:
                if (needNewBlock)
                {
                    _movesText.text = "Jogadas: " + _round;
                    SpawnBlocks(_round++ == 0 ? 2 : 1);
                }

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

            case GameState.Lose://game over
                _gameOverUI.SetActive(true);
                break;

            default:
                break;
        }
    }

    //esse método vai ser responsável por criar o tabuleiro
    private void GenerateGrid()
    {
        //marcando que é o começo do jogo
        _round = 0;
        _maxValue = 0;
        _score = 0;

        _gamePause = false;

        //interface
        _scoreText.text = "0";

        //indicando a necessidade de spawn dos blocos
        needNewBlock = true;

        //carregando configuração de som
        if (PlayerPrefs.HasKey("soundIsOn"))
        {
            if (PlayerPrefs.GetInt("soundIsOn") == 1) TurnOnSound();
            else TurnOffSound();
        }
        else
            soundIsOn = true;

        //carregando volume
        if (PlayerPrefs.HasKey("soundVol"))
            audioManager.LoadVol(PlayerPrefs.GetFloat("soundVol"));
        else
            audioManager.LoadVol(100f);

        //iniciando as listas
        _nodes = new List<Node>();//lista de nodes
        _blocks = new List<Block>();//lista de blocos

        _blocksHistory = new List<History>();

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

    private void SpawnBlock(Node node, int value)
    {
        var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);//instanciando o novo bloco
        block.Init(GetBlockTypeByValue(value));//chamando o método dentro do bloco para passar o valor dele
        block.SetBlock(node);//passando o node onde o bloco está
        _blocks.Add(block);//adicionando o bloco a lista

        //pontuação
        _maxValue = value > _maxValue ? value : _maxValue;
        _pointsText.text = _maxValue.ToString();
    }

    //método para processar e movimentar os blocos
    public void Shift(Vector2 dir)
    {
        needNewBlock = false;//para evitar adicionar um bloco na hora errada
        playMergeSound = false;

        //salvando o score
        _scoreHistory = _score;

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
                        //criando histórico
                        CreateHistory();

                        block.MergeBlock(possibleNode.OccupiedBlock);
                        needNewBlock = true;

                        if (soundIsOn)
                            audioManager.playMoveSfx();

                        playMergeSound = true;

                        //contabilizando pontos
                        _score += (block.Value * 2);

                        //interface
                        //_scoreText.text = _score.ToString();
                        WriteScore(_score);
                    }
                    //verifica se proximo espaço está ocupado
                    if (possibleNode.OccupiedBlock == null)
                    {
                        //criando histórico
                        CreateHistory();

                        next = possibleNode;
                        needNewBlock = true;

                        if (soundIsOn)
                            audioManager.playMoveSfx();
                    }
                }

            } while (next != block.node);

            block.transform.DOMove(block.node.Pos, travelTime);
        }

        //DOTween
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

                if (soundIsOn && playMergeSound)
                    audioManager.playMergeSfx();

                ChangeState(GameState.SpawningBlocks);
            });
    }

    //método para escrever na interface o valor do score de forma reduzida
    private void WriteScore(int value)
    {
        if (value < 10000)
            _scoreText.text = _score.ToString();

        else
        {
            if (value < 1000000)
                _scoreText.text = Math.Round((float)_score / 1000, 1) + "K";

                else
                _scoreText.text = Math.Round((float)_score / 1000000, 1) + "M";
        }
    }

    //método que vai criar o histórico para permitir a função de voltar
    private void CreateHistory()
    {
        //loop para salvar o estado atual dos blocos
        for (var x = 0; x < _blocks.Count; x++)
        {
            if (_blocksHistory.Count < _blocks.Count)
            {
                History h = new History(_blocks[x].Value, _blocks[x].Pos);
                _blocksHistory.Add(h);
            }
            else
                _blocksHistory[x].SetValues(_blocks[x].Value, _blocks[x].Pos);
        }

        //sinalizando que tem histórico para voltar
        wasUndo = false;

        //guardando a quantidade de blocos atual
        previousBlocksAmount = _blocks.Count;
    }

    //método que vai "juntar" os blocos
    private void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.node, baseBlock.Value * 2);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    //método que vai deletar os blocos
    private void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    private Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    private bool CheckGameIsOver()
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

    //métodos de save e load
    public void SaveSettings()
    {
        //salvando se o som está ligado ou não
        PlayerPrefs.SetInt("soundIsOn", soundIsOn ? 1 : 0);

        //salvando o volume
        PlayerPrefs.SetFloat("soundVol", audioManager.GetMasterVol());
    }

    private void SaveData()
    {
        if (CheckGameIsOver()) return;

        PlayerPrefs.SetInt("score", _score);
        PlayerPrefs.SetInt("rounds", _round);

        PlayerPrefs.SetInt("blocksIndex", _blocks.Count);

        for (var x = 0; x < _blocks.Count; x++)
        {
            PlayerPrefs.SetInt("valorB" + x, _blocks[x].Value);
            PlayerPrefs.SetFloat("xB" + x, _blocks[x].Pos.x);
            PlayerPrefs.SetFloat("yB" + x, _blocks[x].Pos.y);
        }

    }

    private void LoadData()
    {
        _gamePause = false;
        needNewBlock = true;

        wasUndo = true;//para evitar bug

        //som
        if (PlayerPrefs.GetInt("soundIsOn") == 1) TurnOnSound();
        else TurnOffSound();

        //volume
        if (PlayerPrefs.HasKey("soundVol"))
            audioManager.LoadVol(PlayerPrefs.GetFloat("soundVol"));

        //iniciando as listas
        _nodes = new List<Node>();//lista de nodes
        _blocks = new List<Block>();//lista de blocos
        _blocksHistory = new List<History>();


        _score = PlayerPrefs.GetInt("score");
        _round = PlayerPrefs.GetInt("rounds");

        _pointsText.text = "0";

        //_scoreText.text = _score.ToString();
        WriteScore(_score);
        _movesText.text = "Jogadas: " + (_round - 1);

        var index = PlayerPrefs.GetInt("blocksIndex");

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
        ChangeState(GameState.WaitingInput);

        //spawnando os blocos
        for (var x = 0; x < index; x++)
        {
            Vector2 vec = new Vector2(PlayerPrefs.GetFloat("xB" + x), PlayerPrefs.GetFloat("yB" + x));//criando o vector 2
            SpawnBlock(_nodes.Find(n => n.Pos == vec), PlayerPrefs.GetInt("valorB" + x));
        }
    }

    //métodos para serem chamados pelos menus
    public void ResetGame()
    {
        //resetando os blocos
        for (var x = _blocks.Count; x > 0; x--)
        {
            Destroy(_blocks[x - 1].gameObject);
            _blocks.Remove(_blocks[x - 1]);
        }

        //resetando os nodes
        foreach (var x in _nodes)
        {
            x.resetNode();
        }

        //tirando a ui de game over da tela
        _gameOverUI.SetActive(false);

        //tirando a ui de menu da tela
        _gameMenu.SetActive(false);
        _gamePause = false;

        //reiniciando valores
        _round = 0;
        _maxValue = 0;
        _score = 0;
        needNewBlock = true;

        //interface
        _pointsText.text = "0";
        _movesText.text = "0";
        _scoreText.text = "0";

        //resetando o GameState
        ChangeState(GameState.SpawningBlocks);
    }

    public void MenuButton(bool Value)
    {
        _gamePause = Value;
        _gameMenu.SetActive(Value);
    }

    public void SettingsMenu(bool Value)
    {
        _settingsMenu.SetActive(Value);
        _gameMenu.SetActive(!Value);
    }

    public void NewGameShowWindow()
    {
        _newGameConfirm.SetActive(true);
    }

    public void SaveLoadShowWindow(bool Value)
    {
        _saveLoadWindow.SetActive(Value);
    }

    public void NewGameConfirm(bool Value)
    {
        if (Value) ResetGame();

        _newGameConfirm.SetActive(false);
    }

    public void TurnOffSound()
    {
        soundIsOn = false;
        _soundOnButton.SetActive(false);
        _soundOffButton.SetActive(true);
    }

    public void TurnOnSound()
    {
        soundIsOn = true;
        _soundOnButton.SetActive(true);
        _soundOffButton.SetActive(false);
    }

    public void SaveGame()
    {
        SaveData();

        toastFactory.SendToastyToast("Jogo Salvo!");
    }

    public void DeleteData()
    {
        PlayerPrefs.SetInt("score", -1);

        toastFactory.SendToastyToast("Dados Deletados!");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("score") || PlayerPrefs.GetInt("score") == -1) //sem dados
        {
            toastFactory.SendToastyToast("Sem Dados Salvos!");
            return;
        }

        wasUndo = true;//sinaliza que já houve um voltar

        //quantidade de vezes que o loop deve rodar para recolocar os blocos
        int size = previousBlocksAmount;

        //resetando os blocos
        for (var x = _blocks.Count; x > 0; x--)
        {
            Destroy(_blocks[x - 1].gameObject);
            _blocks.Remove(_blocks[x - 1]);
        }

        //resetando os nodes
        foreach (var x in _nodes)
        {
            x.resetNode();
        }

        _score = PlayerPrefs.GetInt("score");
        _round = PlayerPrefs.GetInt("rounds");
        _maxValue = 0;

        _pointsText.text = "0";

        //_scoreText.text = _score.ToString();
        WriteScore(_score);
        _movesText.text = "Jogadas: " + (_round - 1);

        var index = PlayerPrefs.GetInt("blocksIndex");

        //spawnando os blocos
        for (var x = 0; x < index; x++)
        {
            Vector2 vec = new Vector2(PlayerPrefs.GetFloat("xB" + x), PlayerPrefs.GetFloat("yB" + x));//criando o vector 2
            SpawnBlock(_nodes.Find(n => n.Pos == vec), PlayerPrefs.GetInt("valorB" + x));
        }

        _gameOverUI.SetActive(false);

    }

    public void Undo()
    {
        //se for o primeiro turno
        if (_round < 2) return;

        //se já houve um voltar
        if (wasUndo) return;

        //se o jogo estiver pausado
        if (_gamePause) return;

        wasUndo = true;//sinaliza que já houve um voltar

        //quantidade de vezes que o loop deve rodar para recolocar os blocos
        int size = previousBlocksAmount;

        //resetando os blocos
        for (var x = _blocks.Count; x > 0; x--)
        {
            Destroy(_blocks[x - 1].gameObject);
            _blocks.Remove(_blocks[x - 1]);
        }

        //resetando os nodes
        foreach (var x in _nodes)
        {
            x.resetNode();
        }

        //reiniciando valores
        _round--;
        _maxValue = 0;
        _score = _scoreHistory;

        //re-colocando os blocos no tabuleiro
        for (var x = size; x > 0; x--)
        {
            SpawnBlock(_nodes.Find(n => n.Pos == _blocksHistory[x - 1]._pos), _blocksHistory[x - 1]._value);
        }

        //interface
        _movesText.text = "Jogadas: " + (_round - 1);
        //_scoreText.text = _score.ToString();
        WriteScore(_score);
    }
}

public class History
{
    public int _value;
    public Vector2 _pos;

    public History(int value, Vector2 pos)
    {
        _value = value;
        _pos = pos;
    }

    public void SetValues(int value, Vector2 pos)
    {
        _value = value;
        _pos = pos;
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
    Lose,
    LoadData
}