using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayingGhost {
    public Ghost ghost;
    public string name;
    public GameObject car;

    public PlayingGhost(string name, Ghost ghost, GameObject car) {
        this.name = name;
        this.ghost = ghost;
        this.car = car;
    }
}
