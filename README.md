# T-Shirt Business Simulator

Unity project for a responsive first-person small-business simulator, built for macOS and Windows.

## Working game brief — source of truth

This section is the current product direction. We should refer back to it before making gameplay, art, or technical decisions, and update it when a design decision changes.

### Premise

The player starts their own t-shirt screen-printing business in a garage. It is a first-person, day-by-day work simulator about learning practical production skills, completing customer orders, protecting limited cash, and growing a business from a single workstation.

### Player experience

- The player moves around their garage workshop in first-person view.
- Primary controls are keyboard and mouse.
- The game progresses through numbered workdays: Day 1, Day 2, and beyond.
- Each day begins in the garage with the player’s active orders and ends when those orders are finished. A timer creates gentle pressure.
- The intended mood is calm and funny, not punishing or corporate.
- The player develops real-looking, understandable production skills, unlocks more capable equipment and techniques, and grows their own business.

### Starting conditions

- Starting cash: **$1,000**.
- Day 1 contains **one customer order**.
- Day 1 teaches a single-colour print using the manual press.
- The long-term end goal is intentionally deferred; the first milestone is making the opening workday satisfying.

### First skill: screen printing

The first skill is one-colour screen printing on t-shirts for customer orders. The initial workstation is a manual screen-printing carousel/press, based on the reference provided in this project discussion. The interaction follows the basic [screen-printing process](https://en.wikipedia.org/wiki/Screen_printing): a stencil blocks parts of a mesh, ink sits above the screen, and a squeegee forces ink through the open mesh onto the shirt. The current slice supplies a ready screen but keeps screen alignment, the visible ink pass, and the squeegee pull as the hands-on skill.

The core skill challenge is correctly aligning and printing each order on a shirt:

1. Receive the order and its print requirements.
2. Fetch a shirt from storage, then use one interaction at the setup area to add the order stencil and cream ink to a screen and collect it. Detailed screen coating and exposure are deferred for now.
3. Place and centre the shirt/design on the press.
4. Physically position the screen-printing tool and pull the colour toward the player at a precise 45° angle.
5. Print the shirt, assess the result, and complete the customer order.

The entire workflow stays in first-person POV. At the press, the collected screen is installed parallel and raised above the platen, aligned over the shirt, lowered onto the fabric, and then printed with a squeegee pulled from the far/top edge toward the player. Alignment, centring, the 45° squeegee angle, and order quality are the core skill challenge. A bad print wastes a shirt and costs the player money. The simulator should make the machine and workflow feel credible without turning routine work into tedious busywork.

### Garage workshop layout

The initial garage has three clearly readable work zones:

- a single manual screen-printing carousel;
- a t-shirt storage area;
- a screen-and-ink setup area for preparing colours and screens.

The visual reference is a compact, practical garage workshop: corrugated walls, concrete floor, exposed lighting, shelves, tools, and enough lived-in detail to feel authentic. It should remain readable and pleasantly stylized rather than grim or visually cluttered.

### Progression

Early play focuses only on one-colour screen printing. Over time, successful orders create money and unlock more advanced printing techniques, equipment, and business capabilities. The exact upgrade path and later skills are intentionally undecided.

## Day 1 vertical-slice plan

Day 1 is a compact tutorial that proves the movement, interaction, printing, quality, time, and money systems together.

### Day flow

1. Spawn inside the garage with **$1,000** visible in the HUD.
2. Read the single customer order: one shirt, one colour, and a specified design placement.
3. Walk to storage and collect the correct blank shirt.
4. At the setup bench, add the order stencil and cream ink and collect the screen with one interaction; detailed coating and exposure are outside the current slice.
5. Load and physically centre the shirt on the press.
6. Lower the screen and perform the print by pulling the squeegee toward the player while maintaining the target 45° angle.
7. Inspect the finished print for position, centring, and print quality.
8. Submit the shirt. A good result completes the order; a failed result wastes the shirt, deducts its cost, and requires another attempt.
9. End the day when the order is complete and show elapsed time, quality, waste, and closing cash.

### First-person interaction model

- **WASD** moves; the mouse looks around.
- A single interaction key handles nearby objects and workstation focus.
- Workstation interactions use the mouse to hold, position, rotate, and pull physical tools.
- The printing gesture is judged continuously rather than by a single button press.
- Clear visual feedback shows whether the squeegee is near 45° without replacing the player’s physical control.

### Day 1 success criteria

- The player can understand the order without outside explanation.
- Walking between all three garage work zones feels quick and natural.
- Shirt alignment and the squeegee pull are readable, tactile, and repeatable.
- Good and bad prints have visibly different outcomes.
- Mistakes affect cash, but a new player cannot become permanently stuck during the tutorial.
- The complete day runs smoothly on the target Mac and a modest Windows laptop.

### Technical and visual direction

- Primary platforms: macOS and Windows.
- Rendering: Unity URP, with scalable low/medium/high quality settings.
- Visual target: attractive, readable, grounded garage-workshop spaces—not photorealism at the expense of performance.
- Performance target: run comfortably on modern integrated/laptop GPUs while allowing better lighting and detail on stronger hardware.

## First launch

1. Open Unity Hub and sign in with a Unity account.
2. Install the current **Unity 6 LTS** editor with the macOS Build Support module.
3. In Hub, choose **Add** and select this folder.
4. Open the project. Unity generates its `Library/` folder on first launch.

## Project layout

- `Assets/Scenes` — playable levels and test scenes
- `Assets/Scripts` — gameplay and simulation code
- `Assets/Prefabs` — reusable world objects
- `Assets/Materials`, `Assets/Art` — visual assets
- `Assets/Settings` — renderer and gameplay settings

## Sensible Mac targets

Develop and profile at 1440×900 or 1080p. For a smooth laptop experience, start with the Universal Render Pipeline, baked lighting, modest shadow distances, and lightweight post-processing. The M4/16 GB configuration is well suited to an attractive stylized or simulation-focused 3D game; massive photoreal open worlds need more careful scope and optimization.
