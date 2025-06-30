# Physics Notes

- The Trackmania steering model doesn't have springs, it's either connected or not
- Cars lose a certain amount of speed when they start drifting, depending on:
	- angle between car and current velocity
	- amount of gas
- The Canyon car keeps its full drive speed when drifting
	- limited by antislip when NOT drifting

## Wheels
- Treat the suspensions as springs with 100% damping
- Apply suspension force per-wheel but acceleration at the center of mass
- Apply body roll separately during turns
