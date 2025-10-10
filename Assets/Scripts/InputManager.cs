using UnityEngine;
using Rewired;
using Rewired.Platforms.XboxOne;

public class InputManager : MonoBehaviour {

    public static Player player { get; private set; }

    void Start() {
        player = ReInput.players.GetPlayer(0);
        player.controllers.hasKeyboard = true;
    }

    public static float GetAxis(string axisName) {
        return player.GetAxis(axisName);
    }

    public static bool Button(string buttonName) {
        if (string.Equals(buttonName, Buttons.CLUTCH) && GameOptions.PaddleShift) return false;
        return player.GetButton(buttonName);
    }

    public static bool ButtonDown(string buttonName) {
        if (string.Equals(buttonName, Buttons.CLUTCH) && GameOptions.PaddleShift) return false;
        return player.GetButtonDown(buttonName);
    }

    public static bool DoubleTap(string buttonName) {
        return player.GetButtonDoublePressDown(buttonName);
    }

    public static bool ButtonUp(string buttonName) {
        if (string.Equals(buttonName, Buttons.CLUTCH) && GameOptions.PaddleShift) return false;
        return player.GetButtonUp(buttonName);
    }

    public static bool ClutchIn() {
        if (GameOptions.PaddleShift) return false;
        else return ButtonDown(Buttons.CLUTCH);
    }

    public static bool ClutchOut() {
        if (GameOptions.PaddleShift) return false;
        else return ButtonUp(Buttons.CLUTCH);
    }

    public static bool Clutch() {
        if (GameOptions.PaddleShift) return false;
        else return Button(Buttons.CLUTCH);
    }

    public static bool ButtonDownWithManualClutch(string buttonName) {
        // eventually just do it with a non-clutch check
        if (GameOptions.PaddleShift) return ButtonDown(buttonName);
        else return Button(Buttons.CLUTCH) && ButtonDown(buttonName);
    }
}
