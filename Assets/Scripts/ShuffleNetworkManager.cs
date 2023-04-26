using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShuffleNetworkManager : NetworkManager
{
    [SerializeField]
    private int _instancesCount = 5;

    [Scene]
    [SerializeField]
    private string _gameScene;

    private readonly List<Scene> _gameScenes = new List<Scene>();
    private List<NetworkConnectionToClient> _waitingPlayers = new List<NetworkConnectionToClient>();
    private Dictionary<ShuffleGameManager, Scene> _managerSceneMap = new Dictionary<ShuffleGameManager, Scene>();
    private Dictionary<NetworkConnectionToClient, ShuffleGameManager> _playersInGame = new Dictionary<NetworkConnectionToClient, ShuffleGameManager>();
    
    private List<LayerMask> _availableLayers = new List<LayerMask>();
    private Dictionary<ShuffleGameManager, LayerMask> _assignedLayerMasks = new Dictionary<ShuffleGameManager, LayerMask>();

    #region Start & Stop Callbacks

    public override void OnStartServer()
    {
        for (int i = 0; i < 10; i++)
        {
            _availableLayers.Add(LayerMask.NameToLayer("GameScene_" + (i+1)));
        }
    }

    // We're additively loading scenes, so GetSceneAt(0) will return the main "container" scene,
    // therefore we start the index at one and loop through instances value inclusively.
    // If instances is zero, the loop is bypassed entirely.
    IEnumerator ServerLoadSubScenesAsync()
    {
        for (int index = 0; index < _instancesCount; index++)
        {
            yield return SceneManager.LoadSceneAsync(_gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
            
            Scene newScene = SceneManager.GetSceneAt(SceneManager.loadedSceneCount - 1);
            _gameScenes.Add(newScene);
        }
    }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer()
    {
        NetworkServer.SendToAll(new SceneMessage
            { sceneName = _gameScene, sceneOperation = SceneOperation.UnloadAdditive });
        StartCoroutine(ServerUnloadSubScenes());
    }

    // Unload the subScenes and unused assets and clear the subScenes list.
    IEnumerator ServerUnloadSubScenes()
    {
        for (int index = 0; index < _gameScenes.Count; index++)
            yield return SceneManager.UnloadSceneAsync(_gameScenes[index]);

        _gameScenes.Clear();
        
        yield return Resources.UnloadUnusedAssets();
    }

    IEnumerator UnloadSceneForGameEnd(ShuffleGameManager pGameManager)
    {
        if (_managerSceneMap.ContainsKey(pGameManager))
        {
            var sceneToUnload = _managerSceneMap[pGameManager];
            _managerSceneMap.Remove(pGameManager);
            _gameScenes.Remove(sceneToUnload);
            yield return SceneManager.UnloadSceneAsync(sceneToUnload);
        }
        yield break;
    }

    public void OnGameFinish(ShuffleGameManager pGameManager)
    {
        StartCoroutine(UnloadSceneForGameEnd(pGameManager));
    }
    
    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient()
    {
        // make sure we're not in host mode
        if (mode == NetworkManagerMode.ClientOnly)
            StartCoroutine(ClientUnloadSubScenes());
    }

    // Unload all but the active scene, which is the "container" scene
    IEnumerator ClientUnloadSubScenes()
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }
    }

    #endregion
    
    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a client adds a new player with NetworkClient.AddPlayer. 
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        _waitingPlayers.Add(conn);
        if (_waitingPlayers.Count > 1)
        {
            StartCoroutine(ServerStartMatch(_waitingPlayers[0],_waitingPlayers[1]));
            _waitingPlayers.RemoveAt(0);
            _waitingPlayers.RemoveAt(0);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        _waitingPlayers.Remove(conn);
        _playersInGame.Remove(conn);
    }

    // This delay is mostly for the host player that loads too fast for the
    // server to have subscenes async loaded from OnStartServer ahead of it.
    IEnumerator ServerStartMatch(NetworkConnectionToClient pPlayerOne,NetworkConnectionToClient pPlayerTwo)
    {
        yield return SceneManager.LoadSceneAsync(_gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        Scene gameScene = SceneManager.GetSceneAt(SceneManager.loadedSceneCount - 1);
        LayerMask physicsScene = _availableLayers[0];
        _gameScenes.Add(gameScene);

        GameObject gameManagerObj = null;
        ShuffleGameManager gameManager = null;
        foreach (GameObject rootGameObject in gameScene.GetRootGameObjects())
        {
            gameManager = rootGameObject.GetComponent<ShuffleGameManager>();
            if (gameManager != null)
            {
                gameManagerObj = gameManager.gameObject;    
                break;
            }
        }

        if (gameManager == null)
        {
            Debug.LogError("Game manager prefab does not contain ShuffleGameManager component");
            NetworkServer.DisconnectAll();
            NetworkServer.Shutdown();
            yield break;
        }
        
        NetworkServer.Spawn(gameManagerObj);
        SceneManager.MoveGameObjectToScene(gameManagerObj,gameScene);

        // Send Scene message to client to additively load the game scene
        
        pPlayerOne.Send(new SceneMessage { sceneName = _gameScene, sceneOperation = SceneOperation.LoadAdditive });
        pPlayerTwo.Send(new SceneMessage { sceneName = _gameScene, sceneOperation = SceneOperation.LoadAdditive });

        // Wait for end of frame before adding the player to ensure Scene Message goes first
        yield return new WaitForEndOfFrame();

        base.OnServerAddPlayer(pPlayerOne);
        base.OnServerAddPlayer(pPlayerTwo);

        ShufflePlayer shufflePlayerTop = pPlayerOne.identity.GetComponent<ShufflePlayer>();
        ShufflePlayer shufflePlayerBottom = pPlayerTwo.identity.GetComponent<ShufflePlayer>();

        if (shufflePlayerTop == null || shufflePlayerBottom == null)
        {
            Debug.LogError("No shuffle player component found");
            yield break;
        }

        // Do this only on server, not on clients
        // This is what allows the NetworkSceneChecker on player and scene objects
        // to isolate matches per scene instance on server.
        SceneManager.MoveGameObjectToScene(shufflePlayerTop.gameObject, gameScene);
        SceneManager.MoveGameObjectToScene(shufflePlayerBottom.gameObject, gameScene);
        
        gameManager.InitiateGameOnServer(shufflePlayerTop,shufflePlayerBottom,gameScene,physicsScene);
        
        _managerSceneMap.Add(gameManager,gameScene);
        _playersInGame.Add(pPlayerOne,gameManager);
        _playersInGame.Add(pPlayerTwo,gameManager);
        
        _availableLayers.Remove(physicsScene);
        _availableLayers.Add(physicsScene);
    }

    #endregion
}