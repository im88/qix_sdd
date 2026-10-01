# Feature Specification: Player Movement and Territory Claiming

**Feature Branch**: `001-player-movement-claiming`

**Created**: 2026-10-01

**Status**: Draft

**Input**: User description: "player movement and territory claiming, no enemies yet"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Move Along the Border (Priority: P1)

The player starts the game and sees a rectangular playfield with an outer frame and a marker on
it. Using the direction keys, the player moves the marker along the frame in four directions.
The marker cannot leave the frame unless the player is drawing (see User Story 2).

**Why this priority**: Movement is the foundation of every other mechanic. On its own it already
gives a launchable, visibly interactive game, which satisfies the constitution's Always Playable
principle.

**Independent Test**: Launch the game, press each direction key, and confirm the marker moves
along the frame and stops at corners and dead ends without leaving the border.

**Acceptance Scenarios**:

1. **Given** the game has just started, **When** the player looks at the screen, **Then** a
   rectangular playfield with a visible frame is shown, with the marker positioned on the frame
   (bottom edge, centered).
2. **Given** the marker is on a horizontal edge of the frame, **When** the player holds left or
   right, **Then** the marker moves one cell per movement step in that direction along the edge.
3. **Given** the marker is on a horizontal edge and not at a corner, **When** the player presses
   up or down without drawing, **Then** the marker does not move.
4. **Given** the marker is at a corner, **When** the player presses the direction that follows
   the frame around the corner, **Then** the marker turns the corner and continues along the
   adjacent edge.
5. **Given** the marker is moving, **When** the player releases all direction keys, **Then** the
   marker stops.

---

### User Story 2 - Draw a Line and Claim Territory (Priority: P2)

While holding the draw key, the player steers the marker off the border into the unclaimed area,
leaving a trail line behind it. When the marker reaches a border again, the line closes off a
region. The smaller of the two regions it creates becomes claimed territory: it is visibly
filled, and from then on its edges count as border the marker can travel along.

**Why this priority**: Claiming territory is the core Qix mechanic and the main point of this
feature. It depends on movement (User Story 1).

**Independent Test**: Start the game, hold draw, cut a line straight across a corner of the
playfield back to the frame, and confirm the enclosed region fills and the marker can then travel
along the new edge.

**Acceptance Scenarios**:

1. **Given** the marker is on a border, **When** the player holds the draw key and moves into
   the unclaimed area, **Then** the marker leaves the border and a visible trail line appears
   along its path.
2. **Given** the marker is drawing, **When** the marker reaches any border cell, **Then** the
   line is completed, the region on the smaller side of the line is filled as claimed, and the
   marker is back on the border.
3. **Given** a line has just closed two regions of different sizes, **When** the fill happens,
   **Then** the smaller region becomes claimed and the larger region stays unclaimed.
4. **Given** a region has been claimed, **When** the player moves the marker without drawing,
   **Then** the marker can travel along the new edges between claimed and unclaimed territory,
   but cannot enter the inside of claimed territory.
5. **Given** the marker is drawing, **When** the player tries to move onto the line being drawn,
   **Then** the move is blocked and the marker stays where it is.
6. **Given** the marker is drawing and away from the border, **When** the player releases the
   draw key, **Then** the marker stops and stays put; it can only move again when the player
   resumes drawing.
7. **Given** the marker is drawing, **When** the player tries to move into claimed territory,
   **Then** the move is blocked.

---

### User Story 3 - Track Progress and Complete the Playfield (Priority: P3)

The player sees what percentage of the playfield has been claimed, updated after every fill. When
the claimed share reaches the target of 75%, the game shows a "playfield complete" message and
lets the player start a fresh playfield or quit.

**Why this priority**: This gives the game a goal and an end state. Claiming already works
without it, so it comes after the core mechanic.

**Independent Test**: Claim regions until the displayed percentage reaches 75%, and confirm the
completion message appears and that restarting resets the playfield to 0%.

**Acceptance Scenarios**:

1. **Given** the game has just started, **When** the player looks at the status display,
   **Then** it shows 0% claimed and the 75% target.
2. **Given** a region has just been claimed, **When** the fill completes, **Then** the displayed
   percentage updates to the new claimed share of the playfield.
3. **Given** a fill brings the claimed share to 75% or more, **When** the fill completes,
   **Then** a completion message is shown and gameplay input pauses.
4. **Given** the completion message is shown, **When** the player chooses to restart, **Then** a
   fresh playfield appears with 0% claimed and the marker at its starting position.
5. **Given** the game is running at any time, **When** the player presses the quit key, **Then**
   the game exits cleanly and the terminal is left usable.

---

### Edge Cases

- **Equal-sized regions**: When a line splits the unclaimed area into two regions of exactly the
  same size, the game claims the region that does **not** contain the topmost unclaimed cell
  (choosing the leftmost cell when several are equally high). The same line shape therefore
  always gives the same result.
- **One-cell steps**: A line only one cell long (for example, cutting a single-cell corner) still
  closes and claims correctly.
- **Line ends on a claimed edge**: A line that starts on the outer frame and ends on the edge of
  previously claimed territory (or the other way round) closes and fills like any other line.
- **Dead ends while drawing**: If the marker is surrounded by its own line or by claimed area on
  all sides it can move to, it stays stuck until another direction becomes possible. With no
  enemies there is no penalty, so the player can wait without consequence.
- **Pockets**: A line that runs right next to itself can enclose small gaps, so a single close
  can create more than two regions. Every region except the largest is claimed; the tie rule
  above decides between equally large regions.
- **Edges that stop being border**: An edge with claimed territory on both sides is no longer
  border, and the marker cannot travel along it.
- **Simultaneous keys**: If two direction keys are pressed at once, the most recently pressed
  direction wins.
