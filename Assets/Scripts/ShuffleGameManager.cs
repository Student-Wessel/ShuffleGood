using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShuffleGameManager : NetworkBehaviour
{
    public GameObject topCameraPosition => _topCameraPosition;
    public GameObject bottomCameraPosition => _bottomCameraPosition;

    public GameObject puckParent => _puckParent;
    public PuckSelector topPuckSelector => _topPuckSelector;
    public PuckSelector bottomPuckSelector => _bottomPuckSelector;
    
    private Scene _scene;

    [SerializeField]
    [ReadOnly]
    private ShuffleGameState _gameState = ShuffleGameState.WAITING;

    public ShuffleGameState gameState => _gameState;

    // Parent object where all the pucks are located
    [SerializeField]
    private GameObject _puckParent;

    [SerializeField]
    [ReadOnly]
    private ShufflePlayer _topPlayer, _bottomPlayer;
    
    [SerializeField]
    private PuckSelector _topPuckSelector,_bottomPuckSelector;

    [SerializeField]
    [ReadOnly]
    private List<Puck> _pucks;
    
    [SerializeField]
    [ReadOnly]
    private List<Puck> _pucksOnTopHalf = new List<Puck>(),_pucksOnBottomHalf = new List<Puck>();

    [SerializeField]
    private GameObject _topCameraPosition;

    [SerializeField]
    private GameObject _bottomCameraPosition;

    private bool _topPlayerReady = false, _bottomPlayerReady = false;

    private LayerMask _physicsLayer;
    
    [Server]
    public void InitiateGameOnServer(ShufflePlayer pTopPlayer,ShufflePlayer pBottomPlayer,Scene pScene, LayerMask physicsLayer)
    {
        _gameState = ShuffleGameState.WAITING;
        _physicsLayer = physicsLayer;
        _topPlayerReady = false;
        _bottomPlayerReady = false;
        
        NetworkServer.OnDisconnectedEvent += OnDisconnectedEvent;

        _scene = pScene;
        _topPlayer = pTopPlayer;
        _bottomPlayer = pBottomPlayer;
        
        if (_puckParent == null)
        {
            Debug.LogError("No puck parent game object found");
            return;
        }
        SetupPucks(_puckParent);
        _topPlayer.SetupPlayer(this,true);
        _bottomPlayer.SetupPlayer(this,false);

        foreach (var puck in _pucksOnTopHalf)
        {
            _topPlayer.GivePuckToClient(puck);
        }

        foreach (var puck in _pucksOnBottomHalf)
        {
            _bottomPlayer.GivePuckToClient(puck);
        }
        // Do ready checks for clients
        
        Invoke(nameof(AskIfPlayersAreReady),1f);
    }
    private void AskIfPlayersAreReady()
    {
        _topPlayer.AskIfPlayerIsReady();
        _bottomPlayer.AskIfPlayerIsReady();
    }
    public void PlayerReadyResponse(ShufflePlayer pPlayer,bool pIsReady)
    {
        if (pPlayer == _topPlayer)
        {
            _topPlayerReady = pIsReady;
        }
        else if (pPlayer == _bottomPlayer)
        {
            _bottomPlayerReady = pIsReady;
        }
        if (_topPlayerReady && _bottomPlayerReady)
        {
            _gameState = ShuffleGameState.PLAYING;
            _topPlayer.NotifyClientGameStart();
            _bottomPlayer.NotifyClientGameStart();
        }
    }
    private void OnDisconnectedEvent(NetworkConnectionToClient pConn)
    {
        if (_gameState == ShuffleGameState.END)
            return;
        
        if (_topPlayer.connectionToClient == pConn)
        {
            ForceWinForPlayer(_bottomPlayer);
        }
        else if (_bottomPlayer.connectionToClient == pConn)
        {
            ForceWinForPlayer(_topPlayer);
        }
    }
    private void SetupPucks(GameObject pPuckParent)
    {
        foreach (Transform child in pPuckParent.transform)
        {
            var puck = child.GetComponent<Puck>();
            
            if (puck == null)
            {
                Debug.LogError("GameObject inside puck parent that isn't a puck");
                continue;
            }

            puck.gameObject.layer = _physicsLayer.value;
            puck.gameObject.SetActive(true);
            _pucks.Add(puck);

            if (puck.IsOnTopSide())
            {
                _pucksOnTopHalf.Add(puck);
            }
            else
            {
                _pucksOnBottomHalf.Add(puck);
            }
            puck.SideChange += PuckOnSideChange;
        }
    }
    private void PuckOnSideChange(Puck pPuck,bool pIsTopSide)
    {
        if (pIsTopSide)
        {
            _pucksOnBottomHalf.Remove(pPuck);
            _bottomPlayer.RemovePuckFromPlayer(pPuck);
            
            _pucksOnTopHalf.Add(pPuck);
            _topPlayer.GivePuckToClient(pPuck);
        }
        else
        {
            _pucksOnTopHalf.Remove(pPuck);
            _topPlayer.RemovePuckFromPlayer(pPuck);
            
            _pucksOnBottomHalf.Add(pPuck);
            _bottomPlayer.GivePuckToClient(pPuck);
        }
    }
    public bool IsPuckOnPlayerSide(ShufflePlayer pPlayer,Puck pPuck)
    {
        if (pPlayer == _topPlayer && _pucksOnTopHalf.Contains(pPuck))
        {
            return true;
        }
        if (pPlayer == _bottomPlayer && _pucksOnBottomHalf.Contains(pPuck))
        {
            return true;
        }

        return false;
    }
    private void Update()
    {
        if (!isServer)
            return;

        bool topClear = _pucksOnTopHalf.Count < 1;
        bool bottomClear = _pucksOnBottomHalf.Count < 1;

        if (topClear && bottomClear)
        {
            // This should never happen, wtf
        }
        else if (topClear)
        {
            EndGame(_topPlayer,_bottomPlayer);
        }
        else if (bottomClear)
        {
            EndGame(_bottomPlayer,_topPlayer);
        }
    }
    private void EndGame(ShufflePlayer pWinner,ShufflePlayer pLoser)
    {
        _gameState = ShuffleGameState.END;
        pWinner.EndGameForPlayer(true,"You have beat your opponent");
        pLoser.EndGameForPlayer(false,"Your opponent beat you");
        Invoke(nameof(ShutDownSceneInstance),2f);
    }
    private void ForceWinForPlayer(ShufflePlayer pPlayer, string pWinReason = "Other player left")
    {
        pPlayer.EndGameForPlayer(true,pWinReason);
        Invoke(nameof(ShutDownSceneInstance),2f);
    }
    private void ShutDownSceneInstance()
    {
        ((ShuffleNetworkManager)NetworkManager.singleton).OnGameFinish(this);
    }
}

public enum ShuffleGameState
{
    WAITING,
    PLAYING,
    END
}