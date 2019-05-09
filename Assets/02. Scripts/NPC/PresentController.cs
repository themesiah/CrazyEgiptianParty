using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PresentController : MonoBehaviour {
    [SerializeField]
    private Rigidbody rb;
    private Transform attachedTo;

    public void AttachTo(Transform t)
    {
        attachedTo = t;
        rb.isKinematic = true;
    }

    public void Unnatach()
    {
        attachedTo = null;
        rb.isKinematic = false;
    }

    public void Launch(Vector3 force)
    {
        rb.AddForce(force);
        Destroy(gameObject, 3f);
    }
	
	// Update is called once per frame
	void Update () {
		if (attachedTo != null)
        {
            transform.position = attachedTo.position;
        }
	}
}
