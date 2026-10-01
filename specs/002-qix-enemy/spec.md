# Feature Specification: Qix Enemy and Lives

**Feature Branch**: `002-qix-enemy`

**Created**: 2026-10-01

**Status**: Draft

**Input**: User description: "One Qix enemy bounces around the open area; touching the player's unfinished line costs a life."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A Qix Bounces Around the Open Area (Priority: P1)

When a playfield starts, a single Qix enemy appears in the middle of the unclaimed area and starts
moving on its own. It travels in straight diagonal lines and bounces off the outer frame and off
the edges of claimed territory, so it always stays inside the unclaimed area. It keeps moving
whether or not the player presses any keys.

**Why this priority**: The Qix is the threat the whole feature is about. On its own it already
changes the game visibly: the screen is now alive, and the player has to watch something other
than their own marker.

**Independent Test**: Launch the game, press no keys, and watch the Qix for 30 seconds: it moves
continuously, bounces off the frame, and never leaves the unclaimed area. Then claim a region and
confirm the Qix bounces off the new edge.

**Acceptance Scenarios**:

1. **Given** a playfield has just started, **When** the player looks at the screen, **Then** the
   Qix is shown inside the unclaimed area, near the center, and looks clearly different from the
   marker, the line, the border, and claimed territory.
2. **Given** the game is running, **When** the player presses no keys, **Then** the Qix keeps
   moving one cell at a time at a steady pace.
3. **Given** the Qix is moving diagonally, **When** its next step would enter a border cell or
   claimed territory, **Then** it bounces: it reverses the blocked part of its direction
   (horizontal, vertical, or both) and continues moving.
4. **Given** the Qix is moving, **When** the player moves the marker along the border, **Then**
   neither the marker nor the Qix is affected; the Qix never moves onto the border.

---

### User Story 2 - Touching the Unfinished Line Costs a Life (Priority: P2)

While the player is drawing, the line they leave behind is vulnerable. If the Qix touches any part
of the unfinished line, including the marker at its tip, the player loses a life. The line is
erased, the marker goes back to the border cell where the line started, and drawing switches off.
The player starts with 3 lives. When the last life is lost, the game is over and the player can
restart or quit.

**Why this priority**: This is the risk that turns claiming territory into a game. It depends on
the Qix existing (User Story 1).

**Independent Test**: Start the game, switch drawing on, and draw a line into the Qix's path.
Confirm the life counter drops by one, the line disappears, and the marker is back at the line's
starting point. Repeat until no lives remain and confirm the game-over message appears.

**Acceptance Scenarios**:

1. **Given** a playfield has just started, **When** the player looks at the status display,
   **Then** it shows 3 lives.
2. **Given** the player is drawing and a line exists, **When** the Qix moves onto any cell of the
   line or onto the marker, **Then** the player loses one life and the status display updates.
3. **Given** a life has just been lost, **When** play resumes, **Then** the line is gone, the
   marker is on the border cell where the line started, drawing is off, and nothing was claimed.
4. **Given** a life has just been lost, **When** the loss happens, **Then** the game shows it
   clearly (for example, a short flash or message) and pauses for about one second before play
   resumes.
5. **Given** the marker is on the border and not drawing, **When** the Qix passes nearby,
   **Then** nothing happens; the marker is safe on the border.
6. **Given** the player has switched drawing off while away from the border, **When** the Qix
   touches the line or the marker, **Then** a life is lost, exactly as if drawing were on.
7. **Given** the player has 1 life left, **When** that life is lost, **Then** a game-over message
   is shown, gameplay input pauses, and the player can restart (R) or quit (Escape).
8. **Given** the game-over or completion message is shown, **When** the player restarts,
   **Then** a fresh playfield appears with 0% claimed, 3 lives, the marker at its starting
   position, and the Qix back near the center.

---

### User Story 3 - Claim the Side Without the Qix (Priority: P3)

When the player closes a line, the region the Qix is in stays unclaimed and every other region
becomes claimed, whatever its size. This replaces the "claim the smaller side" rule of the
previous feature. A player can now trap the Qix in a small area and claim a large region in one
move.

**Why this priority**: Without this rule the Qix could end up inside claimed territory, which
breaks User Story 1. It also adds the classic Qix tactic of boxing the enemy in. It builds on both
earlier stories.

**Independent Test**: Wait until the Qix is in a small corner of the playfield, then cut off that
corner with a line. Confirm the large region is claimed, the small region with the Qix stays
open, and the Qix keeps bouncing inside it.

**Acceptance Scenarios**:

1. **Given** the player closes a line that splits the unclaimed area into two regions, **When**
   the fill happens, **Then** the region containing the Qix stays unclaimed and the other region
   is claimed, even if it is the larger one.
2. **Given** a line encloses pockets against itself, **When** the line closes, **Then** every
   region that does not contain the Qix is claimed.
3. **Given** a fill has just happened, **When** play continues, **Then** the Qix is still in
   unclaimed area and keeps bouncing within its region.
4. **Given** a fill claims a large region, **When** it brings the claimed share to 75% or more,
   **Then** the completion message is shown, exactly as in the previous feature.

---

### Edge Cases

- **Line closes as the Qix arrives**: If the player's move closes the line at the same moment the
  Qix would touch it, the close wins. The fill happens and no life is lost.
- **Qix touches the line start**: The border cell where the line started is border, not line.
  The Qix cannot enter it, so it bounces off it and no life is lost.
- **Bouncing in a corner**: When the Qix's diagonal step is blocked but a horizontal-only or
  vertical-only reversal is enough, it reverses just that part. When both are blocked (a corner),
  it reverses both.
