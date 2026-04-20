/**
 * dna-bg.js — Skinalyze
 * High-performance DNA double-helix animation.
 * Palette: bluish-purple · vibrant pink · dark emerald · rich maroon
 *
 * ── QUICK TWEAKS ───────────────────────────────────────────────
 *  PALETTE     → 4-color cycle for rungs & nodes
 *  NODES       → number of rungs  (more = denser helix)
 *  AMPLITUDE   → horizontal swing width in px
 *  GAP         → half-distance between the two backbones
 *  SPEED       → animation speed  (0.01 slow ↔ 0.03 fast)
 *  CANVAS_W/H  → logical canvas size (mirror in Home.css #dnaBgCanvas)
 *  Position & rotation are set in Home.css › #dnaBgCanvas
 * ───────────────────────────────────────────────────────────────
 */
(function () {
    'use strict';

    /* ── Canvas size ─────────────────────────────────────────── */
    const CANVAS_W = 340;
    const CANVAS_H = 920;

    /* ── Helix geometry ──────────────────────────────────────── */
    const NODES = 26;    // number of base-pair rungs
    const AMPLITUDE = 52;    // horizontal swing of sine wave (px)
    const GAP = 36;    // half-gap between the two backbone strands (px)
    const SPEED = 0.016; // radians per frame

    /* ── 4-color palette ─────────────────────────────────────────
       Each entry controls one rung + its two nodes.
       Alternates across the NODES array (i % 4).
         stroke  → rung connecting line + dark node centre
         node    → outer glow ring + mid fill
    ── */
    const PALETTE = [
        { stroke: '#4B3FA0', node: '#6C5FD4' },   // deep bluish-purple
        { stroke: '#D6186A', node: '#F02480' },   // vibrant pink
        { stroke: '#0D5F4A', node: '#1a8a68' },   // dark emerald
        { stroke: '#850E35', node: '#B01448' },   // rich maroon
    ];

    /* ── Backbone gradient (top → bottom of canvas) ─────────────
       Cycles through the 4 palette hues so the strands themselves
       carry the multi-color identity.
    ── */
    const GRAD_STOPS = [
        { pos: 0.00, color: 'rgba( 75,  63, 160, 0.92)' },  // purple
        { pos: 0.28, color: 'rgba(214,  24, 106, 0.85)' },  // pink
        { pos: 0.55, color: 'rgba( 13,  95,  74, 0.82)' },  // emerald
        { pos: 0.80, color: 'rgba(133,  14,  53, 0.78)' },  // maroon
        { pos: 1.00, color: 'rgba( 75,  63, 160, 0.58)' },  // purple fade
    ];

    /* ── Node sizes (px, before per-node scale factor) ──────── */
    const R_GLOW = 9.0;   // outer glow halo radius
    const R_MID = 6.0;   // mid fill ring
    const R_INNER = 3.5;   // solid centre dot

    /* ── Backbone stroke weight ──────────────────────────────── */
    const STRAND_WIDTH = 3.2;
    const RUNG_WIDTH = 2.2;

    let canvas, ctx, phase = 0;

    /* ── Init ─────────────────────────────────────────────────── */
    function init() {
        canvas = document.getElementById('dnaBgCanvas');
        if (!canvas) return;

        const dpr = window.devicePixelRatio || 1;
        canvas.width = CANVAS_W * dpr;
        canvas.height = CANVAS_H * dpr;
        canvas.style.width = CANVAS_W + 'px';
        canvas.style.height = CANVAS_H + 'px';

        ctx = canvas.getContext('2d');
        ctx.scale(dpr, dpr);

        requestAnimationFrame(loop);
    }

    function loop() {
        phase += SPEED;
        draw();
        requestAnimationFrame(loop);
    }

    /* ── Gradient factory ─────────────────────────────────────── */
    function makeGrad() {
        const g = ctx.createLinearGradient(0, 0, CANVAS_W, CANVAS_H);
        GRAD_STOPS.forEach(s => g.addColorStop(s.pos, s.color));
        return g;
    }

    /* ── Compute node positions for this frame ───────────────── */
    function getNodes() {
        const cx = CANVAS_W / 2;
        const spacing = CANVAS_H / (NODES + 1);
        const L = [], R = [];
        for (let i = 0; i < NODES; i++) {
            const y = spacing + i * spacing;
            const wave = Math.sin(phase + i * (Math.PI * 2 / NODES));
            L.push({ x: cx - GAP - AMPLITUDE * wave, y });
            R.push({ x: cx + GAP + AMPLITUDE * wave, y });
        }
        return { L, R };
    }

    /* ── Draw smooth backbone curve ──────────────────────────── */
    function drawCurve(nodes, grad) {
        ctx.save();
        ctx.globalAlpha = 0.85;
        ctx.strokeStyle = grad;
        ctx.lineWidth = STRAND_WIDTH;
        ctx.lineJoin = 'round';
        ctx.setLineDash([]);
        ctx.beginPath();
        ctx.moveTo(nodes[0].x, nodes[0].y);
        for (let i = 1; i < nodes.length - 1; i++) {
            const mx = (nodes[i].x + nodes[i + 1].x) / 2;
            const my = (nodes[i].y + nodes[i + 1].y) / 2;
            ctx.quadraticCurveTo(nodes[i].x, nodes[i].y, mx, my);
        }
        ctx.lineTo(nodes[nodes.length - 1].x, nodes[nodes.length - 1].y);
        ctx.stroke();
        ctx.restore();
    }

    /* ── Draw dashed rungs with alternating palette color ────── */
    function drawRungs(L, R) {
        for (let i = 0; i < NODES; i++) {
            const col = PALETTE[i % PALETTE.length];
            const wave = Math.sin(phase + i * (Math.PI * 2 / NODES));
            const alpha = 0.35 + 0.52 * Math.abs(wave);

            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.strokeStyle = col.stroke;
            ctx.lineWidth = RUNG_WIDTH;
            ctx.setLineDash([5, 4]);
            ctx.lineCap = 'round';
            ctx.beginPath();
            ctx.moveTo(L[i].x, L[i].y);
            ctx.lineTo(R[i].x, R[i].y);
            ctx.stroke();
            ctx.restore();
        }
    }

    /* ── Draw a single joint node (3-layer: glow › mid › core) ─ */
    function drawNode(pt, paletteIdx, scale) {
        const col = PALETTE[paletteIdx % PALETTE.length];

        // outer glow halo
        ctx.save();
        ctx.globalAlpha = 0.28 * scale;
        ctx.fillStyle = col.node;
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, R_GLOW * scale, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // mid fill ring
        ctx.save();
        ctx.globalAlpha = 0.58 * scale;
        ctx.fillStyle = col.node;
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, R_MID * scale, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // solid inner core
        ctx.save();
        ctx.globalAlpha = 0.96 * scale;
        ctx.fillStyle = col.stroke;
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, R_INNER * scale, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }

    /* ── Main draw call ──────────────────────────────────────── */
    function draw() {
        ctx.clearRect(0, 0, CANVAS_W, CANVAS_H);

        const { L, R } = getNodes();
        const grad = makeGrad();

        drawCurve(L, grad);
        drawCurve(R, grad);
        drawRungs(L, R);

        for (let i = 0; i < NODES; i++) {
            const wave = Math.sin(phase + i * (Math.PI * 2 / NODES));
            const scale = 0.72 + 0.28 * Math.abs(wave);
            // Left strand node uses palette index i, right uses i+2 → different color pairs
            drawNode(L[i], i, scale);
            drawNode(R[i], i + 2, scale);
        }
    }

    /* ── Boot ─────────────────────────────────────────────────── */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();