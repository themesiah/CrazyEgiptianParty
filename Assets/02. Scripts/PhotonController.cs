using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class PhotonController : MonoBehaviourPunCallbacks//, IPunObservable
{
    [SerializeField]
    private UIController uiController;

    [SerializeField]
    private BoardController boardPrefab;
    [SerializeField]
    private GameObject startButton;
    [SerializeField]
    private GameObject waitingText;
    [SerializeField]
    private GameObject[] playerPrefab;
    


    private const string gameVersion = "1";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // Use this for initialization
    void Start () {
        Connect();
	}

    /// <summary>
    /// Start the connection process.
    /// - If already connected, we attempt joining a random room
    /// - if not yet connected, Connect this application instance to Photon Cloud Network
    /// </summary>
    public void Connect()
    {
        PhotonNetwork.NickName = SystemInfo.deviceName;
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)
        {
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            //PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
        // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
        //PhotonNetwork.JoinRandomRoom();

        uiController.SetGameConnected(PhotonNetwork.CountOfRooms > 0);
    }

    public void CreateOrJoin()
    {
        PhotonNetwork.NickName = uiController.GetPlayerName();
        uiController.DeactivatePregameUI();
    }

    public void CreateRoom()
    {
        CreateOrJoin();
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public void JoinRoom()
    {
        CreateOrJoin();
        PhotonNetwork.JoinRandomRoom();
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            startButton.SetActive(true);
            PhotonNetwork.Instantiate(boardPrefab.name, new Vector3(0f, 0f, 0f), boardPrefab.transform.rotation, 0);
        } else
        {
            waitingText.SetActive(true);
        }
        int playerNumber = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        GameObject player = PhotonNetwork.Instantiate(playerPrefab[playerNumber].name, new Vector3(1000f, 0f, 0f), Quaternion.identity, 0);
        PlayerController.LocalPlayer = player.GetComponent<PlayerController>();
        PlayerController.LocalPlayer.playerNumber = playerNumber;
        PlayerController.LocalPlayer.ChangeName(PhotonNetwork.NickName);
    }

    public void StartGame()
    {
        BoardController.BoardInstance.SendStartGame();
    }
}
