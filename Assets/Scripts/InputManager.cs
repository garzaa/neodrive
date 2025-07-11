using UnityEngine;
using Rewired;
using Rewired.Platforms.XboxOne;

public class InputManager : MonoBehaviour {

    public static Player player { get; private set; }

    void Start() {
        player = ReInput.players.GetPlayer(0);
    }

    public static float GetAxis(string axisName) {
        return player.GetAxis(axisName);
    }

    public static bool Button(string buttonName) {
        return player.GetButton(buttonName);
    }

    public static bool ButtonDown(string buttonName) {
        return player.GetButtonDown(buttonName);
    }

    public static bool DoubleTap(string buttonName) {
        return player.GetButtonDoublePressDown(buttonName);
    }

    public static bool ButtonUp(string buttonName) {
        return player.GetButtonUp(buttonName);
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
    public static readonly string GEARUP = "Gear Up";
    public static readonly string GEARDOWN = "Gear Down";
    public static readonly string STARTENGINE = "Start Engine";
    public static readonly string CLUTCH = "Clutch";
    public static readonly string CYCLE_CAMERA = "Cycle Camera";
    public static readonly string TOGGLE_TELEMETRY = "Toggle Telemetry";
    public static readonly string BOOST = "Boost";

    public static readonly string SHIFTLEFT = "ShifterLeft";
    public static readonly string SHIFTRIGHT = "ShifterRight";
    public static readonly string SHIFTUP = "ShifterUp";
    public static readonly string SHIFTDOWN = "ShifterDown";
    public static readonly string SHIFTALT = "ShiftAlt";
}
