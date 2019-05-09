using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MummyController : MonoBehaviour {
    public static MummyController instance;

    [SerializeField]
    private Animator anim;
    [SerializeField]
    private GameObject presentPrefab;
    [SerializeField]
    private Transform presentSpawnPosition;
    private PresentController present;

    public void Awake()
    {
        instance = this;
    }

    public void PlayAnimation()
    {
        anim.Play("Abrir Sarcofago");
    }

	public void SpawnPresent()
    {
        GameObject presentObject = GameObject.Instantiate<GameObject>(presentPrefab, presentSpawnPosition.position, Quaternion.identity);
        present = presentObject.GetComponent<PresentController>();
        present.AttachTo(presentSpawnPosition);
    }

    public void UnattachPresent()
    {
        present.Unnatach();
        present.Launch(transform.forward * 500f + Vector3.up * 700f);
    }
}
