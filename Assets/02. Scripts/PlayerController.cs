using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.Tween;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable {
    public static PlayerController LocalPlayer;
    private BoardController board;
    private BillboardObject bbo;
    public int playerNumber = -1;
    public int points = 0;

    [SerializeField]
    private TextMeshProUGUI playerName;

    [SerializeField]
    private float tickTime = 1f;

    [SerializeField]
    [Range(0.1f,1f)]
    private float moveTime = 0.5f;

    public enum Direction
    {
        None,
        Top,
        Right,
        Left,
        Bottom
    }
    private Direction direction;
    private int currentSquare;

    // Use this for initialization
    void Awake () {
        direction = Direction.None;
        bbo = GetComponentInChildren<BillboardObject>();
    }
	
	// Update is called once per frame
	void Update () {
        if (photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = Direction.Top;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = Direction.Bottom;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                direction = Direction.Left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = Direction.Right;
            }
        }

        if (BoardController.BoardInstance != null && !board)
        {
            board = BoardController.BoardInstance;
        }
    }

    public void OnStart()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(MoveCoroutine());
            Debug.Log("Initialized player");
        }
    }

    public void SetCurrentSquare(int square)
    {
        currentSquare = square;
    }

    public int GetCurrentSquare()
    {
        return currentSquare;
    }

    public void SetColor(Color c)
    {
        GetComponentInChildren<MeshRenderer>().material.color = c;
    }

    public void ChangeName(string name)
    {
        playerName.text = name;
    }

    public string GetName()
    {
        return playerName.text;
    }

    IEnumerator MoveCoroutine()
    {
        while (true)
        {
            Vector3 targetPosition = transform.position;
            System.Action<ITween<Vector3>> updatePlayerPos = (t) =>
            {
                gameObject.transform.position = t.CurrentValue;
            };
            System.Action<ITween<Vector3>> playerMovementFinished = (t) =>
            {
                board.SendPlayerInSquare(currentSquare, playerNumber);
            };
            Vector2Int currentCoordinates = board.GetCoordinates(currentSquare);
            switch (direction)
            {
                case Direction.None:
                    break;
                case Direction.Top:
                    currentCoordinates.y--;
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case Direction.Bottom:
                    currentCoordinates.y++;
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                case Direction.Left:
                    currentCoordinates.x--;
                    transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    break;
                case Direction.Right:
                    currentCoordinates.x++;
                    transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
            }
            bbo.LookAtCamera();
            if (board.CanMoveThere(currentCoordinates.x, currentCoordinates.y))
            {
                currentSquare = board.GetIndex(currentCoordinates.x, currentCoordinates.y);
                targetPosition = board.GetPosition(currentSquare);
            }
            Vector3 jumpPosition = transform.position + (targetPosition - transform.position) / 2f;
            jumpPosition.y += 0.5f;
            gameObject.Tween("MovePlayer", transform.position, jumpPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos).
                ContinueWith(new Vector3Tween().Setup(jumpPosition, targetPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos, playerMovementFinished));
            yield return new WaitForSeconds(tickTime);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerNumber);
            stream.SendNext(currentSquare);
            stream.SendNext(points);
            stream.SendNext(playerName.text);
        } else
        {
            playerNumber = (int)stream.ReceiveNext();
            currentSquare = (int)stream.ReceiveNext();
            points = (int)stream.ReceiveNext();
            playerName.text = (string)stream.ReceiveNext();
        }
    }

}