- **Terminal too small**: If the terminal is smaller than the required size at launch, the game
  shows a message stating the minimum size and does not start until the window is large enough
  or the player quits.
- **Percentage rounding**: The displayed percentage is rounded down to a whole number, so
  "75%" is only shown once the target has actually been reached.

## Requirements *(mandatory)*

### Functional Requirements

**Playfield and movement**

- **FR-001**: The game MUST show a rectangular playfield of fixed size, surrounded by a visible
  outer frame, together with a status area.
- **FR-002**: The game MUST place the marker on the frame at a fixed starting position when a
  playfield starts.
- **FR-003**: The player MUST be able to move the marker in four directions (up, down, left,
  right) using the arrow keys. Diagonal movement is not supported.
- **FR-004**: The marker MUST move at a constant speed, one cell per movement step, for as long as
  a direction key is held.
- **FR-005**: When not drawing, the marker MUST only move along border cells: the outer frame and
  the edges between claimed and unclaimed territory.

**Drawing and claiming**

- **FR-006**: When the player holds the draw key (Space) while moving from a border cell into
  unclaimed area, the game MUST let the marker leave the border and MUST show the line it
  leaves behind.
- **FR-007**: While drawing, the game MUST block movement onto the line being drawn and into
  claimed territory.
- **FR-008**: While the marker is away from the border, it MUST move only while the draw key is
  held. Releasing the draw key MUST stop the marker without penalty.
- **FR-009**: When a drawing marker reaches a border cell, the game MUST complete the line, split
  the unclaimed area along it, and mark every resulting region except the largest as claimed.
  For a simple line this is the smaller side; pockets the line encloses against itself are
  claimed too. The line itself becomes part of the border.
- **FR-010**: When two regions are exactly equal in size, the game MUST choose which one to claim
  deterministically, as described under Edge Cases.
- **FR-011**: Claimed territory MUST look clearly different from unclaimed territory, from the
  line being drawn, and from the border.
- **FR-012**: Cells inside claimed territory that are no longer next to unclaimed area MUST NOT
  be traversable by the marker.

**Progress and session**

- **FR-013**: The status area MUST show the percentage of the playfield claimed, rounded down to
  a whole number, together with the 75% target. It MUST update right after every fill.
- **FR-014**: When the claimed share reaches 75% or more, the game MUST show a completion message
  and offer to restart or quit.
- **FR-015**: Restarting MUST reset the playfield to fully unclaimed and the marker to its
  starting position.
- **FR-016**: The player MUST be able to quit at any time with the Escape key. Quitting MUST
  restore the terminal to a normal, usable state (cursor visible, no leftover colors).
- **FR-017**: The game MUST check the terminal size at launch and show a clear message naming the
  minimum required size if the terminal is too small.
- **FR-018**: The game MUST show the controls (arrow keys, Space to draw, Escape to quit) on
  screen.

### Key Entities

- **Playfield**: The fixed-size rectangular grid of cells the game takes place on. Each cell is
  in one state: unclaimed, claimed, border, or part of the line being drawn.
- **Marker**: The player-controlled cursor. It has a position on the playfield and a mode:
  travelling along the border, or drawing.
- **Line (trail)**: The ordered path of cells the marker has drawn since leaving the border. It
  exists only until it closes, then becomes border.
- **Claimed Region**: A connected area of the playfield that has been claimed. Its size counts
  toward the claimed percentage.
- **Progress**: The claimed share of the playfield as a percentage, compared against the 75%
  completion target.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time player can launch the game and claim their first region within
  60 seconds, using only the on-screen controls hint.
- **SC-002**: Every completed line produces a fill. Across a manual playtest of 20 different line
  shapes (straight, L-shaped, zig-zag, ending on the frame, ending on claimed edges), all 20 fill
  the correct region.
- **SC-003**: The fill and percentage update appear immediately when a line closes, with no
  delay the player can notice (under one tenth of a second).
- **SC-004**: The displayed percentage matches the true claimed share of the playfield, rounded
  down to a whole number, after every fill.
- **SC-005**: Marker movement looks smooth and steady: a held direction key produces movement at
  a constant pace, with no visible stutter or flicker during a 10-minute playtest.
- **SC-006**: A player can play from 0% to completing the playfield in a single session without
  the game crashing or freezing.
- **SC-007**: After quitting, the terminal is immediately usable for typing commands, in 100% of
  quits.

## Assumptions

- **Affected Qix behaviours** (constitution, Development Workflow): player movement, drawing,
  claiming. Not affected (not yet present): the Qix, the Sparx, scoring, levels.
- **No enemies or hazards**: As requested, there is no Qix, no Sparx, no fuse, no lives and no
  way to lose. The player can take as long as they like.
- **Which side gets claimed**: Classic Qix claims the side that does not contain the Qix. With no
  Qix yet, this feature claims the **smaller** region instead. That rule will be replaced once
  enemies are added in a later feature.
- **Single drawing speed**: Classic Qix has a fast and a slow draw (with slow draw scoring double
  points). This feature has a single drawing speed.
- **No score**: Scoring, high scores, and lives are out of scope. Progress is shown only as the
  claimed percentage.
- **Target of 75%**: The completion threshold is taken from classic Qix. There is only one level;
  "restart" starts the same empty playfield again.
- **Fixed playfield**: The playfield has a fixed size that fits a standard terminal window of at
  least 80 columns by 25 rows, including the status area. It does not resize with the window.
- **Controls**: Arrow keys move, Space draws, Escape quits; on the completion screen, R restarts
  and Escape quits. Controls are not configurable.
- **Single player, keyboard only**: There is no mouse, gamepad, or multiplayer support.
- **Platform**: The game runs in Windows Terminal, as stated in the project constitution.
