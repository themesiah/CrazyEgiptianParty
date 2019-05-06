using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotation : MonoBehaviour {
    [SerializeField]
    private float rotateSpeed = 10f;
	
	// Update is called once per frame
	void Update () {
        transform.Rotate(0f, Time.deltaTime * rotateSpeed, 0f);
	}
}
