using UnityEngine;
using System.Collections.Generic;

public class CameraRotate : MonoBehaviour {

    public float snapDistance = 0.5f;
    public List<Vector2> snapVectors;
    public Car car;

    void Start() {
        car = GetComponentInParent<Car>();
    }

    void Update() {
        Vector2 v = InputManager.CameraStick();
        if (v.magnitude == 0f) {
            transform.localRotation = Quaternion.identity;
            return;
        }

        for (int i=0; i<snapVectors.Count; i++) {
            if (Vector2.Distance(v, snapVectors[i]) < snapDistance) {
                v = snapVectors[i];
                break;
            }
        }

        Quaternion airAim = Quaternion.identity;
        // if (!car.IsGrounded()) {
        //     Vector3 cv = car.rb.velocity.normalized;
        //     // makes grounded camera spaz out
        //     airAim = Quaternion.LookRotation(new Vector3(cv.x, cv.y, cv.z), transform.up);
        // }

        // this should be a rotation fromEuler just to actually understand what's going on here
        float rotationAngle = Vector3.SignedAngle(Vector3.forward, new Vector3(v.x, 0, v.y), Vector3.up);
        transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
    }   
}
