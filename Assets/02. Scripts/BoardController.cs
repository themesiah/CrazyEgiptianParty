using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;
using DigitalRuby.Tween;

public class BoardController : MonoBehaviourPunCallbacks, IPunObservable
{
    public static BoardController BoardInstance = null;
    private UIController uiController;
    private Camera mainCamera;

    #region InspectorVariables
    [SerializeField]
    private float minTimeToSpawnPaintItem = 2f;
    [SerializeField]
    private float maxTimeToSpawnPaintItem = 5f;
    [SerializeField]
    private float minTimeToSpawnItem = 2f;
    [SerializeField]
    private float maxTimeToSpawnItem = 5f;
    [SerializeField]
    private GameObject[] itemPrefab;
    [SerializeField]
    private int[] playerStart;
    [SerializeField]
    private Color[] playerColor;
    [SerializeField]
    private float playerY = 1.33f;
    [SerializeField]
    private float itemY = 2.0f;
    [SerializeField]
    private int gameTime = 90;
    [SerializeField]
    float cameraZoomDuration = 5f;
    [SerializeField]
    float cameraRotationDuration = 2f;
    #endregion

    #region PrivateVariables
    private enum ObjectTypes
    {
        PaintObject = 0,
        PaintArea,
        Sneakers,
        MAX
    }
    [SerializeField]
    private PlayerController[] players;
    [SerializeField]
    private GameObject[] squaresArray;
    private Material[] squaresMaterials;
    private int[] squaresPlayer;
    private GameObject[] squaresObjects;
    private int[] squaresObjectTypes;
    private float currentTimer;
    private bool isPlaying = false;
    #endregion

    #region Monocallbacks
    // Use this for initialization
    void Awake()
    {
        players = new PlayerController[4];
        BoardInstance = this;
        uiController = FindObjectOfType<UIController>();
        currentTimer = (float)gameTime;
        mainCamera = Camera.main;
        InitSquares();
    }

    private void Update()
    {
        if (isPlaying)
        {
            currentTimer -= Time.deltaTime;
            uiController.SetTime((int)currentTimer);
            if (PhotonNetwork.IsMasterClient && currentTimer <= 0f)
            {
                EndGame();
            }
        }
    }
    #endregion

    #region PublicApi
    public Vector2Int GetCoordinates(int index)
    {
        int x = index / 10;
        int y = index % 10;
        return new Vector2Int(x, y);
    }

    public int GetIndex(int x, int y)
    {
        return 10 * x + y;
    }

    public Vector3 GetPosition(int index)
    {
        Vector3 pos = squaresArray[index].transform.position;
        pos.y = playerY;
        return pos;
    }

    public Vector3 GetItemPosition(int index)
    {
        Vector3 pos = squaresArray[index].transform.position;
        pos.y = itemY;
        return pos;
    }

    public bool CanMoveThere(int x, int y)
    {
        int index = GetIndex(x, y);
        return !IsOfflimits(x, y) && !Occupied(index);
    }

