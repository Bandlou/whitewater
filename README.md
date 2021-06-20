# Whitewater
A whitewater boat simulation game prototype focused primarily on slalom and freestyle

## Dictionary
- https://www.ville-huningue.fr/fr/dictionnaire-multilingue/

## Inputs
- WASD / JoystickLeft: advance / turn:
  - Turn leaning forward => sweep (circulaire)
  - Turn leaning normally => draw (appel)
  - Turn leaning backward => reverse sweep (rétro)
- IJKL / Mouse / JoystickRight:
  - Vertically: lean forward / backward
  - Horizontally: lean to the left / right (the list)
- Alt left / Mouse Left Click / R1: look to the left
- Alt right / Mouse Right Click / L1: look to the right

## Boat 3D info
- A boat should have around 200 faces to work well with the physic system
- It should also respect the real world size for believable physics (around 300cm x 60cm x 20cm)

## Map building info
- The water mesh should be a grid with a cell's size of 0.05 Unity units (5 cm)
- The cell size can still be adjusted if necessary in the water manager script

Tips:
- Use Unity ProBuilder to create the water tiles (10x10 with 199x199)
- Use Unity PolyBrush to change the height of the tiles' vertices

References:
- https://www.youtube.com/watch?v=JQyntL-Z5bM
- https://www.youtube.com/watch?v=XOjd_qU2Ido
- https://docs.unity3d.com/2019.3/Documentation/Manual/BestPracticeMakingBelievableVisuals1.html
