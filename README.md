# RTSAI

Gameplay Summary:  
The game is a RTS (Real Time Strategy) game that matches a player against an AI in a race of domination. 
This game is based on the 4X genre (eXpand, eXploit, eXterminate, and eXplore). 
The goal is to destroy the enemy's main building.
Each player has a limited amount of construction points, and starts with a main building. 
He can spend them to spawn different units from buildings or construct a new building. 
When a unit or a building dies, the player retrieves the construction points that were spent making it.
He can select multiple units to give them orders, like capturing a capture point or attacking
the enemy’s buildings or units. When a capture point is captured, the player capturing it
gains construction points to make more units.
The player needs to feel the intensity of the match against the AI and needs to be creative to
win against the AI to take advantage of its weakness.

Features List :
- Selecting entities (with the mouse)
- Contextual menu (to make actions for a group of entities)
- Capture points
- Units and buildings
- Units tasks (movement, capture, attack, repear)
- Construction points
- Navigation (Pathfinding, best path according to the environment)
- Formations
- Logic and decision-making state system for each entity (if attacked, a unit will counter attack etc
- Fog of War 
- Minimap 

Inputs: zqsd and navigation keyword to move
- mouse wheel click and drag to move
- Left mouse clic and drag to select
- Left clic to slect building/ button
- Right clic to validate position on map
- Escape to quit

Strategy AI : 
- Selection of a Point Of Interest similarly to a utility system
- Generation of tasks (FSM) from the Point Of Interests (Goal Oriented)
- Since it is goal oriented, units are made to do something
- Usage of a utility system to modify behaviour (to add subjectivity)
- The AI can make squads (groups of units) that have a formation, and that can be split and merged

Individual AI : 
- A queue of tasks are executed
- Can attack, capture and repair