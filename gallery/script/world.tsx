// Putting things in the world from TypeScript.
//
// Every object below is spawned by this file. The engine asks for a frame, the
// step here runs, and what comes back is the whole world — a description, the
// same as the interface. A message that says everything cannot go stale, and
// nothing has to remember what it told the renderer last time.

import { spawn, destroy, all, count, clear, onFrame } from "orbis/scene";

const colours = ["#D9634F", "#5FA8D3", "#7FB069", "#E0B252", "#B48EAD"];

/** How many rings of them, and how wide the arrangement is. */
export const settings = { rings: 4, spin: 0.35, bob: true };

/** Lays the whole thing out again. Called when the settings change. */
export function build() {
  clear();

  // A pillar in the middle, to have something that is not part of a ring.
  spawn({
    id: "core",
    at: [0, 0.4, 0],
    size: [0.8, 2.4, 0.8],
    colour: "#F2F4F7",
  });

  for (let ring = 0; ring < settings.rings; ring++) {
    const radius = 2.4 + ring * 1.9;
    const many = 6 + ring * 4;

    for (let i = 0; i < many; i++) {
      const angle = (i / many) * Math.PI * 2;
      spawn({
        id: `r${ring}-${i}`,
        at: [Math.cos(angle) * radius, -0.6, Math.sin(angle) * radius],
        size: [0.5, 0.5 + ring * 0.25, 0.5],
        turn: (angle * 180) / Math.PI,
        colour: colours[(ring + i) % colours.length],
      });
    }
  }
}

/** One thing at a time, for the button. */
let dropped = 0;

export function drop() {
  const angle = dropped * 2.399963; // the golden angle, so they never line up
  const radius = 0.7 * Math.sqrt(dropped + 1);
  spawn({
    id: `dropped-${dropped}`,
    at: [Math.cos(angle) * radius, 2.5, Math.sin(angle) * radius],
    size: 0.35,
    colour: colours[dropped % colours.length],
  });
  dropped += 1;
}

export function clearDropped() {
  for (let i = 0; i < dropped; i++) destroy(`dropped-${i}`);
  dropped = 0;
}

export function howMany(): number {
  return count();
}

build();

// The step. Everything that moves, moves here — the host only asks for a
// frame and draws whatever it is told.
function step(seconds: number) {
  for (const thing of all()) {
    if (!thing.id.startsWith("r")) continue;

    const ring = Number(thing.id[1]);
    const turn = seconds * settings.spin * (ring % 2 === 0 ? 1 : -1);
    const radius = 2.4 + ring * 1.9;
    const [x, , z] = thing.at;
    const was = Math.atan2(z, x);

    thing.at = [
      Math.cos(was + turn * 0.02) * radius,
      settings.bob ? -0.6 + Math.sin(seconds * 1.4 + ring) * 0.25 : -0.6,
      Math.sin(was + turn * 0.02) * radius,
    ];
  }
}

// Told to the engine, which runs it before asking what is in the world.
onFrame(step);