- **Narrow areas**: In a corridor one cell wide, the Qix may have no diagonal move at all. It then
  moves straight along the corridor and bounces at its ends. If it has no move at all (a
  single-cell region), it stays still.
- **Qix region is tiny**: If the player traps the Qix in a very small region, the Qix keeps
  bouncing there. It cannot escape, and the claimed share may jump past 75% in one move. This is
  intended.
- **Stuck marker while drawing**: As in the previous feature, a marker boxed in by its own line
  and claimed territory cannot move. It stays stuck until the Qix touches the line or the player
  quits. Classic Qix's fuse, which would end this, is out of scope.
- **Lives at completion**: Reaching 75% ends the playfield even if lives were lost. Restarting
  after completion or game over always resets lives to 3.
- **Pause during life loss**: During the short pause after a lost life, key presses (other than
  quit) are ignored and the Qix does not move.

## Requirements *(mandatory)*

### Functional Requirements

**The Qix**

- **FR-001**: The game MUST place exactly one Qix in the unclaimed area, near the center of the
  playfield, whenever a playfield starts, moving in a diagonal direction chosen at random.
- **FR-002**: The Qix MUST move on its own, one cell per step at a steady pace of about 8 steps
  per second, independent of player input. It MUST keep moving while the player is idle.
- **FR-003**: The Qix MUST move in a straight diagonal line and bounce off border cells (outer
  frame and claimed-territory edges) as described under Edge Cases. It MUST never enter a border
  cell or claimed territory.
- **FR-004**: The Qix MUST be drawn so it is clearly distinguishable from the marker, the line,
  the border, unclaimed area, and claimed territory.

**Collisions and lives**

- **FR-005**: The player MUST start each playfield with 3 lives, and the status area MUST show
  the remaining lives at all times.
- **FR-006**: The game MUST take away one life when the Qix moves onto any cell of the unfinished
  line or onto the marker while a line exists. The marker on the border with no line is never
  harmed.
- **FR-007**: After a lost life, the game MUST erase the line, return the marker to the border
  cell where the line started, switch drawing off, and claim nothing.
- **FR-008**: The game MUST make a lost life clearly visible and pause play for about one second
  before resuming. During the pause, the Qix MUST NOT move and only the quit key MUST respond.
- **FR-009**: When the last life is lost, the game MUST show a game-over message, pause gameplay
  input, and offer to restart (R) or quit (Escape).
- **FR-010**: Restarting (after game over or completion) MUST reset the playfield to unclaimed,
  lives to 3, the marker to its starting position, and the Qix to its starting position with a
  new random direction.

**Claiming**

- **FR-011**: When a line closes, the game MUST keep the region containing the Qix unclaimed and
  claim every other region the line creates, regardless of size. This replaces the "claim all but
  the largest region" rule and the equal-size tie rule of the previous feature.
- **FR-012**: If a line closes in the same step that the Qix would touch it, the game MUST treat
  the close as happening first: the fill happens and no life is lost.

### Key Entities

- **Qix**: The enemy. It has a position in the unclaimed area and a diagonal direction of travel.
  It moves on its own and bounces off border cells.
- **Lives**: The number of mistakes the player can still afford on this playfield. Starts at 3,
  drops by one per Qix hit, and ends the game at 0.
- **Line (trail)**: As in the previous feature, plus the border cell where it started, which is
  where the marker returns after a lost life.
- **Game state**: Playing, life-lost pause, game over, or playfield complete. Only "playing"
  accepts movement input.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: During a 10-minute playtest, the Qix never appears on the border, inside claimed
  territory, or outside the playfield.
- **SC-002**: Every time the Qix touches the unfinished line or the drawing marker, exactly one
  life is lost. In a playtest of 20 deliberate hits, all 20 cost exactly one life, and no life is
  lost while the marker is on the border with no line.
- **SC-003**: The Qix moves smoothly at a steady pace with no visible flicker or stalls, whether
  or not the player is pressing keys.
- **SC-004**: After every fill in a playtest of 20 different line shapes, the Qix is still in
  unclaimed area, and the region it was in stayed unclaimed.
- **SC-005**: A player can play from a fresh playfield to either completion or game over, and
  then restart, without the game crashing or freezing.
- **SC-006**: A first-time player understands that the Qix is dangerous to their line, and how
  many lives they have, within their first minute of play, using only what is on screen.

## Assumptions

- **Affected Qix behaviours** (constitution, Development Workflow): the Qix (new), drawing (the
  line becomes vulnerable), claiming (the side without the Qix is claimed), and win/lose
  conditions (lives and game over are new). Not affected: player movement, the Sparx, scoring,
  levels.
- **Builds on feature 001**: Movement, drawing, claiming, the 75% target, the status area, and
  the restart and quit controls work as specified in `specs/001-player-movement-claiming`, except
  where this spec replaces them (which region is claimed).
- **One Qix, simple shape**: The Qix occupies a single cell. Classic Qix's sprawling multi-line
  shape and its erratic, random changes of direction are out of scope; this Qix moves like a
  bouncing ball.
- **3 lives**: The starting number of lives follows classic Qix. Lives are per playfield; there
  is no extra-life bonus.
- **Respawn point**: After a lost life, the marker returns to where the line started, as in
  classic Qix.
- **Only the line is vulnerable**: The Qix harms the player only through the unfinished line and
  the marker at its tip. The Sparx, which threaten the marker on the border, and the fuse, which
  burns along a stalled line, are out of scope.
- **Qix speed**: About 8 steps per second, slower than a held arrow key, so a careful player can
  outrun it. The exact pace may be tuned during playtesting.
- **No score**: Scoring remains out of scope.
