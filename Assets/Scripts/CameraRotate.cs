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

        Vector3 stickRotation = new Vector3(v.x, 0, v.y);

        Quaternion airAim = Quaternion.identity;
        // if (!car.IsGrounded()) {
        //     Vector3 cv = car.carBody.velocity.normalized;
        //     // does not work
        //     // airAim = Quaternion.LookRotation(new Vector3(cv.x, cv.y, cv.z), transform.up);
        // }

        transform.localRotation = Quaternion.LookRotation(new Vector3(v.x, 0, v.y), transform.up) * airAim;
    }   
}
