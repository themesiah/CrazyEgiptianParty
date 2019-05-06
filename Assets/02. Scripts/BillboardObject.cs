using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardObject : MonoBehaviour {
    private Transform targetCamera;

	// Use this for initialization
	void Awake () {
        targetCamera = Camera.main.transform;
	}
	
	// Update is called once per frame
	void Update () {
        LookAtCamera();
    }

    public void LookAtCamera()
    {
        transform.LookAt(transform.position + targetCamera.rotation * Vector3.forward, targetCamera.rotation * Vector3.up);
    }
}
