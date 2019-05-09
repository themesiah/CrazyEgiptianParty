using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DigitalRuby.Tween;

public class SarcophagusController : MonoBehaviour {
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private GameObject cover;
    [SerializeField]
    private float targetAngle;
    [SerializeField]
    private float openingDuration = 2f;

    private UnityAction callback;

	public void Open(UnityAction action)
    {
        callback = action;
        anim.SetTrigger("Open");
    }

    public void FinishOpening()
    {
        callback();
    }
}