    public bool Occupied(int index)
    {
        foreach (PlayerController pc in players)
        {
            if (pc != null)
            {
                if (pc.GetCurrentSquare() == index)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsOfflimits(int x, int y)
    {
        return (x < 0 || x > 9 || y < 0 || y > 9);
    }
    #endregion

    #region PrivateApi
    private void EndGame()
    {
        uiController.SetTime(0);
        isPlaying = false;
        // Get winning player
        int maxPlayer = 0;
        int maxPoints = 0;
        foreach (PlayerController pc in players)
        {
            if (pc != null)
            {
                if (pc.points > maxPoints)
                {
                    maxPoints = pc.points;
                    maxPlayer = pc.playerNumber;
                }
            }
        }

        SendWinner(maxPlayer);
        // Stop spawning items
        StopAllCoroutines();
    }

    private void AddPlayer(PlayerController player, int index)
    {
        players[index] = player;
    }

    private void InitPlayers()
    {
        PlayerController[] playerObjects = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < playerObjects.Length; ++i)
        {
            AddPlayer(playerObjects[i], playerObjects[i].playerNumber);
        }
        uiController.SetMaxPlayers(playerObjects.Length);

        for (int i = 0; i < players.Length; ++i)
        {
            if (players[i] != null)
            {
                int ps = playerStart[i];
                Color pc = playerColor[i];
                players[i].SetCurrentSquare(ps);
                players[i].SetColor(pc);
                if (players[i].photonView.IsMine)
                {
                    Vector3 newPos = GetPosition(ps);
                    newPos.y = playerY;
                    players[i].transform.position = newPos;
                }
                uiController.SetPlayerScore(i, 0);
            }
        }
    }

    private void InitPlayersMovement()
    {
        for (int i = 0; i < players.Length; ++i)
        {
            if (players[i] != null)
            {
                if (players[i].photonView.IsMine)
                {
                    players[i].OnStart();
                }
            }
        }
    }

    private void InitSquares()
    {
        squaresMaterials = new Material[squaresArray.Length];
        squaresPlayer = new int[squaresArray.Length];
        squaresObjects = new GameObject[squaresArray.Length];
        squaresObjectTypes = new int[squaresArray.Length];
        for (int i = 0; i < squaresArray.Length; ++i)
        {
            squaresMaterials[i] = squaresArray[i].GetComponent<MeshRenderer>().material;
            squaresPlayer[i] = -1;
        }
    }

    private void ZoomWinner(int player)
    {
        Vector3 position = players[player].transform.position;
        Quaternion rotationStart = mainCamera.transform.rotation;
        mainCamera.transform.LookAt(position);
        Quaternion rotationTarget = mainCamera.transform.rotation;
        System.Action<ITween<Quaternion>> updateCameraRotation = (r) =>
        {
            mainCamera.gameObject.transform.rotation = r.CurrentValue;
        };
        mainCamera.gameObject.Tween("ZoomCameraR", rotationStart, rotationTarget, cameraRotationDuration, TweenScaleFunctions.CubicEaseOut, updateCameraRotation);

        Vector3 positionStart = mainCamera.transform.position;
        Vector3 positionEnd = positionStart + (position - positionStart) / 2f;

        System.Action<ITween<Vector3>> updateCameraPosition = (p) =>
        {
            mainCamera.gameObject.transform.position = p.CurrentValue;
        };
        mainCamera.gameObject.Tween("ZoomCameraP", positionStart, positionEnd, cameraZoomDuration, TweenScaleFunctions.CubicEaseOut, updateCameraPosition);
    }

    private int GetItemRandomSpawnPoint()
    {
        int rand = -1;
        while (rand < 0 || rand > squaresObjects.Length || squaresObjects[rand] != null || Occupied(rand))
        {
            rand = UnityEngine.Random.Range(0, squaresObjects.Length - 1);
        }
        return rand;
    }
    #endregion

    #region RPCCalls
    public void SendPlayerInSquare(int index, int player)
    {
        photonView.RPC("PaintSquare", RpcTarget.All, index, player);
        photonView.RPC("ProcessPlayerInSquare", RpcTarget.All, index, player);
    }

    public void SendStartGame()
    {
        photonView.RPC("StartGame", RpcTarget.All);
    }

    public void SendWinner(int player)
    {
        photonView.RPC("Winner", RpcTarget.All, player);
    }
    #endregion

    #region RPCFunctions
    [PunRPC]
    public void Winner(int player)
    {
        // Stop players
        foreach (PlayerController pc in players)
        {
            if (pc != null)
            {
                pc.StopAllCoroutines();
            }
        }

        // Set "Player {playername} won" in UI
        uiController.SetWinner(players[player].GetName());
        // Camera zoom to player
        ZoomWinner(player);
    }

    [PunRPC]
    public void StartGame()
    {
        InitPlayers();
        uiController.CountTo0(() =>
        {
            InitPlayersMovement();
            if (!PhotonNetwork.IsMasterClient)
            {
                GameObject.Find("WaitingText").SetActive(false);
            }
            else
            {
                StartCoroutine(PaintObjectInstantiationCoroutine());
                StartCoroutine(PowerupsInstantiationCoroutine());
            }
            isPlaying = true;
            uiController.ActivateTimer();
            AudioManager.instance.PlayMusic();
        });
    }

    [PunRPC]
    public void PaintSquare(int index, int player)
    {
        //squaresMaterials[index].color = playerColor[player];
        squaresMaterials[index].SetColor("_EmissionColor", playerColor[player]);
        squaresPlayer[index] = player;
    }

    [PunRPC]
    public void PaintSqaures(int player, byte[] ix)
    {
        int[] indexes = FormatterSerializer.Deserialize<int[]>(ix);
        foreach(int index in indexes)
        {
            squaresMaterials[index].SetColor("_EmissionColor", playerColor[player]);
            squaresPlayer[index] = player;
        }
    }

    [PunRPC]
    public void GotSneakers(int player)
    {
        if (PlayerController.LocalPlayer.playerNumber == player)
        {
            PlayerController.LocalPlayer.GotSneakers();
        }
    }

    [PunRPC]
    public void ProcessPlayerInSquare(int index, int player)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (squaresObjects[index] != null)
            {
                PhotonNetwork.Destroy(squaresObjects[index].GetComponent<PhotonView>());
                switch(squaresObjectTypes[index])
                {
                    case (int)ObjectTypes.PaintObject:
                        int points = 0;
                        for (int i = 0; i < squaresPlayer.Length; ++i)
                        {
                            if (squaresPlayer[i] == player)
                            {
                                squaresPlayer[i] = -1;
                                //squaresMaterials[i].color = Color.white;
                                squaresMaterials[i].SetColor("_EmissionColor", Color.black);
                                points++;
                            }
                        }
                        photonView.RPC("PlayerWonPoints", RpcTarget.All, player, points);
                        break;
                    case (int)ObjectTypes.PaintArea:
                        {
                            List<int> paintArea = new List<int>();
                            Vector2Int coord = GetCoordinates(index);
                            for (int i = 0; i < 10; ++i)
                            {
                                Vector2Int targetCoord = coord;
                                targetCoord.x = i;
                                paintArea.Add(GetIndex(targetCoord.x, targetCoord.y));

                                targetCoord = coord;
                                targetCoord.y = i;
                                paintArea.Add(GetIndex(targetCoord.x, targetCoord.y));
                            }
                            photonView.RPC("PaintSqaures", RpcTarget.All, player, FormatterSerializer.Serialize(paintArea.ToArray()));
                        }
                        break;
                    case (int)ObjectTypes.Sneakers:
                        photonView.RPC("GotSneakers", RpcTarget.All, player);
                        break;
                }
            }
        }
    }

