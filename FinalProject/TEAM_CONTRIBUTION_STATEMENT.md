# Team Contribution Statement

Project title: Evaluating Character Behavior Trees, Map Node Selection, and Route Strategies in a Turn-Based Grid Card Game

All team members participated in the final presentation preparation, including slide organization, report-content review, rehearsal, and discussion of how to explain the system design and experimental results clearly.

## Member 1: [Ping-Kai Chang]

Primary responsibility: Game system architecture and Experiment 2 / Experiment 3 design.

Member 1 was responsible for the overall game-loop structure and the connection between map progression, resource management, combat scenes, and run-state settlement. This contribution included organizing how a player moves from map-node selection into battle, shop, rest, chest, boss, fail, and victory scenes, and how run-level information such as selected nodes, remaining HP, gold, and rewards is preserved across scenes.

For the experimental work, Member 1 designed Experiment 2 to evaluate how different route strategies affect boss access, boss victory, remaining team HP, gold usage, and selected node composition. Member 1 also designed Experiment 3 to compare different map-structure variants and analyze whether branching, special-room accessibility, and reachable combat pressure create distinguishable strategic choices. In addition, Member 1 helped summarize the experimental findings and connect them to the final report discussion.

Presentation contribution: Member 1 helped prepare the presentation structure for the system overview, map-route evaluation, and experiment-result interpretation.

## Member 2: [Name]

Primary responsibility: Player character design.

Member 2 was responsible for the design and implementation of player-side character behavior. This included defining the roles of the playable characters, their combat responsibilities, and how their actions are selected during battle. The player-side design focused on making each character role distinct, such as damage dealing, defensive support, engagement, and finishing behavior.

Member 2 also contributed to the player combat workflow, including target selection, skill usage, card or unit deployment behavior, HP and shield handling, and the interaction between character roles and Behavior Tree control. This work helped ensure that the combat system could support different Behavior Tree configurations and provide meaningful data for Experiment 1 and the RL comparison.

Presentation contribution: Member 2 helped prepare and explain the player-character design, player-side Behavior Tree logic, and how the character roles affect battle outcomes.

## Member 3: [Guan-Shun Su]

Primary responsibility: Enemy design.

Member 3 was responsible for enemy-side combat design and behavior. This included defining enemy attributes, attack behavior, targeting rules, movement behavior, and the enemy baseline used during combat evaluation. The enemy design provided a stable opponent setting so that player-side Behavior Tree configurations and route-strategy effects could be compared under consistent combat conditions.

Member 3 also contributed to balancing enemy pressure, including attack range, movement speed, target acquisition, and behavior during grid-based battle. By keeping the enemy behavior fixed during the main experiments, this work helped isolate the effects of player-side behavior selection and route decisions.

Presentation contribution: Member 3 helped prepare and explain the enemy design, enemy baseline behavior, and how enemy pressure affects combat evaluation.

## Member 4: [Yung-chi Tan]

Primary responsibility: Map design and Experiment 1 design.

Member 4 was responsible for the map-system design, including the Slay-the-Spire-inspired node structure, route generation, node types, and strategic meaning of different room categories such as battle, elite, shop, rest, chest, and boss nodes. This work shaped the player's long-term decision space before and between combat encounters.

Member 4 also designed Experiment 1, which evaluated the effect of different Behavior Tree configurations on combat performance. This included defining the comparison setup, identifying the controlled variables, and selecting evaluation metrics such as clear rate, timeout rate, battle duration, remaining HP, damage taken, and fixed-score ranking. Member 4 helped connect the combat-layer findings to the broader system design.

Presentation contribution: Member 4 helped prepare and explain the map-generation design, Experiment 1 setup, and how Behavior Tree configuration affects combat performance.

## Shared Contributions

All members contributed to project discussions, final presentation preparation, and revision of the written materials. The team jointly reviewed the final system behavior, discussed the experimental results, and adjusted the presentation narrative so that the implementation, AI techniques, evaluation methodology, and limitations could be communicated clearly.

