# Lennard-Jones
An attempt to model Lennard Jones molecular interactions in VR using Unity.
A compute shader kernel calculates the acceleration on each particle using the Lennard Jones potential from every other molecule
A second kernel updates the velocities using the Verlet integration.
Molecules wrap around the space defined by Size. Commented out code implements barriers to reflect the molecules instead at limits.
A custom shader based on Shinao-Boids draws the molecules using Drawmeshinstancedindirect.
