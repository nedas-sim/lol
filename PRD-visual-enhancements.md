# PRD: Stats Page Visual Enhancements

## Problem Statement

The stats page displays data correctly but feels static and inert. Numbers snap to values, nothing reacts to events, and there is no visual feedback that the page is alive. The experience is functional but not delightful.

## Solution

Add a suite of CSS/JS visual enhancements that make the page feel reactive and expressive — without adding dependencies or a build step. All enhancements are purely cosmetic and additive; the data layer is unchanged.

## User Stories

1. As a stats viewer, I want numbers to count up from zero when I first load the page, so that the totals feel earned rather than instantly appearing.
2. As a stats viewer, I want the total lol counter to briefly pop and glow when a new lol is detected by the poll, so that I get instant gratification without refreshing.
3. As a stats viewer, I want to see an animated flame next to my streak count, so that a long streak feels appropriately dramatic.
4. As a stats viewer, I want the flame to flicker faster and grow more intense as the streak number increases, so that a 50-day streak looks different from a 3-day streak.
5. As a stats viewer, I want the page title to occasionally glitch, so that the page has personality beyond its data.
6. As a stats viewer, I want the glitch to feel intentional and stylized, not broken, so that it reads as a design choice.
7. As a stats viewer, I want the current hour × weekday cell in the heatmap to pulse and glow, so that I can instantly see "I am here right now" without mentally computing the cell.
8. As a stats viewer, I want the pulsing cell to update as the hour changes, so that it always reflects the actual current time.
9. As a stats viewer, I want count-up animations to re-run on subsequent data refreshes (when counts change), so that a new lol registering in the streak or total feels like an event.
10. As a stats viewer, I want all animations to respect `prefers-reduced-motion`, so that the page is usable without visual noise if I want.
11. As a stats viewer, I want animations to work correctly in both dark mode and light mode, so that switching themes does not break or miscolor any effect.
12. As a stats viewer, I want the new-lol pop on the total counter to be subtle enough not to distract if I have the page open in the background, so that it does not feel alarming.
13. As a stats viewer, I want the streak flame to be absent (not rendered) when the streak is zero, so that a broken streak does not show a sad flickering flame.
14. As a stats viewer, I want the glitch title effect to fire on a random interval rather than on a fixed beat, so that it feels organic rather than mechanical.

## Implementation Decisions

### 1. New-lol pop on total counter

- The 2-second poll already computes the new total on each refresh. Track `previousTotal` across calls; when `newTotal > previousTotal`, trigger the animation.
- Animation: CSS keyframe — scale up briefly (1 → 1.15 → 1), add a green text-shadow glow, duration ~400ms. Applied by adding/removing a class (`lol-pop`) on the `.total` element.
- Remove the class after animation ends via `animationend` listener or a `setTimeout` matching the duration.

### 2. Count-up animation on numbers

- Applies to: `.total`, `.streak-box .val` (current streak, best streak, best day).
- On each data refresh where a value changes, animate from the previous value to the new one over ~600ms using `requestAnimationFrame` with an easing function (ease-out quad).
- Track previous values in module-level variables alongside the existing data flow.
- On first load, animate from 0 to the actual value.

### 3. Streak fire

- Render a `<span class="streak-fire">🔥</span>` adjacent to the current streak value element. Hidden (opacity 0, display none) when streak is 0.
- CSS keyframe `flicker`: alternates opacity between 0.8–1.0 and applies a slight scale/rotate wobble.
- Animation duration and intensity scale with streak length: encode breakpoints (1–5 days: slow gentle flicker; 6–14: medium; 15–29: fast; 30+: very fast + larger scale). Apply via data attribute or class tier on the streak box.
- No emoji font loading required; the 🔥 emoji is already rendered by the OS.

### 4. Glitch title

- Target: `<h1>` containing `:lol: Stats`.
- Pure CSS glitch: two `::before`/`::after` pseudo-elements with the same text content, clipped to different horizontal slices, offset by a few pixels in X, colored with the accent color at low opacity. Animated with a keyframe that rapidly steps through clip-rect positions.
- Trigger: a CSS class `glitch-active` added by JS on a random interval (every 20–45s, randomized). Class is removed after ~800ms (one glitch cycle).
- The `data-text` attribute on the `<h1>` feeds the pseudo-element `content: attr(data-text)`.

### 5. Current-cell pulse in hour × weekday heatmap

- After `buildHeatmap()` renders, identify the cell for `(today.getDay(), today.getHours())` and add class `cell-now` to it.
- CSS: `cell-now` has a keyframe `pulse` — box-shadow expands and fades (green glow, ~2s loop, `infinite`).
- Since `buildHeatmap` is called on every poll refresh, the `cell-now` class is naturally re-applied. No separate interval needed.
- The pulse color respects `--green` CSS var so it works in both themes automatically.

### Cross-cutting: reduced motion

All keyframe animations are wrapped in:
```css
@media (prefers-reduced-motion: reduce) {
  /* disable all animation keyframes */
}
```

### Cross-cutting: theme compatibility

- New-lol glow uses `var(--green)` for the text-shadow color.
- Streak fire uses no color (emoji only).
- Glitch pseudo-elements use `var(--accent)` for the color offset.
- Cell pulse uses `var(--green)` for box-shadow.

## Testing Decisions

All testing is manual browser testing — no test framework exists or is needed for a single-file no-build page.

**Good test = observable browser behavior, not implementation internals.**

What to verify per feature:

- **New-lol pop**: artificially increment the total in the DB (or temporarily lower `previousTotal` in the console) and confirm the pop fires exactly once, does not loop, and looks correct in both themes.
- **Count-up**: on first load with a populated DB, all four numbers animate smoothly. On subsequent refreshes with no data change, no animation fires.
- **Streak fire**: set streak to 0 (confirm no fire), 3, 10, 20, 35 (confirm each intensity tier). Confirm absent when streak = 0.
- **Glitch title**: open console, call the glitch trigger function directly to confirm it fires. Wait ~45s on the page to confirm the random timer fires at least once.
- **Cell pulse**: open page at different hours (or temporarily hardcode hour in buildHeatmap) and confirm the correct cell pulses. Confirm pulse color is correct in dark and light mode.
- **Reduced motion**: in Chrome DevTools → Rendering → "Emulate CSS media: prefers-reduced-motion: reduce" — confirm all animations stop.

## Out of Scope

- Sound effects on new lol detection.
- Confetti or particle effects.
- Toast/notification system.
- Any server-side changes; all enhancements are client-side only.
- Animated chart line draw (Chart.js animation is currently disabled with `animation: false` for performance; enabling it is a separate decision).
- Mobile-specific adaptations (page is desktop-only Chromium).

## Further Notes

- All five features are independent; they can be shipped in any order or partially.
- The single-file constraint means all CSS and JS lives in `stats.html`. Feature count is low enough that this is not a concern.
- The glitch effect requires the `<h1>` to carry `data-text=":lol: Stats"` as an attribute — a minor HTML change.
- Count-up and new-lol pop share the "track previous value" pattern; implement them together to avoid duplicating that logic.
