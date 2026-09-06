// A second interface, to show that the first one was not a special case.
//
// A pause menu: a heading, a column of choices, and a footer. Nothing here is
// a widget — it is the same description the engine takes for any interface,
// and the same class names.

import { mount, type Handler } from "orbis";

export const state = {
  chosen: "Resume",
  volume: 0.7,
  difficulty: "Normal",
};

const choices = ["Resume", "Settings", "Controls", "Quit to menu"];
const levels = ["Gentle", "Normal", "Punishing"];

export const actions: Record<string, Handler> = {};

for (const choice of choices) {
  actions[choice] = () => {
    state.chosen = choice;
  };
}

for (const level of levels) {
  actions[level] = () => {
    state.difficulty = level;
  };
}

/** One line of the menu. The chosen one is lit; the rest are not. */
function Choice({ label }: { label: string }) {
  const picked = state.chosen === label;
  return (
    <button
      class={`px-4 py-3 rounded-md text-base w-full
              ${picked ? "bg-ember-500 text-white font-semibold"
                       : "bg-slate-800 text-slate-300"}`}
      key={label}
      onPressed={actions[label]}
    >
      {label}
    </button>
  );
}

function Level({ label }: { label: string }) {
  const picked = state.difficulty === label;
  return (
    <button
      class={`flex-1 px-2 py-2 rounded text-xs text-center
              ${picked ? "bg-steel-600 text-white" : "bg-slate-800 text-slate-400"}`}
      key={label}
      onPressed={actions[label]}
    >
      {label}
    </button>
  );
}

function Menu() {
  return (
    <stack class="full">
      <column class="p-6 gap-4 items-center justify-center full">
        <column class="gap-3 p-6 rounded-lg bg-slate-900 border border-slate-700
                       w-80"
                style="opacity: 0.96">
          <text class="text-2xl font-bold text-slate-100">Paused</text>
          <text class="text-sm text-slate-400 pb-2">
            Sector 12 · holding
          </text>

          {choices.map((label) => <Choice label={label} />)}

          <text class="text-xs uppercase text-slate-400 pt-3">Difficulty</text>
          <row class="gap-2">
            {levels.map((label) => <Level label={label} />)}
          </row>
        </column>
      </column>
    </stack>
  );
}

mount(() => <Menu />);
