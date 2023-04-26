using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MyBox;

public class ShufflePlayer : NetworkBehaviour
{
    private UIJoystick _joystick;
    private UIManager _uiManager;
    
    private bool _isTopPlayer;
    
    private ShuffleGameManager _gameManager;

    [SerializeField]
    private Vector3 _puckCameraPositionOffset;

    [SerializeField]
    [ReadOnly]
    private PuckSelector _myPuckSelector;

    private List<Puck> _myPuckList = new List<Puck>();
    private Puck _currentSelectedPuck = null;

    #region ClientOnlyMethods

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        _uiManager = FindObjectOfType<UIManager>();
        if (isOwned)
        {
            _joystick = _uiManager.joystick;
            _joystick.JoystickReleasedEvent.AddListener(SendShot);
            _joystick.JoystickAimChangeEvent.AddListener(SendAim);
            _uiManager.SwitchToState(UIState.SEARCHFORGAME);
        }
    }

    [Client]
    private void RespondIfReady()
    {
        CmdPlayerIsReadyAnswer(true);
    }

    [Client]
    private void SendAim(Vector2 pAim)
    {
        if (!isOwned)
            return;

        if (!_isTopPlayer)
            pAim = -pAim;

        _myPuckSelector.SetAim(pAim);
    }
    
    [Client]
    private void SendShot(Vector2 pAim)
    {
        if (!isOwned)
            return;

        if (_myPuckSelector.IsTransitioning) // This is a client side check, so potentially hackers could abuse this and get around it, but it's not worth the time to fix it;
            return;

        if (!_isTopPlayer)
            pAim = -pAim;
        
        if (_myPuckList.Count > 0)
        {
            var puck = _myPuckList[0];
            CmdShotFromClient(pAim,puck.netId);
            _myPuckList.RemoveAt(0);
            _myPuckList.Add(puck);

            // This is the puck we just shot, we don't want the puck selector to instantly go there, so we wait
            if (puck == _myPuckList[0])
            {
                _myPuckSelector.UnselectPuck();
                Invoke(nameof(SetFirstPuckToPuckSelector),0.5f);
            }
            else
            {
                SetFirstPuckToPuckSelector();
            }
        }
    }
    
    [Client]
    private void SetFirstPuckToPuckSelector()
    {
        if (!isOwned)
            return;
        
        if (_myPuckList.Count > 0)
        {
            _myPuckSelector.SetNewFollowTarget(_myPuckList[0].transform);
        }
    }
    
    [Client]
    private void SetPuckSelectors()
    {
        if (!isOwned)
            return;

        if (_isTopPlayer)
        {
            _myPuckSelector = _gameManager.topPuckSelector;
            _gameManager.bottomPuckSelector.gameObject.SetActive(false);
        }
        else
        {
            _myPuckSelector = _gameManager.bottomPuckSelector;
            _gameManager.topPuckSelector.gameObject.SetActive(false);
        }
        
        _myPuckSelector.gameObject.SetActive(true);
    }

    [Client]
    private void OnGameEnd(bool pDidWin, string pEndMessage)
    {
        NetworkClient.Disconnect();
        
        // Check if gameScene gets unloaded
        if (pDidWin)
        {
            _uiManager.SwitchToState(UIState.WINPANEL);
        }
        else
        {
            _uiManager.SwitchToState(UIState.LOSEPANEL);
        }
    }

    #endregion
    
    #region ServerOnlyMethods

    [Server]
    public void SetupPlayer(ShuffleGameManager pManager,bool pIsTopPlayer)
    {
        _gameManager = pManager;
        _isTopPlayer = pIsTopPlayer;
        RpcClientSetupPlayer(pIsTopPlayer,pManager.netIdentity);
    }
    
    [Server]
    public void GivePuckToClient(Puck pPuck)
    {
        RpcGivePuckToClient(pPuck.netId);
    }
    
    [Server]
    public void RemovePuckFromPlayer(Puck pPuck)
    {
        RpcRemovePuckFromClient(pPuck.netId);
    }

    [Server]
    public void EndGameForPlayer(bool pDidWin,string pEndMessage)
    {
        RpcGameEndMessage(pDidWin, pEndMessage);
    }

    [Server]
    public void AskIfPlayerIsReady()
    {
        RpcIsPlayerReady();
    }

    [Server]
    private void PlayerReadyResponse(bool pIsReady)
    {
        _gameManager.PlayerReadyResponse(this,pIsReady);
    }

    [Server]
    public void NotifyClientGameStart()
    {
        RpcGameHasStarted();
    }

    #endregion
    
    #region ClientToServerCall

    [Command]
    private void CmdShotFromClient(Vector2 pAim, NetworkIdentity pPuckIdentity)
    {
        if (_gameManager.gameState != ShuffleGameState.PLAYING)
            return;
        
        var puck = pPuckIdentity.GetComponent<Puck>();
        if (_gameManager.IsPuckOnPlayerSide(this, puck)) // We can shoot it
        {
            puck.AddForce(pAim,ForceMode2D.Impulse);
        }
    }

    [Command]
    private void CmdPlayerIsReadyAnswer(bool pIsReady)
    {
        PlayerReadyResponse(pIsReady);
    }
    
    #endregion

    #region ServerToClientCall

    [ClientRpc]
    private void RpcIsPlayerReady()
    {
        if (!isOwned)
            return;
        _uiManager.SwitchToState(UIState.WAITFOROPPONENT);
        CmdPlayerIsReadyAnswer(true);
    }

    [ClientRpc]
    private void RpcGameHasStarted()
    {
        if (!isOwned)
            return;
        
        _uiManager.SwitchToState(UIState.JOYSTICK);
    }
    
    [ClientRpc]
    private void RpcClientSetupPlayer(bool pIsTopPlayer, NetworkIdentity pShuffleGameManager)
    {
        _gameManager = pShuffleGameManager.GetComponent<ShuffleGameManager>();
        _isTopPlayer = pIsTopPlayer!;
        
        if (!isOwned)
            return;

        if (_isTopPlayer)
        {
            Camera.main.transform.position = _gameManager.topCameraPosition.transform.position;
            Camera.main.transform.rotation = _gameManager.topCameraPosition.transform.rotation;
        }
        else
        {
            Camera.main.transform.position = _gameManager.bottomCameraPosition.transform.position;
            Camera.main.transform.rotation = _gameManager.bottomCameraPosition.transform.rotation;
        }

        foreach (Transform puckTransform in _gameManager.puckParent.transform)
        {
            puckTransform.gameObject.SetActive(true);
        }

        SetPuckSelectors();
    }

    [ClientRpc]
    private void RpcGivePuckToClient(NetworkIdentity pPuckIdentity)
    {
        if (!isOwned)
            return;
        
        Puck puck = pPuckIdentity.GetComponent<Puck>();

        if (_myPuckList.Contains(puck))
        {
            Debug.Log("Something went wrong, server gave us a puck we already have");
            return;
        }
        _myPuckList.Add(puck);
    }

    [ClientRpc]
    private void RpcRemovePuckFromClient(NetworkIdentity pPuckIdentity)
    {
        if (!isOwned)
            return;
        
        Puck puck = pPuckIdentity.GetComponent<Puck>();
        _myPuckList.Remove(puck);
        
        if (_myPuckList.Count < 1)
        {
            _currentSelectedPuck = null;
            _myPuckSelector.UnselectPuck();
        }
    }

    [ClientRpc]
    public void RpcGameEndMessage(bool pDidWin, string pEndMessage)
    {
        if (!isOwned)
            return;
        
        OnGameEnd(pDidWin,pEndMessage);
    }

    #endregion
    
    private void Update()
    {
        if (isOwned)
        {
            if (_currentSelectedPuck == null)
            {
                if (_myPuckList.Count > 0)
                {
                    _myPuckSelector.SetNewFollowTarget(_myPuckList[0].transform);
                    _currentSelectedPuck = _myPuckList[0];
                }
            }

            if (_isTopPlayer)
            {
                Vector3 flippedPositionOffset = new Vector3(-_puckCameraPositionOffset.x, -_puckCameraPositionOffset.y, _puckCameraPositionOffset.z);
                Camera.main.transform.position = _myPuckSelector.transform.position + flippedPositionOffset;
                Camera.main.transform.rotation = _gameManager.topCameraPosition.transform.rotation;
            }
            else
            {
                Camera.main.transform.position = _myPuckSelector.transform.position + _puckCameraPositionOffset;
            }
            
        }
    }
}
