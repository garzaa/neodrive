using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingGhost {
    public Ghost ghost;
    public string name;
    public GhostCar car;
    public int frameIndex;

    public PlayingGhost(Ghost ghost, GhostCar car) {
        this.name = ghost.playerName;
        this.ghost = ghost;
        this.car = car;
        frameIndex = 0;
    }
}
