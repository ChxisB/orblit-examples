"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.actions = exports.state = void 0;
const jsx_runtime_1 = require("orblit/jsx-runtime");
// A heads-up display, written in TypeScript and drawn by Flutter.
//
// This is the whole of what a game writes. The engine loads the compiled
// output of this file into its script host, asks it to describe the interface
// after every event, and builds real Flutter widgets from what comes back,
// laid out by Flutter, drawn by Impeller. The class names are the ones anybody
// who has written a web page already knows; what they resolve to is Flutter's
// own layout rather than a second box model pretending to be the web's.
//
// There is no state hook here and no lifecycle. Those exist to drive a
// reconciler, and the reconciler is on the other side, so state is a plain
// object and the interface is described again whenever it changes, which is
// exactly what `build` does on every setState.
const orblit_1 = require("orblit");
/** What the game knows. The host writes into this; the interface reads it. */
exports.state = {
    hull: 0.72,
    score: 1840,
    accent: "ember",
    panel: true,
};
// Named so the host can reach them: a callback cannot cross to Dart, but the
// name of one can.
exports.actions = {
    repair: () => {
        exports.state.hull = Math.min(1, exports.state.hull + 0.12);
    },
    damage: () => {
        exports.state.hull = Math.max(0, exports.state.hull - 0.15);
        exports.state.score = Math.max(0, exports.state.score - 25);
    },
};
function Chip() {
    return ((0, jsx_runtime_1.jsxs)("row", { class: "gap-3 items-center px-4 py-3 rounded-lg bg-slate-900\n                border border-slate-700 shadow", style: "opacity: 0.94", children: [(0, jsx_runtime_1.jsx)("box", { class: `w-3 h-3 rounded-full bg-${exports.state.accent}-500` }), (0, jsx_runtime_1.jsx)("text", { class: "text-lg font-semibold text-slate-100", children: "Sector 12" }), (0, jsx_runtime_1.jsx)("text", { class: "text-sm text-slate-400", children: "\u00B7 holding" })] }));
}
/** A bar is two boxes: the track, and as much of it as is left. */
function Bar({ part }) {
    return ((0, jsx_runtime_1.jsx)("box", { class: "w-full h-2 rounded-full bg-slate-700 clip", children: (0, jsx_runtime_1.jsx)("box", { class: `h-2 rounded-full bg-${exports.state.accent}-500`, style: `width: ${Math.round(part * 224)}px` }) }));
}
function Readout() {
    return ((0, jsx_runtime_1.jsxs)("column", { class: "gap-2 px-4 py-3 rounded-lg bg-slate-900 border\n                   border-slate-700 w-64", style: "opacity: 0.94", children: [(0, jsx_runtime_1.jsxs)("row", { class: "justify-between items-center", children: [(0, jsx_runtime_1.jsx)("text", { class: "text-xs uppercase text-slate-400", children: "Hull" }), (0, jsx_runtime_1.jsxs)("text", { class: "text-xs text-slate-300", children: [Math.round(exports.state.hull * 100), "%"] })] }), (0, jsx_runtime_1.jsx)(Bar, { part: exports.state.hull }), (0, jsx_runtime_1.jsxs)("row", { class: "justify-between items-baseline pt-1", children: [(0, jsx_runtime_1.jsx)("text", { class: "text-xs uppercase text-slate-400", children: "Score" }), (0, jsx_runtime_1.jsx)("text", { class: "text-2xl font-semibold text-slate-100", children: exports.state.score })] })] }));
}
function Actions() {
    return ((0, jsx_runtime_1.jsxs)("row", { class: "gap-2", children: [(0, jsx_runtime_1.jsx)("button", { class: `px-3 py-2 rounded-md bg-${exports.state.accent}-500 text-white
                      text-sm font-medium`, onPressed: exports.actions.repair, children: "Repair" }, "repair"), (0, jsx_runtime_1.jsx)("button", { class: "px-3 py-2 rounded-md bg-slate-800 border border-slate-600\n                     text-slate-200 text-sm", onPressed: exports.actions.damage, children: "Take damage" }, "damage")] }));
}
function Hud() {
    return ((0, jsx_runtime_1.jsx)("stack", { class: "full", children: (0, jsx_runtime_1.jsxs)("column", { class: "p-5 gap-3 items-start top-0 left-0", children: [(0, jsx_runtime_1.jsx)(Chip, {}), (0, jsx_runtime_1.jsx)(Readout, {}), exports.state.panel && (0, jsx_runtime_1.jsx)(Actions, {})] }) }));
}
(0, orblit_1.mount)(() => (0, jsx_runtime_1.jsx)(Hud, {}));
