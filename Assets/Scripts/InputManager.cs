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
