// A heads-up display, written in TypeScript and drawn by Flutter.
//
// This is the whole of what a game writes. The engine loads the compiled
// output of this file into its script host, asks it to describe the interface
// after every event, and builds real Flutter widgets from what comes back —
// laid out by Flutter, drawn by Impeller. The class names are the ones anybody
// who has written a web page already knows; what they resolve to is Flutter's
// own layout rather than a second box model pretending to be the web's.
//
// There is no state hook here and no lifecycle. Those exist to drive a
// reconciler, and the reconciler is on the other side — so state is a plain
// object and the interface is described again whenever it changes, which is
// exactly what `build` does on every setState.

import { mount, type Handler } from "orbis";

/** What the game knows. The host writes into this; the interface reads it. */
export const state = {
  hull: 0.72,
  score: 1840,
  accent: "ember",
  panel: true,
};

// Named so the host can reach them: a callback cannot cross to Dart, but the
// name of one can.
export const actions: Record<string, Handler> = {
  repair: () => {
    state.hull = Math.min(1, state.hull + 0.12);
  },
  damage: () => {
    state.hull = Math.max(0, state.hull - 0.15);
    state.score = Math.max(0, state.score - 25);
  },
};

function Chip() {
  return (
    <row class="gap-3 items-center px-4 py-3 rounded-lg bg-slate-900
                border border-slate-700 shadow"
         style="opacity: 0.94">
      <box class={`w-3 h-3 rounded-full bg-${state.accent}-500`} />
      <text class="text-lg font-semibold text-slate-100">Sector 12</text>
      <text class="text-sm text-slate-400">· holding</text>
    </row>
  );
}

/** A bar is two boxes: the track, and as much of it as is left. */
function Bar({ part }: { part: number }) {
  return (
    <box class="w-full h-2 rounded-full bg-slate-700 clip">
      <box class={`h-2 rounded-full bg-${state.accent}-500`}
           style={`width: ${Math.round(part * 224)}px`} />
    </box>
  );
}

function Readout() {
  return (
    <column class="gap-2 px-4 py-3 rounded-lg bg-slate-900 border
                   border-slate-700 w-64"
            style="opacity: 0.94">
      <row class="justify-between items-center">
        <text class="text-xs uppercase text-slate-400">Hull</text>
        <text class="text-xs text-slate-300">
          {Math.round(state.hull * 100)}%
        </text>
      </row>

      <Bar part={state.hull} />

      <row class="justify-between items-baseline pt-1">
        <text class="text-xs uppercase text-slate-400">Score</text>
        <text class="text-2xl font-semibold text-slate-100">{state.score}</text>
      </row>
    </column>
  );
}

function Actions() {
  return (
    <row class="gap-2">
      <button class={`px-3 py-2 rounded-md bg-${state.accent}-500 text-white
                      text-sm font-medium`}
              key="repair" onPressed={actions.repair}>
        Repair
      </button>
      <button class="px-3 py-2 rounded-md bg-slate-800 border border-slate-600
                     text-slate-200 text-sm"
              key="damage" onPressed={actions.damage}>
        Take damage
      </button>
    </row>
  );
}

function Hud() {
  return (
    <stack class="full">
      <column class="p-5 gap-3 items-start top-0 left-0">
        <Chip />
        <Readout />
        {state.panel && <Actions />}
      </column>
    </stack>
  );
}

mount(() => <Hud />);
