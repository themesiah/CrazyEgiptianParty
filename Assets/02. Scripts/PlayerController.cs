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
    private Animator anim;
    Coroutine sneakersEndCoroutine;
    Coroutine chanclaEndCoroutine;
    public int playerNumber = -1;
    public int points = 0;

    [SerializeField]
    private TextMeshProUGUI playerName;

    [SerializeField]
    private float tickTime = 1f;

    [SerializeField]
    [Range(0.1f,1f)]
    private float moveTime = 0.5f;
    [SerializeField]
    private float animSpeed = 6f;

    [SerializeField]
    [Range(1f, 2f)]
    private float sneakersMultiplier = 1.5f;
    [SerializeField]
    private float sneakersTime = 6f;

    [SerializeField]
    [Range(0.1f, 1f)]
    private float chanclaMultiplier = 0.6f;
    [SerializeField]
    private float chanclaTime = 4f;
    

    private float currentMultiplier = 1f;

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
#if UNITY_ANDROID
    private Rect leftRect;
    private Rect topRect;
    private Rect rightRect;
    private Rect bottomRect;
#endif

    // Use this for initialization
    void Awake () {
        direction = Direction.None;
        anim = GetComponentInChildren<Animator>();
        anim.speed = animSpeed;
        bbo = GetComponentInChildren<BillboardObject>();
#if UNITY_ANDROID
        leftRect = new Rect(0f, 0f, Screen.width / 4f, Screen.height);
        rightRect = new Rect(Screen.width/4f*3f, 0f, Screen.width / 4f, Screen.height);
        bottomRect = new Rect(Screen.width/4f, 0f, Screen.width / 2f, Screen.height/2f);
        topRect = new Rect(Screen.width / 4f, Screen.height/2f, Screen.width / 2f, Screen.height/2f);
#endif
    }

    // Update is called once per frame
    void Update () {
        if (photonView.IsMine)
        {

#if UNITY_ANDROID
            if (Input.touchCount > 0)
            {
                if (leftRect.Contains(Input.touches[0].position))
                {
                    direction = Direction.Left;
                } else if (rightRect.Contains(Input.touches[0].position))
                {
                    direction = Direction.Right;
                } else if (topRect.Contains(Input.touches[0].position))
                {
                    direction = Direction.Top;
                } else if (bottomRect.Contains(Input.touches[0].position))
                {
                    direction = Direction.Bottom;
                }
            }
#else
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
#endif
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
            JumpTrigger();
            gameObject.Tween("MovePlayer", transform.position, jumpPosition, moveTime / (2f*currentMultiplier), TweenScaleFunctions.Linear, updatePlayerPos).
                ContinueWith(new Vector3Tween().Setup(jumpPosition, targetPosition, moveTime / (2f*currentMultiplier), TweenScaleFunctions.Linear, updatePlayerPos, playerMovementFinished));
            yield return new WaitForSeconds(tickTime / currentMultiplier);
        }
    }

    public void JumpTrigger()
    {
        anim.SetTrigger("Jump");
        photonView.RPC("JumpTriggerRPC", RpcTarget.Others);
    }

    [PunRPC]
    public void JumpTriggerRPC()
    {
        anim.SetTrigger("Jump");
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

    public void GotSneakers()
    {
        currentMultiplier = sneakersMultiplier;
        anim.speed = animSpeed * currentMultiplier;
        if (sneakersEndCoroutine != null)
        {
            StopCoroutine(sneakersEndCoroutine);
        }
        sneakersEndCoroutine = StartCoroutine(SneakersEnd());
    }

    IEnumerator SneakersEnd()
    {
        yield return new WaitForSeconds(sneakersTime);
        currentMultiplier = 1f;
        anim.speed = animSpeed * currentMultiplier;
    }

    public void GotChancla()
    {
        currentMultiplier = chanclaMultiplier;
        anim.speed = animSpeed * currentMultiplier;
        if (chanclaEndCoroutine != null)
        {
            StopCoroutine(chanclaEndCoroutine);
        }
        chanclaEndCoroutine = StartCoroutine(ChanclaEnd());
    }

    IEnumerator ChanclaEnd()
    {
        yield return new WaitForSeconds(chanclaTime);
        currentMultiplier = 1f;
        anim.speed = animSpeed * currentMultiplier;
    }
}