    [PunRPC]
    public void PlayerWonPoints(int player, int points)
    {
        players[player].points += points;
        uiController.SetPlayerScore(player, players[player].points);
    }
    #endregion

    #region Coroutines
    IEnumerator PaintObjectInstantiationCoroutine()
    {
        while (true)
        {
            float timeToSpawnPaintItem = UnityEngine.Random.Range(minTimeToSpawnPaintItem, maxTimeToSpawnPaintItem);
            yield return new WaitForSeconds(timeToSpawnPaintItem);
            int rand = GetItemRandomSpawnPoint();
            Vector3 pos = GetItemPosition(rand);
            squaresObjects[rand] = PhotonNetwork.Instantiate("items/"+itemPrefab[0].name, pos, Quaternion.identity, 0);
            squaresObjectTypes[rand] = 0;
        }
    }

    IEnumerator PowerupsInstantiationCoroutine()
    {
        while (true)
        {
            float timeToSpawnItem = UnityEngine.Random.Range(minTimeToSpawnItem, maxTimeToSpawnItem);
            yield return new WaitForSeconds(timeToSpawnItem);
            int rand = GetItemRandomSpawnPoint();
            int type = UnityEngine.Random.Range(0, (int)ObjectTypes.MAX);
            Vector3 pos = GetItemPosition(rand);
            squaresObjects[rand] = PhotonNetwork.Instantiate("items/" + itemPrefab[type].name, pos, Quaternion.identity, 0);
            squaresObjectTypes[rand] = type;
        }
    }
    #endregion

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

}
