using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DigitalRuby.Tween;
using Photon.Pun;

public class SobekInitController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private float duration = 5f;

    public void Run(Vector3 target, UnityAction callback)
    {
        photonView.RPC("StartAnim", RpcTarget.All);
        transform.Rotate(0f, -90f, 0f);
        System.Action<ITween<Vector3>> updateSobekrPos = (t) =>
        {
            gameObject.transform.position = t.CurrentValue;
        };
        System.Action<ITween<Vector3>> sobekMovementFinished = (t) =>
        {
            callback();
        };
        gameObject.Tween("SobekCorriendo", transform.position, target, duration, TweenScaleFunctions.Linear, updateSobekrPos, sobekMovementFinished);
    }

    [PunRPC]
    public void StartAnim()
    {
        anim.SetTrigger("Correr");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}
