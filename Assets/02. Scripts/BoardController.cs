using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using DigitalRuby.Tween;
using System.Linq;

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
    private float itemYappear = 20.0f;
    [SerializeField]
    private float itemDropTime = 2.0f;
    [SerializeField]
    private int gameTime = 90;
    [SerializeField]
    private float cameraZoomDuration = 5f;
    [SerializeField]
    private float cameraRotationDuration = 2f;
    [SerializeField]
    private Transform sobekPosition;
    [SerializeField]
    private GameObject sobekPrefab;
    [SerializeField]
    private Transform mummyPosition;
    [SerializeField]
    private GameObject mummyPrefab;
    [SerializeField]
    private GameObject sarcophagusPrefab;
    [SerializeField]
    private SarcophagusController sarcophagusController;
    [SerializeField]
    private Transform sobekTargetPosition;
    [SerializeField]
    private float timeForSobekMovement = 30f;
    [SerializeField]
    private GameObject sobekBotPrefab;
    [SerializeField]
    private Transform sobekBotSpawn;
    [SerializeField]
    private Transform sobekBotBeginPosition;
    [SerializeField]
    private GameObject pointParticlesPrefab;
    [SerializeField]
    private GameObject[] victoryChars;
    #endregion

    #region PrivateVariables
    private enum ObjectTypes
    {
        PaintObject = 0,
        PaintArea,
        Sneakers,
        PaintObjectx2,
        Swap,
        Chancla,
        MAX
    }
    private int quantityOfPlayers;
    [SerializeField]
    private PlayerController[] players;
    [SerializeField]
    private GameObject[] squaresArray;
    private Material[] squaresMaterials;
    private int[] squaresPlayer;
    private GameObject[] squaresObjects;
    private int[] squaresObjectTypes;
    private ParticleSystem[] pointParticles;
    private float currentTimer;
    private bool isPlaying = false;
    private GameObject sobek;
    private GameObject mummy;
    private GameObject sarcophagus;
    private SobekInitController sobekInitController;
    private SobekController sobekController;
    private int[] results;
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
        pos.y = itemYappear;
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
        if (SobekController.instance != null && SobekController.instance.GetCurrentSquare() == index)
        {
            return true;
        }
        return false;
    }

    public bool IsOfflimits(int x, int y)
    {
        return (x < 0 || x > 9 || y < 0 || y > 9);
    }

    public void SpawnDynamicNPCs()
    {
        sobek = PhotonNetwork.Instantiate("dynamic/" + sobekPrefab.name, sobekPosition.position, sobekPosition.rotation, 0);
        mummy = PhotonNetwork.Instantiate("dynamic/" + mummyPrefab.name, mummyPosition.position, mummyPosition.rotation, 0);
        sobekInitController = sobek.GetComponent<SobekInitController>();
    }
    #endregion

    #region PrivateApi
    private void FadeIn()
    {
        CameraFade cf = gameObject.AddComponent<CameraFade>();
        cf.SetCallback(() => {
            FadeOut();
        });
        cf.StartFade(new Color(0f, 0f, 0f, 1f), 3f);
    }

    private void FadeOut()
    {
        foreach (PlayerController pc in players)
        {
            if (pc != null)
            {
                if (pc.photonView.IsMine)
                {
                    PhotonNetwork.Destroy(pc.photonView);
                }
            }
        }
        if (SobekController.instance != null)
        {
            if (SobekController.instance.photonView.IsMine)
            {
                PhotonNetwork.Destroy(SobekController.instance.photonView);
            }
        }
        MummyController.instance.GetComponent<Animator>().SetTrigger("stop");
        if (PhotonNetwork.IsMasterClient == true)
        {
            foreach (GameObject go in squaresObjects)
            {
                if (go != null)
                {
                    PhotonNetwork.Destroy(go);
                }
            }
        }

        for (int i = 0; i < quantityOfPlayers; ++i)
        {
            victoryChars[i].gameObject.SetActive(true);
        }
        if (quantityOfPlayers > 0)
        {
            victoryChars[results[0]].GetComponent<Animator>().SetInteger("result", 1);
        }
        if (quantityOfPlayers > 1)
        {
            victoryChars[results[1]].GetComponent<Animator>().SetInteger("result", 2);
        }
        if (quantityOfPlayers > 2)
        {
            victoryChars[results[2]].GetComponent<Animator>().SetInteger("result", 3);
        }
        if (quantityOfPlayers > 3)
        {
            victoryChars[results[3]].GetComponent<Animator>().SetInteger("result", 4);
        }
        foreach(GameObject ch in victoryChars)
        {
            if (ch.activeSelf)
            {
                ch.GetComponent<Animator>().SetTrigger("do");
            }
        }

        CameraFade cf = gameObject.AddComponent<CameraFade>();
        cf.SetCallback(() => {
            uiController.End();
        });
        cf.SetScreenOverlayColor(new Color(0f, 0f, 0f, 1f));
        cf.StartFade(new Color(0f,0f,0f,0f), 3f);
        if (results.Length > 0)
        {
            ZoomWinner(results[0]);
        }
    }

    private void EndGame()
    {
        SendWinner();
        // Stop spawning items
        StopAllCoroutines();
    }

    private void AddPlayer(PlayerController player, int index)
    {
        players[index] = player;
    }

    private int GetNextPlayer(int player)
    {
        return (player + 1) % quantityOfPlayers;
    }

    private void InitPlayers()
    {
        PlayerController[] playerObjects = FindObjectsOfType<PlayerController>();
        quantityOfPlayers = playerObjects.Length;
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
        pointParticles = new ParticleSystem[squaresArray.Length];
        for (int i = 0; i < squaresArray.Length; ++i)
        {
            squaresMaterials[i] = squaresArray[i].GetComponent<MeshRenderer>().material;
            squaresPlayer[i] = -1;
            GameObject particleObject = Instantiate(pointParticlesPrefab, squaresArray[i].transform.position, pointParticlesPrefab.transform.rotation);
            pointParticles[i] = particleObject.GetComponent<ParticleSystem>();
        }
    }

    private void ZoomWinner(int player)
    {
        Vector3 position = victoryChars[player].transform.position;
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

    private void SpawnSobekBot()
    {
        GameObject sobekBot = PhotonNetwork.Instantiate("dynamic/" + sobekBotPrefab.name, sobekBotBeginPosition.position, Quaternion.identity, 0);
        sobekController = sobekBot.GetComponent<SobekController>();
        sobekController.Begin(sobekBotBeginPosition.position);
    }
    #endregion

    #region RPCCalls
    public void SendPlayerInSquare(int index, int player)
    {
        photonView.RPC("PaintSquare", RpcTarget.All, index, player);
        photonView.RPC("ProcessPlayerInSquare", RpcTarget.All, index, player);
    }

    public void SendSobekInSquare(int index)
    {
        photonView.RPC("ResetSquare", RpcTarget.All, index);
        photonView.RPC("ProcessSobekInSquare", RpcTarget.All, index);
    }

    public void SendStartGame()
    {
        photonView.RPC("StartGame", RpcTarget.All);
    }

    public void SendWinner()
    {
        photonView.RPC("Winner", RpcTarget.All);
    }
    #endregion

    #region RPCFunctions
    [PunRPC]
    public void Winner()
    {
        uiController.SetTime(0);
        isPlaying = false;
        // Stop players
        PlayerController[] pcs = new PlayerController[quantityOfPlayers];
        for (int i = 0; i < players.Length; ++i)
        {
            if (players[i] != null)
            {
                pcs[i] = players[i];
            }
        }
        PlayerController[] ordered = pcs.OrderBy(pc => pc.points).Reverse().ToArray();
        results = new int[ordered.Length];
        for(int i = 0; i < ordered.Length; ++i)
        {
            results[i] = ordered[i].playerNumber;
        }

        foreach (PlayerController pc in players)
        {
            if (pc != null)
            {
                pc.StopAllCoroutines();
            }
        }
        if (SobekController.instance != null)
        {
            SobekController.instance.StopAllCoroutines();
        }

        /*// Set "Player {playername} won" in UI
        uiController.SetWinner(players[player].GetName());
        // Camera zoom to player
        ZoomWinner(player);*/

        FadeIn();
    }

    [PunRPC]
    public void StartGame()
    {
        InitPlayers();
        MummyController.instance.PlayAnimation();
        sarcophagusController.Open(() => {
            if (!PhotonNetwork.IsMasterClient)
            {
                uiController.DeactivateWaitingText();
            }
            uiController.CountTo0(() =>
            {
                InitPlayersMovement();
                if (PhotonNetwork.IsMasterClient)
                {
                    StartCoroutine(PaintObjectInstantiationCoroutine());
                    StartCoroutine(PowerupsInstantiationCoroutine());
                    StartCoroutine(StartSobekMovement());
                }
                isPlaying = true;
                uiController.ActivateTimer();
                AudioManager.instance.PlayMusic();
            });
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
    public void ResetSquare(int index)
    {
        squaresMaterials[index].SetColor("_EmissionColor", Color.black);
        squaresPlayer[index] = -1;
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
    public void GotChancla(int player)
    {
        if (PlayerController.LocalPlayer.playerNumber == player)
        {
            PlayerController.LocalPlayer.GotChancla();
        }
    }

    [PunRPC]
    public void ProcessSobekInSquare(int index)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (squaresObjects[index] != null)
            {
                if (squaresObjectTypes[index] != -1) // If it doesn't have type it means it didn't hit the floor yet!
                {
                    PhotonNetwork.Destroy(squaresObjects[index].GetComponent<PhotonView>());
                }
            }
        }
        squaresObjectTypes[index] = -1;
        squaresObjects[index] = null;
    }

    [PunRPC]
    public void ProcessPlayerInSquare(int index, int player)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (squaresObjects[index] != null)
            {
                switch(squaresObjectTypes[index])
                {
                    case (int)ObjectTypes.PaintObject:
                        {
                            int points = 0;
                            for (int i = 0; i < squaresPlayer.Length; ++i)
                            {
                                if (squaresPlayer[i] == player)
                                {
                                    points++;
                                }
                            }
                            photonView.RPC("PlayerWonPoints", RpcTarget.All, player, points);
                            photonView.RPC("PlayerResetSquares", RpcTarget.All, player);
                        }
                        break;
                    case (int)ObjectTypes.PaintObjectx2:
                        {
                            int points = 0;
                            for (int i = 0; i < squaresPlayer.Length; ++i)
                            {
                                if (squaresPlayer[i] == player)
                                {
                                    points += 2;
                                }
                            }
                            photonView.RPC("PlayerWonPoints", RpcTarget.All, player, points);
                            photonView.RPC("PlayerResetSquares", RpcTarget.All, player);
                        }
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
                    case (int)ObjectTypes.Chancla:
                        photonView.RPC("GotChancla", RpcTarget.All, player);
                        break;
                    case (int)ObjectTypes.Swap:
                        photonView.RPC("Swap", RpcTarget.All);
                        break;
                }
                if (squaresObjectTypes[index] != -1) // If it doesn't have type it means it didn't hit the floor yet!
                {
                    PhotonNetwork.Destroy(squaresObjects[index].GetComponent<PhotonView>());
                }
            }
        }
        squaresObjectTypes[index] = -1;
        squaresObjects[index] = null;
    }

    [PunRPC]
    public void PlayerResetSquares(int player)
    {
        for (int i = 0; i < squaresPlayer.Length; ++i)
        {
            if (squaresPlayer[i] == player)
            {
                squaresPlayer[i] = -1;
                squaresMaterials[i].SetColor("_EmissionColor", Color.black);
                var main = pointParticles[i].main;
                pointParticles[i].startColor = playerColor[player];
                pointParticles[i].Play();

            }
        }
    }

    [PunRPC]
    public void Swap()
    {
        for (int i = 0; i < squaresPlayer.Length; i++)
        {
            if (squaresPlayer[i] != -1)
            {
                squaresPlayer[i] = GetNextPlayer(squaresPlayer[i]);
                squaresMaterials[i].SetColor("_EmissionColor", playerColor[squaresPlayer[i]]);
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
            timeToSpawnPaintItem /= (quantityOfPlayers/2f);
            yield return new WaitForSeconds(timeToSpawnPaintItem);
            int rand = GetItemRandomSpawnPoint();
            Vector3 pos = GetItemPosition(rand);
            squaresObjects[rand] = PhotonNetwork.Instantiate("items/"+itemPrefab[0].name, pos, Quaternion.identity, 0);
            pos.y = itemY;
            StartCoroutine(DropTween(squaresObjects[rand], pos, rand, 0));
        }
    }

    IEnumerator PowerupsInstantiationCoroutine()
    {
        while (true)
        {
            float timeToSpawnItem = UnityEngine.Random.Range(minTimeToSpawnItem, maxTimeToSpawnItem);
            timeToSpawnItem /= (quantityOfPlayers / 2f);
            yield return new WaitForSeconds(timeToSpawnItem);
            int rand = GetItemRandomSpawnPoint();
            int type = UnityEngine.Random.Range(1, (int)ObjectTypes.MAX);
            Vector3 pos = GetItemPosition(rand);
            squaresObjects[rand] = PhotonNetwork.Instantiate("items/" + itemPrefab[type].name, pos, Quaternion.identity, 0);
            pos.y = itemY;
            StartCoroutine(DropTween(squaresObjects[rand], pos, rand, type));
        }
    }

    IEnumerator DropTween(GameObject go, Vector3 targetPosition, int index, int type)
    {
        float speed = Vector3.Distance(go.transform.position, targetPosition) / itemDropTime;

        while (!go.transform.position.AlmostEquals(targetPosition, 0.01f))
        {
            go.transform.position = Vector3.MoveTowards(go.transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        squaresObjectTypes[index] = type;
        yield return null;
    }

    IEnumerator StartSobekMovement()
    {
        yield return new WaitForSeconds(timeForSobekMovement);
        sobekInitController.Run(sobekTargetPosition.position, () => {
            SpawnSobekBot();
        });
    }
    #endregion

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

}
