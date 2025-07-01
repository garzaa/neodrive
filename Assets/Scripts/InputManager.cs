using UnityEngine;
using Rewired;

public class InputManager : MonoBehaviour {

    static Player player;

    void Start() {
        player = ReInput.players.GetPlayer(0);
    }

    public static Vector2 CameraStick() {
        Vector2 v = new(
            player.GetAxis(Buttons.CAM_X),
            player.GetAxis(Buttons.CAM_Y)
        );

        return v;
    }

    public static float GetAxis(string axisName) {
        return player.GetAxis(axisName);
    }

    public static bool Button(string buttonName) {
        return player.GetButton(buttonName);
    }
}

public static class Buttons {
    public static readonly string CAM_X = "Camera X";
    public static readonly string CAM_Y = "Camera Y";

    public static readonly string GAS = "Gas";
    public static readonly string BRAKE = "Brake";
    public static readonly string STEER = "Steering";
    public static readonly string REVERSE = "Reverse";
    public static readonly string HANDBRAKE = "Handbrake";
}
