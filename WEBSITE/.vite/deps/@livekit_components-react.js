import { i as __toESM, t as require_react } from "./react-CZunmVaX.js";
import { ConnectionQuality, ConnectionState, ExternalE2EEKeyProvider, LocalTrackPublication, LogLevel, MediaDeviceFailure, Mutex as _, ParticipantEvent, ParticipantKind as ParticipantInfo_Kind, RemoteAudioTrack, RemoteTrackPublication, Room, RoomEvent, RpcError, TokenSourceConfigurable, Track, TrackEvent, VideoPresets, compareVersions, createAudioAnalyser, createLocalAudioTrack, createLocalTracks, createLocalVideoTrack, decodeTokenPayload, facingModeFromLocalTrack, getBrowser, isSerializer, serializers, setLogExtension, setLogLevel } from "./livekit-client.js";
//#region node_modules/@livekit/components-react/dist/rolldown-runtime-9ZSF5uba.mjs
var c = Object.create, l = Object.defineProperty, u = Object.getOwnPropertyDescriptor, s = Object.getOwnPropertyNames, v = Object.getPrototypeOf, P = Object.prototype.hasOwnProperty, j$2 = (e, r) => () => (r || (e((r = { exports: {} }).exports, r), e = null), r.exports), b = (e, r, a, p) => {
	if (r && typeof r == "object" || typeof r == "function") for (var _ = s(r), n = 0, o = _.length, t; n < o; n++) t = _[n], !P.call(e, t) && t !== a && l(e, t, {
		get: ((O) => r[O]).bind(null, t),
		enumerable: !(p = u(r, t)) || p.enumerable
	});
	return e;
}, f = (e, r, a) => (a = e != null ? c(v(e)) : {}, b(r || !e || !e.__esModule ? l(a, "default", {
	value: e,
	enumerable: !0
}) : a, e));
//#endregion
//#region node_modules/@livekit/components-react/dist/room-Bfb4OWAI.mjs
var import_react = /* @__PURE__ */ __toESM(require_react(), 1);
var Pi = (e) => {
	const t = import_react.useRef(e);
	return import_react.useEffect(() => {
		t.current = e;
	}), t;
};
function Mi(e, t) {
	const n = Li(), r = Pi(t);
	return import_react.useLayoutEffect(() => {
		let i = !1;
		const o = e.current;
		if (!o) return;
		function s(a, c) {
			i || r.current(a, c);
		}
		return n?.subscribe(o, s), () => {
			i = !0, n?.unsubscribe(o, s);
		};
	}, [
		e.current,
		n,
		r
	]), n?.observer;
}
function Ai() {
	let e = !1, t = [];
	const n = /* @__PURE__ */ new Map();
	if (typeof window > "u") return;
	const r = new ResizeObserver((i, o) => {
		t = t.concat(i), e || window.requestAnimationFrame(() => {
			const s = /* @__PURE__ */ new Set();
			for (let a = 0; a < t.length; a++) s.has(t[a].target) || (s.add(t[a].target), n.get(t[a].target)?.forEach((c) => c(t[a], o)));
			t = [], e = !1;
		}), e = !0;
	});
	return {
		observer: r,
		subscribe(i, o) {
			r.observe(i);
			const s = n.get(i) ?? [];
			s.push(o), n.set(i, s);
		},
		unsubscribe(i, o) {
			const s = n.get(i) ?? [];
			if (s.length === 1) {
				r.unobserve(i), n.delete(i);
				return;
			}
			const a = s.indexOf(o);
			a !== -1 && s.splice(a, 1), n.set(i, s);
		}
	};
}
var Nt, Li = () => Nt || (Nt = Ai()), Oi = (e) => {
	const [t, n] = import_react.useState({
		width: 0,
		height: 0
	});
	return import_react.useLayoutEffect(() => {
		if (e.current) {
			const { width: r, height: i } = e.current.getBoundingClientRect();
			n({
				width: r,
				height: i
			});
		}
	}, [e.current]), Mi(e, import_react.useCallback((r) => n(r.contentRect), [])), t;
};
function j$1(e, t, n = !0) {
	const [r, i] = import_react.useState(t);
	return import_react.useEffect(() => {
		if (n && i(t), typeof window > "u" || !e) return;
		const o = e.subscribe(i);
		return () => o.unsubscribe();
	}, [e, n]), r;
}
var lt = Math.min, Ae$1 = Math.max, ft = Math.round, rt = Math.floor, fe = (e) => ({
	x: e,
	y: e
}), _i = {
	left: "right",
	right: "left",
	bottom: "top",
	top: "bottom"
};
function Ii(e, t, n) {
	return Ae$1(e, lt(t, n));
}
function Et(e, t) {
	return typeof e == "function" ? e(t) : e;
}
function We(e) {
	return e.split("-")[0];
}
function Ct(e) {
	return e.split("-")[1];
}
function Zn(e) {
	return e === "x" ? "y" : "x";
}
function er(e) {
	return e === "y" ? "height" : "width";
}
function Ee$1(e) {
	const t = e[0];
	return t === "t" || t === "b" ? "y" : "x";
}
function tr(e) {
	return Zn(Ee$1(e));
}
function Ri(e, t, n) {
	n === void 0 && (n = !1);
	const r = Ct(e), i = tr(e), o = er(i);
	let s = i === "x" ? r === (n ? "end" : "start") ? "right" : "left" : r === "start" ? "bottom" : "top";
	return t.reference[o] > t.floating[o] && (s = dt(s)), [s, dt(s)];
}
function Di(e) {
	const t = dt(e);
	return [
		qt(e),
		t,
		qt(t)
	];
}
function qt(e) {
	return e.includes("start") ? e.replace("start", "end") : e.replace("end", "start");
}
var Sn = ["left", "right"], En = ["right", "left"], Ni = ["top", "bottom"], Fi = ["bottom", "top"];
function Ui(e, t, n) {
	switch (e) {
		case "top":
		case "bottom": return n ? t ? En : Sn : t ? Sn : En;
		case "left":
		case "right": return t ? Ni : Fi;
		default: return [];
	}
}
function $i(e, t, n, r) {
	const i = Ct(e);
	let o = Ui(We(e), n === "start", r);
	return i && (o = o.map((s) => s + "-" + i), t && (o = o.concat(o.map(qt)))), o;
}
function dt(e) {
	const t = We(e);
	return _i[t] + e.slice(t.length);
}
function Wi(e) {
	var t, n, r, i;
	return {
		top: (t = e.top) != null ? t : 0,
		right: (n = e.right) != null ? n : 0,
		bottom: (r = e.bottom) != null ? r : 0,
		left: (i = e.left) != null ? i : 0
	};
}
function ji(e) {
	return typeof e != "number" ? Wi(e) : {
		top: e,
		right: e,
		bottom: e,
		left: e
	};
}
function pt(e) {
	const { x: t, y: n, width: r, height: i } = e;
	return {
		width: r,
		height: i,
		top: n,
		left: t,
		right: t + r,
		bottom: n + i,
		x: t,
		y: n
	};
}
function Cn(e, t, n) {
	let { reference: r, floating: i } = e;
	const o = Ee$1(t), s = tr(t), a = er(s), c = We(t), u = o === "y", f = r.x + r.width / 2 - i.width / 2, d = r.y + r.height / 2 - i.height / 2, p = r[a] / 2 - i[a] / 2;
	let h;
	switch (c) {
		case "top":
			h = {
				x: f,
				y: r.y - i.height
			};
			break;
		case "bottom":
			h = {
				x: f,
				y: r.y + r.height
			};
			break;
		case "right":
			h = {
				x: r.x + r.width,
				y: d
			};
			break;
		case "left":
			h = {
				x: r.x - i.width,
				y: d
			};
			break;
		default: h = {
			x: r.x,
			y: r.y
		};
	}
	const m = Ct(t);
	return m && (h[s] += p * (m === "end" ? 1 : -1) * (n && u ? -1 : 1)), h;
}
async function Bi(e, t) {
	var n;
	t === void 0 && (t = {});
	const { x: r, y: i, platform: o, rects: s, elements: a, strategy: c } = e, { boundary: u = "clippingAncestors", rootBoundary: f = "viewport", elementContext: d = "floating", altBoundary: p = !1, padding: h = 0 } = Et(t, e), m = ji(h), S = a[p ? d === "floating" ? "reference" : "floating" : d], y = pt(await o.getClippingRect({
		element: (n = await (o.isElement == null ? void 0 : o.isElement(S))) == null || n ? S : S.contextElement || await (o.getDocumentElement == null ? void 0 : o.getDocumentElement(a.floating)),
		boundary: u,
		rootBoundary: f,
		strategy: c
	})), b = d === "floating" ? {
		x: r,
		y: i,
		width: s.floating.width,
		height: s.floating.height
	} : s.reference, w = await (o.getOffsetParent == null ? void 0 : o.getOffsetParent(a.floating)), P = await (o.isElement == null ? void 0 : o.isElement(w)) && await (o.getScale == null ? void 0 : o.getScale(w)) || {
		x: 1,
		y: 1
	}, N = pt(o.convertOffsetParentRelativeRectToViewportRelativeRect ? await o.convertOffsetParentRelativeRectToViewportRelativeRect({
		elements: a,
		rect: b,
		offsetParent: w,
		strategy: c
	}) : b);
	return {
		top: (y.top - N.top + m.top) / P.y,
		bottom: (N.bottom - y.bottom + m.bottom) / P.y,
		left: (y.left - N.left + m.left) / P.x,
		right: (N.right - y.right + m.right) / P.x
	};
}
var Vi = 50, Hi = async (e, t, n) => {
	const { placement: r = "bottom", strategy: i = "absolute", middleware: o = [], platform: s } = n, a = s.detectOverflow ? s : {
		...s,
		detectOverflow: Bi
	}, c = await (s.isRTL == null ? void 0 : s.isRTL(t));
	let u = await s.getElementRects({
		reference: e,
		floating: t,
		strategy: i
	}), { x: f, y: d } = Cn(u, r, c), p = r, h = 0;
	const m = {};
	for (let S = 0; S < o.length; S++) {
		const y = o[S];
		if (!y) continue;
		const { name: b, fn: w } = y, { x: P, y: N, data: M, reset: v } = await w({
			x: f,
			y: d,
			initialPlacement: r,
			placement: p,
			strategy: i,
			middlewareData: m,
			rects: u,
			platform: a,
			elements: {
				reference: e,
				floating: t
			}
		});
		f = P ?? f, d = N ?? d, m[b] = {
			...m[b],
			...M
		}, v && h < Vi && (h++, typeof v == "object" && (v.placement && (p = v.placement), v.rects && (u = v.rects === !0 ? await s.getElementRects({
			reference: e,
			floating: t,
			strategy: i
		}) : v.rects), {x: f, y: d} = Cn(u, p, c)), S = -1);
	}
	return {
		x: f,
		y: d,
		placement: p,
		strategy: i,
		middlewareData: m
	};
}, zi = function(e) {
	return e === void 0 && (e = {}), {
		name: "flip",
		options: e,
		async fn(t) {
			var n, r;
			const { placement: i, middlewareData: o, rects: s, initialPlacement: a, platform: c, elements: u } = t, { mainAxis: f = !0, crossAxis: d = !0, fallbackPlacements: p, fallbackStrategy: h = "bestFit", fallbackAxisSideDirection: m = "none", flipAlignment: S = !0, ...y } = Et(e, t);
			if ((n = o.arrow) != null && n.alignmentOffset) return {};
			const b = We(i), w = Ee$1(a), P = We(a) === a, N = await (c.isRTL == null ? void 0 : c.isRTL(u.floating)), M = p || (P || !S ? [dt(a)] : Di(a)), v = m !== "none";
			!p && v && M.push(...$i(a, S, m, N));
			const g = [a, ...M], E = await c.detectOverflow(t, y), x = [];
			let C = ((r = o.flip) == null ? void 0 : r.overflows) || [];
			if (f && x.push(E[b]), d) {
				const D = Ri(i, s, N);
				x.push(E[D[0]], E[D[1]]);
			}
			if (C = [...C, {
				placement: i,
				overflows: x
			}], !x.every((D) => D <= 0)) {
				var I, T;
				const D = (((I = o.flip) == null ? void 0 : I.index) || 0) + 1, U = g[D];
				if (U && (!(d === "alignment" && w !== Ee$1(U)) || C.every((z) => Ee$1(z.placement) === w ? z.overflows[0] > 0 : !0))) return {
					data: {
						index: D,
						overflows: C
					},
					reset: { placement: U }
				};
				let H = (T = C.filter((z) => z.overflows[0] <= 0).sort((z, V) => z.overflows[1] - V.overflows[1])[0]) == null ? void 0 : T.placement;
				if (!H) switch (h) {
					case "bestFit": {
						var A;
						const z = (A = C.filter((V) => {
							if (v) {
								const Q = Ee$1(V.placement);
								return Q === w || Q === "y";
							}
							return !0;
						}).map((V) => [V.placement, V.overflows.filter((Q) => Q > 0).reduce((Q, xe) => Q + xe, 0)]).sort((V, Q) => V[1] - Q[1])[0]) == null ? void 0 : A[0];
						z && (H = z);
						break;
					}
					case "initialPlacement":
						H = a;
						break;
				}
				if (i !== H) return { reset: { placement: H } };
			}
			return {};
		}
	};
}, qi = /* @__PURE__ */ new Set(["left", "top"]);
async function Ki(e, t) {
	const { placement: n, platform: r, elements: i } = e, o = await (r.isRTL == null ? void 0 : r.isRTL(i.floating)), s = We(n), a = Ct(n), c = Ee$1(n) === "y", u = qi.has(s) ? -1 : 1, f = o && c ? -1 : 1, d = Et(t, e);
	let { mainAxis: p, crossAxis: h, alignmentAxis: m } = typeof d == "number" ? {
		mainAxis: d,
		crossAxis: 0,
		alignmentAxis: null
	} : {
		mainAxis: d.mainAxis || 0,
		crossAxis: d.crossAxis || 0,
		alignmentAxis: d.alignmentAxis
	};
	return a && typeof m == "number" && (h = a === "end" ? m * -1 : m), c ? {
		x: h * f,
		y: p * u
	} : {
		x: p * u,
		y: h * f
	};
}
var Gi = function(e) {
	return e === void 0 && (e = 0), {
		name: "offset",
		options: e,
		async fn(t) {
			var n, r;
			const { x: i, y: o, placement: s, middlewareData: a } = t, c = await Ki(t, e);
			return s === ((n = a.offset) == null ? void 0 : n.placement) && (r = a.arrow) != null && r.alignmentOffset ? {} : {
				x: i + c.x,
				y: o + c.y,
				data: {
					...c,
					placement: s
				}
			};
		}
	};
}, Ji = function(e) {
	return e === void 0 && (e = {}), {
		name: "shift",
		options: e,
		async fn(t) {
			const { x: n, y: r, placement: i, platform: o } = t, { mainAxis: s = !0, crossAxis: a = !1, limiter: c = { fn: (w) => {
				let { x: P, y: N } = w;
				return {
					x: P,
					y: N
				};
			} }, ...u } = Et(e, t), f = {
				x: n,
				y: r
			}, d = await o.detectOverflow(t, u), p = Ee$1(i), h = Zn(p);
			let m = f[h], S = f[p];
			const y = (w, P) => Ii(P + d[w === "y" ? "top" : "left"], P, P - d[w === "y" ? "bottom" : "right"]);
			s && (m = y(h, m)), a && (S = y(p, S));
			const b = c.fn({
				...t,
				[h]: m,
				[p]: S
			});
			return {
				...b,
				data: {
					x: b.x - n,
					y: b.y - r,
					enabled: {
						[h]: s,
						[p]: a
					}
				}
			};
		}
	};
};
function Tt() {
	return typeof window < "u";
}
function He(e) {
	return nr(e) ? (e.nodeName || "").toLowerCase() : "#document";
}
function ie$1(e) {
	var t;
	return (e == null || (t = e.ownerDocument) == null ? void 0 : t.defaultView) || window;
}
function de(e) {
	var t;
	return (t = (nr(e) ? e.ownerDocument : e.document) || window.document) == null ? void 0 : t.documentElement;
}
function nr(e) {
	return Tt() ? e instanceof Node || e instanceof ie$1(e).Node : !1;
}
function ue(e) {
	return Tt() ? e instanceof Element || e instanceof ie$1(e).Element : !1;
}
function ye(e) {
	return Tt() ? e instanceof HTMLElement || e instanceof ie$1(e).HTMLElement : !1;
}
function Tn(e) {
	return !Tt() || typeof ShadowRoot > "u" ? !1 : e instanceof ShadowRoot || e instanceof ie$1(e).ShadowRoot;
}
function Ze(e) {
	const { overflow: t, overflowX: n, overflowY: r, display: i } = le$1(e);
	return /auto|scroll|overlay|hidden|clip/.test(t + r + n) && i !== "inline" && i !== "contents";
}
function Yi(e) {
	return /^(table|td|th)$/.test(He(e));
}
function xt(e) {
	try {
		if (e.matches(":popover-open")) return !0;
	} catch {}
	try {
		return e.matches(":modal");
	} catch {
		return !1;
	}
}
var Qi = /transform|translate|scale|rotate|perspective|filter/, Xi = /paint|layout|strict|content/, ke$1 = (e) => !!e && e !== "none", Ft;
function nn(e) {
	const t = ue(e) ? le$1(e) : e;
	return ke$1(t.transform) || ke$1(t.translate) || ke$1(t.scale) || ke$1(t.rotate) || ke$1(t.perspective) || !rn() && (ke$1(t.backdropFilter) || ke$1(t.filter)) || Qi.test(t.willChange || "") || Xi.test(t.contain || "");
}
function Zi(e) {
	let t = Ce(e);
	for (; ye(t) && !je(t);) {
		if (nn(t)) return t;
		if (xt(t)) return null;
		t = Ce(t);
	}
	return null;
}
function rn() {
	return Ft ??= typeof CSS < "u" && CSS.supports && CSS.supports("-webkit-backdrop-filter", "none"), Ft;
}
function je(e) {
	return /^(html|body|#document)$/.test(He(e));
}
function le$1(e) {
	return ie$1(e).getComputedStyle(e);
}
function kt(e) {
	return ue(e) ? {
		scrollLeft: e.scrollLeft,
		scrollTop: e.scrollTop
	} : {
		scrollLeft: e.scrollX,
		scrollTop: e.scrollY
	};
}
function Ce(e) {
	if (He(e) === "html") return e;
	const t = e.assignedSlot || e.parentNode || Tn(e) && e.host || de(e);
	return Tn(t) ? t.host : t;
}
function rr(e) {
	const t = Ce(e);
	return je(t) ? (e.ownerDocument || e).body : ye(t) && Ze(t) ? t : rr(t);
}
function Ye(e, t, n) {
	var r;
	t === void 0 && (t = []), n === void 0 && (n = !0);
	const i = rr(e), o = i === ((r = e.ownerDocument) == null ? void 0 : r.body), s = ie$1(i);
	if (o) {
		const a = Kt$1(s);
		return t.concat(s, s.visualViewport || [], Ze(i) ? i : [], a && n ? Ye(a) : []);
	} else return t.concat(i, Ye(i, [], n));
}
function Kt$1(e) {
	return e.parent && Object.getPrototypeOf(e.parent) ? e.frameElement : null;
}
function ir(e) {
	const t = le$1(e);
	let n = parseFloat(t.width) || 0, r = parseFloat(t.height) || 0;
	const i = ye(e), o = i ? e.offsetWidth : n, s = i ? e.offsetHeight : r, a = ft(n) !== o || ft(r) !== s;
	return a && (n = o, r = s), {
		width: n,
		height: r,
		$: a
	};
}
function on(e) {
	return ue(e) ? e : e.contextElement;
}
function Fe$1(e) {
	const t = on(e);
	if (!ye(t)) return fe(1);
	const n = t.getBoundingClientRect(), { width: r, height: i, $: o } = ir(t);
	let s = (o ? ft(n.width) : n.width) / r, a = (o ? ft(n.height) : n.height) / i;
	return (!s || !Number.isFinite(s)) && (s = 1), (!a || !Number.isFinite(a)) && (a = 1), {
		x: s,
		y: a
	};
}
var eo = /* @__PURE__ */ fe(0);
function or(e) {
	const t = ie$1(e);
	return !rn() || !t.visualViewport ? eo : {
		x: t.visualViewport.offsetLeft,
		y: t.visualViewport.offsetTop
	};
}
function to(e, t, n) {
	return t === void 0 && (t = !1), !n || t && n !== ie$1(e) ? !1 : t;
}
function Le$1(e, t, n, r) {
	t === void 0 && (t = !1), n === void 0 && (n = !1);
	const i = e.getBoundingClientRect(), o = on(e);
	let s = fe(1);
	t && (r ? ue(r) && (s = Fe$1(r)) : s = Fe$1(e));
	const a = to(o, n, r) ? or(o) : fe(0);
	let c = (i.left + a.x) / s.x, u = (i.top + a.y) / s.y, f = i.width / s.x, d = i.height / s.y;
	if (o) {
		const p = ie$1(o), h = r && ue(r) ? ie$1(r) : r;
		let m = p, S = Kt$1(m);
		for (; S && r && h !== m;) {
			const y = Fe$1(S), b = S.getBoundingClientRect(), w = le$1(S), P = b.left + (S.clientLeft + parseFloat(w.paddingLeft)) * y.x, N = b.top + (S.clientTop + parseFloat(w.paddingTop)) * y.y;
			c *= y.x, u *= y.y, f *= y.x, d *= y.y, c += P, u += N, m = ie$1(S), S = Kt$1(m);
		}
	}
	return pt({
		width: f,
		height: d,
		x: c,
		y: u
	});
}
function Pt(e, t) {
	const n = kt(e).scrollLeft;
	return t ? t.left + n : Le$1(de(e)).left + n;
}
function sr(e, t) {
	const n = e.getBoundingClientRect();
	return {
		x: n.left + t.scrollLeft - Pt(e, n),
		y: n.top + t.scrollTop
	};
}
function no(e) {
	let { elements: t, rect: n, offsetParent: r, strategy: i } = e;
	const o = i === "fixed", s = de(r), a = t ? xt(t.floating) : !1;
	if (r === s || a && o) return n;
	let c = {
		scrollLeft: 0,
		scrollTop: 0
	}, u = fe(1);
	const f = fe(0), d = ye(r);
	if ((d || !d && !o) && ((He(r) !== "body" || Ze(s)) && (c = kt(r)), d)) {
		const h = Le$1(r);
		u = Fe$1(r), f.x = h.x + r.clientLeft, f.y = h.y + r.clientTop;
	}
	const p = s && !d && !o ? sr(s, c) : fe(0);
	return {
		width: n.width * u.x,
		height: n.height * u.y,
		x: n.x * u.x - c.scrollLeft * u.x + f.x + p.x,
		y: n.y * u.y - c.scrollTop * u.y + f.y + p.y
	};
}
function ro(e) {
	return Array.from(e.getClientRects());
}
function io(e) {
	const t = de(e), n = kt(e), r = e.ownerDocument.body, i = Ae$1(t.scrollWidth, t.clientWidth, r.scrollWidth, r.clientWidth), o = Ae$1(t.scrollHeight, t.clientHeight, r.scrollHeight, r.clientHeight);
	let s = -n.scrollLeft + Pt(e);
	const a = -n.scrollTop;
	return le$1(r).direction === "rtl" && (s += Ae$1(t.clientWidth, r.clientWidth) - i), {
		width: i,
		height: o,
		x: s,
		y: a
	};
}
var xn = 25;
function oo(e, t) {
	const n = ie$1(e), r = de(e), i = n.visualViewport;
	let o = r.clientWidth, s = r.clientHeight, a = 0, c = 0;
	if (i) {
		o = i.width, s = i.height;
		const f = rn();
		(!f || f && t === "fixed") && (a = i.offsetLeft, c = i.offsetTop);
	}
	const u = Pt(r);
	if (u <= 0) {
		const f = r.ownerDocument, d = f.body, p = getComputedStyle(d), h = f.compatMode === "CSS1Compat" && parseFloat(p.marginLeft) + parseFloat(p.marginRight) || 0, m = Math.abs(r.clientWidth - d.clientWidth - h);
		m <= xn && (o -= m);
	} else u <= xn && (o += u);
	return {
		width: o,
		height: s,
		x: a,
		y: c
	};
}
function so(e, t) {
	const n = Le$1(e, !0, t === "fixed"), r = n.top + e.clientTop, i = n.left + e.clientLeft, o = ye(e) ? Fe$1(e) : fe(1);
	return {
		width: e.clientWidth * o.x,
		height: e.clientHeight * o.y,
		x: i * o.x,
		y: r * o.y
	};
}
function kn(e, t, n) {
	let r;
	if (t === "viewport") r = oo(e, n);
	else if (t === "document") r = io(de(e));
	else if (ue(t)) r = so(t, n);
	else {
		const i = or(e);
		r = {
			x: t.x - i.x,
			y: t.y - i.y,
			width: t.width,
			height: t.height
		};
	}
	return pt(r);
}
function ar(e, t) {
	const n = Ce(e);
	return n === t || !ue(n) || je(n) ? !1 : le$1(n).position === "fixed" || ar(n, t);
}
function ao(e, t) {
	const n = t.get(e);
	if (n) return n;
	let r = Ye(e, [], !1).filter((a) => ue(a) && He(a) !== "body"), i = null;
	const o = le$1(e).position === "fixed";
	let s = o ? Ce(e) : e;
	for (; ue(s) && !je(s);) {
		const a = le$1(s), c = nn(s);
		!c && a.position === "fixed" && (i = null), (o ? !c && !i : !c && a.position === "static" && i && (i.position === "absolute" || i.position === "fixed") || Ze(s) && !c && ar(e, s)) ? r = r.filter((u) => u !== s) : i = a, s = Ce(s);
	}
	return t.set(e, r), r;
}
function co(e) {
	let { element: t, boundary: n, rootBoundary: r, strategy: i } = e;
	const o = [...n === "clippingAncestors" ? xt(t) ? [] : ao(t, this._c) : [].concat(n), r], s = kn(t, o[0], i);
	let a = s.top, c = s.right, u = s.bottom, f = s.left;
	for (let d = 1; d < o.length; d++) {
		const p = kn(t, o[d], i);
		a = Ae$1(p.top, a), c = lt(p.right, c), u = lt(p.bottom, u), f = Ae$1(p.left, f);
	}
	return {
		width: c - f,
		height: u - a,
		x: f,
		y: a
	};
}
function uo(e) {
	const { width: t, height: n } = ir(e);
	return {
		width: t,
		height: n
	};
}
function lo(e, t, n) {
	const r = ye(t), i = de(t), o = n === "fixed", s = Le$1(e, !0, o, t);
	let a = {
		scrollLeft: 0,
		scrollTop: 0
	};
	const c = fe(0);
	function u() {
		c.x = Pt(i);
	}
	if (r || !r && !o) if ((He(t) !== "body" || Ze(i)) && (a = kt(t)), r) {
		const d = Le$1(t, !0, o, t);
		c.x = d.x + t.clientLeft, c.y = d.y + t.clientTop;
	} else i && u();
	o && !r && i && u();
	const f = i && !r && !o ? sr(i, a) : fe(0);
	return {
		x: s.left + a.scrollLeft - c.x - f.x,
		y: s.top + a.scrollTop - c.y - f.y,
		width: s.width,
		height: s.height
	};
}
function Ut(e) {
	return le$1(e).position === "static";
}
function Pn(e, t) {
	if (!ye(e) || le$1(e).position === "fixed") return null;
	if (t) return t(e);
	let n = e.offsetParent;
	return de(e) === n && (n = n.ownerDocument.body), n;
}
function cr(e, t) {
	const n = ie$1(e);
	if (xt(e)) return n;
	if (!ye(e)) {
		let i = Ce(e);
		for (; i && !je(i);) {
			if (ue(i) && !Ut(i)) return i;
			i = Ce(i);
		}
		return n;
	}
	let r = Pn(e, t);
	for (; r && Yi(r) && Ut(r);) r = Pn(r, t);
	return r && je(r) && Ut(r) && !nn(r) ? n : r || Zi(e) || n;
}
var fo = async function(e) {
	const t = this.getOffsetParent || cr, n = this.getDimensions, r = await n(e.floating);
	return {
		reference: lo(e.reference, await t(e.floating), e.strategy),
		floating: {
			x: 0,
			y: 0,
			width: r.width,
			height: r.height
		}
	};
};
function po(e) {
	return le$1(e).direction === "rtl";
}
var ho = {
	convertOffsetParentRelativeRectToViewportRelativeRect: no,
	getDocumentElement: de,
	getClippingRect: co,
	getOffsetParent: cr,
	getElementRects: fo,
	getClientRects: ro,
	getDimensions: uo,
	getScale: Fe$1,
	isElement: ue,
	isRTL: po
};
function ur(e, t) {
	return e.x === t.x && e.y === t.y && e.width === t.width && e.height === t.height;
}
function vo(e, t) {
	let n = null, r;
	const i = de(e);
	function o() {
		var a;
		clearTimeout(r), (a = n) == null || a.disconnect(), n = null;
	}
	function s(a, c) {
		a === void 0 && (a = !1), c === void 0 && (c = 1), o();
		const u = e.getBoundingClientRect(), { left: f, top: d, width: p, height: h } = u;
		if (a || t(), !p || !h) return;
		const m = rt(d), S = rt(i.clientWidth - (f + p)), y = rt(i.clientHeight - (d + h)), b = rt(f), w = {
			rootMargin: -m + "px " + -S + "px " + -y + "px " + -b + "px",
			threshold: Ae$1(0, lt(1, c)) || 1
		};
		let P = !0;
		function N(M) {
			const v = M[0].intersectionRatio;
			if (v !== c) {
				if (!P) return s();
				v ? s(!1, v) : r = setTimeout(() => {
					s(!1, 1e-7);
				}, 1e3);
			}
			v === 1 && !ur(u, e.getBoundingClientRect()) && s(), P = !1;
		}
		try {
			n = new IntersectionObserver(N, {
				...w,
				root: i.ownerDocument
			});
		} catch {
			n = new IntersectionObserver(N, w);
		}
		n.observe(e);
	}
	return s(!0), o;
}
function mo(e, t, n, r) {
	r === void 0 && (r = {});
	const { ancestorScroll: i = !0, ancestorResize: o = !0, elementResize: s = typeof ResizeObserver == "function", layoutShift: a = typeof IntersectionObserver == "function", animationFrame: c = !1 } = r, u = on(e), f = i || o ? [...u ? Ye(u) : [], ...t ? Ye(t) : []] : [];
	f.forEach((b) => {
		i && b.addEventListener("scroll", n, { passive: !0 }), o && b.addEventListener("resize", n);
	});
	const d = u && a ? vo(u, n) : null;
	let p = -1, h = null;
	s && (h = new ResizeObserver((b) => {
		let [w] = b;
		w && w.target === u && h && t && (h.unobserve(t), cancelAnimationFrame(p), p = requestAnimationFrame(() => {
			var P;
			(P = h) == null || P.observe(t);
		})), n();
	}), u && !c && h.observe(u), t && h.observe(t));
	let m, S = c ? Le$1(e) : null;
	c && y();
	function y() {
		const b = Le$1(e);
		S && !ur(S, b) && n(), S = b, m = requestAnimationFrame(y);
	}
	return n(), () => {
		var b;
		f.forEach((w) => {
			i && w.removeEventListener("scroll", n), o && w.removeEventListener("resize", n);
		}), d?.(), (b = h) == null || b.disconnect(), h = null, c && cancelAnimationFrame(m);
	};
}
var go = Gi, bo = Ji, yo = zi, wo = (e, t, n) => {
	const r = /* @__PURE__ */ new Map(), i = {
		platform: ho,
		...n
	}, o = {
		...i.platform,
		_c: r
	};
	return Hi(e, t, {
		...i,
		platform: o
	});
}, So = /* @__PURE__ */ j$2(((e, t) => {
	(function(n, r) {
		"use strict";
		typeof define == "function" && define.amd ? define(r) : typeof t == "object" && t.exports ? t.exports = r() : n.log = r();
	})(e, function() {
		"use strict";
		var n = function() {}, r = "undefined", i = typeof window !== r && typeof window.navigator !== r && /Trident\/|MSIE /.test(window.navigator.userAgent), o = [
			"trace",
			"debug",
			"info",
			"warn",
			"error"
		], s = {}, a = null;
		function c(y, b) {
			var w = y[b];
			if (typeof w.bind == "function") return w.bind(y);
			try {
				return Function.prototype.bind.call(w, y);
			} catch {
				return function() {
					return Function.prototype.apply.apply(w, [y, arguments]);
				};
			}
		}
		function u() {
			console.log && (console.log.apply ? console.log.apply(console, arguments) : Function.prototype.apply.apply(console.log, [console, arguments])), console.trace && console.trace();
		}
		function f(y) {
			return y === "debug" && (y = "log"), typeof console === r ? !1 : y === "trace" && i ? u : console[y] !== void 0 ? c(console, y) : console.log !== void 0 ? c(console, "log") : n;
		}
		function d() {
			for (var y = this.getLevel(), b = 0; b < o.length; b++) {
				var w = o[b];
				this[w] = b < y ? n : this.methodFactory(w, y, this.name);
			}
			if (this.log = this.debug, typeof console === r && y < this.levels.SILENT) return "No console available for logging";
		}
		function p(y) {
			return function() {
				typeof console !== r && (d.call(this), this[y].apply(this, arguments));
			};
		}
		function h(y, b, w) {
			return f(y) || p.apply(this, arguments);
		}
		function m(y, b) {
			var w = this, P, N, M, v = "loglevel";
			typeof y == "string" ? v += ":" + y : typeof y == "symbol" && (v = void 0);
			function g(T) {
				var A = (o[T] || "silent").toUpperCase();
				if (!(typeof window === r || !v)) {
					try {
						window.localStorage[v] = A;
						return;
					} catch {}
					try {
						window.document.cookie = encodeURIComponent(v) + "=" + A + ";";
					} catch {}
				}
			}
			function E() {
				var T;
				if (!(typeof window === r || !v)) {
					try {
						T = window.localStorage[v];
					} catch {}
					if (typeof T === r) try {
						var A = window.document.cookie, D = encodeURIComponent(v), U = A.indexOf(D + "=");
						U !== -1 && (T = /^([^;]+)/.exec(A.slice(U + D.length + 1))[1]);
					} catch {}
					return w.levels[T] === void 0 && (T = void 0), T;
				}
			}
			function x() {
				if (!(typeof window === r || !v)) {
					try {
						window.localStorage.removeItem(v);
					} catch {}
					try {
						window.document.cookie = encodeURIComponent(v) + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC";
					} catch {}
				}
			}
			function C(T) {
				var A = T;
				if (typeof A == "string" && w.levels[A.toUpperCase()] !== void 0 && (A = w.levels[A.toUpperCase()]), typeof A == "number" && A >= 0 && A <= w.levels.SILENT) return A;
				throw new TypeError("log.setLevel() called with invalid level: " + T);
			}
			w.name = y, w.levels = {
				TRACE: 0,
				DEBUG: 1,
				INFO: 2,
				WARN: 3,
				ERROR: 4,
				SILENT: 5
			}, w.methodFactory = b || h, w.getLevel = function() {
				return M ?? N ?? P;
			}, w.setLevel = function(T, A) {
				return M = C(T), A !== !1 && g(M), d.call(w);
			}, w.setDefaultLevel = function(T) {
				N = C(T), E() || w.setLevel(T, !1);
			}, w.resetLevel = function() {
				M = null, x(), d.call(w);
			}, w.enableAll = function(T) {
				w.setLevel(w.levels.TRACE, T);
			}, w.disableAll = function(T) {
				w.setLevel(w.levels.SILENT, T);
			}, w.rebuild = function() {
				if (a !== w && (P = C(a.getLevel())), d.call(w), a === w) for (var T in s) s[T].rebuild();
			}, P = C(a ? a.getLevel() : "WARN");
			var I = E();
			I != null && (M = C(I)), d.call(w);
		}
		a = new m(), a.getLogger = function(b) {
			if (typeof b != "symbol" && typeof b != "string" || b === "") throw new TypeError("You must supply a name when creating a logger.");
			var w = s[b];
			return w || (w = s[b] = new m(b, a.methodFactory)), w;
		};
		var S = typeof window !== r ? window.log : void 0;
		return a.noConflict = function() {
			return typeof window !== r && window.log === a && (window.log = S), a;
		}, a.getLoggers = function() {
			return s;
		}, a.default = a, a;
	});
})), Gt$1 = function(e, t) {
	return Gt$1 = Object.setPrototypeOf || { __proto__: [] } instanceof Array && function(n, r) {
		n.__proto__ = r;
	} || function(n, r) {
		for (var i in r) Object.prototype.hasOwnProperty.call(r, i) && (n[i] = r[i]);
	}, Gt$1(e, t);
};
function we$1(e, t) {
	if (typeof t != "function" && t !== null) throw new TypeError("Class extends value " + String(t) + " is not a constructor or null");
	Gt$1(e, t);
	function n() {
		this.constructor = e;
	}
	e.prototype = t === null ? Object.create(t) : (n.prototype = t.prototype, new n());
}
function Eo(e, t, n, r) {
	function i(o) {
		return o instanceof n ? o : new n(function(s) {
			s(o);
		});
	}
	return new (n || (n = Promise))(function(o, s) {
		function a(f) {
			try {
				u(r.next(f));
			} catch (d) {
				s(d);
			}
		}
		function c(f) {
			try {
				u(r.throw(f));
			} catch (d) {
				s(d);
			}
		}
		function u(f) {
			f.done ? o(f.value) : i(f.value).then(a, c);
		}
		u((r = r.apply(e, t || [])).next());
	});
}
function lr(e, t) {
	var n = {
		label: 0,
		sent: function() {
			if (o[0] & 1) throw o[1];
			return o[1];
		},
		trys: [],
		ops: []
	}, r, i, o, s = Object.create((typeof Iterator == "function" ? Iterator : Object).prototype);
	return s.next = a(0), s.throw = a(1), s.return = a(2), typeof Symbol == "function" && (s[Symbol.iterator] = function() {
		return this;
	}), s;
	function a(u) {
		return function(f) {
			return c([u, f]);
		};
	}
	function c(u) {
		if (r) throw new TypeError("Generator is already executing.");
		for (; s && (s = 0, u[0] && (n = 0)), n;) try {
			if (r = 1, i && (o = u[0] & 2 ? i.return : u[0] ? i.throw || ((o = i.return) && o.call(i), 0) : i.next) && !(o = o.call(i, u[1])).done) return o;
			switch (i = 0, o && (u = [u[0] & 2, o.value]), u[0]) {
				case 0:
				case 1:
					o = u;
					break;
				case 4: return n.label++, {
					value: u[1],
					done: !1
				};
				case 5:
					n.label++, i = u[1], u = [0];
					continue;
				case 7:
					u = n.ops.pop(), n.trys.pop();
					continue;
				default:
					if (o = n.trys, !(o = o.length > 0 && o[o.length - 1]) && (u[0] === 6 || u[0] === 2)) {
						n = 0;
						continue;
					}
					if (u[0] === 3 && (!o || u[1] > o[0] && u[1] < o[3])) {
						n.label = u[1];
						break;
					}
					if (u[0] === 6 && n.label < o[1]) {
						n.label = o[1], o = u;
						break;
					}
					if (o && n.label < o[2]) {
						n.label = o[2], n.ops.push(u);
						break;
					}
					o[2] && n.ops.pop(), n.trys.pop();
					continue;
			}
			u = t.call(e, n);
		} catch (f) {
			u = [6, f], i = 0;
		} finally {
			r = o = 0;
		}
		if (u[0] & 5) throw u[1];
		return {
			value: u[0] ? u[1] : void 0,
			done: !0
		};
	}
}
function Be$1(e) {
	var t = typeof Symbol == "function" && Symbol.iterator, n = t && e[t], r = 0;
	if (n) return n.call(e);
	if (e && typeof e.length == "number") return { next: function() {
		return e && r >= e.length && (e = void 0), {
			value: e && e[r++],
			done: !e
		};
	} };
	throw new TypeError(t ? "Object is not iterable." : "Symbol.iterator is not defined.");
}
function ge$1(e, t) {
	var n = typeof Symbol == "function" && e[Symbol.iterator];
	if (!n) return e;
	var r = n.call(e), i, o = [], s;
	try {
		for (; (t === void 0 || t-- > 0) && !(i = r.next()).done;) o.push(i.value);
	} catch (a) {
		s = { error: a };
	} finally {
		try {
			i && !i.done && (n = r.return) && n.call(r);
		} finally {
			if (s) throw s.error;
		}
	}
	return o;
}
function Te$1(e, t, n) {
	if (n || arguments.length === 2) for (var r = 0, i = t.length, o; r < i; r++) (o || !(r in t)) && (o || (o = Array.prototype.slice.call(t, 0, r)), o[r] = t[r]);
	return e.concat(o || Array.prototype.slice.call(t));
}
function Ue(e) {
	return this instanceof Ue ? (this.v = e, this) : new Ue(e);
}
function Co(e, t, n) {
	if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
	var r = n.apply(e, t || []), i, o = [];
	return i = Object.create((typeof AsyncIterator == "function" ? AsyncIterator : Object).prototype), a("next"), a("throw"), a("return", s), i[Symbol.asyncIterator] = function() {
		return this;
	}, i;
	function s(h) {
		return function(m) {
			return Promise.resolve(m).then(h, d);
		};
	}
	function a(h, m) {
		r[h] && (i[h] = function(S) {
			return new Promise(function(y, b) {
				o.push([
					h,
					S,
					y,
					b
				]) > 1 || c(h, S);
			});
		}, m && (i[h] = m(i[h])));
	}
	function c(h, m) {
		try {
			u(r[h](m));
		} catch (S) {
			p(o[0][3], S);
		}
	}
	function u(h) {
		h.value instanceof Ue ? Promise.resolve(h.value.v).then(f, d) : p(o[0][2], h);
	}
	function f(h) {
		c("next", h);
	}
	function d(h) {
		c("throw", h);
	}
	function p(h, m) {
		h(m), o.shift(), o.length && c(o[0][0], o[0][1]);
	}
}
function To(e) {
	if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
	var t = e[Symbol.asyncIterator], n;
	return t ? t.call(e) : (e = typeof Be$1 == "function" ? Be$1(e) : e[Symbol.iterator](), n = {}, r("next"), r("throw"), r("return"), n[Symbol.asyncIterator] = function() {
		return this;
	}, n);
	function r(o) {
		n[o] = e[o] && function(s) {
			return new Promise(function(a, c) {
				s = e[o](s), i(a, c, s.done, s.value);
			});
		};
	}
	function i(o, s, a, c) {
		Promise.resolve(c).then(function(u) {
			o({
				value: u,
				done: a
			});
		}, s);
	}
}
function $(e) {
	return typeof e == "function";
}
function sn(e) {
	var t = function(r) {
		Error.call(r), r.stack = (/* @__PURE__ */ new Error()).stack;
	}, n = e(t);
	return n.prototype = Object.create(Error.prototype), n.prototype.constructor = n, n;
}
var $t = sn(function(e) {
	return function(n) {
		e(this), this.message = n ? n.length + ` errors occurred during unsubscription:
` + n.map(function(r, i) {
			return i + 1 + ") " + r.toString();
		}).join(`
  `) : "", this.name = "UnsubscriptionError", this.errors = n;
	};
});
function ht(e, t) {
	if (e) {
		var n = e.indexOf(t);
		0 <= n && e.splice(n, 1);
	}
}
var et = (function() {
	function e(t) {
		this.initialTeardown = t, this.closed = !1, this._parentage = null, this._finalizers = null;
	}
	return e.prototype.unsubscribe = function() {
		var t, n, r, i, o;
		if (!this.closed) {
			this.closed = !0;
			var s = this._parentage;
			if (s) if (this._parentage = null, Array.isArray(s)) try {
				for (var a = Be$1(s), c = a.next(); !c.done; c = a.next()) c.value.remove(this);
			} catch (m) {
				t = { error: m };
			} finally {
				try {
					c && !c.done && (n = a.return) && n.call(a);
				} finally {
					if (t) throw t.error;
				}
			}
			else s.remove(this);
			var u = this.initialTeardown;
			if ($(u)) try {
				u();
			} catch (m) {
				o = m instanceof $t ? m.errors : [m];
			}
			var f = this._finalizers;
			if (f) {
				this._finalizers = null;
				try {
					for (var d = Be$1(f), p = d.next(); !p.done; p = d.next()) {
						var h = p.value;
						try {
							Mn(h);
						} catch (m) {
							o = o ?? [], m instanceof $t ? o = Te$1(Te$1([], ge$1(o)), ge$1(m.errors)) : o.push(m);
						}
					}
				} catch (m) {
					r = { error: m };
				} finally {
					try {
						p && !p.done && (i = d.return) && i.call(d);
					} finally {
						if (r) throw r.error;
					}
				}
			}
			if (o) throw new $t(o);
		}
	}, e.prototype.add = function(t) {
		var n;
		if (t && t !== this) if (this.closed) Mn(t);
		else {
			if (t instanceof e) {
				if (t.closed || t._hasParent(this)) return;
				t._addParent(this);
			}
			(this._finalizers = (n = this._finalizers) !== null && n !== void 0 ? n : []).push(t);
		}
	}, e.prototype._hasParent = function(t) {
		var n = this._parentage;
		return n === t || Array.isArray(n) && n.includes(t);
	}, e.prototype._addParent = function(t) {
		var n = this._parentage;
		this._parentage = Array.isArray(n) ? (n.push(t), n) : n ? [n, t] : t;
	}, e.prototype._removeParent = function(t) {
		var n = this._parentage;
		n === t ? this._parentage = null : Array.isArray(n) && ht(n, t);
	}, e.prototype.remove = function(t) {
		var n = this._finalizers;
		n && ht(n, t), t instanceof e && t._removeParent(this);
	}, e.EMPTY = (function() {
		var t = new e();
		return t.closed = !0, t;
	})(), e;
})(), fr = et.EMPTY;
function dr(e) {
	return e instanceof et || e && "closed" in e && $(e.remove) && $(e.add) && $(e.unsubscribe);
}
function Mn(e) {
	$(e) ? e() : e.unsubscribe();
}
var Oe$1 = {
	onUnhandledError: null,
	onStoppedNotification: null,
	Promise: void 0,
	useDeprecatedSynchronousErrorHandling: !1,
	useDeprecatedNextContext: !1
}, vt = {
	setTimeout: function(e, t) {
		for (var n = [], r = 2; r < arguments.length; r++) n[r - 2] = arguments[r];
		var i = vt.delegate;
		return i?.setTimeout ? i.setTimeout.apply(i, Te$1([e, t], ge$1(n))) : setTimeout.apply(void 0, Te$1([e, t], ge$1(n)));
	},
	clearTimeout: function(e) {
		return (vt.delegate?.clearTimeout || clearTimeout)(e);
	},
	delegate: void 0
};
function pr(e) {
	vt.setTimeout(function() {
		var t = Oe$1.onUnhandledError;
		if (t) t(e);
		else throw e;
	});
}
function mt() {}
var xo = (function() {
	return an("C", void 0, void 0);
})();
function ko(e) {
	return an("E", void 0, e);
}
function Po(e) {
	return an("N", e, void 0);
}
function an(e, t, n) {
	return {
		kind: e,
		value: t,
		error: n
	};
}
var Me = null;
function at(e) {
	if (Oe$1.useDeprecatedSynchronousErrorHandling) {
		var t = !Me;
		if (t && (Me = {
			errorThrown: !1,
			error: null
		}), e(), t) {
			var n = Me, r = n.errorThrown, i = n.error;
			if (Me = null, r) throw i;
		}
	} else e();
}
function Mo(e) {
	Oe$1.useDeprecatedSynchronousErrorHandling && Me && (Me.errorThrown = !0, Me.error = e);
}
var cn = (function(e) {
	we$1(t, e);
	function t(n) {
		var r = e.call(this) || this;
		return r.isStopped = !1, n ? (r.destination = n, dr(n) && n.add(r)) : r.destination = _o, r;
	}
	return t.create = function(n, r, i) {
		return new Qe(n, r, i);
	}, t.prototype.next = function(n) {
		this.isStopped ? jt(Po(n), this) : this._next(n);
	}, t.prototype.error = function(n) {
		this.isStopped ? jt(ko(n), this) : (this.isStopped = !0, this._error(n));
	}, t.prototype.complete = function() {
		this.isStopped ? jt(xo, this) : (this.isStopped = !0, this._complete());
	}, t.prototype.unsubscribe = function() {
		this.closed || (this.isStopped = !0, e.prototype.unsubscribe.call(this), this.destination = null);
	}, t.prototype._next = function(n) {
		this.destination.next(n);
	}, t.prototype._error = function(n) {
		try {
			this.destination.error(n);
		} finally {
			this.unsubscribe();
		}
	}, t.prototype._complete = function() {
		try {
			this.destination.complete();
		} finally {
			this.unsubscribe();
		}
	}, t;
})(et), Ao = Function.prototype.bind;
function Wt(e, t) {
	return Ao.call(e, t);
}
var Lo = (function() {
	function e(t) {
		this.partialObserver = t;
	}
	return e.prototype.next = function(t) {
		var n = this.partialObserver;
		if (n.next) try {
			n.next(t);
		} catch (r) {
			it(r);
		}
	}, e.prototype.error = function(t) {
		var n = this.partialObserver;
		if (n.error) try {
			n.error(t);
		} catch (r) {
			it(r);
		}
		else it(t);
	}, e.prototype.complete = function() {
		var t = this.partialObserver;
		if (t.complete) try {
			t.complete();
		} catch (n) {
			it(n);
		}
	}, e;
})(), Qe = (function(e) {
	we$1(t, e);
	function t(n, r, i) {
		var o = e.call(this) || this, s;
		if ($(n) || !n) s = {
			next: n ?? void 0,
			error: r ?? void 0,
			complete: i ?? void 0
		};
		else {
			var a;
			o && Oe$1.useDeprecatedNextContext ? (a = Object.create(n), a.unsubscribe = function() {
				return o.unsubscribe();
			}, s = {
				next: n.next && Wt(n.next, a),
				error: n.error && Wt(n.error, a),
				complete: n.complete && Wt(n.complete, a)
			}) : s = n;
		}
		return o.destination = new Lo(s), o;
	}
	return t;
})(cn);
function it(e) {
	Oe$1.useDeprecatedSynchronousErrorHandling ? Mo(e) : pr(e);
}
function Oo(e) {
	throw e;
}
function jt(e, t) {
	var n = Oe$1.onStoppedNotification;
	n && vt.setTimeout(function() {
		return n(e, t);
	});
}
var _o = {
	closed: !0,
	next: mt,
	error: Oo,
	complete: mt
}, un = (function() {
	return typeof Symbol == "function" && Symbol.observable || "@@observable";
})();
function Mt(e) {
	return e;
}
function Io(e) {
	return e.length === 0 ? Mt : e.length === 1 ? e[0] : function(n) {
		return e.reduce(function(r, i) {
			return i(r);
		}, n);
	};
}
var B = (function() {
	function e(t) {
		t && (this._subscribe = t);
	}
	return e.prototype.lift = function(t) {
		var n = new e();
		return n.source = this, n.operator = t, n;
	}, e.prototype.subscribe = function(t, n, r) {
		var i = this, o = Do(t) ? t : new Qe(t, n, r);
		return at(function() {
			var s = i, a = s.operator, c = s.source;
			o.add(a ? a.call(o, c) : c ? i._subscribe(o) : i._trySubscribe(o));
		}), o;
	}, e.prototype._trySubscribe = function(t) {
		try {
			return this._subscribe(t);
		} catch (n) {
			t.error(n);
		}
	}, e.prototype.forEach = function(t, n) {
		var r = this;
		return n = An(n), new n(function(i, o) {
			var s = new Qe({
				next: function(a) {
					try {
						t(a);
					} catch (c) {
						o(c), s.unsubscribe();
					}
				},
				error: o,
				complete: i
			});
			r.subscribe(s);
		});
	}, e.prototype._subscribe = function(t) {
		var n;
		return (n = this.source) === null || n === void 0 ? void 0 : n.subscribe(t);
	}, e.prototype[un] = function() {
		return this;
	}, e.prototype.pipe = function() {
		for (var t = [], n = 0; n < arguments.length; n++) t[n] = arguments[n];
		return Io(t)(this);
	}, e.prototype.toPromise = function(t) {
		var n = this;
		return t = An(t), new t(function(r, i) {
			var o;
			n.subscribe(function(s) {
				return o = s;
			}, function(s) {
				return i(s);
			}, function() {
				return r(o);
			});
		});
	}, e.create = function(t) {
		return new e(t);
	}, e;
})();
function An(e) {
	var t;
	return (t = e ?? Oe$1.Promise) !== null && t !== void 0 ? t : Promise;
}
function Ro(e) {
	return e && $(e.next) && $(e.error) && $(e.complete);
}
function Do(e) {
	return e && e instanceof cn || Ro(e) && dr(e);
}
function No(e) {
	return $(e?.lift);
}
function Y(e) {
	return function(t) {
		if (No(t)) return t.lift(function(n) {
			try {
				return e(n, this);
			} catch (r) {
				this.error(r);
			}
		});
		throw new TypeError("Unable to lift unknown Observable type");
	};
}
function te(e, t, n, r, i) {
	return new Fo(e, t, n, r, i);
}
var Fo = (function(e) {
	we$1(t, e);
	function t(n, r, i, o, s, a) {
		var c = e.call(this, n) || this;
		return c.onFinalize = s, c.shouldUnsubscribe = a, c._next = r ? function(u) {
			try {
				r(u);
			} catch (f) {
				n.error(f);
			}
		} : e.prototype._next, c._error = o ? function(u) {
			try {
				o(u);
			} catch (f) {
				n.error(f);
			} finally {
				this.unsubscribe();
			}
		} : e.prototype._error, c._complete = i ? function() {
			try {
				i();
			} catch (u) {
				n.error(u);
			} finally {
				this.unsubscribe();
			}
		} : e.prototype._complete, c;
	}
	return t.prototype.unsubscribe = function() {
		var n;
		if (!this.shouldUnsubscribe || this.shouldUnsubscribe()) {
			var r = this.closed;
			e.prototype.unsubscribe.call(this), !r && ((n = this.onFinalize) === null || n === void 0 || n.call(this));
		}
	}, t;
})(cn), Uo = sn(function(e) {
	return function() {
		e(this), this.name = "ObjectUnsubscribedError", this.message = "object unsubscribed";
	};
}), be$1 = (function(e) {
	we$1(t, e);
	function t() {
		var n = e.call(this) || this;
		return n.closed = !1, n.currentObservers = null, n.observers = [], n.isStopped = !1, n.hasError = !1, n.thrownError = null, n;
	}
	return t.prototype.lift = function(n) {
		var r = new Ln(this, this);
		return r.operator = n, r;
	}, t.prototype._throwIfClosed = function() {
		if (this.closed) throw new Uo();
	}, t.prototype.next = function(n) {
		var r = this;
		at(function() {
			var i, o;
			if (r._throwIfClosed(), !r.isStopped) {
				r.currentObservers || (r.currentObservers = Array.from(r.observers));
				try {
					for (var s = Be$1(r.currentObservers), a = s.next(); !a.done; a = s.next()) a.value.next(n);
				} catch (c) {
					i = { error: c };
				} finally {
					try {
						a && !a.done && (o = s.return) && o.call(s);
					} finally {
						if (i) throw i.error;
					}
				}
			}
		});
	}, t.prototype.error = function(n) {
		var r = this;
		at(function() {
			if (r._throwIfClosed(), !r.isStopped) {
				r.hasError = r.isStopped = !0, r.thrownError = n;
				for (var i = r.observers; i.length;) i.shift().error(n);
			}
		});
	}, t.prototype.complete = function() {
		var n = this;
		at(function() {
			if (n._throwIfClosed(), !n.isStopped) {
				n.isStopped = !0;
				for (var r = n.observers; r.length;) r.shift().complete();
			}
		});
	}, t.prototype.unsubscribe = function() {
		this.isStopped = this.closed = !0, this.observers = this.currentObservers = null;
	}, Object.defineProperty(t.prototype, "observed", {
		get: function() {
			var n;
			return ((n = this.observers) === null || n === void 0 ? void 0 : n.length) > 0;
		},
		enumerable: !1,
		configurable: !0
	}), t.prototype._trySubscribe = function(n) {
		return this._throwIfClosed(), e.prototype._trySubscribe.call(this, n);
	}, t.prototype._subscribe = function(n) {
		return this._throwIfClosed(), this._checkFinalizedStatuses(n), this._innerSubscribe(n);
	}, t.prototype._innerSubscribe = function(n) {
		var r = this, i = this, o = i.hasError, s = i.isStopped, a = i.observers;
		return o || s ? fr : (this.currentObservers = null, a.push(n), new et(function() {
			r.currentObservers = null, ht(a, n);
		}));
	}, t.prototype._checkFinalizedStatuses = function(n) {
		var r = this, i = r.hasError, o = r.thrownError, s = r.isStopped;
		i ? n.error(o) : s && n.complete();
	}, t.prototype.asObservable = function() {
		var n = new B();
		return n.source = this, n;
	}, t.create = function(n, r) {
		return new Ln(n, r);
	}, t;
})(B), Ln = (function(e) {
	we$1(t, e);
	function t(n, r) {
		var i = e.call(this) || this;
		return i.destination = n, i.source = r, i;
	}
	return t.prototype.next = function(n) {
		var r, i;
		(i = (r = this.destination) === null || r === void 0 ? void 0 : r.next) === null || i === void 0 || i.call(r, n);
	}, t.prototype.error = function(n) {
		var r, i;
		(i = (r = this.destination) === null || r === void 0 ? void 0 : r.error) === null || i === void 0 || i.call(r, n);
	}, t.prototype.complete = function() {
		var n, r;
		(r = (n = this.destination) === null || n === void 0 ? void 0 : n.complete) === null || r === void 0 || r.call(n);
	}, t.prototype._subscribe = function(n) {
		var r, i;
		return (i = (r = this.source) === null || r === void 0 ? void 0 : r.subscribe(n)) !== null && i !== void 0 ? i : fr;
	}, t;
})(be$1), hr = (function(e) {
	we$1(t, e);
	function t(n) {
		var r = e.call(this) || this;
		return r._value = n, r;
	}
	return Object.defineProperty(t.prototype, "value", {
		get: function() {
			return this.getValue();
		},
		enumerable: !1,
		configurable: !0
	}), t.prototype._subscribe = function(n) {
		var r = e.prototype._subscribe.call(this, n);
		return !r.closed && n.next(this._value), r;
	}, t.prototype.getValue = function() {
		var n = this, r = n.hasError, i = n.thrownError, o = n._value;
		if (r) throw i;
		return this._throwIfClosed(), o;
	}, t.prototype.next = function(n) {
		e.prototype.next.call(this, this._value = n);
	}, t;
})(be$1), vr = {
	now: function() {
		return (vr.delegate || Date).now();
	},
	delegate: void 0
}, $o = (function(e) {
	we$1(t, e);
	function t(n, r) {
		return e.call(this) || this;
	}
	return t.prototype.schedule = function(n, r) {
		return r === void 0 && (r = 0), this;
	}, t;
})(et), gt = {
	setInterval: function(e, t) {
		for (var n = [], r = 2; r < arguments.length; r++) n[r - 2] = arguments[r];
		var i = gt.delegate;
		return i?.setInterval ? i.setInterval.apply(i, Te$1([e, t], ge$1(n))) : setInterval.apply(void 0, Te$1([e, t], ge$1(n)));
	},
	clearInterval: function(e) {
		return (gt.delegate?.clearInterval || clearInterval)(e);
	},
	delegate: void 0
}, Wo = (function(e) {
	we$1(t, e);
	function t(n, r) {
		var i = e.call(this, n, r) || this;
		return i.scheduler = n, i.work = r, i.pending = !1, i;
	}
	return t.prototype.schedule = function(n, r) {
		var i;
		if (r === void 0 && (r = 0), this.closed) return this;
		this.state = n;
		var o = this.id, s = this.scheduler;
		return o != null && (this.id = this.recycleAsyncId(s, o, r)), this.pending = !0, this.delay = r, this.id = (i = this.id) !== null && i !== void 0 ? i : this.requestAsyncId(s, this.id, r), this;
	}, t.prototype.requestAsyncId = function(n, r, i) {
		return i === void 0 && (i = 0), gt.setInterval(n.flush.bind(n, this), i);
	}, t.prototype.recycleAsyncId = function(n, r, i) {
		if (i === void 0 && (i = 0), i != null && this.delay === i && this.pending === !1) return r;
		r != null && gt.clearInterval(r);
	}, t.prototype.execute = function(n, r) {
		if (this.closed) return /* @__PURE__ */ new Error("executing a cancelled action");
		this.pending = !1;
		var i = this._execute(n, r);
		if (i) return i;
		this.pending === !1 && this.id != null && (this.id = this.recycleAsyncId(this.scheduler, this.id, null));
	}, t.prototype._execute = function(n, r) {
		var i = !1, o;
		try {
			this.work(n);
		} catch (s) {
			i = !0, o = s || /* @__PURE__ */ new Error("Scheduled action threw falsy error");
		}
		if (i) return this.unsubscribe(), o;
	}, t.prototype.unsubscribe = function() {
		if (!this.closed) {
			var n = this, r = n.id, i = n.scheduler, o = i.actions;
			this.work = this.state = this.scheduler = null, this.pending = !1, ht(o, this), r != null && (this.id = this.recycleAsyncId(i, r, null)), this.delay = null, e.prototype.unsubscribe.call(this);
		}
	}, t;
})($o), On = (function() {
	function e(t, n) {
		n === void 0 && (n = e.now), this.schedulerActionCtor = t, this.now = n;
	}
	return e.prototype.schedule = function(t, n, r) {
		return n === void 0 && (n = 0), new this.schedulerActionCtor(this, t).schedule(r, n);
	}, e.now = vr.now, e;
})(), Bo = new ((function(e) {
	we$1(t, e);
	function t(n, r) {
		r === void 0 && (r = On.now);
		var i = e.call(this, n, r) || this;
		return i.actions = [], i._active = !1, i;
	}
	return t.prototype.flush = function(n) {
		var r = this.actions;
		if (this._active) {
			r.push(n);
			return;
		}
		var i;
		this._active = !0;
		do
			if (i = n.execute(n.state, n.delay)) break;
		while (n = r.shift());
		if (this._active = !1, i) {
			for (; n = r.shift();) n.unsubscribe();
			throw i;
		}
	}, t;
})(On))(Wo);
function Vo(e) {
	return e && $(e.schedule);
}
function Ho(e) {
	return e[e.length - 1];
}
function ln(e) {
	return Vo(Ho(e)) ? e.pop() : void 0;
}
var fn = (function(e) {
	return e && typeof e.length == "number" && typeof e != "function";
});
function mr(e) {
	return $(e?.then);
}
function gr(e) {
	return $(e[un]);
}
function br(e) {
	return Symbol.asyncIterator && $(e?.[Symbol.asyncIterator]);
}
function yr(e) {
	return /* @__PURE__ */ new TypeError("You provided " + (e !== null && typeof e == "object" ? "an invalid object" : "'" + e + "'") + " where a stream was expected. You can provide an Observable, Promise, ReadableStream, Array, AsyncIterable, or Iterable.");
}
function zo() {
	return typeof Symbol != "function" || !Symbol.iterator ? "@@iterator" : Symbol.iterator;
}
var wr = zo();
function Sr(e) {
	return $(e?.[wr]);
}
function Er(e) {
	return Co(this, arguments, function() {
		var n, r, i, o;
		return lr(this, function(s) {
			switch (s.label) {
				case 0: n = e.getReader(), s.label = 1;
				case 1: s.trys.push([
					1,
					,
					9,
					10
				]), s.label = 2;
				case 2: return [4, Ue(n.read())];
				case 3: return r = s.sent(), i = r.value, o = r.done, o ? [4, Ue(void 0)] : [3, 5];
				case 4: return [2, s.sent()];
				case 5: return [4, Ue(i)];
				case 6: return [4, s.sent()];
				case 7: return s.sent(), [3, 2];
				case 8: return [3, 10];
				case 9: return n.releaseLock(), [7];
				case 10: return [2];
			}
		});
	});
}
function Cr(e) {
	return $(e?.getReader);
}
function se(e) {
	if (e instanceof B) return e;
	if (e != null) {
		if (gr(e)) return qo(e);
		if (fn(e)) return Ko(e);
		if (mr(e)) return Go(e);
		if (br(e)) return Tr(e);
		if (Sr(e)) return Jo(e);
		if (Cr(e)) return Yo(e);
	}
	throw yr(e);
}
function qo(e) {
	return new B(function(t) {
		var n = e[un]();
		if ($(n.subscribe)) return n.subscribe(t);
		throw new TypeError("Provided object does not correctly implement Symbol.observable");
	});
}
function Ko(e) {
	return new B(function(t) {
		for (var n = 0; n < e.length && !t.closed; n++) t.next(e[n]);
		t.complete();
	});
}
function Go(e) {
	return new B(function(t) {
		e.then(function(n) {
			t.closed || (t.next(n), t.complete());
		}, function(n) {
			return t.error(n);
		}).then(null, pr);
	});
}
function Jo(e) {
	return new B(function(t) {
		var n, r;
		try {
			for (var i = Be$1(e), o = i.next(); !o.done; o = i.next()) {
				var s = o.value;
				if (t.next(s), t.closed) return;
			}
		} catch (a) {
			n = { error: a };
		} finally {
			try {
				o && !o.done && (r = i.return) && r.call(i);
			} finally {
				if (n) throw n.error;
			}
		}
		t.complete();
	});
}
function Tr(e) {
	return new B(function(t) {
		Qo(e, t).catch(function(n) {
			return t.error(n);
		});
	});
}
function Yo(e) {
	return Tr(Er(e));
}
function Qo(e, t) {
	var n, r, i, o;
	return Eo(this, void 0, void 0, function() {
		var s, a;
		return lr(this, function(c) {
			switch (c.label) {
				case 0: c.trys.push([
					0,
					5,
					6,
					11
				]), n = To(e), c.label = 1;
				case 1: return [4, n.next()];
				case 2:
					if (r = c.sent(), !!r.done) return [3, 4];
					if (s = r.value, t.next(s), t.closed) return [2];
					c.label = 3;
				case 3: return [3, 1];
				case 4: return [3, 11];
				case 5: return a = c.sent(), i = { error: a }, [3, 11];
				case 6: return c.trys.push([
					6,
					,
					9,
					10
				]), r && !r.done && (o = n.return) ? [4, o.call(n)] : [3, 8];
				case 7: c.sent(), c.label = 8;
				case 8: return [3, 10];
				case 9:
					if (i) throw i.error;
					return [7];
				case 10: return [7];
				case 11: return t.complete(), [2];
			}
		});
	});
}
function me(e, t, n, r, i) {
	r === void 0 && (r = 0), i === void 0 && (i = !1);
	var o = t.schedule(function() {
		n(), i ? e.add(this.schedule(null, r)) : this.unsubscribe();
	}, r);
	if (e.add(o), !i) return o;
}
function xr(e, t) {
	return t === void 0 && (t = 0), Y(function(n, r) {
		n.subscribe(te(r, function(i) {
			return me(r, e, function() {
				return r.next(i);
			}, t);
		}, function() {
			return me(r, e, function() {
				return r.complete();
			}, t);
		}, function(i) {
			return me(r, e, function() {
				return r.error(i);
			}, t);
		}));
	});
}
function kr(e, t) {
	return t === void 0 && (t = 0), Y(function(n, r) {
		r.add(e.schedule(function() {
			return n.subscribe(r);
		}, t));
	});
}
function Xo(e, t) {
	return se(e).pipe(kr(t), xr(t));
}
function Zo(e, t) {
	return se(e).pipe(kr(t), xr(t));
}
function es(e, t) {
	return new B(function(n) {
		var r = 0;
		return t.schedule(function() {
			r === e.length ? n.complete() : (n.next(e[r++]), n.closed || this.schedule());
		});
	});
}
function ts$1(e, t) {
	return new B(function(n) {
		var r;
		return me(n, t, function() {
			r = e[wr](), me(n, t, function() {
				var i, o, s;
				try {
					i = r.next(), o = i.value, s = i.done;
				} catch (a) {
					n.error(a);
					return;
				}
				s ? n.complete() : n.next(o);
			}, 0, !0);
		}), function() {
			return $(r?.return) && r.return();
		};
	});
}
function Pr(e, t) {
	if (!e) throw new Error("Iterable cannot be null");
	return new B(function(n) {
		me(n, t, function() {
			var r = e[Symbol.asyncIterator]();
			me(n, t, function() {
				r.next().then(function(i) {
					i.done ? n.complete() : n.next(i.value);
				});
			}, 0, !0);
		});
	});
}
function ns$1(e, t) {
	return Pr(Er(e), t);
}
function rs$1(e, t) {
	if (e != null) {
		if (gr(e)) return Xo(e, t);
		if (fn(e)) return es(e, t);
		if (mr(e)) return Zo(e, t);
		if (br(e)) return Pr(e, t);
		if (Sr(e)) return ts$1(e, t);
		if (Cr(e)) return ns$1(e, t);
	}
	throw yr(e);
}
function $e$1(e, t) {
	return t ? rs$1(e, t) : se(e);
}
function Jt$1() {
	for (var e = [], t = 0; t < arguments.length; t++) e[t] = arguments[t];
	return $e$1(e, ln(e));
}
function is$1(e) {
	return e instanceof Date && !isNaN(e);
}
var os$1 = sn(function(e) {
	return function(n) {
		n === void 0 && (n = null), e(this), this.message = "Timeout has occurred", this.name = "TimeoutError", this.info = n;
	};
});
function ss(e, t) {
	var n = is$1(e) ? { first: e } : typeof e == "number" ? { each: e } : e, r = n.first, i = n.each, o = n.with, s = o === void 0 ? as : o, a = n.scheduler, c = a === void 0 ? t ?? Bo : a, u = n.meta, f = u === void 0 ? null : u;
	if (r == null && i == null) throw new TypeError("No timeout provided.");
	return Y(function(d, p) {
		var h, m, S = null, y = 0, b = function(w) {
			m = me(p, c, function() {
				try {
					h.unsubscribe(), se(s({
						meta: f,
						lastValue: S,
						seen: y
					})).subscribe(p);
				} catch (P) {
					p.error(P);
				}
			}, w);
		};
		h = d.subscribe(te(p, function(w) {
			m?.unsubscribe(), y++, p.next(S = w), i > 0 && b(i);
		}, void 0, void 0, function() {
			m?.closed || m?.unsubscribe(), S = null;
		})), !y && b(r != null ? typeof r == "number" ? r : +r - c.now() : i);
	});
}
function as(e) {
	throw new os$1(e);
}
function W$1(e, t) {
	return Y(function(n, r) {
		var i = 0;
		n.subscribe(te(r, function(o) {
			r.next(e.call(t, o, i++));
		}));
	});
}
var cs$1 = Array.isArray;
function us$1(e, t) {
	return cs$1(t) ? e.apply(void 0, Te$1([], ge$1(t))) : e(t);
}
function ls(e) {
	return W$1(function(t) {
		return us$1(e, t);
	});
}
function fs(e, t, n, r, i, o, s, a) {
	var c = [], u = 0, f = 0, d = !1, p = function() {
		d && !c.length && !u && t.complete();
	}, h = function(S) {
		return u < r ? m(S) : c.push(S);
	}, m = function(S) {
		o && t.next(S), u++;
		var y = !1;
		se(n(S, f++)).subscribe(te(t, function(b) {
			i?.(b), o ? h(b) : t.next(b);
		}, function() {
			y = !0;
		}, void 0, function() {
			if (y) try {
				u--;
				for (var b = function() {
					var w = c.shift();
					s ? me(t, s, function() {
						return m(w);
					}) : m(w);
				}; c.length && u < r;) b();
				p();
			} catch (w) {
				t.error(w);
			}
		}));
	};
	return e.subscribe(te(t, h, function() {
		d = !0, p();
	})), function() {
		a?.();
	};
}
function Ve$1(e, t, n) {
	return n === void 0 && (n = Infinity), $(t) ? Ve$1(function(r, i) {
		return W$1(function(o, s) {
			return t(r, o, i, s);
		})(se(e(r, i)));
	}, n) : (typeof t == "number" && (n = t), Y(function(r, i) {
		return fs(r, i, e, n);
	}));
}
function ds(e) {
	return e === void 0 && (e = Infinity), Ve$1(Mt, e);
}
function ps() {
	return ds(1);
}
function bt() {
	for (var e = [], t = 0; t < arguments.length; t++) e[t] = arguments[t];
	return ps()($e$1(e, ln(e)));
}
var hs = ["addListener", "removeListener"], vs = ["addEventListener", "removeEventListener"], ms = ["on", "off"];
function Yt$1(e, t, n, r) {
	if ($(n) && (r = n, n = void 0), r) return Yt$1(e, t, n).pipe(ls(r));
	var i = ge$1(ys(e) ? vs.map(function(a) {
		return function(c) {
			return e[a](t, c, n);
		};
	}) : gs(e) ? hs.map(_n(e, t)) : bs(e) ? ms.map(_n(e, t)) : [], 2), o = i[0], s = i[1];
	if (!o && fn(e)) return Ve$1(function(a) {
		return Yt$1(a, t, n);
	})(se(e));
	if (!o) throw new TypeError("Invalid event target");
	return new B(function(a) {
		var c = function() {
			for (var u = [], f = 0; f < arguments.length; f++) u[f] = arguments[f];
			return a.next(1 < u.length ? u : u[0]);
		};
		return o(c), function() {
			return s(c);
		};
	});
}
function _n(e, t) {
	return function(n) {
		return function(r) {
			return e[n](t, r);
		};
	};
}
function gs(e) {
	return $(e.addListener) && $(e.removeListener);
}
function bs(e) {
	return $(e.on) && $(e.off);
}
function ys(e) {
	return $(e.addEventListener) && $(e.removeEventListener);
}
function At(e, t) {
	return Y(function(n, r) {
		var i = 0;
		n.subscribe(te(r, function(o) {
			return e.call(t, o, i++) && r.next(o);
		}));
	});
}
function ws(e, t, n, r, i) {
	return function(o, s) {
		var a = n, c = t, u = 0;
		o.subscribe(te(s, function(f) {
			var d = u++;
			c = a ? e(c, f, d) : (a = !0, f), r && s.next(c);
		}, i && (function() {
			a && s.next(c), s.complete();
		})));
	};
}
function Ss(e, t) {
	return $(t) ? Ve$1(e, t, 1) : Ve$1(e, 1);
}
function Es(e, t) {
	return t === void 0 && (t = Mt), e = e ?? Cs, Y(function(n, r) {
		var i, o = !0;
		n.subscribe(te(r, function(s) {
			var a = t(s);
			(o || !e(i, a)) && (o = !1, i = a, r.next(s));
		}));
	});
}
function Cs(e, t) {
	return e === t;
}
function Ts(e) {
	return Y(function(t, n) {
		try {
			t.subscribe(n);
		} finally {
			n.add(e);
		}
	});
}
function ct(e, t) {
	return Y(ws(e, t, arguments.length >= 2, !0));
}
function xs(e) {
	e === void 0 && (e = {});
	var t = e.connector, n = t === void 0 ? function() {
		return new be$1();
	} : t, r = e.resetOnError, i = r === void 0 ? !0 : r, o = e.resetOnComplete, s = o === void 0 ? !0 : o, a = e.resetOnRefCountZero, c = a === void 0 ? !0 : a;
	return function(u) {
		var f, d, p, h = 0, m = !1, S = !1, y = function() {
			d?.unsubscribe(), d = void 0;
		}, b = function() {
			y(), f = p = void 0, m = S = !1;
		}, w = function() {
			var P = f;
			b(), P?.unsubscribe();
		};
		return Y(function(P, N) {
			h++, !S && !m && y();
			var M = p = p ?? n();
			N.add(function() {
				h--, h === 0 && !S && !m && (d = Bt(w, c));
			}), M.subscribe(N), !f && h > 0 && (f = new Qe({
				next: function(v) {
					return M.next(v);
				},
				error: function(v) {
					S = !0, y(), d = Bt(b, i, v), M.error(v);
				},
				complete: function() {
					m = !0, y(), d = Bt(b, s), M.complete();
				}
			}), se(P).subscribe(f));
		})(u);
	};
}
function Bt(e, t) {
	for (var n = [], r = 2; r < arguments.length; r++) n[r - 2] = arguments[r];
	if (t === !0) {
		e();
		return;
	}
	if (t !== !1) {
		var i = new Qe({ next: function() {
			i.unsubscribe(), e();
		} });
		return se(t.apply(void 0, Te$1([], ge$1(n)))).subscribe(i);
	}
}
function ks(e) {
	return Y(function(t, n) {
		var r = !1, i = te(n, function() {
			i?.unsubscribe(), r = !0;
		}, mt);
		se(e).subscribe(i), t.subscribe(te(n, function(o) {
			return r && n.next(o);
		}));
	});
}
function J$1() {
	for (var e = [], t = 0; t < arguments.length; t++) e[t] = arguments[t];
	var n = ln(e);
	return Y(function(r, i) {
		(n ? bt(e, r, n) : bt(e, r)).subscribe(i);
	});
}
function Mr(e, t) {
	return Y(function(n, r) {
		var i = null, o = 0, s = !1, a = function() {
			return s && !i && r.complete();
		};
		n.subscribe(te(r, function(c) {
			i?.unsubscribe();
			var u = 0, f = o++;
			se(e(c, f)).subscribe(i = te(r, function(d) {
				return r.next(t ? t(c, d, f, u++) : d);
			}, function() {
				i = null, a();
			}));
		}, function() {
			s = !0, a();
		}));
	});
}
function In(e) {
	return Y(function(t, n) {
		se(e).subscribe(te(n, function() {
			return n.complete();
		}, mt)), !n.closed && t.subscribe(n);
	});
}
function Ps(e, t, n) {
	var r = $(e) || t || n ? {
		next: e,
		error: t,
		complete: n
	} : e;
	return r ? Y(function(i, o) {
		var s;
		(s = r.subscribe) === null || s === void 0 || s.call(r);
		var a = !0;
		i.subscribe(te(o, function(c) {
			var u;
			(u = r.next) === null || u === void 0 || u.call(r, c), o.next(c);
		}, function() {
			var c;
			a = !1, (c = r.complete) === null || c === void 0 || c.call(r), o.complete();
		}, function(c) {
			var u;
			a = !1, (u = r.error) === null || u === void 0 || u.call(r, c), o.error(c);
		}, function() {
			var c, u;
			a && ((c = r.unsubscribe) === null || c === void 0 || c.call(r)), (u = r.finalize) === null || u === void 0 || u.call(r);
		}));
	}) : Mt;
}
var Ms = /* @__PURE__ */ f(So(), 1), As = Object.defineProperty, Ls = Object.defineProperties, Os = Object.getOwnPropertyDescriptors, Rn = Object.getOwnPropertySymbols, _s = Object.prototype.hasOwnProperty, Is = Object.prototype.propertyIsEnumerable, Dn = (e, t) => (t = Symbol[e]) ? t : /* @__PURE__ */ Symbol.for("Symbol." + e), Nn = (e, t, n) => t in e ? As(e, t, {
	enumerable: !0,
	configurable: !0,
	writable: !0,
	value: n
}) : e[t] = n, ae = (e, t) => {
	for (var n in t || (t = {})) _s.call(t, n) && Nn(e, n, t[n]);
	if (Rn) for (var n of Rn(t)) Is.call(t, n) && Nn(e, n, t[n]);
	return e;
}, Pe$1 = (e, t) => Ls(e, Os(t)), Z$1 = (e, t, n) => new Promise((r, i) => {
	var o = (c) => {
		try {
			a(n.next(c));
		} catch (u) {
			i(u);
		}
	}, s = (c) => {
		try {
			a(n.throw(c));
		} catch (u) {
			i(u);
		}
	}, a = (c) => c.done ? r(c.value) : Promise.resolve(c.value).then(o, s);
	a((n = n.apply(e, t)).next());
}), Rs = (e, t, n) => (t = e[Dn("asyncIterator")]) ? t.call(e) : (e = e[Dn("iterator")](), t = {}, n = (r, i) => (i = e[r]) && (t[r] = (o) => new Promise((s, a, c) => (o = i.call(e, o), c = o.done, Promise.resolve(o.value).then((u) => s({
	value: u,
	done: c
}), a)))), n("next"), n("return"), t);
function ee(e) {
	return typeof e > "u" ? !1 : Ds(e) || Ns(e);
}
function Ds(e) {
	var t;
	return e ? e.hasOwnProperty("participant") && e.hasOwnProperty("source") && e.hasOwnProperty("track") && typeof ((t = e.publication) == null ? void 0 : t.track) < "u" : !1;
}
function Ns(e) {
	return e ? e.hasOwnProperty("participant") && e.hasOwnProperty("source") && e.hasOwnProperty("publication") && typeof e.publication < "u" : !1;
}
function Xe(e) {
	return e ? e.hasOwnProperty("participant") && e.hasOwnProperty("source") && typeof e.publication > "u" : !1;
}
function G(e) {
	if (typeof e == "string" || typeof e == "number") return `${e}`;
	if (Xe(e)) return `${e.participant.identity}_${e.source}_placeholder`;
	if (ee(e)) return `${e.participant.identity}_${e.publication.source}_${e.publication.trackSid}`;
	throw new Error(`Can't generate a id for the given track reference: ${e}`);
}
function Hc(e, t) {
	return e === void 0 || t === void 0 ? !1 : ee(e) && ee(t) ? e.publication.trackSid === t.publication.trackSid : G(e) === G(t);
}
function Fs(e, t) {
	return typeof t > "u" ? !1 : ee(e) ? t.some((n) => n.participant.identity === e.participant.identity && ee(n) && n.publication.trackSid === e.publication.trackSid) : Xe(e) ? t.some((n) => n.participant.identity === e.participant.identity && Xe(n) && n.source === e.source) : !1;
}
function Us(e, t) {
	return Xe(e) && ee(t) && t.participant.identity === e.participant.identity && t.source === e.source;
}
function zc() {
	const e = document.createElement("p");
	e.style.width = "100%", e.style.height = "200px";
	const t = document.createElement("div");
	t.style.position = "absolute", t.style.top = "0px", t.style.left = "0px", t.style.visibility = "hidden", t.style.width = "200px", t.style.height = "150px", t.style.overflow = "hidden", t.appendChild(e), document.body.appendChild(t);
	const n = e.offsetWidth;
	t.style.overflow = "scroll";
	let r = e.offsetWidth;
	return n === r && (r = t.clientWidth), document.body.removeChild(t), n - r;
}
function qc() {
	return typeof document < "u";
}
function $s(e) {
	e = ae({}, e);
	const t = `(?:(?:(?:[a-z]+:)?//)?|www\\.)(?:\\S+(?::\\S*)?@)?(?:localhost|${(/* @__PURE__ */ new RegExp("(?:25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]\\d|\\d)(?:\\.(?:25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]\\d|\\d)){3}", "g")).source}|(?:(?:[a-z\\u00a1-\\uffff0-9][-_]*)*[a-z\\u00a1-\\uffff0-9]+)(?:\\.(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)*(?:\\.(?:[a-z\\u00a1-\\uffff]{2,}))\\.?)(?::\\d{2,5})?(?:[/?#][^\\s"]*)?`;
	return e.exact ? new RegExp(`(?:^${t}$)`, "i") : new RegExp(t, "ig");
}
var Fn = "[^\\.\\s@:](?:[^\\s@:]*[^\\s@:\\.])?@[^\\.\\s@]+(?:\\.[^\\.\\s@]+)*";
function Ws({ exact: e } = {}) {
	return e ? new RegExp(`^${Fn}$`) : new RegExp(Fn, "g");
}
function Kc(e, t, n) {
	return mo(e, t, () => Z$1(null, null, function* () {
		const { x: r, y: i } = yield wo(e, t, {
			placement: "top",
			middleware: [
				go(6),
				yo(),
				bo({ padding: 5 })
			]
		});
		n?.(r, i);
	}));
}
function Gc(e, t) {
	return !e.contains(t.target);
}
var Jc = () => ({
	email: Ws(),
	url: $s({})
});
function Yc(e, t) {
	const n = Object.entries(t).map(([o, s], a) => Array.from(e.matchAll(s)).map(({ index: c, 0: u }) => ({
		type: o,
		weight: a,
		content: u,
		index: c ?? 0
	}))).flat().sort((o, s) => {
		const a = o.index - s.index;
		return a !== 0 ? a : o.weight - s.weight;
	}).filter(({ index: o }, s, a) => {
		if (s === 0) return !0;
		const c = a[s - 1];
		return c.index + c.content.length <= o;
	}), r = [];
	let i = 0;
	for (const { type: o, content: s, index: a } of n) a > i && r.push(e.substring(i, a)), r.push({
		type: o,
		content: s
	}), i = a + s.length;
	return e.length > i && r.push(e.substring(i)), r;
}
var Ar = [
	...[
		RoomEvent.ConnectionStateChanged,
		RoomEvent.RoomMetadataChanged,
		RoomEvent.ActiveSpeakersChanged,
		RoomEvent.ConnectionQualityChanged,
		RoomEvent.ParticipantConnected,
		RoomEvent.ParticipantDisconnected,
		RoomEvent.ParticipantPermissionsChanged,
		RoomEvent.ParticipantMetadataChanged,
		RoomEvent.ParticipantNameChanged,
		RoomEvent.ParticipantAttributesChanged,
		RoomEvent.TrackMuted,
		RoomEvent.TrackUnmuted,
		RoomEvent.TrackPublished,
		RoomEvent.TrackUnpublished,
		RoomEvent.TrackStreamStateChanged,
		RoomEvent.TrackSubscriptionFailed,
		RoomEvent.TrackSubscriptionPermissionChanged,
		RoomEvent.TrackSubscriptionStatusChanged
	],
	RoomEvent.LocalTrackPublished,
	RoomEvent.LocalTrackUnpublished
], Bs = [
	ParticipantEvent.TrackPublished,
	ParticipantEvent.TrackUnpublished,
	ParticipantEvent.TrackMuted,
	ParticipantEvent.TrackUnmuted,
	ParticipantEvent.TrackStreamStateChanged,
	ParticipantEvent.TrackSubscribed,
	ParticipantEvent.TrackUnsubscribed,
	ParticipantEvent.TrackSubscriptionPermissionChanged,
	ParticipantEvent.TrackSubscriptionFailed,
	ParticipantEvent.LocalTrackPublished,
	ParticipantEvent.LocalTrackUnpublished
], Lr = [
	...[
		ParticipantEvent.ConnectionQualityChanged,
		ParticipantEvent.IsSpeakingChanged,
		ParticipantEvent.ParticipantMetadataChanged,
		ParticipantEvent.ParticipantPermissionsChanged,
		ParticipantEvent.TrackMuted,
		ParticipantEvent.TrackUnmuted,
		ParticipantEvent.TrackPublished,
		ParticipantEvent.TrackUnpublished,
		ParticipantEvent.TrackStreamStateChanged,
		ParticipantEvent.TrackSubscriptionFailed,
		ParticipantEvent.TrackSubscriptionPermissionChanged,
		ParticipantEvent.TrackSubscriptionStatusChanged
	],
	ParticipantEvent.LocalTrackPublished,
	ParticipantEvent.LocalTrackUnpublished
], R = Ms.default.getLogger("lk-components-js");
R.setDefaultLevel("WARN");
function Qc(e, t = {}) {
	var n;
	R.setLevel(e), setLogLevel((n = t.liveKitClientLogLevel) != null ? n : e);
}
function Xc(e, t = {}) {
	var n;
	const r = R.methodFactory;
	R.methodFactory = (i, o, s) => {
		const a = r(i, o, s), c = LogLevel[i], u = c >= o && c < LogLevel.silent;
		return (f, d) => {
			d ? a(f, d) : a(f), u && e(c, f, d);
		};
	}, R.setLevel(R.getLevel()), setLogExtension((n = t.liveKitClientLogExtension) != null ? n : e);
}
var Hs = [
	{
		columns: 1,
		rows: 1
	},
	{
		columns: 1,
		rows: 2,
		orientation: "portrait"
	},
	{
		columns: 2,
		rows: 1,
		orientation: "landscape"
	},
	{
		columns: 2,
		rows: 2,
		minWidth: 560
	},
	{
		columns: 3,
		rows: 3,
		minWidth: 700
	},
	{
		columns: 4,
		rows: 4,
		minWidth: 960
	},
	{
		columns: 5,
		rows: 5,
		minWidth: 1100
	}
];
function Or(e, t, n, r) {
	if (e.length < 1) throw new Error("At least one grid layout definition must be provided.");
	const i = zs(e);
	if (n <= 0 || r <= 0) return i[0];
	let o = 0;
	const s = n / r > 1 ? "landscape" : "portrait";
	let a = i.find((c, u, f) => {
		o = u;
		const d = f.findIndex((p, h) => {
			const m = !p.orientation || p.orientation === s, S = h > u, y = p.maxTiles === c.maxTiles;
			return S && y && m;
		}) !== -1;
		return c.maxTiles >= t && !d;
	});
	if (a === void 0) if (a = i[i.length - 1], a) R.warn(`No layout found for: participantCount: ${t}, width/height: ${n}/${r} fallback to biggest available layout (${a}).`);
	else throw new Error("No layout or fallback layout found.");
	if ((n < a.minWidth || r < a.minHeight) && o > 0) {
		const c = i[o - 1];
		a = Or(i.slice(0, o), c.maxTiles, n, r);
	}
	return a;
}
function zs(e) {
	return [...e].map((t) => {
		var n, r;
		return {
			name: `${t.columns}x${t.rows}`,
			columns: t.columns,
			rows: t.rows,
			maxTiles: t.columns * t.rows,
			minWidth: (n = t.minWidth) != null ? n : 0,
			minHeight: (r = t.minHeight) != null ? r : 0,
			orientation: t.orientation
		};
	}).sort((t, n) => t.maxTiles !== n.maxTiles ? t.maxTiles - n.maxTiles : t.minWidth !== 0 || n.minWidth !== 0 ? t.minWidth - n.minWidth : t.minHeight !== 0 || n.minHeight !== 0 ? t.minHeight - n.minHeight : 0);
}
function Zc() {
	return typeof navigator < "u" && navigator.mediaDevices && !!navigator.mediaDevices.getDisplayMedia;
}
function qs(e, t) {
	var n;
	return Pe$1(ae({}, e), {
		receivedAtMediaTimestamp: (n = t.rtpTimestamp) != null ? n : 0,
		receivedAt: t.timestamp
	});
}
function Ks(e, t, n) {
	return [...e, ...t].reduceRight((r, i) => (r.find((o) => o.id === i.id) || r.unshift(i), r), []).slice(0 - n);
}
var ce$1 = /* @__PURE__ */ ((e) => (e.AgentState = "lk.agent.state", e.PublishOnBehalf = "lk.publish_on_behalf", e.TranscriptionFinal = "lk.transcription_final", e.TranscriptionSegmentId = "lk.segment_id", e.TranscribedTrackId = "lk.transcribed_track_id", e.Expression = "lk.expression", e))(ce$1 || {}), _r = [], Ir = {
	showChat: !1,
	unreadMessages: 0,
	showSettings: !1
};
function Rr(e) {
	return typeof e == "object";
}
function Dr(e) {
	return Array.isArray(e) && e.filter(Rr).length > 0;
}
function Nr(e, t) {
	return t.audioLevel - e.audioLevel;
}
function Fr(e, t) {
	return e.isSpeaking === t.isSpeaking ? 0 : e.isSpeaking ? -1 : 1;
}
function Ur(e, t) {
	var n, r, i, o;
	return e.lastSpokeAt !== void 0 || t.lastSpokeAt !== void 0 ? ((r = (n = t.lastSpokeAt) == null ? void 0 : n.getTime()) != null ? r : 0) - ((o = (i = e.lastSpokeAt) == null ? void 0 : i.getTime()) != null ? o : 0) : 0;
}
function yt(e, t) {
	var n, r, i, o;
	return ((r = (n = e.joinedAt) == null ? void 0 : n.getTime()) != null ? r : 0) - ((o = (i = t.joinedAt) == null ? void 0 : i.getTime()) != null ? o : 0);
}
function Gs(e, t) {
	return ee(e) ? ee(t) ? 0 : -1 : ee(t) ? 1 : 0;
}
function Js(e, t) {
	const n = e.participant.isCameraEnabled;
	return n !== t.participant.isCameraEnabled ? n ? -1 : 1 : 0;
}
function Ys(e) {
	const t = [], n = [], r = [], i = [];
	e.forEach((a) => {
		a.participant.isLocal && a.source === Track.Source.Camera ? t.push(a) : a.source === Track.Source.ScreenShare ? n.push(a) : a.source === Track.Source.Camera ? r.push(a) : i.push(a);
	});
	const o = Qs(n), s = Xs(r);
	return [
		...t,
		...o,
		...s,
		...i
	];
}
function Qs(e) {
	const t = [], n = [];
	return e.forEach((r) => {
		r.participant.isLocal ? t.push(r) : n.push(r);
	}), t.sort((r, i) => yt(r.participant, i.participant)), n.sort((r, i) => yt(r.participant, i.participant)), [...n, ...t];
}
function Xs(e) {
	const t = [], n = [];
	return e.forEach((r) => {
		r.participant.isLocal ? t.push(r) : n.push(r);
	}), n.sort((r, i) => r.participant.isSpeaking && i.participant.isSpeaking ? Nr(r.participant, i.participant) : r.participant.isSpeaking !== i.participant.isSpeaking ? Fr(r.participant, i.participant) : r.participant.lastSpokeAt !== i.participant.lastSpokeAt ? Ur(r.participant, i.participant) : ee(r) !== ee(i) ? Gs(r, i) : r.participant.isCameraEnabled !== i.participant.isCameraEnabled ? Js(r, i) : yt(r.participant, i.participant)), [...t, ...n];
}
function Un(e) {
	const t = [...e];
	t.sort((r, i) => {
		if (r.isSpeaking && i.isSpeaking) return Nr(r, i);
		if (r.isSpeaking !== i.isSpeaking) return Fr(r, i);
		if (r.lastSpokeAt !== i.lastSpokeAt) return Ur(r, i);
		const o = r.videoTrackPublications.size > 0;
		return o !== i.videoTrackPublications.size > 0 ? o ? -1 : 1 : yt(r, i);
	});
	const n = t.find((r) => r.isLocal);
	if (n) {
		const r = t.indexOf(n);
		r >= 0 && (t.splice(r, 1), t.length > 0 ? t.splice(0, 0, n) : t.push(n));
	}
	return t;
}
function Zs(e, t) {
	return e.reduce((n, r, i) => i % t === 0 ? [...n, [r]] : [...n.slice(0, -1), [...n.slice(-1)[0], r]], []);
}
function $n(e, t) {
	const n = Math.max(e.length, t.length);
	return new Array(n).fill([]).map((r, i) => [e[i], t[i]]);
}
function wt(e, t, n) {
	return e.filter((r) => !t.map((i) => n(i)).includes(n(r)));
}
function Qt$1(e) {
	return e.map((t) => typeof t == "string" || typeof t == "number" ? `${t}` : G(t));
}
function ea$1(e, t) {
	return {
		dropped: wt(e, t, G),
		added: wt(t, e, G)
	};
}
function ta$1(e) {
	return e.added.length !== 0 || e.dropped.length !== 0;
}
function Xt$1(e, t) {
	const n = t.findIndex((r) => G(r) === G(e));
	if (n === -1) throw new Error(`Element not part of the array: ${G(e)} not in ${Qt$1(t)}`);
	return n;
}
function na$1(e, t, n) {
	const r = Xt$1(e, n), i = Xt$1(t, n);
	return n.splice(r, 1, t), n.splice(i, 1, e), n;
}
function ra$1(e, t) {
	const n = Xt$1(e, t);
	return t.splice(n, 1), t;
}
function ia$1(e, t) {
	return [...t, e];
}
function Vt(e, t) {
	return Zs(e, t);
}
function oa$1(e, t, n) {
	let r = sa$1(e, t);
	if (r.length < t.length) {
		const i = wt(t, r, G);
		r = [...r, ...i];
	}
	if ($n(Vt(r, n), Vt(t, n)).forEach(([i, o], s) => {
		if (i && o) {
			const a = Vt(r, n)[s], c = ea$1(a, o);
			ta$1(c) && (R.debug(`Detected visual changes on page: ${s}, current: ${Qt$1(i)}, next: ${Qt$1(o)}`, { changes: c }), c.added.length === c.dropped.length && $n(c.added, c.dropped).forEach(([u, f]) => {
				if (u && f) r = na$1(u, f, r);
				else throw new Error(`For a swap action we need a addition and a removal one is missing: ${u}, ${f}`);
			}), c.added.length === 0 && c.dropped.length > 0 && c.dropped.forEach((u) => {
				r = ra$1(u, r);
			}), c.added.length > 0 && c.dropped.length === 0 && c.added.forEach((u) => {
				r = ia$1(u, r);
			}));
		}
	}), r.length > t.length) {
		const i = wt(r, t, G);
		r = r.filter((o) => !i.map(G).includes(G(o)));
	}
	return r;
}
function sa$1(e, t) {
	return e.map((n) => {
		return t.find((i) => G(n) === G(i) || typeof n != "number" && Xe(n) && ee(i) && Us(n, i)) ?? n;
	});
}
function ne(e) {
	return `lk-${e}`;
}
function aa$1(e) {
	const t = Zt$1(e), n = dn(e.participant).pipe(W$1(() => Zt$1(e)), J$1(t));
	return {
		className: ne(e.source === Track.Source.Camera || e.source === Track.Source.ScreenShare ? "participant-media-video" : "participant-media-audio"),
		trackObserver: n
	};
}
function Zt$1(e) {
	if (ee(e)) return e.publication;
	{
		const { source: t, name: n, participant: r } = e;
		if (t && n) return r.getTrackPublications().find((i) => i.source === t && i.trackName === n);
		if (n) return r.getTrackPublicationByName(n);
		if (t) return r.getTrackPublication(t);
		throw new Error("At least one of source and name needs to be defined");
	}
}
function _e$1(e, ...t) {
	return new B((n) => {
		const r = () => {
			n.next(e);
		};
		return t.forEach((o) => {
			e.on(o, r);
		}), () => {
			t.forEach((o) => {
				e.off(o, r);
			});
		};
	}).pipe(J$1(e));
}
function ze$1(e, t) {
	return new B((n) => {
		const r = (...o) => {
			n.next(o);
		};
		return e.on(t, r), () => {
			e.off(t, r);
		};
	});
}
function ca$1(e) {
	return ze$1(e, RoomEvent.ConnectionStateChanged).pipe(W$1(([t]) => t), J$1(e.state));
}
function ua$1(e) {
	return _e$1(e, RoomEvent.RoomMetadataChanged, RoomEvent.ConnectionStateChanged).pipe(W$1((t) => ({
		name: t.name,
		metadata: t.metadata
	})));
}
function la$1(e) {
	return ze$1(e, RoomEvent.ActiveSpeakersChanged).pipe(W$1(([t]) => t));
}
function $r(e, t, n = !0) {
	return bt(new B((r) => {
		Room.getLocalDevices(e, n).then((i) => {
			r.next(i), r.complete();
		}).catch((i) => {
			t?.(i), r.next([]), r.complete();
		});
	}), new B((r) => {
		var i;
		const o = () => Z$1(null, null, function* () {
			try {
				const s = yield Room.getLocalDevices(e, n);
				r.next(s);
			} catch (s) {
				t?.(s);
			}
		});
		if (typeof window < "u") {
			if (!window.isSecureContext) throw new Error("Accessing media devices is available only in secure contexts (HTTPS and localhost), in some or all supporting browsers. See: https://developer.mozilla.org/en-US/docs/Web/API/Navigator/mediaDevices");
			(i = navigator?.mediaDevices) == null || i.addEventListener("devicechange", o);
		}
		return () => {
			var s;
			(s = navigator?.mediaDevices) == null || s.removeEventListener("devicechange", o);
		};
	}));
}
function fa$1(e) {
	return ze$1(e, RoomEvent.DataReceived);
}
function Wr(e) {
	return _e$1(e, RoomEvent.AudioPlaybackStatusChanged).pipe(W$1((t) => ({ canPlayAudio: t.canPlaybackAudio })));
}
function da$1(e) {
	return _e$1(e, RoomEvent.VideoPlaybackStatusChanged).pipe(W$1((t) => ({ canPlayVideo: t.canPlaybackVideo })));
}
function pa$1(e, t) {
	return ze$1(e, RoomEvent.ActiveDeviceChanged).pipe(At(([n]) => n === t), W$1(([n, r]) => (R.debug("activeDeviceObservable | RoomEvent.ActiveDeviceChanged", {
		kind: n,
		deviceId: r
	}), r)));
}
function ha$1(e, t) {
	return ze$1(e, RoomEvent.ParticipantEncryptionStatusChanged).pipe(At(([, n]) => t?.identity === n?.identity || !n && t?.identity === e.localParticipant.identity), W$1(([n]) => n), J$1(t?.isLocal ? t.isE2EEEnabled : !!t?.isEncrypted));
}
function va$1(e) {
	return ze$1(e, RoomEvent.RecordingStatusChanged).pipe(W$1(([t]) => t), J$1(e.isRecording));
}
function qe(e, ...t) {
	return new B((n) => {
		const r = () => {
			n.next(e);
		};
		return t.forEach((o) => {
			e.on(o, r);
		}), () => {
			t.forEach((o) => {
				e.off(o, r);
			});
		};
	}).pipe(J$1(e));
}
function dn(e) {
	return qe(e, ParticipantEvent.TrackMuted, ParticipantEvent.TrackUnmuted, ParticipantEvent.ParticipantPermissionsChanged, ParticipantEvent.TrackPublished, ParticipantEvent.TrackUnpublished, ParticipantEvent.LocalTrackPublished, ParticipantEvent.LocalTrackUnpublished, ParticipantEvent.MediaDevicesError, ParticipantEvent.TrackSubscriptionStatusChanged).pipe(W$1((t) => {
		const { isMicrophoneEnabled: n, isCameraEnabled: r, isScreenShareEnabled: i } = t, o = t.getTrackPublication(Track.Source.Microphone);
		return {
			isCameraEnabled: r,
			isMicrophoneEnabled: n,
			isScreenShareEnabled: i,
			cameraTrack: t.getTrackPublication(Track.Source.Camera),
			microphoneTrack: o,
			participant: t
		};
	}));
}
function jr(e) {
	if (e) return qe(e, ParticipantEvent.ParticipantMetadataChanged, ParticipantEvent.ParticipantNameChanged).pipe(W$1(({ name: t, identity: n, metadata: r }) => ({
		name: t,
		identity: n,
		metadata: r
	})), J$1({
		name: e.name,
		identity: e.identity,
		metadata: e.metadata
	}));
}
function ma(e) {
	return Lt(e, ParticipantEvent.ConnectionQualityChanged).pipe(W$1(([t]) => t), J$1(e.connectionQuality));
}
function Lt(e, t) {
	return new B((n) => {
		const r = (...o) => {
			n.next(o);
		};
		return e.on(t, r), () => {
			e.off(t, r);
		};
	});
}
function Br(e) {
	var t, n, r, i;
	return qe(e.participant, ParticipantEvent.TrackMuted, ParticipantEvent.TrackUnmuted, ParticipantEvent.TrackSubscribed, ParticipantEvent.TrackUnsubscribed, ParticipantEvent.LocalTrackPublished, ParticipantEvent.LocalTrackUnpublished).pipe(W$1((o) => {
		var s, a;
		return (a = ((s = e.publication) != null ? s : o.getTrackPublication(e.source))?.isMuted) != null ? a : !0;
	}), J$1((i = (r = (t = e.publication) == null ? void 0 : t.isMuted) != null ? r : (n = e.participant.getTrackPublication(e.source)) == null ? void 0 : n.isMuted) != null ? i : !0));
}
function ga$1(e) {
	return Lt(e, ParticipantEvent.IsSpeakingChanged).pipe(W$1(([t]) => t));
}
function ba$1(e, t = {}) {
	var n;
	let r;
	const i = new B((a) => (r = a, () => s.unsubscribe())).pipe(J$1(Array.from(e.remoteParticipants.values()))), o = (n = t.additionalRoomEvents) != null ? n : Ar, s = _e$1(e, ...Array.from(/* @__PURE__ */ new Set([
		RoomEvent.ParticipantConnected,
		RoomEvent.ParticipantDisconnected,
		RoomEvent.ConnectionStateChanged,
		...o
	]))).subscribe((a) => r?.next(Array.from(a.remoteParticipants.values())));
	return e.remoteParticipants.size > 0 && r?.next(Array.from(e.remoteParticipants.values())), i;
}
function ya$1(e, t, n = {}) {
	var r;
	const i = (r = n.additionalEvents) != null ? r : Lr;
	return _e$1(e, RoomEvent.ParticipantConnected, RoomEvent.ParticipantDisconnected, RoomEvent.ConnectionStateChanged).pipe(Mr((o) => {
		const s = o.getParticipantByIdentity(t);
		return s ? qe(s, ...i) : new B((a) => a.next(void 0));
	}), J$1(e.getParticipantByIdentity(t)));
}
function Vr(e) {
	return Lt(e, ParticipantEvent.ParticipantPermissionsChanged).pipe(W$1(() => e.permissions), J$1(e.permissions));
}
function wa$1(e, { kind: t, identity: n }, r = {}) {
	var i;
	const o = (i = r.additionalEvents) != null ? i : Lr, s = (a) => {
		let c = !0;
		return t && (c = c && a.kind === t), n && (c = c && a.identity === n), c;
	};
	return _e$1(e, RoomEvent.ParticipantConnected, RoomEvent.ParticipantDisconnected, RoomEvent.ConnectionStateChanged).pipe(Mr((a) => {
		const c = Array.from(a.remoteParticipants.values()).find((u) => s(u));
		return c ? qe(c, ...o) : new B((u) => u.next(void 0));
	}), J$1(Array.from(e.remoteParticipants.values()).find((a) => s(a))));
}
function en(e) {
	return typeof e > "u" ? new B() : Lt(e, ParticipantEvent.AttributesChanged).pipe(W$1(([t]) => ({
		changed: t,
		attributes: e.attributes
	})), J$1({
		changed: e.attributes,
		attributes: e.attributes
	}));
}
function Sa$1(e, t, n, r, i) {
	const { localParticipant: o } = t, s = (f, d) => {
		let p = !1;
		switch (f) {
			case Track.Source.Camera:
				p = d.isCameraEnabled;
				break;
			case Track.Source.Microphone:
				p = d.isMicrophoneEnabled;
				break;
			case Track.Source.ScreenShare:
				p = d.isScreenShareEnabled;
				break;
			default: break;
		}
		return p;
	}, a = dn(o).pipe(W$1((f) => s(e, f.participant)), J$1(s(e, o))), c = new be$1(), u = (f, d) => Z$1(null, null, function* () {
		try {
			switch (d ?? (d = n), c.next(!0), e) {
				case Track.Source.Camera: return yield o.setCameraEnabled(f ?? !o.isCameraEnabled, d, r), o.isCameraEnabled;
				case Track.Source.Microphone: return yield o.setMicrophoneEnabled(f ?? !o.isMicrophoneEnabled, d, r), o.isMicrophoneEnabled;
				case Track.Source.ScreenShare: return yield o.setScreenShareEnabled(f ?? !o.isScreenShareEnabled, d, r), o.isScreenShareEnabled;
				default: throw new TypeError("Tried to toggle unsupported source");
			}
		} catch (p) {
			if (i && p instanceof Error) {
				i?.(p);
				return;
			} else throw p;
		} finally {
			c.next(!1);
		}
	});
	return {
		className: ne("button"),
		toggle: u,
		enabledObserver: a,
		pendingObserver: c.asObservable()
	};
}
function Ea$1() {
	let e = !1;
	const t = new be$1(), n = new be$1(), r = (i) => Z$1(null, null, function* () {
		n.next(!0), e = i ?? !e, t.next(e), n.next(!1);
	});
	return {
		className: ne("button"),
		toggle: r,
		enabledObserver: t.asObservable(),
		pendingObserver: n.asObservable()
	};
}
function Ca$1(e, t, n) {
	const r = new hr(void 0), i = pa$1(t, e), o = (s, ...a) => Z$1(null, [s, ...a], function* (c, u = {}) {
		var f, d, p;
		if (n) {
			yield n.setDeviceId(u.exact ? { exact: c } : c);
			const h = yield n.getDeviceId(!1);
			r.next(c === "default" && n.mediaStreamTrack.label.startsWith("Default") ? c : h);
		} else if (t) {
			const h = getBrowser();
			if (e === "audiooutput" && (h?.name === "Safari" || h?.os === "iOS")) {
				R.warn("Switching audio output device is not supported on Safari and iOS.");
				return;
			}
			R.debug(`Switching active device of kind "${e}" with id ${c}.`), yield t.switchActiveDevice(e, c, u.exact);
			const m = (f = t.getActiveDevice(e)) != null ? f : c;
			m !== c && c !== "default" && R.info(`We tried to select the device with id (${c}), but the browser decided to select the device with id (${m}) instead.`);
			let S;
			e === "audioinput" ? S = (d = t.localParticipant.getTrackPublication(Track.Source.Microphone)) == null ? void 0 : d.track : e === "videoinput" && (S = (p = t.localParticipant.getTrackPublication(Track.Source.Camera)) == null ? void 0 : p.track);
			const y = c === "default" && !S || c === "default" && S?.mediaStreamTrack.label.startsWith("Default");
			r.next(y ? c : m);
		}
	});
	return {
		className: ne("media-device-select"),
		activeDeviceObservable: i,
		setActiveMediaDevice: o
	};
}
function Ta$1(e) {
	const t = (n) => {
		e.disconnect(n);
	};
	return {
		className: ne("disconnect-button"),
		disconnect: t
	};
}
function xa$1(e) {
	return {
		className: ne("connection-quality"),
		connectionQualityObserver: ma(e)
	};
}
function ka$1(e) {
	let t = "track-muted-indicator-camera";
	switch (e.source) {
		case Track.Source.Camera:
			t = "track-muted-indicator-camera";
			break;
		case Track.Source.Microphone:
			t = "track-muted-indicator-microphone";
			break;
		default: break;
	}
	return {
		className: ne(t),
		mediaMutedObserver: Br(e)
	};
}
function eu(e) {
	return {
		className: "lk-participant-name",
		infoObserver: jr(e)
	};
}
function Pa$1() {
	return { className: ne("participant-tile") };
}
var Hr = {
	CHAT: "lk.chat",
	TRANSCRIPTION: "lk.transcription"
}, Ma$1 = { CHAT: "lk-chat-topic" };
function zr(e, t) {
	return Z$1(this, arguments, function* (n, r, i = {}) {
		const { reliable: o, destinationIdentities: s, topic: a } = i;
		yield n.publishData(r, {
			destinationIdentities: s,
			topic: a,
			reliable: o
		});
	});
}
function qr(e, t, n) {
	const r = Array.isArray(t) ? t : [t], i = fa$1(e).pipe(At(([, , , c]) => t === void 0 || c !== void 0 && r.includes(c)), W$1(([c, u, , f]) => {
		const d = {
			payload: c,
			topic: f,
			from: u
		};
		return n?.(d), d;
	}));
	let o;
	return {
		messageObservable: i,
		isSendingObservable: new B((c) => {
			o = c;
		}),
		send: (c, ...u) => Z$1(null, [c, ...u], function* (f, d = {}) {
			o.next(!0);
			try {
				yield zr(e.localParticipant, f, ae({ topic: r[0] }, d));
			} finally {
				o.next(!1);
			}
		})
	};
}
var Aa$1 = class {
	constructor(e, t) {
		this._isResolved = !1, this.onFinally = t, this.promise = new Promise((n, r) => Z$1(this, null, function* () {
			this.resolve = n, this.reject = r, e && (yield e(n, r));
		})).finally(() => {
			var n;
			this._isResolved = !0, (n = this.onFinally) == null || n.call(this);
		});
	}
	get isResolved() {
		return this._isResolved;
	}
}, ot = /* @__PURE__ */ new WeakMap(), st = /* @__PURE__ */ new Map();
function La$1(e) {
	return e.ignoreLegacy == !0;
}
var Oa$1 = (e) => JSON.parse(new TextDecoder().decode(e)), _a$1 = (e) => new TextEncoder().encode(JSON.stringify(e));
function Ia$1(e, t) {
	var n, r, i, o, s, a;
	const c = () => {
		var M, v, g;
		return ((M = e.serverInfo) == null ? void 0 : M.edition) === 1 || !!((v = e.serverInfo) != null && v.version) && compareVersions((g = e.serverInfo) == null ? void 0 : g.version, "1.8.2") > 0;
	}, u = new be$1(), f = (n = t?.channelTopic) != null ? n : Hr.CHAT, d = (r = t?.channelTopic) != null ? r : Ma$1.CHAT;
	let p = !1;
	ot.has(e) || (p = !0);
	const h = (i = ot.get(e)) != null ? i : /* @__PURE__ */ new Map(), m = (o = h.get(f)) != null ? o : new be$1();
	h.set(f, m), ot.set(e, h);
	const S = (s = t?.messageDecoder) != null ? s : Oa$1;
	if (p) {
		e.registerTextStreamHandler(f, (v, g) => Z$1(null, null, function* () {
			const { id: E, timestamp: x, attributes: C, attachedStreamIds: I } = v.info, T = new Map((I ?? []).map((A) => [A, new Aa$1()]));
			st.set(E, T), $e$1(v).pipe(ct((A, D) => A + D), Ve$1((A) => T.size === 0 ? Jt$1({
				chunk: A,
				attachedFiles: []
			}) : $e$1(T.values()).pipe(Ss((D) => $e$1(D.promise)), ct((D, U) => [...D, new File(U.buffer, U.fileName, { type: U.mimeType })], []), W$1((D) => ({
				chunk: A,
				attachedFiles: D
			})))), W$1(({ chunk: A, attachedFiles: D }) => ({
				id: E,
				timestamp: x,
				message: A,
				from: e.getParticipantByIdentity(g.identity),
				type: "chatMessage",
				attributes: C,
				attachedFiles: D
			})), Ts(() => st.delete(E))).subscribe({ next: (A) => m.next(A) });
		})), e.registerByteStreamHandler(f, (v) => Z$1(null, null, function* () {
			var g, E;
			const { id: x } = v.info, C = Array.from(st).find(([, V]) => V.has(x));
			if (!C) return;
			const I = C[0], T = [];
			try {
				for (var A = Rs(v), D, U, H; D = !(U = yield A.next()).done; D = !1) {
					const V = U.value;
					T.push(V);
				}
			} catch (V) {
				H = [V];
			} finally {
				try {
					D && (U = A.return) && (yield U.call(A));
				} finally {
					if (H) throw H[0];
				}
			}
			const z = (g = st.get(I)) == null ? void 0 : g.get(x);
			z && ((E = z.resolve) == null || E.call(z, {
				fileName: v.info.name,
				mimeType: v.info.mimeType,
				buffer: T
			}));
		}));
		const { messageObservable: M } = qr(e, [d]);
		M.pipe(W$1((v) => {
			const g = S(v.payload);
			if (!La$1(g)) return Pe$1(ae({}, g), {
				type: "chatMessage",
				from: v.from
			});
		}), At((v) => !!v), In(u)).subscribe(m);
	}
	const y = m.pipe(ct((M, v) => {
		if ("id" in v && M.find((g) => {
			var E, x;
			return ((E = g.from) == null ? void 0 : E.identity) === ((x = v.from) == null ? void 0 : x.identity) && g.id === v.id;
		})) {
			const g = M.findIndex((E) => E.id === v.id);
			if (g > -1) {
				const E = M[g];
				M[g] = Pe$1(ae({}, v), {
					timestamp: E.timestamp,
					editTimestamp: v.timestamp
				});
			}
			return [...M];
		}
		return [...M, v];
	}, []), In(u)), b = new hr(!1), w = (a = t?.messageEncoder) != null ? a : _a$1, P = (M, v) => Z$1(null, null, function* () {
		v || (v = {}), v.topic ?? (v.topic = f), b.next(!0);
		try {
			const g = {
				id: (yield e.localParticipant.sendText(M, v)).id,
				timestamp: Date.now(),
				message: M
			}, E = Pe$1(ae({}, Pe$1(ae({}, g), { attachedFiles: v.attachments })), {
				type: "chatMessage",
				from: e.localParticipant,
				attributes: v.attributes
			});
			m.next(E);
			const x = w(Pe$1(ae({}, g), { ignoreLegacy: c() }));
			try {
				yield zr(e.localParticipant, x, {
					reliable: !0,
					topic: d
				});
			} catch (C) {
				R.info("could not send message in legacy chat format", C);
			}
			return E;
		} finally {
			b.next(!1);
		}
	});
	function N() {
		u.next(), u.complete(), m.complete(), ot.delete(e), e.unregisterTextStreamHandler(f), e.unregisterByteStreamHandler(f);
	}
	return e.once(RoomEvent.Disconnected, N), {
		messageObservable: y,
		isSendingObservable: b,
		send: P
	};
}
function Ra$1() {
	const e = (t) => Z$1(null, null, function* () {
		R.info("Start Audio for room: ", t), yield t.startAudio();
	});
	return {
		className: ne("start-audio-button"),
		roomAudioPlaybackAllowedObservable: Wr,
		handleStartAudioPlayback: e
	};
}
function Da$1() {
	const e = (t) => Z$1(null, null, function* () {
		R.info("Start Video for room: ", t), yield t.startVideo();
	});
	return {
		className: ne("start-audio-button"),
		roomVideoPlaybackAllowedObservable: da$1,
		handleStartVideoPlayback: e
	};
}
function Na$1() {
	return { className: [ne("button"), ne("chat-toggle")].join(" ") };
}
function Fa$1() {
	return { className: [ne("button"), ne("focus-toggle-button")].join(" ") };
}
function Ua() {
	return { className: "lk-clear-pin-button lk-button" };
}
function $a$1() {
	return { className: "lk-room-container" };
}
function Wn(e, t, n = !0) {
	const r = [e.localParticipant, ...Array.from(e.remoteParticipants.values())], i = [];
	return r.forEach((o) => {
		t.forEach((s) => {
			const a = Array.from(o.trackPublications.values()).filter((c) => c.source === s && (!n || c.track)).map((c) => ({
				participant: o,
				publication: c,
				source: c.source
			}));
			i.push(...a);
		});
	}), {
		trackReferences: i,
		participants: r
	};
}
function jn(e, t, n = !1) {
	const { sources: r, kind: i, name: o } = t;
	return Array.from(e.trackPublications.values()).filter((s) => (!r || r.includes(s.source)) && (!i || s.kind === i) && (!o || s.trackName === o) && (!n || s.track)).map((s) => ({
		participant: e,
		publication: s,
		source: s.source
	}));
}
function Wa(e, t, n) {
	var r, i;
	const o = (r = n.additionalRoomEvents) != null ? r : Ar, s = (i = n.onlySubscribed) != null ? i : !0;
	return _e$1(e, ...Array.from((/* @__PURE__ */ new Set([
		RoomEvent.ParticipantConnected,
		RoomEvent.ParticipantDisconnected,
		RoomEvent.ConnectionStateChanged,
		RoomEvent.LocalTrackPublished,
		RoomEvent.LocalTrackUnpublished,
		RoomEvent.TrackPublished,
		RoomEvent.TrackUnpublished,
		RoomEvent.TrackSubscriptionStatusChanged,
		...o
	])).values())).pipe(W$1((a) => {
		const c = Wn(a, t, s);
		return R.debug(`TrackReference[] was updated. (length ${c.trackReferences.length})`, c), c;
	}), J$1(Wn(e, t, s)));
}
function ja(e, t) {
	return qe(e, ...Bs).pipe(W$1((n) => {
		const r = jn(n, t);
		return R.debug(`TrackReference[] was updated. (length ${r.length})`, r), r;
	}), J$1(jn(e, t)));
}
function Kr(e, t) {
	return new B((n) => {
		const r = (...o) => {
			n.next(o);
		};
		return e.on(t, r), () => {
			e.off(t, r);
		};
	});
}
function Ba$1(e) {
	return Kr(e, TrackEvent.TranscriptionReceived);
}
function Va$1(e) {
	return Kr(e, TrackEvent.TimeSyncUpdate).pipe(W$1(([t]) => t));
}
function tu(e, t = 1e3) {
	if (e === null) return Jt$1(!1);
	const n = Yt$1(e, "mousemove", { passive: !0 }).pipe(W$1(() => !0)), r = n.pipe(ss({
		each: t,
		with: () => bt(Jt$1(!1), r.pipe(ks(n)))
	}), Es());
	return r;
}
function Ha(e, t) {
	if (typeof localStorage > "u") {
		R.error("Local storage is not available.");
		return;
	}
	try {
		if (t) {
			const n = Object.fromEntries(Object.entries(t).filter(([, r]) => r !== ""));
			localStorage.setItem(e, JSON.stringify(n));
		}
	} catch (n) {
		R.error(`Error setting item to local storage: ${n}`);
	}
}
function za$1(e) {
	if (typeof localStorage > "u") {
		R.error("Local storage is not available.");
		return;
	}
	try {
		const t = localStorage.getItem(e);
		if (!t) {
			R.warn(`Item with key ${e} does not exist in local storage.`);
			return;
		}
		return JSON.parse(t);
	} catch (t) {
		R.error(`Error getting item from local storage: ${t}`);
		return;
	}
}
function qa(e) {
	return {
		load: () => za$1(e),
		save: (t) => Ha(e, t)
	};
}
var Ka$1 = "lk-user-choices", Ge = {
	videoEnabled: !0,
	audioEnabled: !0,
	videoDeviceId: "default",
	audioDeviceId: "default",
	username: ""
}, { load: Ga$1, save: Ja$1 } = qa(Ka$1);
function Ya(e, t = !1) {
	t !== !0 && Ja$1(e);
}
function Qa$1(e, t = !1) {
	var n, r, i, o, s;
	const a = {
		videoEnabled: (n = e?.videoEnabled) != null ? n : Ge.videoEnabled,
		audioEnabled: (r = e?.audioEnabled) != null ? r : Ge.audioEnabled,
		videoDeviceId: (i = e?.videoDeviceId) != null ? i : Ge.videoDeviceId,
		audioDeviceId: (o = e?.audioDeviceId) != null ? o : Ge.audioDeviceId,
		username: (s = e?.username) != null ? s : Ge.username
	};
	if (t) return a;
	{
		const c = Ga$1();
		return ae(ae({}, a), c ?? {});
	}
}
var Ht = null, zt = null, Xa$1 = 0;
function Bn() {
	return Ht || (Ht = /* @__PURE__ */ new Map()), Ht;
}
function Za$1() {
	return zt || (zt = /* @__PURE__ */ new WeakMap()), zt;
}
function ec(e, t) {
	const n = Za$1();
	let r = n.get(e);
	return r || (r = `room_${Xa$1++}`, n.set(e, r)), `${r}:${t}`;
}
function tc(e, t) {
	const n = ec(e, t), r = Bn(), i = r.get(n);
	if (i) return i;
	const o = new be$1();
	let s = [];
	const a = "lk.segment_id", c = o.pipe(Ps({
		subscribe: () => {
			e.registerTextStreamHandler(t, (u, f) => Z$1(null, null, function* () {
				var d;
				const p = $e$1(u).pipe(ct((m, S) => m + S, "")), h = !!((d = u.info.attributes) != null && d[a]);
				p.subscribe((m) => {
					const S = s.findIndex((y) => {
						var b, w;
						return y.streamInfo.id === u.info.id || h && ((b = y.streamInfo.attributes) == null ? void 0 : b[a]) === ((w = u.info.attributes) == null ? void 0 : w[a]);
					});
					S !== -1 ? (s[S] = Pe$1(ae({}, s[S]), {
						text: m,
						streamInfo: u.info
					}), o.next([...s])) : (s.push({
						text: m,
						participantInfo: f,
						streamInfo: u.info
					}), o.next([...s]));
				});
			}));
		},
		finalize: () => {
			e.unregisterTextStreamHandler(t);
		}
	}), xs());
	return r.set(n, c), e.on(RoomEvent.Disconnected, () => {
		Bn().delete(n), s = [], o.next([]);
	}), c;
}
function Gr(e, t) {
	if (t.msg === "show_chat") return {
		...e,
		showChat: !0,
		unreadMessages: 0
	};
	if (t.msg === "hide_chat") return {
		...e,
		showChat: !1
	};
	if (t.msg === "toggle_chat") {
		const n = {
			...e,
			showChat: !e.showChat
		};
		return n.showChat === !0 && (n.unreadMessages = 0), n;
	} else return t.msg === "unread_msg" ? {
		...e,
		unreadMessages: t.count
	} : t.msg === "toggle_settings" ? {
		...e,
		showSettings: !e.showSettings
	} : { ...e };
}
function Jr(e, t) {
	return t.msg === "set_pin" ? [t.trackReference] : t.msg === "clear_pin" ? [] : { ...e };
}
var Yr = import_react.createContext(void 0);
function Qr() {
	const e = import_react.useContext(Yr);
	if (!e) throw Error("Tried to access LayoutContext context outside a LayoutContextProvider provider.");
	return e;
}
function nc(e) {
	const t = Xr();
	if (e ?? (e = t), !e) throw Error("Tried to access LayoutContext context outside a LayoutContextProvider provider.");
	return e;
}
function nu() {
	const [e, t] = import_react.useReducer(Jr, _r), [n, r] = import_react.useReducer(Gr, Ir);
	return {
		pin: {
			dispatch: t,
			state: e
		},
		widget: {
			dispatch: r,
			state: n
		}
	};
}
function ru(e) {
	const [t, n] = import_react.useReducer(Jr, _r), [r, i] = import_react.useReducer(Gr, Ir);
	return e ?? {
		pin: {
			dispatch: n,
			state: t
		},
		widget: {
			dispatch: i,
			state: r
		}
	};
}
function Xr() {
	return import_react.useContext(Yr);
}
var Zr = import_react.createContext(void 0);
function iu() {
	const e = import_react.useContext(Zr);
	if (!e) throw Error("tried to access track context outside of track context provider");
	return e;
}
function ei() {
	return import_react.useContext(Zr);
}
function pn(e) {
	const t = ei(), n = e ?? t;
	if (!n) throw new Error("No TrackRef, make sure you are inside a TrackRefContext or pass the TrackRef explicitly");
	return n;
}
var ti = import_react.createContext(void 0);
function ou() {
	const e = import_react.useContext(ti);
	if (!e) throw Error("tried to access participant context outside of participant context provider");
	return e;
}
function Ot() {
	return import_react.useContext(ti);
}
function Ie(e) {
	const t = Ot(), n = ei(), r = e ?? t ?? n?.participant;
	if (!r) throw new Error("No participant provided, make sure you are inside a participant context or pass the participant explicitly");
	return r;
}
var hn = import_react.createContext(void 0);
function _t() {
	const e = import_react.useContext(hn);
	if (!e) throw Error("tried to access room context outside of livekit room component");
	return e;
}
function It() {
	return import_react.useContext(hn);
}
function oe(e) {
	const t = It(), n = e ?? t;
	if (!n) throw new Error("No room provided, make sure you are inside a Room context or pass the room explicitly");
	return n;
}
var ni = import_react.createContext(void 0);
function su() {
	const e = import_react.useContext(ni);
	if (!e) throw Error("tried to access session context outside of SessionProvider component");
	return e;
}
function ri() {
	return import_react.useContext(ni);
}
function ii(e) {
	const t = ri(), n = e ?? t;
	if (!n) throw new Error("No session provided, make sure you are inside a Session context or pass the session explicitly");
	return n;
}
var oi = import_react.createContext(void 0);
function au(e) {
	const t = import_react.useContext(oi);
	if (e === !0) {
		if (t) return t;
		throw Error("tried to access feature context, but none is present");
	}
	return t;
}
function cu(e) {
	const t = oe(e), n = import_react.useCallback(async () => {
		await t.startAudio();
	}, [t]), { canPlayAudio: r } = j$1(import_react.useMemo(() => Wr(t), [t]), { canPlayAudio: t.canPlaybackAudio });
	return {
		canPlayAudio: r,
		startAudio: n
	};
}
function si(e) {
	var t, n, r = "";
	if (typeof e == "string" || typeof e == "number") r += e;
	else if (typeof e == "object") if (Array.isArray(e)) {
		var i = e.length;
		for (t = 0; t < i; t++) e[t] && (n = si(e[t])) && (r && (r += " "), r += n);
	} else for (n in e) e[n] && (r && (r += " "), r += n);
	return r;
}
function ai() {
	for (var e, t, n = 0, r = "", i = arguments.length; n < i; n++) (e = arguments[n]) && (t = si(e)) && (r && (r += " "), r += t);
	return r;
}
function rc(...e) {
	return (...t) => {
		for (const n of e) if (typeof n == "function") try {
			n(...t);
		} catch (r) {
			console.error(r);
		}
	};
}
function pe(...e) {
	const t = { ...e[0] };
	for (let n = 1; n < e.length; n++) {
		const r = e[n];
		for (const i in r) {
			const o = t[i], s = r[i];
			typeof o == "function" && typeof s == "function" && i[0] === "o" && i[1] === "n" && i.charCodeAt(2) >= 65 && i.charCodeAt(2) <= 90 ? t[i] = rc(o, s) : (i === "className" || i === "UNSAFE_className") && typeof o == "string" && typeof s == "string" ? t[i] = ai(o, s) : t[i] = s !== void 0 ? s : o;
		}
	}
	return t;
}
function uu(e) {
	const { state: t, dispatch: n } = Qr().pin;
	return { buttonProps: import_react.useMemo(() => {
		const { className: r } = Ua();
		return pe(e, {
			className: r,
			disabled: !t?.length,
			onClick: () => {
				n && n({ msg: "clear_pin" });
			}
		});
	}, [
		e,
		n,
		t
	]) };
}
function lu(e = {}) {
	const t = Ie(e.participant), { className: n, connectionQualityObserver: r } = import_react.useMemo(() => xa$1(t), [t]);
	return {
		className: n,
		quality: j$1(r, t.connectionQuality)
	};
}
function tt(e) {
	const t = oe(e);
	return j$1(import_react.useMemo(() => ca$1(t), [t]), t.state);
}
function fu(e, t) {
	const n = typeof e == "function" ? e : t, r = typeof e == "string" ? e : void 0, i = _t(), { send: o, messageObservable: s, isSendingObservable: a } = import_react.useMemo(() => qr(i, r, n), [
		i,
		r,
		n
	]);
	return {
		message: j$1(s, void 0),
		send: o,
		isSending: j$1(a, !1)
	};
}
function du(e) {
	const t = _t(), n = tt(t);
	return { buttonProps: import_react.useMemo(() => {
		const { className: r, disconnect: i } = Ta$1(t);
		return pe(e, {
			className: r,
			onClick: () => i(e.stopTracks ?? !0),
			disabled: n === ConnectionState.Disconnected
		});
	}, [
		t,
		e,
		n
	]) };
}
function ic(e) {
	if (e.publication instanceof LocalTrackPublication) {
		const t = e.publication.track;
		if (t) {
			const { facingMode: n } = facingModeFromLocalTrack(t);
			return n;
		}
	}
	return "undefined";
}
function pu({ trackRef: e, props: t }) {
	const n = pn(e), r = Xr(), { className: i } = import_react.useMemo(() => Fa$1(), []), o = import_react.useMemo(() => Fs(n, r?.pin.state), [n, r?.pin.state]);
	return {
		mergedProps: import_react.useMemo(() => pe(t, {
			className: i,
			onClick: (s) => {
				t.onClick?.(s), o ? r?.pin.dispatch?.({ msg: "clear_pin" }) : r?.pin.dispatch?.({
					msg: "set_pin",
					trackReference: n
				});
			}
		}), [
			t,
			i,
			n,
			o,
			r?.pin
		]),
		inFocus: o
	};
}
function hu(e, t, n = {}) {
	const r = n.gridLayouts ?? Hs, { width: i, height: o } = Oi(e), s = Or(r, t, i, o);
	return import_react.useEffect(() => {
		e.current && s && (e.current.style.setProperty("--lk-col-count", s?.columns.toString()), e.current.style.setProperty("--lk-row-count", s?.rows.toString()));
	}, [e, s]), {
		layout: s,
		containerWidth: i,
		containerHeight: o
	};
}
function Vn(e, t = {}) {
	const n = Ie(typeof e == "string" ? t.participant : e.participant), r = typeof e == "string" ? {
		participant: n,
		source: e
	} : e, [i, o] = import_react.useState(!!(r.publication?.isMuted || n.getTrackPublication(r.source)?.isMuted));
	return import_react.useEffect(() => {
		const s = Br(r).subscribe(o);
		return () => s.unsubscribe();
	}, [G(r)]), i;
}
function oc(e) {
	const t = Ie(e);
	return j$1(import_react.useMemo(() => ga$1(t), [t]), t.isSpeaking);
}
function sc(e) {
	return e !== void 0;
}
function vu(...e) {
	return pe(...e.filter(sc));
}
function mu(e, t, n) {
	return import_react.Children.map(e, (r) => import_react.isValidElement(r) && import_react.Children.only(e) ? (r.props.className && (t ?? (t = {}), t.className = ai(r.props.className, t.className), t.style = {
		...r.props.style,
		...t.style
	}), import_react.cloneElement(r, {
		...t,
		key: n
	})) : r);
}
function gu(e) {
	if (typeof window < "u" && typeof process < "u" && true) {
		const t = e ?? document.querySelector(".lk-room-container");
		t && !getComputedStyle(t).getPropertyValue("--lk-has-imported-styles") && R.warn("It looks like you're not using the `@livekit/components-styles package`. To render the UI with the default styling, please import it in your layout or page.");
	}
}
function ac(e, t) {
	return e === "processor" && t && typeof t == "object" && "name" in t ? t.name : (e === "e2ee" || e === "encryption") && t ? "e2ee-enabled" : t;
}
var cc = {
	connect: !0,
	audio: !1,
	video: !1
};
function uc(e) {
	const { token: t, serverUrl: n, options: r, room: i, connectOptions: o, connect: s, audio: a, video: c, screen: u, onConnected: f, onDisconnected: d, onError: p, onMediaDeviceFailure: h, onEncryptionError: m, simulateParticipants: S, ...y } = {
		...cc,
		...e
	};
	r && i && R.warn("when using a manually created room, the options object will be ignored. set the desired options directly when creating the room instead.");
	const [b, w] = import_react.useState(), P = import_react.useRef(s);
	import_react.useEffect(() => {
		w(i ?? new Room(r));
	}, [i, JSON.stringify(r, ac)]);
	const N = import_react.useMemo(() => {
		const { className: M } = $a$1();
		return pe(y, { className: M });
	}, [y]);
	return import_react.useEffect(() => {
		if (!b) return;
		const M = () => {
			const C = b.localParticipant;
			R.debug("trying to publish local tracks"), Promise.all([
				C.setMicrophoneEnabled(!!a, typeof a != "boolean" ? a : void 0),
				C.setCameraEnabled(!!c, typeof c != "boolean" ? c : void 0),
				C.setScreenShareEnabled(!!u, typeof u != "boolean" ? u : void 0)
			]).catch((I) => {
				R.warn(I), p?.(I);
			});
		}, v = (C, I) => {
			const T = MediaDeviceFailure.getFailure(C);
			h?.(T, I);
		}, g = (C) => {
			m?.(C);
		}, E = (C) => {
			d?.(C);
		}, x = () => {
			f?.();
		};
		return b.on(RoomEvent.SignalConnected, M).on(RoomEvent.MediaDevicesError, v).on(RoomEvent.EncryptionError, g).on(RoomEvent.Disconnected, E).on(RoomEvent.Connected, x), () => {
			b.off(RoomEvent.SignalConnected, M).off(RoomEvent.MediaDevicesError, v).off(RoomEvent.EncryptionError, g).off(RoomEvent.Disconnected, E).off(RoomEvent.Connected, x);
		};
	}, [
		b,
		a,
		c,
		u,
		p,
		m,
		h,
		f,
		d
	]), import_react.useEffect(() => {
		if (b) {
			if (S) {
				b.simulateParticipants({
					participants: { count: S },
					publish: {
						audio: !0,
						useRealTracks: !0
					}
				});
				return;
			}
			if (s) {
				if (P.current = !0, R.debug("connecting"), !t) {
					R.debug("no token yet");
					return;
				}
				if (!n) {
					R.warn("no livekit url provided"), p?.(Error("no livekit url provided"));
					return;
				}
				b.connect(n, t, o).catch((M) => {
					R.warn(M), P.current === !0 && p?.(M);
				});
			} else R.debug("disconnecting because connect is false"), P.current = !1, b.disconnect();
		}
	}, [
		s,
		t,
		JSON.stringify(o),
		b,
		p,
		n,
		S
	]), import_react.useEffect(() => {
		if (b) return () => {
			R.info("disconnecting on onmount"), b.disconnect();
		};
	}, [b]), {
		room: b,
		htmlProps: N
	};
}
function ci(e = {}) {
	const t = oe(e.room), [n, r] = import_react.useState(t.localParticipant), [i, o] = import_react.useState(n.isMicrophoneEnabled), [s, a] = import_react.useState(n.isCameraEnabled), [c, u] = import_react.useState(n.isScreenShareEnabled), [f, d] = import_react.useState(n.lastMicrophoneError), [p, h] = import_react.useState(n.lastCameraError), [m, S] = import_react.useState(void 0), [y, b] = import_react.useState(void 0), w = (P) => {
		a(P.isCameraEnabled), o(P.isMicrophoneEnabled), u(P.isScreenShareEnabled), b(P.cameraTrack), S(P.microphoneTrack), d(P.participant.lastMicrophoneError), h(P.participant.lastCameraError), r(P.participant);
	};
	return import_react.useEffect(() => {
		const P = dn(t.localParticipant).subscribe(w);
		return () => P.unsubscribe();
	}, [t]), {
		isMicrophoneEnabled: i,
		isScreenShareEnabled: c,
		isCameraEnabled: s,
		microphoneTrack: m,
		cameraTrack: y,
		lastMicrophoneError: f,
		lastCameraError: p,
		localParticipant: n
	};
}
function bu() {
	const e = _t();
	return j$1(import_react.useMemo(() => Vr(e.localParticipant), [e]), e.localParticipant.permissions);
}
function yu({ kind: e, room: t, track: n, requestPermissions: r, onError: i }) {
	const o = It(), s = import_react.useMemo(() => t ?? o ?? new Room(), [t, o]), a = j$1(import_react.useMemo(() => $r(e, i, r), [
		e,
		r,
		i
	]), []), [c, u] = import_react.useState(s?.getActiveDevice(e) ?? "default"), { className: f, activeDeviceObservable: d, setActiveMediaDevice: p } = import_react.useMemo(() => Ca$1(e, s, n), [
		e,
		s,
		n
	]);
	return import_react.useEffect(() => {
		const h = d.subscribe((m) => {
			m && (R.info("setCurrentDeviceId", m), u(m));
		});
		return () => {
			h?.unsubscribe();
		};
	}, [d]), {
		devices: a,
		className: f,
		activeDeviceId: c,
		setActiveMediaDevice: p
	};
}
function wu({ kind: e, onError: t }) {
	return j$1(import_react.useMemo(() => $r(e, t), [e, t]), []);
}
function lc(e, t, n = {}) {
	const r = import_react.useRef([]), i = import_react.useRef(-1), o = t !== i.current, s = typeof n.customSortFunction == "function" ? n.customSortFunction(e) : Ys(e);
	let a = [...s];
	if (o === !1) try {
		a = oa$1(r.current, s, t);
	} catch (c) {
		R.error("Error while running updatePages(): ", c);
	}
	return o ? r.current = s : r.current = a, i.current = t, a;
}
function Su(e, t) {
	const [n, r] = import_react.useState(1), i = Math.max(Math.ceil(t.length / e), 1);
	n > i && r(i);
	const o = n * e, s = o - e, a = (u) => {
		r((f) => u === "next" ? f === i ? f : f + 1 : f === 1 ? f : f - 1);
	};
	return {
		totalPageCount: i,
		nextPage: () => a("next"),
		prevPage: () => a("previous"),
		setPage: (u) => {
			u > i ? r(i) : u < 1 ? r(1) : r(u);
		},
		firstItemIndex: s,
		lastItemIndex: o,
		tracks: lc(t, e).slice(s, o),
		currentPage: n
	};
}
function fc(e = {}) {
	let t = Ot();
	e.participant && (t = e.participant);
	const { identity: n, name: r, metadata: i } = j$1(import_react.useMemo(() => jr(t), [t]), {
		name: t?.name,
		identity: t?.identity,
		metadata: t?.metadata
	});
	return {
		identity: n,
		name: r,
		metadata: i
	};
}
function Eu(e = {}) {
	const t = Ie(e.participant);
	return j$1(import_react.useMemo(() => Vr(t), [t]), t.permissions);
}
function Cu({ trackRef: e, onParticipantClick: t, disableSpeakingIndicator: n, htmlProps: r }) {
	const i = pn(e), o = import_react.useMemo(() => {
		const { className: p } = Pa$1();
		return pe(r, {
			className: p,
			onClick: (h) => {
				if (r.onClick?.(h), typeof t == "function") {
					const m = i.publication ?? i.participant.getTrackPublication(i.source);
					t({
						participant: i.participant,
						track: m
					});
				}
			}
		});
	}, [
		r,
		t,
		i.publication,
		i.source,
		i.participant
	]), s = i.participant.getTrackPublication(Track.Source.Microphone), a = import_react.useMemo(() => ({
		participant: i.participant,
		source: Track.Source.Microphone,
		publication: s
	}), [s, i.participant]), c = Vn(i), u = Vn(a), f = oc(i.participant), d = ic(i);
	return { elementProps: {
		"data-lk-audio-muted": u,
		"data-lk-video-muted": c,
		"data-lk-speaking": n === !0 ? !1 : f,
		"data-lk-local-participant": i.participant.isLocal,
		"data-lk-source": i.source,
		"data-lk-facing-mode": d,
		...o
	} };
}
function vn(e = {}) {
	const t = oe(e.room), [n, r] = import_react.useState([]);
	return import_react.useEffect(() => {
		const i = ba$1(t, { additionalRoomEvents: e.updateOnlyOn }).subscribe(r);
		return () => i.unsubscribe();
	}, [t, JSON.stringify(e.updateOnlyOn)]), n;
}
function dc(e = {}) {
	const t = vn(e), { localParticipant: n } = ci(e);
	return import_react.useMemo(() => [n, ...t], [n, t]);
}
function Tu(e) {
	return e = nc(e), import_react.useMemo(() => e?.pin.state !== void 0 && e.pin.state.length >= 1 ? e.pin.state : [], [e.pin.state]);
}
function xu(e, t = {}) {
	const n = _t(), [r] = import_react.useState(t.updateOnlyOn), i = import_react.useMemo(() => typeof e == "string" ? ya$1(n, e, { additionalEvents: r }) : wa$1(n, e, { additionalEvents: r }), [
		n,
		JSON.stringify(e),
		r
	]), [o, s] = import_react.useState({ p: void 0 });
	return import_react.useEffect(() => {
		const a = i.subscribe((c) => s({ p: c }));
		return () => a.unsubscribe();
	}, [i]), o.p;
}
function ku(e = {}) {
	const t = oe(e.room), { name: n, metadata: r } = j$1(import_react.useMemo(() => ua$1(t), [t]), {
		name: t.name,
		metadata: t.metadata
	});
	return {
		name: n,
		metadata: r
	};
}
function pc(e) {
	const t = oe(e?.room);
	return j$1(import_react.useMemo(() => la$1(t), [t]), t.activeSpeakers);
}
function Pu(e) {
	const [t, n] = import_react.useState(Un(e)), r = pc();
	return import_react.useEffect(() => {
		n(Un(e));
	}, [r, e]), t;
}
function Mu({ room: e, props: t }) {
	const n = oe(e), { className: r, roomAudioPlaybackAllowedObservable: i, handleStartAudioPlayback: o } = import_react.useMemo(() => Ra$1(), []), { canPlayAudio: s } = j$1(import_react.useMemo(() => i(n), [n, i]), { canPlayAudio: n.canPlaybackAudio });
	return {
		mergedProps: import_react.useMemo(() => pe(t, {
			className: r,
			onClick: () => {
				o(n);
			},
			style: { display: s ? "none" : "block" }
		}), [
			t,
			r,
			s,
			o,
			n
		]),
		canPlayAudio: s
	};
}
function Au({ room: e, props: t }) {
	const n = oe(e), { className: r, roomVideoPlaybackAllowedObservable: i, handleStartVideoPlayback: o } = import_react.useMemo(() => Da$1(), []), { canPlayVideo: s } = j$1(import_react.useMemo(() => i(n), [n, i]), { canPlayVideo: n.canPlaybackVideo });
	return {
		mergedProps: import_react.useMemo(() => pe(t, {
			className: r,
			onClick: () => {
				o(n);
			},
			style: { display: s ? "none" : "block" }
		}), [
			t,
			r,
			s,
			o,
			n
		]),
		canPlayVideo: s
	};
}
function Lu(e, t = {}) {
	const n = import_react.useRef(null), r = import_react.useRef(null), i = t.minSwipeDistance ?? 50, o = (c) => {
		r.current = null, n.current = c.targetTouches[0].clientX;
	}, s = (c) => {
		r.current = c.targetTouches[0].clientX;
	}, a = import_react.useCallback(() => {
		if (!n.current || !r.current) return;
		const c = n.current - r.current, u = c > i, f = c < -i;
		u && t.onLeftSwipe && t.onLeftSwipe(), f && t.onRightSwipe && t.onRightSwipe();
	}, [i, t]);
	import_react.useEffect(() => {
		const c = e.current;
		return c && (c.addEventListener("touchstart", o, { passive: !0 }), c.addEventListener("touchmove", s, { passive: !0 }), c.addEventListener("touchend", a, { passive: !0 })), () => {
			c && (c.removeEventListener("touchstart", o), c.removeEventListener("touchmove", s), c.removeEventListener("touchend", a));
		};
	}, [e, a]);
}
function Ou({ props: e }) {
	const { dispatch: t, state: n } = Qr().widget, { className: r } = import_react.useMemo(() => Na$1(), []);
	return { mergedProps: import_react.useMemo(() => pe(e, {
		className: r,
		onClick: () => {
			t && t({ msg: "toggle_chat" });
		},
		"aria-pressed": n?.showChat ? "true" : "false",
		"data-lk-unread-msgs": n ? n.unreadMessages < 10 ? n.unreadMessages.toFixed(0) : "9+" : "0"
	}), [
		e,
		r,
		t,
		n
	]) };
}
function _u(e, t, n = {}) {
	const [r, i] = import_react.useState(void 0);
	return import_react.useEffect(() => {
		if (e === void 0) throw Error("token endpoint needs to be defined");
		if (n.userInfo?.identity === void 0) return;
		(async () => {
			R.debug("fetching token");
			const s = new URLSearchParams({
				...n.userInfo,
				roomName: t
			}), a = await fetch(`${e}?${s.toString()}`);
			if (!a.ok) {
				R.error(`Could not fetch token. Server responded with status ${a.status}: ${a.statusText}`);
				return;
			}
			const { accessToken: c } = await a.json();
			i(c);
		})();
	}, [
		e,
		t,
		JSON.stringify(n)
	]), r;
}
function Iu(e) {
	const t = pn(e), { className: n, mediaMutedObserver: r } = import_react.useMemo(() => ka$1(t), [G(t)]);
	return {
		isMuted: j$1(r, !!(t.publication?.isMuted || t.participant.getTrackPublication(t.source)?.isMuted)),
		className: n
	};
}
function Ru({ source: e, onChange: t, initialState: n, captureOptions: r, publishOptions: i, onDeviceError: o, room: s, ...a }) {
	const c = It(), u = import_react.useMemo(() => s ?? c, [s, c]), f = u?.localParticipant?.getTrackPublication(e), d = import_react.useRef(!1), { toggle: p, className: h, pendingObserver: m, enabledObserver: S } = import_react.useMemo(() => u ? Sa$1(e, u, r, i, o) : Ea$1(), [
		u,
		e,
		JSON.stringify(r),
		i
	]), y = j$1(m, !1), b = j$1(S, n ?? !!f?.isEnabled);
	import_react.useEffect(() => {
		t?.(b, d.current), d.current = !1;
	}, [b, t]), import_react.useEffect(() => {
		n !== void 0 && (R.debug("forcing initial toggle state", e, n), p(n));
	}, []);
	const w = import_react.useMemo(() => pe(a, { className: h }), [a, h]), P = import_react.useCallback((N) => {
		d.current = !0, p().catch(() => d.current = !1), a.onClick?.(N);
	}, [a, p]);
	return {
		toggle: p,
		enabled: b,
		pending: y,
		track: f,
		buttonProps: {
			...w,
			"aria-pressed": b,
			"data-lk-source": e,
			"data-lk-enabled": b,
			disabled: y,
			onClick: P
		}
	};
}
function Du(e = [
	Track.Source.Camera,
	Track.Source.Microphone,
	Track.Source.ScreenShare,
	Track.Source.ScreenShareAudio,
	Track.Source.Unknown
], t = {}) {
	const n = oe(t.room), [r, i] = import_react.useState([]), [o, s] = import_react.useState([]), a = import_react.useMemo(() => e.map((c) => Rr(c) ? c.source : c), [JSON.stringify(e)]);
	return import_react.useEffect(() => {
		const c = Wa(n, a, {
			additionalRoomEvents: t.updateOnlyOn,
			onlySubscribed: t.onlySubscribed
		}).subscribe(({ trackReferences: u, participants: f }) => {
			R.debug("setting track bundles", u, f), i(u), s(f);
		});
		return () => c.unsubscribe();
	}, [
		n,
		JSON.stringify(t.onlySubscribed),
		JSON.stringify(t.updateOnlyOn),
		JSON.stringify(e)
	]), import_react.useMemo(() => {
		if (Dr(e)) {
			const c = vc(e, o), u = Array.from(r);
			return o.forEach((f) => {
				c.has(f.identity) && (c.get(f.identity) ?? []).forEach((d) => {
					if (r.find(({ participant: h, publication: m }) => f.identity === h.identity && m.source === d)) return;
					R.debug(`Add ${d} placeholder for participant ${f.identity}.`);
					const p = {
						participant: f,
						source: d
					};
					u.push(p);
				});
			}), u;
		} else return r;
	}, [
		r,
		o,
		e
	]);
}
function hc(e, t) {
	const n = new Set(e);
	for (const r of t) n.delete(r);
	return n;
}
function vc(e, t) {
	const n = /* @__PURE__ */ new Map();
	if (Dr(e)) {
		const r = e.filter((i) => i.withPlaceholder).map((i) => i.source);
		t.forEach((i) => {
			const o = i.getTrackPublications().map((a) => a.track?.source).filter((a) => a !== void 0), s = Array.from(hc(new Set(r), new Set(o)));
			s.length > 0 && n.set(i.identity, s);
		});
	}
	return n;
}
function mc(e) {
	const [t, n] = import_react.useState(Zt$1(e)), { trackObserver: r } = import_react.useMemo(() => aa$1(e), [e.participant.sid ?? e.participant.identity, e.source]);
	return import_react.useEffect(() => {
		const i = r.subscribe((o) => {
			n(o);
		});
		return () => i?.unsubscribe();
	}, [r]), {
		participant: e.participant,
		source: e.source ?? Track.Source.Unknown,
		publication: t
	};
}
function Nu(e, t) {
	return mc({
		name: e,
		participant: Ie(t)
	});
}
function gc(e) {
	const t = oe(e?.room), n = tt(t), r = import_react.useMemo(() => n === ConnectionState.Disconnected, [n]), i = import_react.useMemo(() => Ia$1(t, e), [
		t,
		e,
		r
	]), o = j$1(i.isSendingObservable, !1), s = j$1(i.messageObservable, []);
	return {
		send: i.send,
		chatMessages: s,
		isSending: o
	};
}
function Fu(e = {}) {
	const [t, n] = import_react.useState(Qa$1(e.defaults, e.preventLoad ?? !1)), r = import_react.useCallback((c) => {
		n((u) => ({
			...u,
			audioEnabled: c
		}));
	}, []), i = import_react.useCallback((c) => {
		n((u) => ({
			...u,
			videoEnabled: c
		}));
	}, []), o = import_react.useCallback((c) => {
		n((u) => ({
			...u,
			audioDeviceId: c
		}));
	}, []), s = import_react.useCallback((c) => {
		n((u) => ({
			...u,
			videoDeviceId: c
		}));
	}, []), a = import_react.useCallback((c) => {
		n((u) => ({
			...u,
			username: c
		}));
	}, []);
	return import_react.useEffect(() => {
		Ya(t, e.preventSave ?? !1);
	}, [t, e.preventSave]), {
		userChoices: t,
		saveAudioInputEnabled: r,
		saveVideoInputEnabled: i,
		saveAudioInputDeviceId: o,
		saveVideoInputDeviceId: s,
		saveUsername: a
	};
}
function Uu(e, t = {}) {
	const n = Ie(e), r = oe(t.room);
	return j$1(import_react.useMemo(() => ha$1(r, n), [r, n]), n.isLocal ? n.isE2EEEnabled : !!n?.isEncrypted);
}
function $u(e, t = {
	fftSize: 32,
	smoothingTimeConstant: 0
}) {
	const n = ee(e) ? e.publication.track : e, [r, i] = import_react.useState(0);
	return import_react.useEffect(() => {
		if (!n || !n.mediaStream) return;
		const { cleanup: o, analyser: s } = createAudioAnalyser(n, t), a = s.frequencyBinCount, c = new Uint8Array(a), f = setInterval(() => {
			s.getByteFrequencyData(c);
			let d = 0;
			for (let p = 0; p < c.length; p++) {
				const h = c[p];
				d += h * h;
			}
			i(Math.sqrt(d / c.length) / 255);
		}, 1e3 / 30);
		return () => {
			o(), clearInterval(f);
		};
	}, [
		n,
		n?.mediaStream,
		JSON.stringify(t)
	]), r;
}
var bc = (e) => {
	const t = (n) => {
		let r = 1 - Math.max(-100, Math.min(-10, n)) * -1 / 100;
		return r = Math.sqrt(r), r;
	};
	return e.map((n) => n === -Infinity ? 0 : t(n));
}, yc = {
	bands: 5,
	loPass: 100,
	hiPass: 600,
	updateInterval: 32,
	analyserOptions: { fftSize: 2048 }
};
function Wu(e, t = {}) {
	const n = e instanceof Track ? e : e?.publication?.track, r = {
		...yc,
		...t
	}, [i, o] = import_react.useState(new Array(r.bands).fill(0));
	return import_react.useEffect(() => {
		if (!n || !n?.mediaStream) {
			o((p) => p.slice().fill(0));
			return;
		}
		const { analyser: s, cleanup: a } = createAudioAnalyser(n, r.analyserOptions), c = s.frequencyBinCount, u = new Float32Array(c), d = setInterval(() => {
			s.getFloatFrequencyData(u);
			let p = new Float32Array(u.length);
			for (let y = 0; y < u.length; y++) p[y] = u[y];
			p = p.slice(r.loPass, r.hiPass);
			const h = bc(p), m = h.length, S = [];
			for (let y = 0; y < r.bands; y++) {
				const b = Math.floor(y * m / r.bands), w = Math.floor((y + 1) * m / r.bands), P = h.slice(b, w), N = P.length;
				if (N === 0) S.push(0);
				else {
					const M = P.reduce((v, g) => v += g, 0);
					S.push(M / N);
				}
			}
			o(S);
		}, r.updateInterval);
		return () => {
			a(), clearInterval(d);
		};
	}, [
		n,
		n?.mediaStream,
		JSON.stringify(t)
	]), i;
}
var wc = {
	barCount: 120,
	volMultiplier: 5,
	updateInterval: 20
};
function ju(e, t = {}) {
	const n = e instanceof Track ? e : e?.publication?.track, r = {
		...wc,
		...t
	}, i = import_react.useRef(/* @__PURE__ */ new Float32Array()), o = import_react.useRef(performance.now()), s = import_react.useRef(0), [a, c] = import_react.useState([]), u = import_react.useCallback((f) => {
		c(Array.from(Ec(f, r.barCount).map((d) => Math.sqrt(d) * r.volMultiplier)));
	}, []);
	return import_react.useEffect(() => {
		if (!n || !n?.mediaStream) return;
		const { analyser: f, cleanup: d } = createAudioAnalyser(n, { fftSize: Hn(r.barCount) }), p = Hn(r.barCount), h = new Float32Array(p), m = () => {
			if (S = requestAnimationFrame(m), f.getFloatTimeDomainData(h), i.current.map((y, b) => y + h[b]), s.current += 1, performance.now() - o.current >= r.updateInterval) u(h.map((b) => b / s.current)), o.current = performance.now(), s.current = 0;
		};
		let S = requestAnimationFrame(m);
		return () => {
			d(), cancelAnimationFrame(S);
		};
	}, [
		n,
		n?.mediaStream,
		JSON.stringify(t),
		u
	]), { bars: a };
}
function Hn(e) {
	return e < 32 ? 32 : Sc(e);
}
function Sc(e) {
	let t = 2;
	for (; e >>= 1;) t <<= 1;
	return t;
}
function Ec(e, t) {
	const n = Math.floor(e.length / t), r = new Float32Array(t);
	for (let i = 0; i < t; i++) {
		const o = n * i;
		let s = 0;
		for (let a = 0; a < n; a++) s = s + Math.abs(e[o + a]);
		r[i] = s / n;
	}
	return r;
}
function St(e, t = {}) {
	let n, r;
	typeof t == "string" ? n = t : (n = t?.participantIdentity, r = t?.room);
	const i = Ot(), o = dc({
		room: r,
		updateOnlyOn: []
	}), s = import_react.useMemo(() => n ? o.find((a) => a.identity === n) : i, [
		n,
		o,
		i
	]);
	return j$1(import_react.useMemo(() => {
		if (s) return ja(s, { sources: e });
	}, [s, JSON.stringify(e)]), []);
}
function Cc(e) {
	return j$1(import_react.useMemo(() => e?.publication?.track ? Va$1(e?.publication.track) : void 0, [e?.publication?.track]), {
		timestamp: Date.now(),
		rtpTimestamp: e?.publication?.track?.rtpTimestamp
	});
}
var Tc = { bufferSize: 100 };
function xc(e, t) {
	const n = {
		...Tc,
		...t
	}, [r, i] = import_react.useState([]), o = Cc(e), s = (a) => {
		n.onTranscription?.(a), i((c) => Ks(c, a.map((u) => qs(u, o)), n.bufferSize));
	};
	return import_react.useEffect(() => {
		if (!e?.publication) return;
		const a = Ba$1(e.publication).subscribe((c) => {
			s(...c);
		});
		return () => {
			a.unsubscribe();
		};
	}, [e && G(e), s]), { segments: r };
}
function kc(e = {}) {
	const t = Ot(), n = e.participant ?? t;
	return j$1(import_react.useMemo(() => en(n), [n]), { attributes: n?.attributes });
}
function Bu(e, t = {}) {
	const n = Ie(t.participant), [r, i] = import_react.useState(n.attributes[e]);
	return import_react.useEffect(() => {
		if (!n) return;
		const o = en(n).subscribe((s) => {
			s.changed[e] !== void 0 && i(s.attributes[e]);
		});
		return () => {
			o.unsubscribe();
		};
	}, [n, e]), r;
}
var zn = ce$1.AgentState;
function Vu() {
	const e = vn(), t = e.find((f) => f.kind === ParticipantInfo_Kind.AGENT && !(ce$1.PublishOnBehalf in f.attributes)), n = e.find((f) => f.kind === ParticipantInfo_Kind.AGENT && f.attributes[ce$1.PublishOnBehalf] === t?.identity), r = St([Track.Source.Microphone, Track.Source.Camera], t?.identity), i = St([Track.Source.Microphone, Track.Source.Camera], n?.identity), o = r.find((f) => f.source === Track.Source.Microphone) ?? i.find((f) => f.source === Track.Source.Microphone), s = r.find((f) => f.source === Track.Source.Camera) ?? i.find((f) => f.source === Track.Source.Camera), { segments: a } = xc(o), c = tt(), { attributes: u } = kc({ participant: t });
	return {
		agent: t,
		state: import_react.useMemo(() => c === ConnectionState.Disconnected ? "disconnected" : c === ConnectionState.Connecting || !t || !u?.[zn] ? "connecting" : u[zn], [
			u,
			t,
			c
		]),
		audioTrack: o,
		videoTrack: s,
		agentTranscriptions: a,
		agentAttributes: u
	};
}
function Hu(e) {
	const t = oe(e), n = tt(t);
	return j$1(import_react.useMemo(() => va$1(t), [t, n]), t.isRecording);
}
function Pc(e, t) {
	const n = oe(t?.room), r = tt(n) === ConnectionState.Disconnected, i = import_react.useMemo(() => tc(n, e), [n, e]);
	return { textStreams: j$1(r ? void 0 : i, []) };
}
function ui(e) {
	const { participantIdentities: t, trackSids: n } = e ?? {}, { textStreams: r } = Pc(Hr.TRANSCRIPTION, { room: e?.room });
	return import_react.useMemo(() => r.filter((i) => t ? t.includes(i.participantInfo.identity) : !0).filter((i) => n ? n.includes(i.streamInfo.attributes?.[ce$1.TranscribedTrackId] ?? "") : !0), [
		r,
		t,
		n
	]);
}
var Mc = ce$1.Expression, zu = 2, Ac = 150, Lc = 20;
function Oc(e, t, n) {
	return `${e.streamInfo.id ?? e.streamInfo.attributes?.[ce$1.TranscriptionSegmentId] ?? t}:${e.participantInfo.identity}:${n.mood ?? ""}:${n.expression ?? ""}`;
}
function qn(e) {
	const t = e.streamInfo.attributes?.[Mc];
	if (!t) return null;
	try {
		const n = JSON.parse(t), r = n.expression?.trim() || null;
		return !r && !n.mood ? null : {
			mood: n.mood ?? null,
			expression: r
		};
	} catch {
		return null;
	}
}
function qu(e) {
	const { ttlTurns: t = 2, ...n } = e ?? {}, r = ui(n), [i, o] = import_react.useState(null);
	return import_react.useEffect(() => {
		let s = 0;
		const a = setInterval(() => {
			o((c) => {
				let u = null;
				for (let f = r.length - 1; f >= 0; f--) {
					const d = qn(r[f]);
					if (d) {
						u = Oc(r[f], f, d);
						break;
					}
				}
				return u === c ? c : u;
			}), ++s >= Lc && clearInterval(a);
		}, Ac);
		return () => clearInterval(a);
	}, [r]), import_react.useMemo(() => {
		let s = null, a = null;
		const c = [];
		for (let f = r.length - 1; f >= 0; f--) {
			const d = r[f], p = qn(d);
			if (p) {
				s = p, a = d.participantInfo.identity;
				break;
			}
			c.push(d.participantInfo.identity);
		}
		if (!s) return {
			mood: null,
			expression: null
		};
		const u = c.filter((f) => f === a).length;
		return t > 0 && u >= t ? {
			mood: null,
			expression: null
		} : s;
	}, [
		r,
		i,
		t
	]);
}
var Kn = 2, Gn = 400, Jn = 3, Yn = 1e3;
function Ku(e) {
	const t = (0, import_react.useRef)([]), n = (0, import_react.useMemo)(() => new _(), []), r = (0, import_react.useCallback)(async () => n.lock().then(async (f) => {
		for (;;) {
			const d = t.current.pop();
			if (!d) {
				f();
				break;
			}
			switch (d.type) {
				case "connect":
					await d.room.connect(...d.args).then(d.resolve).catch(d.reject);
					break;
				case "disconnect":
					await d.room.disconnect(...d.args).then(d.resolve).catch(d.reject);
					break;
			}
		}
	}), []), i = (0, import_react.useRef)([]), o = (0, import_react.useCallback)((f) => {
		let d = 0;
		i.current = i.current.filter((p) => {
			const h = f.getTime() - p.getTime() < Yn;
			return h && (d += 1), h;
		}), d > Jn && R.warn(`useSequentialRoomConnectDisconnect: room changed reference rapidly (over ${Jn}x in ${Yn}ms). This is not recommended.`);
	}, []);
	(0, import_react.useEffect)(() => {
		t.current = [];
		const f = /* @__PURE__ */ new Date();
		i.current.push(f), o(f);
	}, [e, o]);
	const s = (0, import_react.useRef)([]), a = (0, import_react.useCallback)((f) => {
		let d = 0;
		s.current = s.current.filter((p) => {
			const h = f.getTime() - p.getTime() < Gn;
			return h && (d += 1), h;
		}), d > Kn && R.warn(`useSequentialRoomConnectDisconnect: room connect / disconnect occurring in rapid sequence (over ${Kn}x in ${Gn}ms). This is not recommended and may be the sign of a bug like a useEffect dependency changing every render.`);
	}, []), c = (0, import_react.useCallback)(async (...f) => new Promise((d, p) => {
		if (!e) throw new Error("Called connect(), but room was unset");
		const h = /* @__PURE__ */ new Date();
		a(h), t.current.push({
			type: "connect",
			room: e,
			args: f,
			resolve: d,
			reject: p
		}), s.current.push(h), r();
	}), [
		e,
		a,
		r
	]), u = (0, import_react.useCallback)(async (...f) => new Promise((d, p) => {
		if (!e) throw new Error("Called discconnect(), but room was unset");
		const h = /* @__PURE__ */ new Date();
		a(h), t.current.push({
			type: "disconnect",
			room: e,
			args: f,
			resolve: d,
			reject: p
		}), s.current.push(h), r();
	}), [
		e,
		a,
		r
	]);
	return {
		connect: e ? c : null,
		disconnect: e ? u : null
	};
}
var mn = (/* @__PURE__ */ j$2(((e, t) => {
	var n = typeof Reflect == "object" ? Reflect : null, r = n && typeof n.apply == "function" ? n.apply : function(g, E, x) {
		return Function.prototype.apply.call(g, E, x);
	}, i;
	n && typeof n.ownKeys == "function" ? i = n.ownKeys : Object.getOwnPropertySymbols ? i = function(g) {
		return Object.getOwnPropertyNames(g).concat(Object.getOwnPropertySymbols(g));
	} : i = function(g) {
		return Object.getOwnPropertyNames(g);
	};
	function o(v) {
		console && console.warn && console.warn(v);
	}
	var s = Number.isNaN || function(g) {
		return g !== g;
	};
	function a() {
		a.init.call(this);
	}
	t.exports = a, t.exports.once = P, a.EventEmitter = a, a.prototype._events = void 0, a.prototype._eventsCount = 0, a.prototype._maxListeners = void 0;
	var c = 10;
	function u(v) {
		if (typeof v != "function") throw new TypeError("The \"listener\" argument must be of type Function. Received type " + typeof v);
	}
	Object.defineProperty(a, "defaultMaxListeners", {
		enumerable: !0,
		get: function() {
			return c;
		},
		set: function(v) {
			if (typeof v != "number" || v < 0 || s(v)) throw new RangeError("The value of \"defaultMaxListeners\" is out of range. It must be a non-negative number. Received " + v + ".");
			c = v;
		}
	}), a.init = function() {
		(this._events === void 0 || this._events === Object.getPrototypeOf(this)._events) && (this._events = /* @__PURE__ */ Object.create(null), this._eventsCount = 0), this._maxListeners = this._maxListeners || void 0;
	}, a.prototype.setMaxListeners = function(g) {
		if (typeof g != "number" || g < 0 || s(g)) throw new RangeError("The value of \"n\" is out of range. It must be a non-negative number. Received " + g + ".");
		return this._maxListeners = g, this;
	};
	function f(v) {
		return v._maxListeners === void 0 ? a.defaultMaxListeners : v._maxListeners;
	}
	a.prototype.getMaxListeners = function() {
		return f(this);
	}, a.prototype.emit = function(g) {
		for (var E = [], x = 1; x < arguments.length; x++) E.push(arguments[x]);
		var C = g === "error", I = this._events;
		if (I !== void 0) C = C && I.error === void 0;
		else if (!C) return !1;
		if (C) {
			var T;
			if (E.length > 0 && (T = E[0]), T instanceof Error) throw T;
			var A = /* @__PURE__ */ new Error("Unhandled error." + (T ? " (" + T.message + ")" : ""));
			throw A.context = T, A;
		}
		var D = I[g];
		if (D === void 0) return !1;
		if (typeof D == "function") r(D, this, E);
		else for (var U = D.length, H = y(D, U), x = 0; x < U; ++x) r(H[x], this, E);
		return !0;
	};
	function d(v, g, E, x) {
		var C, I, T;
		if (u(E), I = v._events, I === void 0 ? (I = v._events = /* @__PURE__ */ Object.create(null), v._eventsCount = 0) : (I.newListener !== void 0 && (v.emit("newListener", g, E.listener ? E.listener : E), I = v._events), T = I[g]), T === void 0) T = I[g] = E, ++v._eventsCount;
		else if (typeof T == "function" ? T = I[g] = x ? [E, T] : [T, E] : x ? T.unshift(E) : T.push(E), C = f(v), C > 0 && T.length > C && !T.warned) {
			T.warned = !0;
			var A = /* @__PURE__ */ new Error("Possible EventEmitter memory leak detected. " + T.length + " " + String(g) + " listeners added. Use emitter.setMaxListeners() to increase limit");
			A.name = "MaxListenersExceededWarning", A.emitter = v, A.type = g, A.count = T.length, o(A);
		}
		return v;
	}
	a.prototype.addListener = function(g, E) {
		return d(this, g, E, !1);
	}, a.prototype.on = a.prototype.addListener, a.prototype.prependListener = function(g, E) {
		return d(this, g, E, !0);
	};
	function p() {
		if (!this.fired) return this.target.removeListener(this.type, this.wrapFn), this.fired = !0, arguments.length === 0 ? this.listener.call(this.target) : this.listener.apply(this.target, arguments);
	}
	function h(v, g, E) {
		var x = {
			fired: !1,
			wrapFn: void 0,
			target: v,
			type: g,
			listener: E
		}, C = p.bind(x);
		return C.listener = E, x.wrapFn = C, C;
	}
	a.prototype.once = function(g, E) {
		return u(E), this.on(g, h(this, g, E)), this;
	}, a.prototype.prependOnceListener = function(g, E) {
		return u(E), this.prependListener(g, h(this, g, E)), this;
	}, a.prototype.removeListener = function(g, E) {
		var x, C, I, T, A;
		if (u(E), C = this._events, C === void 0) return this;
		if (x = C[g], x === void 0) return this;
		if (x === E || x.listener === E) --this._eventsCount === 0 ? this._events = /* @__PURE__ */ Object.create(null) : (delete C[g], C.removeListener && this.emit("removeListener", g, x.listener || E));
		else if (typeof x != "function") {
			for (I = -1, T = x.length - 1; T >= 0; T--) if (x[T] === E || x[T].listener === E) {
				A = x[T].listener, I = T;
				break;
			}
			if (I < 0) return this;
			I === 0 ? x.shift() : b(x, I), x.length === 1 && (C[g] = x[0]), C.removeListener !== void 0 && this.emit("removeListener", g, A || E);
		}
		return this;
	}, a.prototype.off = a.prototype.removeListener, a.prototype.removeAllListeners = function(g) {
		var E, x = this._events, C;
		if (x === void 0) return this;
		if (x.removeListener === void 0) return arguments.length === 0 ? (this._events = /* @__PURE__ */ Object.create(null), this._eventsCount = 0) : x[g] !== void 0 && (--this._eventsCount === 0 ? this._events = /* @__PURE__ */ Object.create(null) : delete x[g]), this;
		if (arguments.length === 0) {
			var I = Object.keys(x), T;
			for (C = 0; C < I.length; ++C) T = I[C], T !== "removeListener" && this.removeAllListeners(T);
			return this.removeAllListeners("removeListener"), this._events = /* @__PURE__ */ Object.create(null), this._eventsCount = 0, this;
		}
		if (E = x[g], typeof E == "function") this.removeListener(g, E);
		else if (E !== void 0) for (C = E.length - 1; C >= 0; C--) this.removeListener(g, E[C]);
		return this;
	};
	function m(v, g, E) {
		var x = v._events;
		if (x === void 0) return [];
		var C = x[g];
		return C === void 0 ? [] : typeof C == "function" ? E ? [C.listener || C] : [C] : E ? w(C) : y(C, C.length);
	}
	a.prototype.listeners = function(g) {
		return m(this, g, !0);
	}, a.prototype.rawListeners = function(g) {
		return m(this, g, !1);
	}, a.listenerCount = function(v, g) {
		return typeof v.listenerCount == "function" ? v.listenerCount(g) : S.call(v, g);
	}, a.prototype.listenerCount = S;
	function S(v) {
		var g = this._events;
		if (g !== void 0) {
			var E = g[v];
			if (typeof E == "function") return 1;
			if (E !== void 0) return E.length;
		}
		return 0;
	}
	a.prototype.eventNames = function() {
		return this._eventsCount > 0 ? i(this._events) : [];
	};
	function y(v, g) {
		for (var E = new Array(g), x = 0; x < g; ++x) E[x] = v[x];
		return E;
	}
	function b(v, g) {
		for (; g + 1 < v.length; g++) v[g] = v[g + 1];
		v.pop();
	}
	function w(v) {
		for (var g = new Array(v.length), E = 0; E < g.length; ++E) g[E] = v[E].listener || v[E];
		return g;
	}
	function P(v, g) {
		return new Promise(function(E, x) {
			function C(T) {
				v.removeListener(g, I), x(T);
			}
			function I() {
				typeof v.removeListener == "function" && v.removeListener("error", C), E([].slice.call(arguments));
			}
			M(v, g, I, { once: !0 }), g !== "error" && N(v, C, { once: !0 });
		});
	}
	function N(v, g, E) {
		typeof v.on == "function" && M(v, "error", g, E);
	}
	function M(v, g, E, x) {
		if (typeof v.on == "function") x.once ? v.once(g, E) : v.on(g, E);
		else if (typeof v.addEventListener == "function") v.addEventListener(g, function C(I) {
			x.once && v.removeEventListener(g, C), E(I);
		});
		else throw new TypeError("The \"emitter\" argument must be of type EventEmitter. Received type " + typeof v);
	}
})))(), Ic = 2e4, Gu = /* @__PURE__ */ (function(e) {
	return e.CameraChanged = "cameraChanged", e.MicrophoneChanged = "microphoneChanged", e.StateChanged = "stateChanged", e;
})({}), re = (e) => ({
	isConnected: e === "listening" || e === "thinking" || e === "speaking",
	canListen: e === "pre-connect-buffering" || e === "listening" || e === "thinking" || e === "speaking",
	isFinished: e === "disconnected" || e === "failed",
	isPending: e === "connecting" || e === "initializing" || e === "idle"
}), Rc = () => {
	const [e, t] = import_react.useState(null), [n, r] = import_react.useState(null), i = import_react.useRef("connecting"), o = import_react.useRef(!1), s = (a) => setTimeout(() => {
		if (!o.current) {
			t("Agent did not join the room.");
			return;
		}
		const { isConnected: c } = re(i.current);
		if (!c) {
			t("Agent joined the room but did not complete initializing.");
			return;
		}
	}, a ?? Ic);
	return {
		agentTimeoutFailureReason: e,
		startAgentTimeout: import_react.useCallback((a) => {
			n && clearTimeout(n), t(null), r(s(a)), i.current = "connecting", o.current = !1;
		}, [n]),
		clearAgentTimeout: import_react.useCallback(() => {
			n && clearTimeout(n), t(null), r(null), i.current = "connecting", o.current = !1;
		}, [n]),
		clearAgentTimeoutFailureReason: import_react.useCallback(() => {
			t(null);
		}, []),
		updateAgentTimeoutState: import_react.useCallback((a) => {
			i.current = a;
		}, []),
		updateAgentTimeoutParticipantExists: import_react.useCallback((a) => {
			o.current = a;
		}, [])
	};
};
function Dc(e, t) {
	const n = import_react.useRef(t);
	return import_react.useEffect(() => {
		n.current = t;
	}, [t]), {
		waitUntilConnected: import_react.useCallback(async (r) => {
			const { isConnected: i } = re(n.current);
			if (!i) return new Promise((o, s) => {
				const a = (f) => {
					const { isConnected: d } = re(f);
					d && (u(), o());
				}, c = () => {
					u(), s(/* @__PURE__ */ new Error("useAgent(/* ... */).waitUntilConnected - signal aborted"));
				}, u = () => {
					e.off("stateChanged", a), r?.removeEventListener("abort", c);
				};
				e.on("stateChanged", a), r?.addEventListener("abort", c);
			});
		}, [e]),
		waitUntilCouldBeListening: import_react.useCallback(async (r) => {
			const { canListen: i } = re(n.current);
			if (!i) return new Promise((o, s) => {
				const a = (f) => {
					const { canListen: d } = re(f);
					d && (u(), o());
				}, c = () => {
					u(), s(/* @__PURE__ */ new Error("useAgent(/* ... */).waitUntilCouldBeListening - signal aborted"));
				}, u = () => {
					e.off("stateChanged", a), r?.removeEventListener("abort", c);
				};
				e.on("stateChanged", a), r?.addEventListener("abort", c);
			});
		}, [e]),
		waitUntilFinished: import_react.useCallback(async (r) => {
			const { isFinished: i } = re(n.current);
			if (!i) return new Promise((o, s) => {
				const a = (f) => {
					const { isFinished: d } = re(f);
					d && (u(), o());
				}, c = () => {
					u(), s(/* @__PURE__ */ new Error("useAgent(/* ... */).waitUntilFinished - signal aborted"));
				}, u = () => {
					e.off("stateChanged", a), r?.removeEventListener("abort", c);
				};
				e.on("stateChanged", a), r?.addEventListener("abort", c);
			});
		}, [e])
	};
}
function li(e) {
	const t = ri();
	if (e = e ?? t, !e) throw new Error("No session provided, make sure you are inside a Session context or pass the session explicitly");
	const { room: n, internal: { agentConnectTimeoutMilliseconds: r, agentTimeoutFailureReason: i, startAgentTimeout: o, clearAgentTimeout: s, clearAgentTimeoutFailureReason: a, updateAgentTimeoutState: c, updateAgentTimeoutParticipantExists: u } } = e, f = import_react.useMemo(() => new mn.EventEmitter(), []), d = vn({ room: n }), p = import_react.useMemo(() => d.find((L) => L.kind === ParticipantInfo_Kind.AGENT && !(ce$1.PublishOnBehalf in L.attributes)) ?? null, [d]), h = import_react.useMemo(() => p ? d.find((L) => L.kind === ParticipantInfo_Kind.AGENT && L.attributes[ce$1.PublishOnBehalf] === p.identity) ?? null : null, [p, d]), m = p?.attributes ?? {}, S = St([Track.Source.Camera, Track.Source.Microphone], {
		room: n,
		participantIdentity: p?.identity
	}), y = St([Track.Source.Camera, Track.Source.Microphone], {
		room: n,
		participantIdentity: h?.identity
	}), b = import_react.useMemo(() => S.find((L) => L.source === Track.Source.Camera) ?? y.find((L) => L.source === Track.Source.Camera), [S, y]);
	import_react.useEffect(() => {
		f.emit("cameraChanged", b);
	}, [f, b]);
	const w = import_react.useMemo(() => S.find((L) => L.source === Track.Source.Microphone) ?? y.find((L) => L.source === Track.Source.Microphone), [S, y]);
	import_react.useEffect(() => {
		f.emit("microphoneChanged", w);
	}, [f, w]);
	const [P, N] = import_react.useState(n.state);
	import_react.useEffect(() => {
		const L = (X) => {
			N(X);
		};
		return n.on(RoomEvent.ConnectionStateChanged, L), () => {
			n.off(RoomEvent.ConnectionStateChanged, L);
		};
	}, [n]), import_react.useEffect(() => {
		p && a();
	}, [p]);
	const [M, v] = import_react.useState(null);
	import_react.useEffect(() => {
		if (!p) return;
		const L = (X) => {
			X.identity === p?.identity && v("Agent left the room unexpectedly.");
		};
		return n.on(RoomEvent.ParticipantDisconnected, L), () => {
			n.off(RoomEvent.ParticipantDisconnected, L);
		};
	}, [p, n]), import_react.useEffect(() => {
		P === ConnectionState.Disconnected && v(null);
	}, [P]);
	const [g, E] = import_react.useState(() => n.localParticipant.getTrackPublication(Track.Source.Microphone) ?? null);
	import_react.useEffect(() => {
		const L = () => {
			E(n.localParticipant.getTrackPublication(Track.Source.Microphone) ?? null);
		}, X = () => {
			E(null);
		};
		return n.localParticipant.on(ParticipantEvent.LocalTrackPublished, L), n.localParticipant.on(ParticipantEvent.LocalTrackUnpublished, X), () => {
			n.localParticipant.off(ParticipantEvent.LocalTrackPublished, L), n.localParticipant.off(ParticipantEvent.LocalTrackUnpublished, X);
		};
	}, [n.localParticipant]);
	const x = import_react.useMemo(() => {
		const L = [];
		return i && L.push(i), M && L.push(M), L;
	}, [i, M]), C = import_react.useMemo(() => {
		if (x.length > 0) return "failed";
		let L = "disconnected";
		return P !== ConnectionState.Disconnected && (L = "connecting"), g && (L = "pre-connect-buffering"), p && m[ce$1.AgentState] && (L = m[ce$1.AgentState]), L;
	}, [
		x,
		P,
		g,
		p,
		m
	]);
	import_react.useEffect(() => {
		f.emit("stateChanged", C), c(C);
	}, [f, C]), import_react.useEffect(() => {
		u(p !== null);
	}, [p]);
	const I = e.connectionState === "disconnected";
	import_react.useEffect(() => {
		if (!I) return o(r), () => {
			s();
		};
	}, [I, r]);
	const { identity: T, name: A, metadata: D } = fc({ participant: p ?? void 0 }), U = import_react.useMemo(() => {
		const L = {
			attributes: m,
			internal: {
				agentParticipant: p,
				workerParticipant: h,
				emitter: f
			}
		};
		switch (C) {
			case "disconnected": return {
				...L,
				identity: void 0,
				name: void 0,
				metadata: void 0,
				state: C,
				...re(C),
				failureReasons: null,
				cameraTrack: void 0,
				microphoneTrack: void 0
			};
			case "connecting": return {
				...L,
				identity: void 0,
				name: void 0,
				metadata: void 0,
				state: C,
				...re(C),
				failureReasons: null,
				cameraTrack: void 0,
				microphoneTrack: void 0
			};
			case "initializing":
			case "idle": return {
				...L,
				identity: T,
				name: A,
				metadata: D,
				state: C,
				...re(C),
				failureReasons: null,
				cameraTrack: b,
				microphoneTrack: w
			};
			case "pre-connect-buffering": return {
				...L,
				identity: T,
				name: A,
				metadata: D,
				state: C,
				...re(C),
				failureReasons: null,
				cameraTrack: b,
				microphoneTrack: w
			};
			case "listening":
			case "thinking":
			case "speaking": return {
				...L,
				identity: T,
				name: A,
				metadata: D,
				state: C,
				...re(C),
				failureReasons: null,
				cameraTrack: b,
				microphoneTrack: w
			};
			case "failed": return {
				...L,
				identity: void 0,
				name: void 0,
				metadata: void 0,
				state: "failed",
				...re("failed"),
				failureReasons: x,
				cameraTrack: void 0,
				microphoneTrack: void 0
			};
		}
	}, [
		T,
		A,
		D,
		m,
		f,
		p,
		C,
		b,
		w
	]), { waitUntilConnected: H, waitUntilCouldBeListening: z, waitUntilFinished: V } = Dc(f, C), Q = import_react.useCallback((L) => new Promise((X, Re) => {
		const Se = (K) => {
			K && (F(), X(K));
		}, he = () => {
			F(), Re(/* @__PURE__ */ new Error("useAgent(/* ... */).waitUntilCamera - signal aborted"));
		}, F = () => {
			f.off("cameraChanged", Se), L?.removeEventListener("abort", he);
		};
		f.on("cameraChanged", Se), L?.addEventListener("abort", he);
	}), [f]), xe = import_react.useCallback((L) => new Promise((X, Re) => {
		const Se = (K) => {
			K && (F(), X(K));
		}, he = () => {
			F(), Re(/* @__PURE__ */ new Error("useAgent(/* ... */).waitUntilMicrophone - signal aborted"));
		}, F = () => {
			f.off("microphoneChanged", Se), L?.removeEventListener("abort", he);
		};
		f.on("microphoneChanged", Se), L?.addEventListener("abort", he);
	}), [f]);
	return import_react.useMemo(() => ({
		...U,
		waitUntilConnected: H,
		waitUntilCouldBeListening: z,
		waitUntilFinished: V,
		waitUntilCamera: Q,
		waitUntilMicrophone: xe
	}), [
		U,
		H,
		z,
		V,
		Q,
		xe
	]);
}
var Ju = /* @__PURE__ */ (function(e) {
	return e.ConnectionStateChanged = "connectionStateChanged", e.MediaDevicesError = "mediaDevicesError", e.EncryptionError = "encryptionError", e;
})({});
function Nc(e) {
	return typeof e == "object" && e !== null && "room" in e && "connectionState" in e && "internal" in e;
}
function Fc(e, t) {
	const n = /* @__PURE__ */ new Set([...Object.keys(e), ...Object.keys(t)]);
	for (const r of n) switch (r) {
		case "roomName":
		case "participantName":
		case "participantIdentity":
		case "participantMetadata":
		case "participantAttributes":
		case "agentName":
		case "agentMetadata":
		case "deployment":
			if (e[r] !== t[r]) return !1;
			break;
		default: throw new Error(`Options key ${r} not being checked for equality!`);
	}
	return !0;
}
function Uc(e, t) {
	const n = import_react.useRef(t);
	return import_react.useEffect(() => {
		n.current = t;
	}, [t]), import_react.useCallback(async (r, i) => {
		if (n.current !== r) return new Promise((o, s) => {
			const a = (f) => {
				f === r && (u(), o());
			}, c = () => {
				u(), s(/* @__PURE__ */ new Error(`useSession(/* ... */).waitUntilConnectionState(${r}, /* signal */) - signal aborted`));
			}, u = () => {
				e.off("connectionStateChanged", a), i?.removeEventListener("abort", c);
			};
			e.on("connectionStateChanged", a), i?.addEventListener("abort", c);
		});
	}, [e]);
}
function $c(e, t) {
	const n = e instanceof TokenSourceConfigurable, r = import_react.useRef(n ? t : null);
	return import_react.useEffect(() => {
		if (!n) {
			r.current = null;
			return;
		}
		r.current !== null && Fc(r.current, t) || (r.current = t);
	}, [n, t]), import_react.useCallback(async (i) => {
		if (n) {
			if (!r.current) throw new Error("AgentSession - memoized token fetch options are not set, but the passed tokenSource was an instance of TokenSourceConfigurable. If you are seeing this please make a new GitHub issue!");
			return e.fetch(r.current, i);
		} else return e.fetch();
	}, [n, e]);
}
function Yu(e, t = {}) {
	const { room: n, agentConnectTimeoutMilliseconds: r, encryption: i, ...o } = t, s = i && "e2eeManager" in i ? i.e2eeManager : null, a = i && !("e2eeManager" in i) ? i.key ?? null : null, c = i && !("e2eeManager" in i) ? i.worker ?? null : null, u = It(), f = import_react.useRef(null), d = import_react.useMemo(() => typeof a == "string" || a instanceof ArrayBuffer ? (f.current === null && (f.current = new ExternalE2EEKeyProvider()), f.current.setKey(a).catch((F) => R.error(F)), f.current) : a, [a]), p = import_react.useMemo(() => {
		const F = u ?? n;
		if (F) return F;
		const K = !!(d && c), ve = !!s, Rt = K || ve, De = {};
		K ? De.encryption = {
			keyProvider: d,
			worker: c
		} : ve ? De.encryption = { e2eeManager: s } : i !== void 0 && R.warn("useSession options encryption was set, but neither encryption.key with encryption.worker nor encryption.e2eeManager was provided.");
		const nt = new Room(De);
		return Rt && nt.setE2EEEnabled(!0), nt;
	}, [
		u,
		n,
		d,
		c,
		s
	]);
	import_react.useEffect(() => () => {
		p.disconnect();
	}, [p]);
	const h = import_react.useMemo(() => new mn.EventEmitter(), []), m = import_react.useCallback((F) => ({ isConnected: F === ConnectionState.Connected || F === ConnectionState.Reconnecting || F === ConnectionState.SignalReconnecting }), []), [S, y] = import_react.useState(p.state);
	import_react.useEffect(() => {
		const F = (K) => {
			y(K);
		};
		return p.on(RoomEvent.ConnectionStateChanged, F), () => {
			p.off(RoomEvent.ConnectionStateChanged, F);
		};
	}, [p]), import_react.useEffect(() => {
		const F = async (K) => {
			h.emit("mediaDevicesError", K);
		};
		return p.on(RoomEvent.MediaDevicesError, F), () => {
			p.off(RoomEvent.MediaDevicesError, F);
		};
	}, [p, h]), import_react.useEffect(() => {
		const F = async (K) => {
			h.emit("encryptionError", K);
		};
		return p.on(RoomEvent.EncryptionError, F), () => {
			p.off(RoomEvent.EncryptionError, F);
		};
	}, [p, h]);
	const { localParticipant: b } = ci({ room: p }), w = b.getTrackPublication(Track.Source.Camera), P = import_react.useMemo(() => {
		if (w) return {
			source: Track.Source.Camera,
			participant: b,
			publication: w
		};
	}, [b, w]), N = b.getTrackPublication(Track.Source.Microphone), M = import_react.useMemo(() => {
		if (N) return {
			source: Track.Source.Microphone,
			participant: b,
			publication: N
		};
	}, [b, N]), v = b.getTrackPublication(Track.Source.ScreenShare), g = import_react.useMemo(() => {
		if (v) return {
			source: Track.Source.ScreenShare,
			participant: b,
			publication: v
		};
	}, [b, v]), { agentTimeoutFailureReason: E, startAgentTimeout: x, clearAgentTimeout: C, clearAgentTimeoutFailureReason: I, updateAgentTimeoutState: T, updateAgentTimeoutParticipantExists: A } = Rc(), D = import_react.useMemo(() => ({
		emitter: h,
		tokenSource: e,
		agentConnectTimeoutMilliseconds: r,
		agentTimeoutFailureReason: E,
		startAgentTimeout: x,
		clearAgentTimeout: C,
		clearAgentTimeoutFailureReason: I,
		updateAgentTimeoutState: T,
		updateAgentTimeoutParticipantExists: A
	}), [
		h,
		r,
		e,
		E,
		x,
		C,
		I,
		T,
		A
	]), U = import_react.useMemo(() => {
		const F = {
			room: p,
			internal: D
		};
		switch (S) {
			case ConnectionState.Connecting: return {
				...F,
				connectionState: ConnectionState.Connecting,
				...m(ConnectionState.Connecting),
				local: {
					cameraTrack: void 0,
					microphoneTrack: void 0,
					screenShareTrack: void 0
				}
			};
			case ConnectionState.Connected:
			case ConnectionState.Reconnecting:
			case ConnectionState.SignalReconnecting: return {
				...F,
				connectionState: S,
				...m(S),
				local: {
					cameraTrack: P,
					microphoneTrack: M,
					screenShareTrack: g
				}
			};
			case ConnectionState.Disconnected: return {
				...F,
				connectionState: ConnectionState.Disconnected,
				...m(ConnectionState.Disconnected),
				local: {
					cameraTrack: void 0,
					microphoneTrack: void 0,
					screenShareTrack: void 0
				}
			};
		}
	}, [
		D,
		p,
		S,
		P,
		M,
		m
	]);
	import_react.useEffect(() => {
		h.emit("connectionStateChanged", U.connectionState);
	}, [h, U.connectionState]);
	const H = Uc(h, U.connectionState), z = import_react.useCallback(async (F) => H(ConnectionState.Connected, F), [H]), V = import_react.useCallback(async (F) => H(ConnectionState.Disconnected, F), [H]), Q = import_react.useCallback(async (F) => p.setE2EEEnabled(F), [p]), xe = li(import_react.useMemo(() => ({
		connectionState: U.connectionState,
		room: p,
		internal: D
	}), [
		U,
		p,
		D
	])), L = $c(e, o), X = import_react.useRef(!1), Re = import_react.useCallback(async (F = {}) => {
		const { signal: K, tracks: ve = { microphone: {
			enabled: !0,
			publishOptions: { preConnectBuffer: !0 }
		} }, roomConnectOptions: Rt } = F;
		await V(K), X.current = !1;
		const De = () => {
			p.disconnect();
		};
		K?.addEventListener("abort", De);
		const nt = () => {
			X.current || L(!0);
		};
		p.once(RoomEvent.Disconnected, nt);
		let gn = !1;
		await Promise.all([
			L().then(({ serverUrl: fi, participantToken: bn }) => (gn = (decodeTokenPayload(bn).roomConfig?.agents?.length ?? 0) > 0, p.connect(fi, bn, Rt))),
			ve.microphone?.enabled ? p.localParticipant.setMicrophoneEnabled(!0, void 0, ve.microphone?.publishOptions ?? {}) : Promise.resolve(),
			ve.camera?.enabled ? p.localParticipant.setCameraEnabled(!0, void 0, ve.camera?.publishOptions ?? {}) : Promise.resolve(),
			ve.screenShare?.enabled ? p.localParticipant.setScreenShareEnabled(!0, void 0, ve.screenShare?.publishOptions ?? {}) : Promise.resolve()
		]), await z(K), gn && await xe.waitUntilConnected(K), K?.removeEventListener("abort", De);
	}, [
		p,
		V,
		L,
		z,
		xe.waitUntilConnected
	]), Se = import_react.useCallback(async () => {
		X.current = !0, L(!0), await p.disconnect();
	}, [p, L]), he = import_react.useCallback(async () => {
		const F = await L();
		await p.prepareConnection(F.serverUrl, F.participantToken);
	}, [L, p]);
	return import_react.useEffect(() => {
		he().catch((F) => {
			console.warn("WARNING: Room.prepareConnection failed:", F);
		});
	}, []), import_react.useMemo(() => ({
		...U,
		waitUntilConnected: z,
		waitUntilDisconnected: V,
		prepareConnection: he,
		start: Re,
		end: Se,
		setEncryptionEnabled: Q
	}), [
		U,
		z,
		V,
		he,
		Re,
		Se,
		Q
	]);
}
function Qu(e, t, n, r) {
	const i = import_react.useMemo(() => () => {}, []), o = import_react.useCallback(n ?? i, r ?? []), s = r ? o : n, a = import_react.useMemo(() => e ? "internal" in e ? e.internal.emitter : e : null, [e]);
	import_react.useEffect(() => {
		if (!(!a || !s)) return a.on(t, s), () => {
			a.off(t, s);
		};
	}, [
		a,
		t,
		s
	]);
}
var Xu = /* @__PURE__ */ (function(e) {
	return e.MessageReceived = "messageReceived", e;
})({});
function Zu(e) {
	const { room: t } = ii(e), n = import_react.useMemo(() => new mn.EventEmitter(), []), r = li(e), i = ui({ room: t }), o = gc(import_react.useMemo(() => ({ room: t }), [t])), s = import_react.useMemo(() => i.map((d) => {
		switch (d.participantInfo.identity) {
			case t.localParticipant.identity: return {
				type: "userTranscript",
				message: d.text,
				id: d.streamInfo.id,
				timestamp: d.streamInfo.timestamp,
				attributes: d.streamInfo.attributes,
				from: t.localParticipant
			};
			case r.internal.agentParticipant?.identity:
			case r.internal.workerParticipant?.identity: return {
				type: "agentTranscript",
				message: d.text,
				id: d.streamInfo.id,
				timestamp: d.streamInfo.timestamp,
				attributes: d.streamInfo.attributes,
				from: r.internal.agentParticipant?.identity === d.participantInfo.identity ? r.internal.agentParticipant : r.internal.workerParticipant
			};
			default: return {
				type: "agentTranscript",
				message: d.text,
				id: d.streamInfo.id,
				timestamp: d.streamInfo.timestamp,
				attributes: d.streamInfo.attributes,
				from: Array.from(t.remoteParticipants.values()).find((p) => p.identity === d.participantInfo.identity)
			};
		}
	}), [i, t]), a = import_react.useMemo(() => [...s, ...o.chatMessages], [s, o.chatMessages]), c = import_react.useRef(/* @__PURE__ */ new Map()), u = import_react.useMemo(() => {
		const d = /* @__PURE__ */ new Date();
		for (const p of a) c.current.has(p.id) || c.current.set(p.id, d);
		return a.sort((p, h) => {
			const m = c.current.get(p.id), S = c.current.get(h.id);
			return typeof m > "u" || typeof S > "u" ? 0 : m.getTime() - S.getTime();
		});
	}, [a]), f = import_react.useRef(/* @__PURE__ */ new Set());
	return import_react.useEffect(() => {
		for (const d of u) f.current.has(d.id) || (f.current.add(d.id), n.emit("messageReceived", d));
	}, [u]), import_react.useMemo(() => ({
		messages: u,
		send: o.send,
		isSending: o.isSending,
		internal: { emitter: n }
	}), [
		u,
		o.send,
		o.isSending
	]);
}
function el(e, t, n, r) {
	let i, o, s, a;
	Nc(e) ? (i = e, o = t, s = n, a = r) : (o = e, s = t, a = n);
	const { room: c } = ii(i), u = import_react.useRef(s);
	u.current = s;
	const f = import_react.useRef(a);
	f.current = a, import_react.useEffect(() => {
		if (o) return c.registerRpcMethod(o, async (p) => {
			const h = f.current?.fromIdentity;
			if (h && p.callerIdentity !== h) throw RpcError.builtIn("UNSUPPORTED_METHOD", `Method not available for caller ${p.callerIdentity}`);
			const m = u.current;
			if (!m) throw RpcError.builtIn("APPLICATION_ERROR", `No handler registered for method "${o}"`);
			const S = f.current?.serializer ?? serializers.json();
			let y;
			try {
				y = S.parse(p.payload);
			} catch (w) {
				throw RpcError.builtIn("APPLICATION_ERROR", `Failed to parse RPC payload: ${w}`);
			}
			const b = await m(y, p);
			try {
				return S.serialize(b);
			} catch (w) {
				throw RpcError.builtIn("APPLICATION_ERROR", `Failed to serialize RPC response: ${w}`);
			}
		}), () => {
			c.unregisterRpcMethod(o);
		};
	}, [c, o]);
	const d = import_react.useCallback(async (p, h = serializers.json()) => {
		if (isSerializer(h)) {
			let m;
			try {
				m = h.serialize(p.payload);
			} catch (y) {
				throw RpcError.builtIn("APPLICATION_ERROR", `Failed to serialize RPC payload: ${y}`);
			}
			const S = await c.localParticipant.performRpc({
				destinationIdentity: p.destinationIdentity,
				method: p.method,
				payload: m,
				responseTimeout: p.responseTimeout
			});
			try {
				return h.parse(S);
			} catch (y) {
				throw RpcError.builtIn("APPLICATION_ERROR", `Failed to parse RPC response: ${y}`);
			}
		} else return c.localParticipant.performRpc(p);
	}, [c]);
	return import_react.useMemo(() => ({ perform: d }), [d]);
}
var tl = /* @__PURE__ */ import_react.forwardRef(function(t, n) {
	const { room: r, htmlProps: i } = uc(t);
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: n,
		...i
	}, r && /* @__PURE__ */ import_react.createElement(hn.Provider, { value: r }, /* @__PURE__ */ import_react.createElement(oi.Provider, { value: t.featureFlags }, t.children)));
});
//#endregion
//#region node_modules/@livekit/components-react/dist/prefabs-BEB1UEnC.mjs
function Qt(t) {
	const a = (i) => typeof window < "u" ? window.matchMedia(i).matches : !1, [n, c] = import_react.useState(a(t));
	function r() {
		c(a(t));
	}
	return import_react.useEffect(() => {
		const i = window.matchMedia(t);
		return r(), i.addListener ? i.addListener(r) : i.addEventListener("change", r), () => {
			i.removeListener ? i.removeListener(r) : i.removeEventListener("change", r);
		};
	}, [t]), n;
}
var Te = /* @__PURE__ */ import_react.forwardRef(function(a, n) {
	const { mergedProps: c } = Ou({ props: a });
	return /* @__PURE__ */ import_react.createElement("button", {
		ref: n,
		...c
	}, a.children);
}), Pe = /* @__PURE__ */ import_react.forwardRef(function(a, n) {
	const { buttonProps: c } = du(a);
	return /* @__PURE__ */ import_react.createElement("button", {
		ref: n,
		...c
	}, a.children);
}), Gt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentColor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M1.354.646a.5.5 0 1 0-.708.708l14 14a.5.5 0 0 0 .708-.708L11 10.293V4.5A1.5 1.5 0 0 0 9.5 3H3.707zM0 4.5a1.5 1.5 0 0 1 .943-1.393l9.532 9.533c-.262.224-.603.36-.975.36h-8A1.5 1.5 0 0 1 0 11.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "m15.2 3.6-2.8 2.1a1 1 0 0 0-.4.8v3a1 1 0 0 0 .4.8l2.8 2.1a.5.5 0 0 0 .8-.4V4a.5.5 0 0 0-.8-.4" })), Kt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentColor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M0 4.5A1.5 1.5 0 0 1 1.5 3h8A1.5 1.5 0 0 1 11 4.5v7A1.5 1.5 0 0 1 9.5 13h-8A1.5 1.5 0 0 1 0 11.5zM15.2 3.6l-2.8 2.1a1 1 0 0 0-.4.8v3a1 1 0 0 0 .4.8l2.8 2.1a.5.5 0 0 0 .8-.4V4a.5.5 0 0 0-.8-.4" })), Jt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	viewBox: "0 0 24 24",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "#FFF",
	d: "M4.99 3.99a1 1 0 0 0-.697 1.717L10.586 12l-6.293 6.293a1 1 0 1 0 1.414 1.414L12 13.414l6.293 6.293a1 1 0 1 0 1.414-1.414L13.414 12l6.293-6.293a1 1 0 0 0-.727-1.717 1 1 0 0 0-.687.303L12 10.586 5.707 4.293a1 1 0 0 0-.717-.303"
})), Zt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 18,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M0 2.75A2.75 2.75 0 0 1 2.75 0h10.5A2.75 2.75 0 0 1 16 2.75v13.594a.75.75 0 0 1-1.234.572l-3.691-3.12a1.25 1.25 0 0 0-.807-.296H2.75A2.75 2.75 0 0 1 0 10.75zM2.75 1.5c-.69 0-1.25.56-1.25 1.25v8c0 .69.56 1.25 1.25 1.25h7.518c.65 0 1.279.23 1.775.65l2.457 2.077V2.75c0-.69-.56-1.25-1.25-1.25z",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M3 4.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5m0 2a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5m0 2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5",
	clipRule: "evenodd"
})), Ee = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentcolor",
	fillRule: "evenodd",
	d: "M5.293 2.293a1 1 0 0 1 1.414 0l4.823 4.823a1.25 1.25 0 0 1 0 1.768l-4.823 4.823a1 1 0 0 1-1.414-1.414L9.586 8 5.293 3.707a1 1 0 0 1 0-1.414",
	clipRule: "evenodd"
})), Xt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("g", {
	stroke: "currentColor",
	strokeLinecap: "round",
	strokeLinejoin: "round",
	strokeWidth: 1.5
}, /* @__PURE__ */ import_react.createElement("path", { d: "M10 1.75h4.25m0 0V6m0-4.25L9 7M6 14.25H1.75m0 0V10m0 4.25L7 9" }))), Yt = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentcolor",
	fillRule: "evenodd",
	d: "M8.961.894C8.875-.298 7.125-.298 7.04.894c-.066.912-1.246 1.228-1.76.472-.67-.99-2.186-.115-1.664.96.399.824-.465 1.688-1.288 1.289-1.076-.522-1.95.994-.961 1.665.756.513.44 1.693-.472 1.759-1.192.086-1.192 1.836 0 1.922.912.066 1.228 1.246.472 1.76-.99.67-.115 2.186.96 1.664.824-.399 1.688.465 1.289 1.288-.522 1.076.994 1.95 1.665.961.513-.756 1.693-.44 1.759.472.086 1.192 1.836 1.192 1.922 0 .066-.912 1.246-1.228 1.76-.472.67.99 2.186.115 1.664-.96-.399-.824.465-1.687 1.288-1.289 1.076.522 1.95-.994.961-1.665-.756-.513-.44-1.693.472-1.759 1.192-.086 1.192-1.836 0-1.922-.912-.066-1.228-1.246-.472-1.76.99-.67.115-2.186-.96-1.664-.824.399-1.687-.465-1.289-1.288.522-1.076-.994-1.95-1.665-.961-.513.756-1.693.44-1.759-.472M8 13A5 5 0 1 0 8 3a5 5 0 0 0 0 10",
	clipRule: "evenodd"
})), ea = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M2 2.75A2.75 2.75 0 0 1 4.75 0h6.5A2.75 2.75 0 0 1 14 2.75v10.5A2.75 2.75 0 0 1 11.25 16h-6.5A2.75 2.75 0 0 1 2 13.25v-.5a.75.75 0 0 1 1.5 0v.5c0 .69.56 1.25 1.25 1.25h6.5c.69 0 1.25-.56 1.25-1.25V2.75c0-.69-.56-1.25-1.25-1.25h-6.5c-.69 0-1.25.56-1.25 1.25v.5a.75.75 0 0 1-1.5 0z",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M8.78 7.47a.75.75 0 0 1 0 1.06l-2.25 2.25a.75.75 0 1 1-1.06-1.06l.97-.97H1.75a.75.75 0 0 1 0-1.5h4.69l-.97-.97a.75.75 0 0 1 1.06-1.06z",
	clipRule: "evenodd"
})), ta = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentcolor",
	fillRule: "evenodd",
	d: "M4 6.104V4a4 4 0 1 1 8 0v2.104c1.154.326 2 1.387 2 2.646v4.5A2.75 2.75 0 0 1 11.25 16h-6.5A2.75 2.75 0 0 1 2 13.25v-4.5c0-1.259.846-2.32 2-2.646M5.5 4a2.5 2.5 0 0 1 5 0v2h-5z",
	clipRule: "evenodd"
})), aa = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentColor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M12.227 11.52a5.48 5.48 0 0 0 1.246-2.97.5.5 0 0 0-.995-.1 4.5 4.5 0 0 1-.962 2.359l-1.07-1.07C10.794 9.247 11 8.647 11 8V3a3 3 0 0 0-6 0v1.293L1.354.646a.5.5 0 1 0-.708.708l14 14a.5.5 0 0 0 .708-.708zM8 12.5c.683 0 1.33-.152 1.911-.425l.743.743c-.649.359-1.378.59-2.154.66V15h2a.5.5 0 0 1 0 1h-5a.5.5 0 0 1 0-1h2v-1.522a5.5 5.5 0 0 1-4.973-4.929.5.5 0 0 1 .995-.098A4.5 4.5 0 0 0 8 12.5" }), /* @__PURE__ */ import_react.createElement("path", { d: "M8.743 10.907 5 7.164V8a3 3 0 0 0 3.743 2.907" })), na = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentColor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fillRule: "evenodd",
	d: "M2.975 8.002a.5.5 0 0 1 .547.449 4.5 4.5 0 0 0 8.956 0 .5.5 0 1 1 .995.098A5.5 5.5 0 0 1 8.5 13.478V15h2a.5.5 0 0 1 0 1h-5a.5.5 0 0 1 0-1h2v-1.522a5.5 5.5 0 0 1-4.973-4.929.5.5 0 0 1 .448-.547",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", { d: "M5 3a3 3 0 1 1 6 0v5a3 3 0 0 1-6 0z" })), ra = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentcolor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-6a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-6a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" })), ca = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentcolor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("g", { opacity: .25 }, /* @__PURE__ */ import_react.createElement("path", { d: "M12 .5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M12 .5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }))), la = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentcolor",
	...t
}, /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("g", { opacity: .25 }, /* @__PURE__ */ import_react.createElement("path", { d: "M6 6.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M6 6.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-6a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M12 .5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }))), ia = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "currentColor",
	...t
}, /* @__PURE__ */ import_react.createElement("g", { opacity: .25 }, /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-6a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }), /* @__PURE__ */ import_react.createElement("path", { d: "M0 11.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5zm6-6a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 .5.5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5z" }))), Ae = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 20,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M0 2.75A2.75 2.75 0 0 1 2.75 0h14.5A2.75 2.75 0 0 1 20 2.75v10.5A2.75 2.75 0 0 1 17.25 16H2.75A2.75 2.75 0 0 1 0 13.25zM2.75 1.5c-.69 0-1.25.56-1.25 1.25v10.5c0 .69.56 1.25 1.25 1.25h14.5c.69 0 1.25-.56 1.25-1.25V2.75c0-.69-.56-1.25-1.25-1.25z",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M9.47 4.22a.75.75 0 0 1 1.06 0l2.25 2.25a.75.75 0 0 1-1.06 1.06l-.97-.97v4.69a.75.75 0 0 1-1.5 0V6.56l-.97.97a.75.75 0 0 1-1.06-1.06z",
	clipRule: "evenodd"
})), sa = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 20,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("g", { fill: "currentColor" }, /* @__PURE__ */ import_react.createElement("path", { d: "M7.28 4.22a.75.75 0 0 0-1.06 1.06L8.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L10 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L11.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L10 6.94z" }), /* @__PURE__ */ import_react.createElement("path", {
	fillRule: "evenodd",
	d: "M2.75 0A2.75 2.75 0 0 0 0 2.75v10.5A2.75 2.75 0 0 0 2.75 16h14.5A2.75 2.75 0 0 0 20 13.25V2.75A2.75 2.75 0 0 0 17.25 0zM1.5 2.75c0-.69.56-1.25 1.25-1.25h14.5c.69 0 1.25.56 1.25 1.25v10.5c0 .69-.56 1.25-1.25 1.25H2.75c-.69 0-1.25-.56-1.25-1.25z",
	clipRule: "evenodd"
}))), ke = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M8 0a.75.75 0 0 1 .75.75v2.5a.75.75 0 0 1-1.5 0V.75A.75.75 0 0 1 8 0",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M8 12a.75.75 0 0 1 .75.75v2.5a.75.75 0 0 1-1.5 0v-2.5A.75.75 0 0 1 8 12",
	clipRule: "evenodd",
	opacity: .7
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M12 1.072a.75.75 0 0 1 .274 1.024l-1.25 2.165a.75.75 0 0 1-1.299-.75l1.25-2.165A.75.75 0 0 1 12 1.072",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M6 11.464a.75.75 0 0 1 .274 1.025l-1.25 2.165a.75.75 0 0 1-1.299-.75l1.25-2.165A.75.75 0 0 1 6 11.464",
	clipRule: "evenodd",
	opacity: .6
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M14.928 4a.75.75 0 0 1-.274 1.025l-2.165 1.25a.75.75 0 1 1-.75-1.3l2.165-1.25A.75.75 0 0 1 14.928 4",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M4.536 10a.75.75 0 0 1-.275 1.024l-2.165 1.25a.75.75 0 0 1-.75-1.298l2.165-1.25A.75.75 0 0 1 4.536 10",
	clipRule: "evenodd",
	opacity: .5
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M16 8a.75.75 0 0 1-.75.75h-2.5a.75.75 0 0 1 0-1.5h2.5A.75.75 0 0 1 16 8",
	clipRule: "evenodd"
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M4 8a.75.75 0 0 1-.75.75H.75a.75.75 0 0 1 0-1.5h2.5A.75.75 0 0 1 4 8",
	clipRule: "evenodd",
	opacity: .4
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M14.928 12a.75.75 0 0 1-1.024.274l-2.165-1.25a.75.75 0 0 1 .75-1.299l2.165 1.25A.75.75 0 0 1 14.928 12",
	clipRule: "evenodd",
	opacity: .9
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M4.536 6a.75.75 0 0 1-1.025.275l-2.165-1.25a.75.75 0 1 1 .75-1.3l2.165 1.25A.75.75 0 0 1 4.536 6",
	clipRule: "evenodd",
	opacity: .3
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M12 14.928a.75.75 0 0 1-1.024-.274l-1.25-2.165a.75.75 0 0 1 1.298-.75l1.25 2.165A.75.75 0 0 1 12 14.928",
	clipRule: "evenodd",
	opacity: .8
}), /* @__PURE__ */ import_react.createElement("path", {
	fill: "currentColor",
	fillRule: "evenodd",
	d: "M6 4.536a.75.75 0 0 1-1.024-.275l-1.25-2.165a.75.75 0 1 1 1.299-.75l1.25 2.165A.75.75 0 0 1 6 4.536",
	clipRule: "evenodd",
	opacity: .2
})), oa = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	xmlns: "http://www.w3.org/2000/svg",
	width: 16,
	height: 16,
	fill: "none",
	...t
}, /* @__PURE__ */ import_react.createElement("g", {
	stroke: "currentColor",
	strokeLinecap: "round",
	strokeLinejoin: "round",
	strokeWidth: 1.5
}, /* @__PURE__ */ import_react.createElement("path", { d: "M13.25 7H9m0 0V2.75M9 7l5.25-5.25M2.75 9H7m0 0v4.25M7 9l-5.25 5.25" }))), ua = /* @__PURE__ */ import_react.forwardRef(function({ trackRef: a, ...n }, c) {
	const r = ei(), { mergedProps: i, inFocus: o } = pu({
		trackRef: a ?? r,
		props: n
	});
	return /* @__PURE__ */ import_react.createElement(Yr.Consumer, null, (l) => l !== void 0 && /* @__PURE__ */ import_react.createElement("button", {
		ref: c,
		...i
	}, n.children ? n.children : o ? /* @__PURE__ */ import_react.createElement(oa, null) : /* @__PURE__ */ import_react.createElement(Xt, null)));
}), ce = /* @__PURE__ */ import_react.forwardRef(function({ kind: a, initialSelection: n, onActiveDeviceChange: c, onDeviceListChange: r, onDeviceSelectError: i, exactMatch: o, track: l, requestPermissions: s, onError: d, ...u }, h) {
	const m = It(), g = import_react.useRef("default"), { devices: f, activeDeviceId: E, setActiveMediaDevice: p, className: C } = yu({
		kind: a,
		room: m,
		track: l,
		requestPermissions: s,
		onError: import_react.useCallback((v) => {
			m && m.emit(RoomEvent.MediaDevicesError, v), d?.(v);
		}, [m, d])
	});
	import_react.useEffect(() => {
		n !== void 0 && p(n);
	}, [p]), import_react.useEffect(() => {
		typeof r == "function" && r(f);
	}, [r, f]), import_react.useEffect(() => {
		E !== g.current && c?.(E), g.current = E;
	}, [E]);
	const k = async (v) => {
		try {
			await p(v, { exact: o ?? !0 });
		} catch (w) {
			if (w instanceof Error) i?.(w);
			else throw w;
		}
	}, R = import_react.useMemo(() => vu(u, { className: C }, { className: "lk-list" }), [C, u]), M = !!f.find((v) => v.label.toLowerCase().startsWith("default"));
	function D(v, w, b) {
		return v === w || !M && b === 0 && w === "default";
	}
	return /* @__PURE__ */ import_react.createElement("ul", {
		ref: h,
		...R
	}, f.map((v, w) => /* @__PURE__ */ import_react.createElement("li", {
		key: v.deviceId,
		id: v.deviceId,
		"data-lk-active": D(v.deviceId, E, w),
		"aria-selected": D(v.deviceId, E, w),
		role: "option"
	}, /* @__PURE__ */ import_react.createElement("button", {
		className: "lk-button",
		onClick: () => k(v.deviceId)
	}, v.label))));
}), Ne = /* @__PURE__ */ import_react.forwardRef(function({ label: a, ...n }, c) {
	const r = _t(), { mergedProps: i, canPlayAudio: o } = Mu({
		room: r,
		props: n
	}), { mergedProps: l, canPlayVideo: s } = Au({
		room: r,
		props: i
	}), { style: d, ...u } = l;
	return d.display = o && s ? "none" : "block", /* @__PURE__ */ import_react.createElement("button", {
		ref: c,
		style: d,
		...u
	}, a ?? `Start ${o ? "Video" : "Audio"}`);
});
function xe(t, a) {
	switch (t) {
		case Track.Source.Microphone: return a ? /* @__PURE__ */ import_react.createElement(na, null) : /* @__PURE__ */ import_react.createElement(aa, null);
		case Track.Source.Camera: return a ? /* @__PURE__ */ import_react.createElement(Kt, null) : /* @__PURE__ */ import_react.createElement(Gt, null);
		case Track.Source.ScreenShare: return a ? /* @__PURE__ */ import_react.createElement(sa, null) : /* @__PURE__ */ import_react.createElement(Ae, null);
		default: return;
	}
}
function da(t) {
	switch (t) {
		case ConnectionQuality.Excellent: return /* @__PURE__ */ import_react.createElement(ra, null);
		case ConnectionQuality.Good: return /* @__PURE__ */ import_react.createElement(ca, null);
		case ConnectionQuality.Poor: return /* @__PURE__ */ import_react.createElement(la, null);
		default: return /* @__PURE__ */ import_react.createElement(ia, null);
	}
}
var j = /* @__PURE__ */ import_react.forwardRef(function({ showIcon: a, ...n }, c) {
	const { buttonProps: r, enabled: i } = Ru(n), [o, l] = import_react.useState(!1);
	return import_react.useEffect(() => {
		l(!0);
	}, []), o && /* @__PURE__ */ import_react.createElement("button", {
		ref: c,
		...r
	}, (a ?? !0) && xe(n.source, i), n.children);
}), Le = /* @__PURE__ */ import_react.forwardRef(function(a, n) {
	const { className: c, quality: r } = lu(a), i = import_react.useMemo(() => ({
		...vu(a, { className: c }),
		"data-lk-quality": r
	}), [
		r,
		a,
		c
	]);
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: n,
		...i
	}, a.children ?? da(r));
}), le = /* @__PURE__ */ import_react.forwardRef(function({ participant: a, ...n }, c) {
	const r = Ie(a), { className: i, infoObserver: o } = import_react.useMemo(() => eu(r), [r]), { identity: l, name: s } = j$1(o, {
		name: r.name,
		identity: r.identity,
		metadata: r.metadata
	}), d = import_react.useMemo(() => vu(n, {
		className: i,
		"data-lk-participant-name": s
	}), [
		n,
		i,
		s
	]);
	return /* @__PURE__ */ import_react.createElement("span", {
		ref: c,
		...d
	}, s !== "" ? s : l, n.children);
}), De = /* @__PURE__ */ import_react.forwardRef(function({ trackRef: a, show: n = "always", ...c }, r) {
	const { className: i, isMuted: o } = Iu(a), l = n === "always" || n === "muted" && o || n === "unmuted" && !o, s = import_react.useMemo(() => vu(c, { className: i }), [i, c]);
	return l ? /* @__PURE__ */ import_react.createElement("div", {
		ref: r,
		...s,
		"data-lk-muted": o
	}, c.children ?? xe(a.source, !o)) : null;
}), Ve = (t) => /* @__PURE__ */ import_react.createElement("svg", {
	width: 320,
	height: 320,
	viewBox: "0 0 320 320",
	preserveAspectRatio: "xMidYMid meet",
	fill: "none",
	xmlns: "http://www.w3.org/2000/svg",
	...t
}, /* @__PURE__ */ import_react.createElement("path", {
	d: "M160 180C204.182 180 240 144.183 240 100C240 55.8172 204.182 20 160 20C115.817 20 79.9997 55.8172 79.9997 100C79.9997 144.183 115.817 180 160 180Z",
	fill: "white",
	fillOpacity: .25
}), /* @__PURE__ */ import_react.createElement("path", {
	d: "M97.6542 194.614C103.267 191.818 109.841 192.481 115.519 195.141C129.025 201.466 144.1 205 159.999 205C175.899 205 190.973 201.466 204.48 195.141C210.158 192.481 216.732 191.818 222.345 194.614C262.703 214.719 291.985 253.736 298.591 300.062C300.15 310.997 291.045 320 280 320H39.9997C28.954 320 19.8495 310.997 21.4087 300.062C28.014 253.736 57.2966 214.72 97.6542 194.614Z",
	fill: "white",
	fillOpacity: .25
}));
function ze(t, a = {}) {
	const [n, c] = import_react.useState(Zt$1(t)), [r, i] = import_react.useState(n?.isMuted), [o, l] = import_react.useState(n?.isSubscribed), [s, d] = import_react.useState(n?.track), [u, h] = import_react.useState("landscape"), m = import_react.useRef(), { className: g, trackObserver: f } = import_react.useMemo(() => aa$1(t), [
		t.participant.sid ?? t.participant.identity,
		t.source,
		ee(t) && t.publication.trackSid
	]);
	return import_react.useEffect(() => {
		const E = f.subscribe((p) => {
			R.debug("update track", p), c(p), i(p?.isMuted), l(p?.isSubscribed), d(p?.track);
		});
		return () => E?.unsubscribe();
	}, [f]), import_react.useEffect(() => (s && (m.current && s.detach(m.current), a.element?.current && !(t.participant.isLocal && s?.kind === "audio") && s.attach(a.element.current)), m.current = a.element?.current, () => {
		m.current && s?.detach(m.current);
	}), [s, a.element]), import_react.useEffect(() => {
		if (typeof n?.dimensions?.width == "number" && typeof n?.dimensions?.height == "number") h(n.dimensions.width > n.dimensions.height ? "landscape" : "portrait");
	}, [n]), {
		publication: n,
		isMuted: r,
		isSubscribed: o,
		track: s,
		elementProps: vu(a.props, {
			className: g,
			"data-lk-local-participant": t.participant.isLocal,
			"data-lk-source": n?.source,
			...n?.kind === "video" && { "data-lk-orientation": u }
		})
	};
}
var be = /* @__PURE__ */ f((/* @__PURE__ */ j$2(((t, a) => {
	var n = "Expected a function", c = NaN, r = "[object Symbol]", i = /^\s+|\s+$/g, o = /^[-+]0x[0-9a-f]+$/i, l = /^0b[01]+$/i, s = /^0o[0-7]+$/i, d = parseInt, u = typeof global == "object" && global && global.Object === Object && global, h = typeof self == "object" && self && self.Object === Object && self, m = u || h || Function("return this")(), g = Object.prototype.toString, f = Math.max, E = Math.min, p = function() {
		return m.Date.now();
	};
	function C(v, w, b) {
		var T, V, B, x, N, L, F = 0, A = !1, _ = !1, H = !0;
		if (typeof v != "function") throw new TypeError(n);
		w = D(w) || 0, k(b) && (A = !!b.leading, _ = "maxWait" in b, B = _ ? f(D(b.maxWait) || 0, w) : B, H = "trailing" in b ? !!b.trailing : H);
		function Q(I) {
			var O = T, K = V;
			return T = V = void 0, F = I, x = v.apply(K, O), x;
		}
		function te(I) {
			return F = I, N = setTimeout(y, w), A ? Q(I) : x;
		}
		function G(I) {
			var O = I - L, K = I - F, pe = w - O;
			return _ ? E(pe, B - K) : pe;
		}
		function X(I) {
			var O = I - L, K = I - F;
			return L === void 0 || O >= w || O < 0 || _ && K >= B;
		}
		function y() {
			var I = p();
			if (X(I)) return U(I);
			N = setTimeout(y, G(I));
		}
		function U(I) {
			return N = void 0, H && T ? Q(I) : (T = V = void 0, x);
		}
		function He() {
			N !== void 0 && clearTimeout(N), F = 0, T = L = V = N = void 0;
		}
		function Ue() {
			return N === void 0 ? x : U(p());
		}
		function ae() {
			var I = p(), O = X(I);
			if (T = arguments, V = this, L = I, O) {
				if (N === void 0) return te(L);
				if (_) return N = setTimeout(y, w), Q(L);
			}
			return N === void 0 && (N = setTimeout(y, w)), x;
		}
		return ae.cancel = He, ae.flush = Ue, ae;
	}
	function k(v) {
		var w = typeof v;
		return !!v && (w == "object" || w == "function");
	}
	function R(v) {
		return !!v && typeof v == "object";
	}
	function M(v) {
		return typeof v == "symbol" || R(v) && g.call(v) == r;
	}
	function D(v) {
		if (typeof v == "number") return v;
		if (M(v)) return c;
		if (k(v)) {
			var w = typeof v.valueOf == "function" ? v.valueOf() : v;
			v = k(w) ? w + "" : w;
		}
		if (typeof v != "string") return v === 0 ? v : +v;
		v = v.replace(i, "");
		var b = l.test(v);
		return b || s.test(v) ? d(v.slice(2), b ? 2 : 8) : o.test(v) ? c : +v;
	}
	a.exports = C;
})))(), 1);
function fa(t) {
	const a = (0, import_react.useRef)(t);
	a.current = t, (0, import_react.useEffect)(() => () => {
		a.current();
	}, []);
}
function ha(t, a = 500, n) {
	const c = (0, import_react.useRef)();
	fa(() => {
		c.current && c.current.cancel();
	});
	const r = (0, import_react.useMemo)(() => {
		const i = (0, be.default)(t, a, n), o = (...l) => i(...l);
		return o.cancel = () => {
			i.cancel();
		}, o.isPending = () => !!c.current, o.flush = () => i.flush(), o;
	}, [
		t,
		a,
		n
	]);
	return (0, import_react.useEffect)(() => {
		c.current = (0, be.default)(t, a, n);
	}, [
		t,
		a,
		n
	]), r;
}
function va(t, a, n) {
	const c = n?.equalityFn ?? ((d, u) => d === u), r = t instanceof Function ? t() : t, [i, o] = (0, import_react.useState)(r), l = (0, import_react.useRef)(r), s = ha(o, a, n);
	return c(l.current, r) || (s(r), l.current = r), [i, s];
}
function ga({ threshold: t = 0, root: a = null, rootMargin: n = "0%", freezeOnceVisible: c = !1, initialIsIntersecting: r = !1, onChange: i } = {}) {
	var o;
	const [l, s] = (0, import_react.useState)(null), [d, u] = (0, import_react.useState)(() => ({
		isIntersecting: r,
		entry: void 0
	})), h = (0, import_react.useRef)();
	h.current = i;
	const m = ((o = d.entry) == null ? void 0 : o.isIntersecting) && c;
	(0, import_react.useEffect)(() => {
		if (!l || !("IntersectionObserver" in window) || m) return;
		let E;
		const p = new IntersectionObserver((C) => {
			const k = Array.isArray(p.thresholds) ? p.thresholds : [p.thresholds];
			C.forEach((R) => {
				const M = R.isIntersecting && k.some((D) => R.intersectionRatio >= D);
				u({
					isIntersecting: M,
					entry: R
				}), h.current && h.current(M, R), M && c && E && (E(), E = void 0);
			});
		}, {
			threshold: t,
			root: a,
			rootMargin: n
		});
		return p.observe(l), () => {
			p.disconnect();
		};
	}, [
		l,
		JSON.stringify(t),
		a,
		n,
		m,
		c
	]);
	const g = (0, import_react.useRef)(null);
	(0, import_react.useEffect)(() => {
		var E;
		!l && (E = d.entry) != null && E.target && !c && !m && g.current !== d.entry.target && (g.current = d.entry.target, u({
			isIntersecting: r,
			entry: void 0
		}));
	}, [
		l,
		d.entry,
		c,
		m,
		r
	]);
	const f = [
		s,
		!!d.isIntersecting,
		d.entry
	];
	return f.ref = f[0], f.isIntersecting = f[1], f.entry = f[2], f;
}
var pa = /* @__PURE__ */ import_react.forwardRef(function({ onTrackClick: a, onClick: n, onSubscriptionStatusChanged: c, trackRef: r, manageSubscription: i, ...o }, l) {
	const s = pn(r), d = import_react.useRef(null);
	import_react.useImperativeHandle(l, () => d.current);
	const { ref: u, entry: h } = ga({ root: null }), [m] = va(h, 3e3), g = import_react.useRef(null), f = import_react.useRef(!1);
	import_react.useEffect(() => {
		f.current = !!i, g.current = s.publication instanceof RemoteTrackPublication ? s.publication : null;
	}, [s.publication, i]), import_react.useEffect(() => {
		i && s.publication instanceof RemoteTrackPublication && m?.isIntersecting === !1 && h?.isIntersecting === !1 && s.publication.setSubscribed(!1);
	}, [
		m,
		h,
		s,
		i
	]), import_react.useEffect(() => {
		i && s.publication instanceof RemoteTrackPublication && h?.isIntersecting === !0 && s.publication.setSubscribed(!0);
	}, [
		h,
		s,
		i
	]), import_react.useEffect(() => () => {
		f.current && g.current?.setSubscribed(!1);
	}, []);
	const { elementProps: E, publication: p, isSubscribed: C } = ze(s, {
		element: d,
		props: o
	});
	import_react.useEffect(() => {
		c?.(!!C);
	}, [C, c]);
	const k = (M) => {
		n?.(M), a?.({
			participant: s?.participant,
			track: p
		});
	}, R = import_react.useCallback((M) => {
		d.current = M, u(M);
	}, [u]);
	return /* @__PURE__ */ import_react.createElement("video", {
		ref: R,
		...E,
		muted: !0,
		onClick: k
	});
}), he = /* @__PURE__ */ import_react.forwardRef(function({ trackRef: a, onSubscriptionStatusChanged: n, volume: c, ...r }, i) {
	const o = pn(a), l = import_react.useRef(null);
	import_react.useImperativeHandle(i, () => l.current);
	const { elementProps: s, isSubscribed: d, track: u, publication: h } = ze(o, {
		element: l,
		props: r
	});
	return import_react.useEffect(() => {
		n?.(!!d);
	}, [d, n]), import_react.useEffect(() => {
		u === void 0 || c === void 0 || (u instanceof RemoteAudioTrack ? u.setVolume(c) : R.warn("Volume can only be set on remote audio tracks."));
	}, [c, u]), import_react.useEffect(() => {
		h === void 0 || r.muted === void 0 || (h instanceof RemoteTrackPublication ? h.setEnabled(!r.muted) : R.warn("Can only call setEnabled on remote track publications."));
	}, [
		r.muted,
		h,
		u
	]), /* @__PURE__ */ import_react.createElement("audio", {
		ref: l,
		...s
	});
});
function Ea(t) {
	const a = !!Ot();
	return t.participant && !a ? /* @__PURE__ */ import_react.createElement(ti.Provider, { value: t.participant }, t.children) : /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, t.children);
}
function ka(t) {
	const a = !!ei();
	return t.trackRef && !a ? /* @__PURE__ */ import_react.createElement(Zr.Provider, { value: t.trackRef }, t.children) : /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, t.children);
}
var ie = /* @__PURE__ */ import_react.forwardRef(function({ trackRef: a, children: n, onParticipantClick: c, disableSpeakingIndicator: r, ...i }, o) {
	const l = pn(a), { elementProps: s } = Cu({
		htmlProps: i,
		disableSpeakingIndicator: r,
		onParticipantClick: c,
		trackRef: l
	}), d = Uu(l.participant), u = Xr(), h = au()?.autoSubscription, m = import_react.useCallback((g) => {
		l.source && !g && u && u.pin.dispatch && Fs(l, u.pin.state) && u.pin.dispatch({ msg: "clear_pin" });
	}, [l, u]);
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: o,
		style: { position: "relative" },
		...s
	}, /* @__PURE__ */ import_react.createElement(ka, { trackRef: l }, /* @__PURE__ */ import_react.createElement(Ea, { participant: l.participant }, n ?? /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, ee(l) && (l.publication?.kind === "video" || l.source === Track.Source.Camera || l.source === Track.Source.ScreenShare) ? /* @__PURE__ */ import_react.createElement(pa, {
		trackRef: l,
		onSubscriptionStatusChanged: m,
		manageSubscription: h
	}) : ee(l) && /* @__PURE__ */ import_react.createElement(he, {
		trackRef: l,
		onSubscriptionStatusChanged: m
	}), /* @__PURE__ */ import_react.createElement("div", { className: "lk-participant-placeholder" }, /* @__PURE__ */ import_react.createElement(Ve, null)), /* @__PURE__ */ import_react.createElement("div", { className: "lk-participant-metadata" }, /* @__PURE__ */ import_react.createElement("div", { className: "lk-participant-metadata-item" }, l.source === Track.Source.Camera ? /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, d && /* @__PURE__ */ import_react.createElement(ta, { style: { marginRight: "0.25rem" } }), /* @__PURE__ */ import_react.createElement(De, {
		trackRef: {
			participant: l.participant,
			source: Track.Source.Microphone
		},
		show: "muted"
	}), /* @__PURE__ */ import_react.createElement(le, null)) : /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement(Ae, { style: { marginRight: "0.25rem" } }), /* @__PURE__ */ import_react.createElement(le, null, "'s screen"))), /* @__PURE__ */ import_react.createElement(Le, { className: "lk-participant-metadata-item" }))), /* @__PURE__ */ import_react.createElement(ua, { trackRef: l }))));
});
function ba(t) {
	const a = vu(t, { className: "lk-focus-layout" });
	return /* @__PURE__ */ import_react.createElement("div", a, t.children);
}
function Sa({ trackRef: t, ...a }) {
	return /* @__PURE__ */ import_react.createElement(ie, {
		trackRef: t,
		...a
	});
}
function ve({ tracks: t, ...a }) {
	return /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, t.map((n) => /* @__PURE__ */ import_react.createElement(Zr.Provider, {
		value: n,
		key: G(n)
	}, mu(a.children))));
}
function wa({ totalPageCount: t, nextPage: a, prevPage: n, currentPage: c, pagesContainer: r }) {
	const [i, o] = import_react.useState(!1);
	return import_react.useEffect(() => {
		let l;
		return r && (l = tu(r.current, 2e3).subscribe(o)), () => {
			l && l.unsubscribe();
		};
	}, [r]), /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-pagination-control",
		"data-lk-user-interaction": i
	}, /* @__PURE__ */ import_react.createElement("button", {
		className: "lk-button",
		onClick: n
	}, /* @__PURE__ */ import_react.createElement(Ee, null)), /* @__PURE__ */ import_react.createElement("span", { className: "lk-pagination-count" }, `${c} of ${t}`), /* @__PURE__ */ import_react.createElement("button", {
		className: "lk-button",
		onClick: a
	}, /* @__PURE__ */ import_react.createElement(Ee, null)));
}
var Ca = /* @__PURE__ */ import_react.forwardRef(function({ totalPageCount: a, currentPage: n }, c) {
	const r = new Array(a).fill("").map((i, o) => o + 1 === n ? /* @__PURE__ */ import_react.createElement("span", {
		"data-lk-active": !0,
		key: o
	}) : /* @__PURE__ */ import_react.createElement("span", { key: o }));
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: c,
		className: "lk-pagination-indicator"
	}, r);
});
function Ma({ tracks: t, ...a }) {
	const n = import_react.createRef(), c = import_react.useMemo(() => vu(a, { className: "lk-grid-layout" }), [a]), { layout: r } = hu(n, t.length), i = Su(r.maxTiles, t);
	return Lu(n, {
		onLeftSwipe: i.nextPage,
		onRightSwipe: i.prevPage
	}), /* @__PURE__ */ import_react.createElement("div", {
		ref: n,
		"data-lk-pagination": i.totalPageCount > 1,
		...c
	}, /* @__PURE__ */ import_react.createElement(ve, { tracks: i.tracks }, a.children), t.length > r.maxTiles && /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement(Ca, {
		totalPageCount: i.totalPageCount,
		currentPage: i.currentPage
	}), /* @__PURE__ */ import_react.createElement(wa, {
		pagesContainer: n,
		...i
	})));
}
var ya = 130, Ra = 140, Se = 1, Fe = 16 / 10, Ia = (1 - Fe) * -1;
function Ta({ tracks: t, orientation: a, ...n }) {
	const c = import_react.useRef(null), [r, i] = import_react.useState(0), { width: o, height: l } = Oi(c), s = a || (l >= o ? "vertical" : "horizontal"), d = s === "vertical" ? Math.max(o * Ia, ya) : Math.max(l * Fe, Ra), u = zc(), h = Math.max(s === "vertical" ? (l - u) / d : (o - u) / d, Se);
	let m = Math.round(h);
	Math.abs(h - r) < .5 ? m = Math.round(r) : r !== h && i(h);
	const g = lc(t, m);
	return import_react.useLayoutEffect(() => {
		c.current && (c.current.dataset.lkOrientation = s, c.current.style.setProperty("--lk-max-visible-tiles", m.toString()));
	}, [m, s]), /* @__PURE__ */ import_react.createElement("aside", {
		key: s,
		className: "lk-carousel",
		ref: c,
		...n
	}, /* @__PURE__ */ import_react.createElement(ve, { tracks: g }, n.children));
}
function Oe({ value: t, onPinChange: a, onWidgetChange: n, children: c }) {
	const r = ru(t);
	return import_react.useEffect(() => {
		R.debug("PinState Updated", { state: r.pin.state }), a && r.pin.state && a(r.pin.state);
	}, [r.pin.state, a]), import_react.useEffect(() => {
		R.debug("Widget Updated", { widgetState: r.widget.state }), n && r.widget.state && n(r.widget.state);
	}, [n, r.widget.state]), /* @__PURE__ */ import_react.createElement(Yr.Provider, { value: r }, c);
}
function Pa({ room: t, volume: a, muted: n }) {
	const c = Du([
		Track.Source.Microphone,
		Track.Source.ScreenShareAudio,
		Track.Source.Unknown
	], {
		updateOnlyOn: [],
		onlySubscribed: !0,
		room: t
	}).filter((r) => !r.participant.isLocal && r.publication.kind === Track.Kind.Audio);
	return /* @__PURE__ */ import_react.createElement("div", { style: { display: "none" } }, c.map((r) => /* @__PURE__ */ import_react.createElement(he, {
		key: G(r),
		trackRef: r,
		volume: a,
		muted: n
	})));
}
function Aa(t) {
	const a = import_react.useMemo(() => vu(t, { className: "lk-toast" }), [t]);
	return /* @__PURE__ */ import_react.createElement("div", a, t.children);
}
var Na = (t) => {
	const a = [];
	for (let n = 0; n < t; n++) a.push([n, t - 1 - n]);
	return a;
}, we = (t) => [[Math.floor(t / 2)], [-1]], xa = (t, a, n) => {
	const [c, r] = (0, import_react.useState)(0), [i, o] = (0, import_react.useState)([[]]);
	(0, import_react.useEffect)(() => {
		if (t === "thinking") o(we(a));
		else if (t === "connecting" || t === "initializing") o([...Na(a)]);
		else o(t === "listening" ? we(a) : t === void 0 || t === "speaking" ? [new Array(a).fill(0).map((s, d) => d)] : [[]]);
		r(0);
	}, [t, a]);
	const l = (0, import_react.useRef)(null);
	return (0, import_react.useEffect)(() => {
		let s = performance.now();
		const d = (u) => {
			u - s >= n && (r((h) => h + 1), s = u), l.current = requestAnimationFrame(d);
		};
		return l.current = requestAnimationFrame(d), () => {
			l.current !== null && cancelAnimationFrame(l.current);
		};
	}, [
		n,
		a,
		t,
		i.length
	]), i[c % i.length];
}, La = /* @__PURE__ */ new Map([
	["connecting", 2e3],
	["initializing", 2e3],
	["listening", 500],
	["thinking", 150]
]), Da = (t, a) => {
	if (t === void 0) return 1e3;
	let n = La.get(t);
	return n && t === "connecting" && (n /= a), n;
}, _e = /* @__PURE__ */ import_react.forwardRef(function({ state: a, options: n, barCount: c = 15, trackRef: r, track: i, children: o, ...l }, s) {
	const d = vu(l, { className: "lk-audio-bar-visualizer" });
	let u = ei();
	(r || i) && (u = r || i);
	const h = Wu(u, {
		bands: c,
		loPass: 100,
		hiPass: 200
	}), m = n?.minHeight ?? 20, g = n?.maxHeight ?? 100, f = xa(a, c, Da(a, c) ?? 100);
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: s,
		...d,
		"data-lk-va-state": a
	}, h.map((E, p) => o ? mu(o, {
		"data-lk-highlighted": f.includes(p),
		"data-lk-bar-index": p,
		className: "lk-audio-bar",
		style: { height: `${Math.min(g, Math.max(m, E * 100 + 5))}%` }
	}) : /* @__PURE__ */ import_react.createElement("span", {
		key: p,
		"data-lk-highlighted": f.includes(p),
		"data-lk-bar-index": p,
		className: `lk-audio-bar ${f.includes(p) && "lk-highlighted"}`,
		style: { height: `${Math.min(g, Math.max(m, E * 100 + 5))}%` }
	})));
}), Va = /* @__PURE__ */ import_react.forwardRef(function({ children: a, disableSpeakingIndicator: n, onParticipantClick: c, trackRef: r, ...i }, o) {
	const l = pn(r), { elementProps: s } = Cu({
		trackRef: l,
		htmlProps: i,
		disableSpeakingIndicator: n,
		onParticipantClick: c
	});
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: o,
		style: {
			position: "relative",
			minHeight: "160px"
		},
		...s
	}, /* @__PURE__ */ import_react.createElement(Zr.Provider, { value: l }, a ?? /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, ee(l) && /* @__PURE__ */ import_react.createElement(he, { trackRef: l }), /* @__PURE__ */ import_react.createElement(_e, {
		barCount: 7,
		options: { minHeight: 8 }
	}), /* @__PURE__ */ import_react.createElement("div", { className: "lk-participant-metadata" }, /* @__PURE__ */ import_react.createElement("div", { className: "lk-participant-metadata-item" }, /* @__PURE__ */ import_react.createElement(De, { trackRef: l }), /* @__PURE__ */ import_react.createElement(le, null)), /* @__PURE__ */ import_react.createElement(Le, { className: "lk-participant-metadata-item" })))));
});
function za(t) {
	const [a, n] = import_react.useState(void 0), c = tt(t.room);
	return import_react.useEffect(() => {
		switch (c) {
			case ConnectionState.Reconnecting:
				n(/* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement(ke, { className: "lk-spinner" }), " Reconnecting"));
				break;
			case ConnectionState.Connecting:
				n(/* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement(ke, { className: "lk-spinner" }), " Connecting"));
				break;
			case ConnectionState.Disconnected:
				n(/* @__PURE__ */ import_react.createElement(import_react.Fragment, null, "Disconnected"));
				break;
			default:
				n(void 0);
				break;
		}
	}, [c]), a ? /* @__PURE__ */ import_react.createElement(Aa, { className: "lk-toast-connection-state" }, a) : /* @__PURE__ */ import_react.createElement(import_react.Fragment, null);
}
var Fa = /* @__PURE__ */ import_react.forwardRef(function({ entry: a, hideName: n = !1, hideTimestamp: c = !1, messageFormatter: r, ...i }, o) {
	const l = import_react.useMemo(() => r ? r(a.message) : a.message, [a.message, r]), s = !!a.editTimestamp, d = new Date(a.timestamp), u = typeof navigator < "u" ? navigator.language : "en-US", h = a.from?.name ?? a.from?.identity;
	return /* @__PURE__ */ import_react.createElement("li", {
		ref: o,
		className: "lk-chat-entry",
		title: d.toLocaleTimeString(u, { timeStyle: "full" }),
		"data-lk-message-origin": a.from?.isLocal ? "local" : "remote",
		...i
	}, (!c || !n || s) && /* @__PURE__ */ import_react.createElement("span", { className: "lk-meta-data" }, !n && /* @__PURE__ */ import_react.createElement("strong", { className: "lk-participant-name" }, h), (!c || s) && /* @__PURE__ */ import_react.createElement("span", { className: "lk-timestamp" }, s && "edited ", d.toLocaleTimeString(u, { timeStyle: "short" }))), /* @__PURE__ */ import_react.createElement("span", { className: "lk-message-body" }, l), /* @__PURE__ */ import_react.createElement("span", { className: "lk-message-attachements" }, a.attachedFiles?.map((m) => m.type.startsWith("image/") && /* @__PURE__ */ import_react.createElement("img", {
		style: {
			maxWidth: "300px",
			maxHeight: "300px"
		},
		key: m.name,
		src: URL.createObjectURL(m),
		alt: m.name
	}))));
});
function Qa(t) {
	return Yc(t, Jc()).map((a, n) => {
		if (typeof a == "string") return a;
		{
			const c = a.content.toString(), r = a.type === "url" ? /^http(s?):\/\//.test(c) ? c : `https://${c}` : `mailto:${c}`;
			return /* @__PURE__ */ import_react.createElement("a", {
				className: "lk-chat-link",
				key: n,
				href: r,
				target: "_blank",
				rel: "noreferrer"
			}, c);
		}
	});
}
function $e({ messageFormatter: t, messageDecoder: a, messageEncoder: n, channelTopic: c, ...r }) {
	const i = import_react.useRef(null), o = import_react.useRef(null), { chatMessages: l, send: s, isSending: d } = gc(import_react.useMemo(() => ({
		messageDecoder: a,
		messageEncoder: n,
		channelTopic: c
	}), [
		a,
		n,
		c
	])), u = Xr(), h = import_react.useRef(0);
	async function m(g) {
		g.preventDefault(), o.current && o.current.value.trim() !== "" && (await s(o.current.value), o.current.value = "", o.current.focus());
	}
	return import_react.useEffect(() => {
		i && i.current?.scrollTo({ top: i.current.scrollHeight });
	}, [i, l]), import_react.useEffect(() => {
		if (!u || l.length === 0) return;
		if (u.widget.state?.showChat && l.length > 0 && h.current !== l[l.length - 1]?.timestamp) {
			h.current = l[l.length - 1]?.timestamp;
			return;
		}
		const g = l.filter((E) => !h.current || E.timestamp > h.current).length, { widget: f } = u;
		g > 0 && f.state?.unreadMessages !== g && f.dispatch?.({
			msg: "unread_msg",
			count: g
		});
	}, [l, u?.widget]), /* @__PURE__ */ import_react.createElement("div", {
		...r,
		className: "lk-chat"
	}, /* @__PURE__ */ import_react.createElement("div", { className: "lk-chat-header" }, "Messages", u && /* @__PURE__ */ import_react.createElement(Te, { className: "lk-close-button" }, /* @__PURE__ */ import_react.createElement(Jt, null))), /* @__PURE__ */ import_react.createElement("ul", {
		className: "lk-list lk-chat-messages",
		ref: i
	}, r.children ? l.map((g, f) => mu(r.children, {
		entry: g,
		key: g.id ?? f,
		messageFormatter: t
	})) : l.map((g, f, E) => {
		const p = f >= 1 && E[f - 1].from === g.from, C = f >= 1 && g.timestamp - E[f - 1].timestamp < 6e4;
		return /* @__PURE__ */ import_react.createElement(Fa, {
			key: g.id ?? f,
			hideName: p,
			hideTimestamp: p === !1 ? !1 : C,
			entry: g,
			messageFormatter: t
		});
	})), /* @__PURE__ */ import_react.createElement("form", {
		className: "lk-chat-form",
		onSubmit: m
	}, /* @__PURE__ */ import_react.createElement("input", {
		className: "lk-form-control lk-chat-form-input",
		disabled: d,
		ref: o,
		type: "text",
		placeholder: "Enter a message...",
		onInput: (g) => g.stopPropagation(),
		onKeyDown: (g) => g.stopPropagation(),
		onKeyUp: (g) => g.stopPropagation()
	}), /* @__PURE__ */ import_react.createElement("button", {
		type: "submit",
		className: "lk-button lk-chat-form-button",
		disabled: d
	}, "Send")));
}
function Z({ kind: t, initialSelection: a, onActiveDeviceChange: n, tracks: c, requestPermissions: r = !1, ...i }) {
	const [o, l] = import_react.useState(!1), [s, d] = import_react.useState([]), [u, h] = import_react.useState(!0), [m, g] = import_react.useState(r), f = (k, R$1) => {
		R.debug("handle device change"), l(!1), n?.(k, R$1);
	}, E = import_react.useRef(null), p = import_react.useRef(null);
	import_react.useLayoutEffect(() => {
		o && g(!0);
	}, [o]), import_react.useLayoutEffect(() => {
		let k;
		return E.current && p.current && (s || u) && (k = Kc(E.current, p.current, (R, M) => {
			p.current && Object.assign(p.current.style, {
				left: `${R}px`,
				top: `${M}px`
			});
		})), h(!1), () => {
			k?.();
		};
	}, [
		E,
		p,
		s,
		u
	]);
	const C = import_react.useCallback((k) => {
		p.current && k.target !== E.current && o && Gc(p.current, k) && l(!1);
	}, [
		o,
		p,
		E
	]);
	return import_react.useEffect(() => (document.addEventListener("click", C), () => {
		document.removeEventListener("click", C);
	}), [C]), /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement("button", {
		className: "lk-button lk-button-menu",
		"aria-pressed": o,
		...i,
		onClick: () => l(!o),
		ref: E
	}, i.children), !i.disabled && /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-device-menu",
		ref: p,
		style: { visibility: o ? "visible" : "hidden" }
	}, t ? /* @__PURE__ */ import_react.createElement(ce, {
		initialSelection: a,
		onActiveDeviceChange: (k) => f(t, k),
		onDeviceListChange: d,
		kind: t,
		track: c?.[t],
		requestPermissions: m
	}) : /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement("div", { className: "lk-device-menu-heading" }, "Audio inputs"), /* @__PURE__ */ import_react.createElement(ce, {
		kind: "audioinput",
		onActiveDeviceChange: (k) => f("audioinput", k),
		onDeviceListChange: d,
		track: c?.audioinput,
		requestPermissions: m
	}), /* @__PURE__ */ import_react.createElement("div", { className: "lk-device-menu-heading" }, "Video inputs"), /* @__PURE__ */ import_react.createElement(ce, {
		kind: "videoinput",
		onActiveDeviceChange: (k) => f("videoinput", k),
		onDeviceListChange: d,
		track: c?.videoinput,
		requestPermissions: m
	}))));
}
function ge() {
	import_react.useEffect(() => {
		gu();
	}, []);
}
function Oa(t, a) {
	const [n, c] = import_react.useState(), r = import_react.useMemo(() => new _(), []);
	return import_react.useEffect(() => {
		let i = !1, o = [];
		return r.lock().then(async (l) => {
			try {
				(t.audio || t.video) && (o = await createLocalTracks(t), i ? o.forEach((s) => s.stop()) : c(o));
			} catch (s) {
				a && s instanceof Error ? a(s) : R.error(s);
			} finally {
				l();
			}
		}), () => {
			i = !0, o.forEach((l) => {
				l.stop();
			});
		};
	}, [
		JSON.stringify(t, ac),
		a,
		r
	]), n;
}
function Ga(t, a, n) {
	const [c, r] = import_react.useState(null), [i, o] = import_react.useState(!1), l = wu({ kind: n }), [s, d] = import_react.useState(void 0), [u, h] = import_react.useState(), [m, g] = import_react.useState(a);
	import_react.useEffect(() => {
		g(a);
	}, [a]);
	const f = async (C, k) => {
		try {
			const R = k === "videoinput" ? await createLocalVideoTrack({
				deviceId: C,
				resolution: VideoPresets.h720.resolution
			}) : await createLocalAudioTrack({ deviceId: C }), M = await R.getDeviceId(!1);
			M && C !== M && (p.current = M, g(M)), h(R);
		} catch (R) {
			R instanceof Error && r(R);
		}
	}, E = async (C, k) => {
		await C.setDeviceId(k), p.current = k;
	}, p = import_react.useRef(m);
	return import_react.useEffect(() => {
		t && !u && !c && !i && (R.debug("creating track", n), o(!0), f(m, n).finally(() => {
			o(!1);
		}));
	}, [
		t,
		u,
		c,
		i
	]), import_react.useEffect(() => {
		u && (t ? s?.deviceId && p.current !== s?.deviceId ? (R.debug(`switching ${n} device from`, p.current, s.deviceId), E(u, s.deviceId)) : (R.debug(`unmuting local ${n} track`), u.unmute()) : (R.debug(`muting ${n} track`), u.mute().then(() => R.debug(u.mediaStreamTrack))));
	}, [
		u,
		s,
		t,
		n
	]), import_react.useEffect(() => () => {
		u && (R.debug(`stopping local ${n} track`), u.stop(), u.mute());
	}, []), import_react.useEffect(() => {
		d(l?.find((C) => C.deviceId === m));
	}, [m, l]), {
		selectedDevice: s,
		localTrack: u,
		deviceError: c
	};
}
function Ka({ defaults: t = {}, onValidate: a, onSubmit: n, onError: c, debug: r, joinLabel: i = "Join Room", micLabel: o = "Microphone", camLabel: l = "Camera", userLabel: s = "Username", persistUserChoices: d = !0, videoProcessor: u, ...h }) {
	const { userChoices: m, saveAudioInputDeviceId: g, saveAudioInputEnabled: f, saveVideoInputDeviceId: E, saveVideoInputEnabled: p, saveUsername: C } = Fu({
		defaults: t,
		preventSave: !d,
		preventLoad: !d
	}), [k, R$2] = import_react.useState(m), [M, D] = import_react.useState(k.audioEnabled), [v, w] = import_react.useState(k.videoEnabled), [b, T] = import_react.useState(k.audioDeviceId), [V, B] = import_react.useState(k.videoDeviceId), [x, N] = import_react.useState(k.username);
	import_react.useEffect(() => {
		f(M);
	}, [M, f]), import_react.useEffect(() => {
		p(v);
	}, [v, p]), import_react.useEffect(() => {
		g(b);
	}, [b, g]), import_react.useEffect(() => {
		E(V);
	}, [V, E]), import_react.useEffect(() => {
		C(x);
	}, [x, C]);
	const L = Oa({
		audio: M ? { deviceId: m.audioDeviceId } : !1,
		video: v ? {
			deviceId: m.videoDeviceId,
			processor: u
		} : !1
	}, c), F = import_react.useRef(null), A = import_react.useMemo(() => L?.filter((y) => y.kind === Track.Kind.Video)[0], [L]), _ = import_react.useMemo(() => {
		if (A) {
			const { facingMode: y } = facingModeFromLocalTrack(A);
			return y;
		} else return "undefined";
	}, [A]), H = import_react.useMemo(() => L?.filter((y) => y.kind === Track.Kind.Audio)[0], [L]);
	import_react.useEffect(() => (F.current && A && (A.unmute(), A.attach(F.current)), () => {
		A?.detach();
	}), [A]);
	const [Q, te] = import_react.useState(), G = import_react.useCallback((y) => typeof a == "function" ? a(y) : y.username !== "", [a]);
	import_react.useEffect(() => {
		const y = {
			username: x,
			videoEnabled: v,
			videoDeviceId: V,
			audioEnabled: M,
			audioDeviceId: b
		};
		R$2(y), te(G(y));
	}, [
		x,
		v,
		G,
		M,
		b,
		V
	]);
	function X(y) {
		y.preventDefault(), G(k) ? typeof n == "function" && n(k) : R.warn("Validation failed with: ", k);
	}
	return ge(), /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-prejoin",
		...h
	}, /* @__PURE__ */ import_react.createElement("div", { className: "lk-video-container" }, A && /* @__PURE__ */ import_react.createElement("video", {
		ref: F,
		width: "1280",
		height: "720",
		"data-lk-facing-mode": _
	}), (!A || !v) && /* @__PURE__ */ import_react.createElement("div", { className: "lk-camera-off-note" }, /* @__PURE__ */ import_react.createElement(Ve, null))), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-container" }, /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group audio" }, /* @__PURE__ */ import_react.createElement(j, {
		initialState: M,
		source: Track.Source.Microphone,
		onChange: (y) => D(y)
	}, o), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-menu" }, /* @__PURE__ */ import_react.createElement(Z, {
		initialSelection: b,
		kind: "audioinput",
		disabled: !H,
		tracks: { audioinput: H },
		onActiveDeviceChange: (y, U) => T(U)
	}))), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group video" }, /* @__PURE__ */ import_react.createElement(j, {
		initialState: v,
		source: Track.Source.Camera,
		onChange: (y) => w(y)
	}, l), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-menu" }, /* @__PURE__ */ import_react.createElement(Z, {
		initialSelection: V,
		kind: "videoinput",
		disabled: !A,
		tracks: { videoinput: A },
		onActiveDeviceChange: (y, U) => B(U)
	})))), /* @__PURE__ */ import_react.createElement("form", { className: "lk-username-container" }, /* @__PURE__ */ import_react.createElement("input", {
		className: "lk-form-control",
		id: "username",
		name: "username",
		type: "text",
		defaultValue: x,
		placeholder: s,
		onChange: (y) => N(y.target.value),
		autoComplete: "off"
	}), /* @__PURE__ */ import_react.createElement("button", {
		className: "lk-button lk-join-button",
		type: "submit",
		onClick: X,
		disabled: !Q
	}, i)), r && /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, /* @__PURE__ */ import_react.createElement("strong", null, "User Choices:"), /* @__PURE__ */ import_react.createElement("ul", {
		className: "lk-list",
		style: {
			overflow: "hidden",
			maxWidth: "15rem"
		}
	}, /* @__PURE__ */ import_react.createElement("li", null, "Username: ", `${k.username}`), /* @__PURE__ */ import_react.createElement("li", null, "Video Enabled: ", `${k.videoEnabled}`), /* @__PURE__ */ import_react.createElement("li", null, "Audio Enabled: ", `${k.audioEnabled}`), /* @__PURE__ */ import_react.createElement("li", null, "Video Device: ", `${k.videoDeviceId}`), /* @__PURE__ */ import_react.createElement("li", null, "Audio Device: ", `${k.audioDeviceId}`))));
}
function _a({ props: t }) {
	const { dispatch: a, state: n } = Qr().widget, c = "lk-button lk-settings-toggle";
	return { mergedProps: import_react.useMemo(() => pe(t, {
		className: c,
		onClick: () => {
			a && a({ msg: "toggle_settings" });
		},
		"aria-pressed": n?.showSettings ? "true" : "false"
	}), [
		t,
		c,
		a,
		n
	]) };
}
var $a = /* @__PURE__ */ import_react.forwardRef(function(a, n) {
	const { mergedProps: c } = _a({ props: a });
	return /* @__PURE__ */ import_react.createElement("button", {
		ref: n,
		...c
	}, a.children);
}), Ba = (t) => {
	switch (t) {
		case Track.Source.Camera: return 1;
		case Track.Source.Microphone: return 2;
		case Track.Source.ScreenShare: return 3;
		default: return 0;
	}
};
function Be({ variation: t, controls: a, saveUserChoices: n = !0, onDeviceError: c, ...r }) {
	const [i, o] = import_react.useState(!1), l = Xr();
	import_react.useEffect(() => {
		l?.widget.state?.showChat !== void 0 && o(l?.widget.state?.showChat);
	}, [l?.widget.state?.showChat]);
	const s = Qt(`(max-width: ${i ? 1e3 : 760}px)`) ? "minimal" : "verbose";
	t ?? (t = s);
	const d = {
		leave: !0,
		...a
	}, u = bu();
	if (!u) d.camera = !1, d.chat = !1, d.microphone = !1, d.screenShare = !1;
	else {
		const b = (T) => u.canPublish && (u.canPublishSources.length === 0 || u.canPublishSources.includes(Ba(T)));
		d.camera ?? (d.camera = b(Track.Source.Camera)), d.microphone ?? (d.microphone = b(Track.Source.Microphone)), d.screenShare ?? (d.screenShare = b(Track.Source.ScreenShare)), d.chat ?? (d.chat = u.canPublishData && a?.chat);
	}
	const h = import_react.useMemo(() => t === "minimal" || t === "verbose", [t]), m = import_react.useMemo(() => t === "textOnly" || t === "verbose", [t]), g = Zc(), [f, E] = import_react.useState(!1), p = import_react.useCallback((b) => {
		E(b);
	}, [E]), C = vu({ className: "lk-control-bar" }, r), { saveAudioInputEnabled: k, saveVideoInputEnabled: R, saveAudioInputDeviceId: M, saveVideoInputDeviceId: D } = Fu({ preventSave: !n }), v = import_react.useCallback((b, T) => T ? k(b) : null, [k]), w = import_react.useCallback((b, T) => T ? R(b) : null, [R]);
	return /* @__PURE__ */ import_react.createElement("div", C, d.microphone && /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group" }, /* @__PURE__ */ import_react.createElement(j, {
		source: Track.Source.Microphone,
		showIcon: h,
		onChange: v,
		onDeviceError: (b) => c?.({
			source: Track.Source.Microphone,
			error: b
		})
	}, m && "Microphone"), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-menu" }, /* @__PURE__ */ import_react.createElement(Z, {
		kind: "audioinput",
		onActiveDeviceChange: (b, T) => M(T ?? "default")
	}))), d.camera && /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group" }, /* @__PURE__ */ import_react.createElement(j, {
		source: Track.Source.Camera,
		showIcon: h,
		onChange: w,
		onDeviceError: (b) => c?.({
			source: Track.Source.Camera,
			error: b
		})
	}, m && "Camera"), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-menu" }, /* @__PURE__ */ import_react.createElement(Z, {
		kind: "videoinput",
		onActiveDeviceChange: (b, T) => D(T ?? "default")
	}))), d.screenShare && g && /* @__PURE__ */ import_react.createElement(j, {
		source: Track.Source.ScreenShare,
		captureOptions: {
			audio: !0,
			selfBrowserSurface: "include"
		},
		showIcon: h,
		onChange: p,
		onDeviceError: (b) => c?.({
			source: Track.Source.ScreenShare,
			error: b
		})
	}, m && (f ? "Stop screen share" : "Share screen")), d.chat && /* @__PURE__ */ import_react.createElement(Te, null, h && /* @__PURE__ */ import_react.createElement(Zt, null), m && "Chat"), d.settings && /* @__PURE__ */ import_react.createElement($a, null, h && /* @__PURE__ */ import_react.createElement(Yt, null), m && "Settings"), d.leave && /* @__PURE__ */ import_react.createElement(Pe, null, h && /* @__PURE__ */ import_react.createElement(ea, null), m && "Leave"), /* @__PURE__ */ import_react.createElement(Ne, null));
}
function Ja({ chatMessageFormatter: t, chatMessageDecoder: a, chatMessageEncoder: n, SettingsComponent: c, ...r }) {
	const [i, o] = import_react.useState({
		showChat: !1,
		unreadMessages: 0,
		showSettings: !1
	}), l = import_react.useRef(null), s = Du([{
		source: Track.Source.Camera,
		withPlaceholder: !0
	}, {
		source: Track.Source.ScreenShare,
		withPlaceholder: !1
	}], {
		updateOnlyOn: [RoomEvent.ActiveSpeakersChanged],
		onlySubscribed: !1
	}), d = (f) => {
		R.debug("updating widget state", f), o(f);
	}, u = nu(), h = s.filter(ee).filter((f) => f.publication.source === Track.Source.ScreenShare), m = Tu(u)?.[0], g = s.filter((f) => !Hc(f, m));
	return import_react.useEffect(() => {
		if (h.some((f) => f.publication.isSubscribed) && l.current === null ? (R.debug("Auto set screen share focus:", { newScreenShareTrack: h[0] }), u.pin.dispatch?.({
			msg: "set_pin",
			trackReference: h[0]
		}), l.current = h[0]) : l.current && !h.some((f) => f.publication.trackSid === l.current?.publication?.trackSid) && (R.debug("Auto clearing screen share focus."), u.pin.dispatch?.({ msg: "clear_pin" }), l.current = null), m && !ee(m)) {
			const f = s.find((E) => E.participant.identity === m.participant.identity && E.source === m.source);
			f !== m && ee(f) && u.pin.dispatch?.({
				msg: "set_pin",
				trackReference: f
			});
		}
	}, [
		h.map((f) => `${f.publication.trackSid}_${f.publication.isSubscribed}`).join(),
		m?.publication?.trackSid,
		s
	]), ge(), /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-video-conference",
		...r
	}, qc() && /* @__PURE__ */ import_react.createElement(Oe, {
		value: u,
		onWidgetChange: d
	}, /* @__PURE__ */ import_react.createElement("div", { className: "lk-video-conference-inner" }, m ? /* @__PURE__ */ import_react.createElement("div", { className: "lk-focus-layout-wrapper" }, /* @__PURE__ */ import_react.createElement(ba, null, /* @__PURE__ */ import_react.createElement(Ta, { tracks: g }, /* @__PURE__ */ import_react.createElement(ie, null)), m && /* @__PURE__ */ import_react.createElement(Sa, { trackRef: m }))) : /* @__PURE__ */ import_react.createElement("div", { className: "lk-grid-layout-wrapper" }, /* @__PURE__ */ import_react.createElement(Ma, { tracks: s }, /* @__PURE__ */ import_react.createElement(ie, null))), /* @__PURE__ */ import_react.createElement(Be, { controls: {
		chat: !0,
		settings: !!c
	} })), /* @__PURE__ */ import_react.createElement($e, {
		style: { display: i.showChat ? "grid" : "none" },
		messageFormatter: t,
		messageEncoder: n,
		messageDecoder: a
	}), c && /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-settings-menu-modal",
		style: { display: i.showSettings ? "block" : "none" }
	}, /* @__PURE__ */ import_react.createElement(c, null))), /* @__PURE__ */ import_react.createElement(Pa, null), /* @__PURE__ */ import_react.createElement(za, null));
}
function Za({ ...t }) {
	const [a, n] = import_react.useState({
		showChat: !1,
		unreadMessages: 0
	}), c = Du([Track.Source.Microphone]);
	return ge(), /* @__PURE__ */ import_react.createElement(Oe, { onWidgetChange: n }, /* @__PURE__ */ import_react.createElement("div", {
		className: "lk-audio-conference",
		...t
	}, /* @__PURE__ */ import_react.createElement("div", { className: "lk-audio-conference-stage" }, /* @__PURE__ */ import_react.createElement(ve, { tracks: c }, /* @__PURE__ */ import_react.createElement(Va, null))), /* @__PURE__ */ import_react.createElement(Be, { controls: {
		microphone: !0,
		screenShare: !1,
		camera: !1,
		chat: !0
	} }), a.showChat && /* @__PURE__ */ import_react.createElement($e, null)));
}
function Xa({ controls: t, saveUserChoices: a = !0, onDeviceError: n, ...c }) {
	const r = {
		leave: !0,
		microphone: !0,
		...t
	}, i = bu(), { microphoneTrack: o, localParticipant: l } = ci(), s = import_react.useMemo(() => ({
		participant: l,
		source: Track.Source.Microphone,
		publication: o
	}), [l, o]);
	i ? r.microphone ?? (r.microphone = i.canPublish) : r.microphone = !1;
	const d = vu({ className: "lk-agent-control-bar" }, c), { saveAudioInputEnabled: u, saveAudioInputDeviceId: h } = Fu({ preventSave: !a }), m = import_react.useCallback((g, f) => {
		f && u(g);
	}, [u]);
	return /* @__PURE__ */ import_react.createElement("div", d, r.microphone && /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group" }, /* @__PURE__ */ import_react.createElement(j, {
		source: Track.Source.Microphone,
		showIcon: !0,
		onChange: m,
		onDeviceError: (g) => n?.({
			source: Track.Source.Microphone,
			error: g
		})
	}, /* @__PURE__ */ import_react.createElement(_e, {
		trackRef: s,
		barCount: 7,
		options: { minHeight: 5 }
	})), /* @__PURE__ */ import_react.createElement("div", { className: "lk-button-group-menu" }, /* @__PURE__ */ import_react.createElement(Z, {
		kind: "audioinput",
		onActiveDeviceChange: (g, f) => h(f ?? "default")
	}))), r.leave && /* @__PURE__ */ import_react.createElement(Pe, null, "Disconnect"), /* @__PURE__ */ import_react.createElement(Ne, null));
}
//#endregion
//#region node_modules/@livekit/components-react/dist/index.mjs
var ts = /* @__PURE__ */ import_react.forwardRef(function(s, a) {
	const { buttonProps: o } = uu(s);
	return /* @__PURE__ */ import_react.createElement("button", {
		ref: a,
		...o
	}, s.children);
}), os = /* @__PURE__ */ import_react.forwardRef(function({ room: s, ...a }, o) {
	const n = tt(s);
	return /* @__PURE__ */ import_react.createElement("div", {
		ref: o,
		...a
	}, n);
}), ns = /* @__PURE__ */ import_react.forwardRef(function({ label: s = "Allow Audio", ...a }, o) {
	const { mergedProps: n } = Mu({
		room: oe(a.room),
		props: a
	});
	return /* @__PURE__ */ import_react.createElement("button", {
		ref: o,
		...n
	}, s);
}), is = /* @__PURE__ */ import_react.forwardRef(function({ trackRef: s, ...a }, o) {
	const u = Wu(pn(s), {
		bands: 7,
		loPass: 300
	});
	return /* @__PURE__ */ import_react.createElement("svg", {
		ref: o,
		width: "100%",
		height: "100%",
		viewBox: "0 0 200 90",
		...a,
		className: "lk-audio-visualizer"
	}, /* @__PURE__ */ import_react.createElement("rect", {
		x: "0",
		y: "0",
		width: "100%",
		height: "100%"
	}), /* @__PURE__ */ import_react.createElement("g", { style: { transform: `translate(${130 / 2}px, 0)` } }, u.map((r, c) => /* @__PURE__ */ import_react.createElement("rect", {
		key: c,
		x: c * 10,
		y: 90 / 2 - r * 50 / 2,
		width: 6,
		height: r * 50
	}))));
});
function rs({ participants: t, ...s }) {
	return /* @__PURE__ */ import_react.createElement(import_react.Fragment, null, t.map((a) => /* @__PURE__ */ import_react.createElement(ti.Provider, {
		value: a,
		key: a.identity
	}, mu(s.children))));
}
var cs = /* @__PURE__ */ import_react.forwardRef(function({ childrenPosition: s = "before", children: a, ...o }, n) {
	const { name: i } = ku();
	return /* @__PURE__ */ import_react.createElement("span", {
		ref: n,
		...o
	}, s === "before" && a, i, s === "after" && a);
});
function us(t) {
	return /* @__PURE__ */ import_react.createElement(ni.Provider, { value: t.session }, /* @__PURE__ */ import_react.createElement(hn.Provider, { value: t.session.room }, t.children));
}
//#endregion
export { Gu as AgentEvent, Za as AudioConference, he as AudioTrack, is as AudioVisualizer, _e as BarVisualizer, Gt as CameraDisabledIcon, Kt as CameraIcon, Ta as CarouselLayout, $e as Chat, Jt as ChatCloseIcon, Fa as ChatEntry, Zt as ChatIcon, Te as ChatToggle, Ee as Chevron, ts as ClearPinButton, Le as ConnectionQualityIndicator, os as ConnectionState, za as ConnectionStateToast, Be as ControlBar, zu as DEFAULT_MOOD_TTL_TURNS, Pe as DisconnectButton, Mc as EXPRESSION_ATTRIBUTE, Sa as FocusLayout, ba as FocusLayoutContainer, ua as FocusToggle, Xt as FocusToggleIcon, Yt as GearIcon, Ma as GridLayout, oi as LKFeatureContext, Yr as LayoutContext, Oe as LayoutContextProvider, ea as LeaveIcon, tl as LiveKitRoom, ta as LockLockedIcon, Z as MediaDeviceMenu, ce as MediaDeviceSelect, Xu as MessagesEvent, aa as MicDisabledIcon, na as MicIcon, Va as ParticipantAudioTile, ti as ParticipantContext, Ea as ParticipantContextIfNeeded, rs as ParticipantLoop, le as ParticipantName, Ve as ParticipantPlaceholder, ie as ParticipantTile, Ka as PreJoin, ra as QualityExcellentIcon, ca as QualityGoodIcon, la as QualityPoorIcon, ia as QualityUnknownIcon, Pa as RoomAudioRenderer, hn as RoomContext, cs as RoomName, Ae as ScreenShareIcon, sa as ScreenShareStopIcon, Ju as SessionEvent, us as SessionProvider, ke as SpinnerIcon, ns as StartAudio, Ne as StartMediaButton, Aa as Toast, ve as TrackLoop, De as TrackMutedIndicator, Zr as TrackRefContext, ka as TrackRefContextIfNeeded, j as TrackToggle, oa as UnfocusToggleIcon, Ja as VideoConference, pa as VideoTrack, Xa as VoiceAssistantControlBar, Qa as formatChatMessageLinks, ee as isTrackReference, Nc as isUseSessionReturn, qn as parseExpression, Xc as setLogExtension, Qc as setLogLevel, li as useAgent, qu as useAgentExpression, cu as useAudioPlayback, ju as useAudioWaveform, gc as useChat, Ou as useChatToggle, uu as useClearPinButton, lu as useConnectionQualityIndicator, tt as useConnectionState, nu as useCreateLayoutContext, fu as useDataChannel, du as useDisconnectButton, ru as useEnsureCreateLayoutContext, nc as useEnsureLayoutContext, Ie as useEnsureParticipant, oe as useEnsureRoom, ii as useEnsureSession, pn as useEnsureTrackRef, Qu as useEvents, ic as useFacingMode, au as useFeatureContext, pu as useFocusToggle, hu as useGridLayout, Uu as useIsEncrypted, Vn as useIsMuted, Hu as useIsRecording, oc as useIsSpeaking, Qr as useLayoutContext, uc as useLiveKitRoom, ci as useLocalParticipant, bu as useLocalParticipantPermissions, Xr as useMaybeLayoutContext, Ot as useMaybeParticipantContext, It as useMaybeRoomContext, ri as useMaybeSessionContext, ei as useMaybeTrackRefContext, yu as useMediaDeviceSelect, wu as useMediaDevices, Wu as useMultibandTrackVolume, Su as usePagination, Bu as useParticipantAttribute, kc as useParticipantAttributes, ou as useParticipantContext, fc as useParticipantInfo, Eu as useParticipantPermissions, Cu as useParticipantTile, St as useParticipantTracks, dc as useParticipants, Fu as usePersistentUserChoices, Tu as usePinnedTracks, Ga as usePreviewDevice, Oa as usePreviewTracks, xu as useRemoteParticipant, vn as useRemoteParticipants, _t as useRoomContext, ku as useRoomInfo, el as useRpc, Ku as useSequentialRoomConnectDisconnect, Yu as useSession, su as useSessionContext, Zu as useSessionMessages, Pu as useSortedParticipants, pc as useSpeakingParticipants, Mu as useStartAudio, Au as useStartVideo, Lu as useSwipe, Pc as useTextStream, _u as useToken, Nu as useTrackByName, Iu as useTrackMutedIndicator, iu as useTrackRefContext, Ru as useTrackToggle, xc as useTrackTranscription, $u as useTrackVolume, Du as useTracks, ui as useTranscriptions, lc as useVisualStableUpdate, Vu as useVoiceAssistant };

//# sourceMappingURL=@livekit_components-react.js.map