using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Car : MonoBehaviour {

    public GameObject wheelTemplate;
    public Transform WheelFL, WheelFR, WheelRL, WheelRR;
    public CarSettings settings;
    public GameObject centerOfGravity;

    Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        if (WheelFL.transform.childCount == 0) {
            AddWheels();
        }
        rb.centerOfMass = centerOfGravity.transform.localPosition;
    }

    void Update() {
        
    }

    [ContextMenu("Add Wheels")]
    void AddWheels() {
        rb = GetComponent<Rigidbody>();
        foreach (Transform t in new Transform[]{WheelFL, WheelFR, WheelRL, WheelRR}) {
            GameObject g = Instantiate(wheelTemplate, t);
            g.transform.localPosition = Vector3.zero;
            g.transform.localScale = Vector3.one;
            g.transform.localRotation = Quaternion.identity;
            g.GetComponent<FixedJoint>().connectedBody = rb;
        }
    }
}
