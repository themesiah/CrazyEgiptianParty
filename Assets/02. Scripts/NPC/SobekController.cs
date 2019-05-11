using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.Tween;
using Photon.Pun;
using TMPro;

public class SobekController : MonoBehaviourPunCallbacks, IPunObservable {
    public static SobekController instance;
    private BoardController board;
    private Animator anim;

    [SerializeField]
    private int startingSquare = 49;

    [SerializeField]
    private float tickTime = 1f;

    [SerializeField]
    [Range(0.1f,1f)]
    private float moveTime = 0.5f;
    [SerializeField]
    private float animSpeed = 6f;

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
        anim = GetComponentInChildren<Animator>();
        anim.speed = animSpeed;
        instance = this;
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
            Debug.Log("Initialized sobek");
        }
    }

    public void Begin(Vector3 targetPosition)
    {
        direction = Direction.Top;
        System.Action<ITween<Vector3>> updateSobekPos = (t) =>
        {
            gameObject.transform.position = t.CurrentValue;
        };
        System.Action<ITween<Vector3>> sobekMovementFinished = (t) =>
        {
            StartCoroutine(MoveStartCoroutine());
        };

        Vector3 jumpPosition = transform.position + (targetPosition - transform.position) / 2f;
        jumpPosition.y += 0.5f;
        JumpTrigger();
        gameObject.Tween("MoveSobekBot", transform.position, jumpPosition, moveTime / 2f, TweenScaleFunctions.Linear, updateSobekPos).
            ContinueWith(new Vector3Tween().Setup(jumpPosition, targetPosition, moveTime / 2f, TweenScaleFunctions.Linear, updateSobekPos, sobekMovementFinished));
    }

    public void SetCurrentSquare(int square)
    {
        currentSquare = square;
    }

    public int GetCurrentSquare()
    {
        return currentSquare;
    }

    IEnumerator MoveStartCoroutine()
    {
        Vector2Int currentCoordinates = board.GetCoordinates(startingSquare);
        while (!board.CanMoveThere(currentCoordinates.x, currentCoordinates.y))
        {
            yield return null;
        }

        System.Action<ITween<Vector3>> updatePlayerPos = (t) =>
        {
            gameObject.transform.position = t.CurrentValue;
        };
        System.Action<ITween<Vector3>> playerMovementFinished = (t) =>
        {
            StartCoroutine(MoveCoroutine());
        };
        currentSquare = board.GetIndex(currentCoordinates.x, currentCoordinates.y);
        Vector3 targetPosition = board.GetPosition(currentSquare);
        Vector3 jumpPosition = transform.position + (targetPosition - transform.position) / 2f;
        jumpPosition.y += 0.5f;
        JumpTrigger();
        gameObject.Tween("MoveSobekBot", transform.position, jumpPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos).
            ContinueWith(new Vector3Tween().Setup(jumpPosition, targetPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos, playerMovementFinished));
    }

    IEnumerator MoveCoroutine()
    {
        while (true)
        {
            direction = (Direction)UnityEngine.Random.Range(1, ((int)Direction.Bottom) + 1);
            Vector3 targetPosition = transform.position;
            System.Action<ITween<Vector3>> updatePlayerPos = (t) =>
            {
                gameObject.transform.position = t.CurrentValue;
            };
            System.Action<ITween<Vector3>> playerMovementFinished = (t) =>
            {
                board.SendSobekInSquare(currentSquare);
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
            if (board.CanMoveThere(currentCoordinates.x, currentCoordinates.y))
            {
                currentSquare = board.GetIndex(currentCoordinates.x, currentCoordinates.y);
                targetPosition = board.GetPosition(currentSquare);
            }
            Vector3 jumpPosition = transform.position + (targetPosition - transform.position) / 2f;
            jumpPosition.y += 0.5f;
            JumpTrigger();
            gameObject.Tween("MoveSobekBot", transform.position, jumpPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos).
                ContinueWith(new Vector3Tween().Setup(jumpPosition, targetPosition, moveTime / 2f, TweenScaleFunctions.Linear, updatePlayerPos, playerMovementFinished));
            yield return new WaitForSeconds(tickTime);
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

    }
}
