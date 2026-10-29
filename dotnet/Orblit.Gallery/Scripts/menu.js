"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.actions = exports.state = void 0;
const jsx_runtime_1 = require("orblit/jsx-runtime");
// A second interface, to show that the first one was not a special case.
//
// A pause menu: a heading, a column of choices, and a footer. Nothing here is
// a widget. It is the same description the engine takes for any interface,
// and the same class names.
const orblit_1 = require("orblit");
exports.state = {
    chosen: "Resume",
    volume: 0.7,
    difficulty: "Normal",
};
const choices = ["Resume", "Settings", "Controls", "Quit to menu"];
const levels = ["Gentle", "Normal", "Punishing"];
exports.actions = {};
for (const choice of choices) {
    exports.actions[choice] = () => {
        exports.state.chosen = choice;
    };
}
for (const level of levels) {
    exports.actions[level] = () => {
        exports.state.difficulty = level;
    };
}
/** One line of the menu. The chosen one is lit; the rest are not. */
function Choice({ label }) {
    const picked = exports.state.chosen === label;
    return ((0, jsx_runtime_1.jsx)("button", { class: `px-4 py-3 rounded-md text-base w-full
              ${picked ? "bg-ember-500 text-white font-semibold"
            : "bg-slate-800 text-slate-300"}`, onPressed: exports.actions[label], children: label }, label));
}
function Level({ label }) {
    const picked = exports.state.difficulty === label;
    return ((0, jsx_runtime_1.jsx)("button", { class: `flex-1 px-2 py-2 rounded text-xs text-center
              ${picked ? "bg-steel-600 text-white" : "bg-slate-800 text-slate-400"}`, onPressed: exports.actions[label], children: label }, label));
}
function Menu() {
    return ((0, jsx_runtime_1.jsx)("stack", { class: "full", children: (0, jsx_runtime_1.jsx)("column", { class: "p-6 gap-4 items-center justify-center full", children: (0, jsx_runtime_1.jsxs)("column", { class: "gap-3 p-6 rounded-lg bg-slate-900 border border-slate-700\n                       w-80", style: "opacity: 0.96", children: [(0, jsx_runtime_1.jsx)("text", { class: "text-2xl font-bold text-slate-100", children: "Paused" }), (0, jsx_runtime_1.jsx)("text", { class: "text-sm text-slate-400 pb-2", children: "Sector 12 \u00B7 holding" }), choices.map((label) => (0, jsx_runtime_1.jsx)(Choice, { label: label })), (0, jsx_runtime_1.jsx)("text", { class: "text-xs uppercase text-slate-400 pt-3", children: "Difficulty" }), (0, jsx_runtime_1.jsx)("row", { class: "gap-2", children: levels.map((label) => (0, jsx_runtime_1.jsx)(Level, { label: label })) })] }) }) }));
}
(0, orblit_1.mount)(() => (0, jsx_runtime_1.jsx)(Menu, {}));
