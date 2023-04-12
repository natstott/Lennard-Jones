# Lennard-Jones
An attempt to model Lennard Jones molecular interactions in VR using Unity.
A compute shader kernel calculates the acceleration on each particle using the Lennard Jones potential from every other molecule
A second kernel updates the velocities using the Verlet integration.
Molecules wrap around the space defined by Size. Commented out code implements barriers to reflect the molecules instead at limits.
A custom shader based on Shinao-Boids draws the molecules using Drawmeshinstancedindirect.

Temperature not yet implemented. A Cooling float is used to either increase or decrease speed of all particles, e.g. 0.99 removes 1% of velocity per step.
This can be used for heating too, but keep to small variations such as 1.0001.

CreateBoidCrystal arranges the molecules in a closedpacked arrangement - At present they still seem to have lattice energy at zero K-
need to work out correct lattice spacing. There is a variable to set it, currently using 1.3 as divisor from normal spacing?


Performance in otherwise empty VR scene is around 180FPS with 16384 molecules on an RX7800 XT
![Screenshot](/Capture.PNG "Screenshot")
