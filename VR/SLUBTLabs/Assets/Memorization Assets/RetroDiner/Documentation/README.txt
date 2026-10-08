================================================================================
  RETRO DINER - Stylized 3D Environment Pack
================================================================================

Thank you for purchasing Retro Diner!

This package contains a stylized retro American diner building, interior and
exterior props, furniture, kitchen equipment, food items, and a ready-made demo
scene with lighting.


REQUIREMENTS
------------
  - Unity 6 (6000.x) or newer recommended
  - Universal Render Pipeline (URP) 17.x
  - Render Pipeline: URP Lit shader


PACKAGE CONTENTS
----------------
  Models/        - 21 FBX source meshes
  Prefabs/       - 21 drag-and-drop prefabs (use these in your scenes)
  Materials/     - URP Lit materials
  Textures/      - Albedo, normal, and metallic/smoothness maps
  Scenes/        - Demo.unity showcase scene
  Settings/      - Stylized post-processing profile (DemoStylizedProfile)


PREFAB LIST
-----------

  Building
  --------
  Dinner_building 1   - Full diner building (exterior + interior)

  Furniture
  ---------
  Dinner_Table        - Standard dining table
  Dinner_TableV2      - Alternate dining table variant
  Dinner_Chair 1      - Dining chair
  Dinner_Stand        - Booth / standing seating
  Dinner_Counter      - Service counter
  Dinner_Menu         - Wall or table menu prop

  Kitchen & Equipment
  -------------------
  CoffeeMashine       - Espresso / coffee machine
  CoffeeGrinder       - Coffee grinder
  CoffeeRig           - Coffee equipment rig
  CoffeeBag           - Coffee bag prop
  Freezer             - Freezer unit
  CupBox              - Cup storage box
  Milk                - Milk bottle / carton
  MilkBox             - Milk box / crate

  Tableware
  ---------
  CoffeMug            - Coffee mug
  CoffeePlate         - Coffee plate
  ReguralPlate        - Regular dining plate

  Food
  ----
  CheeseBurger        - Cheeseburger prop
  Cake                - Cake prop
  Donut               - Donut prop


QUICK START
-----------
  1. Import the package into a URP Unity project.
  2. Open Assets/RetroDiner/Scenes/Demo.unity to view the showcase scene.
  3. Drag prefabs from Assets/RetroDiner/Prefabs/ into your own scene.
  4. Assign URP Lit materials if needed (materials are in Assets/RetroDiner/Materials/).


SCALE & ORIENTATION
-------------------
  - Models use 1 Unity unit = 1 meter.
  - All meshes were re-exported from Blender with correct scale and orientation.
  - Prefab roots are at position (0, 0, 0), rotation (0, 0, 0), and scale (1, 1, 1).
  - Main building origin is at ground level.


MATERIALS
---------
  Main            - Primary wall / surface material
  Floor           - Checkered floor tile material
  Ceiling         - Interior ceiling material
  indoorWall      - Interior wall panels
  Glass           - Transparent glass
  BlackMetal      - Dark metal trim
  BlackMetal 1    - Alternate dark metal variant
  Whitemetal      - Light metal / chrome accents
  Brown           - Brown accent surfaces
  Blue            - Blue accent surfaces
  EmissionWhite   - White glowing emission (neon signs, lights)
  EmissionRed     - Red glowing emission (neon signs, accents)


DEMO SCENE LIGHTING
-------------------
  The demo scene is set up for a night exterior screenshot with warm interior glow:
    - Dark blue ambient + fog outside
    - Diner Interior Warm - Main warm interior light
    - Interior Window Glow - Warm light at window height (visible from outside)
    - Interior Counter Glow - Counter area warmth
    - Neon Sign Glow + Neon Accent - Sign and accent color pop
    - DemoNightStylizedProfile - Night post-processing with extra bloom

  Day/morning lights (Sun Key Light, Window Morning Light) are disabled.
  To switch back to morning: enable those lights, disable night interior
  glows, and set Global Volume to DemoStylizedProfile.

  Lighting objects are grouped under "Stylized Lighting" in the hierarchy.
  Global Volume uses Assets/RetroDiner/Settings/DemoNightStylizedProfile.asset.


SUPPORT
-------
  For questions or issues, contact the publisher through the Unity Asset Store
  publisher page.


VERSION HISTORY
---------------
  v1.1 - Updated Blender re-export
         - 21 models with matching prefabs
         - Added Cake, Donut, Freezer, CoffeeBag, MilkBox, CoffeePlate,
           ReguralPlate, CoffeMug, Dinner_TableV2, Dinner_Stand
         - Re-exported all meshes with corrected scale and orientation
         - Added Brown and Blue materials

  v1.0 - Initial release
         - Diner building, furniture, kitchen props, and demo scene

================================================================================
