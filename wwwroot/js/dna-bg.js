/**
 * dna-bg.js — Skinalyze
 * True 3D projection Luminous Helix in Dark Emerald
 */
(function () {
    'use strict';
    let canvas, ctx, phase = 0;

    const SPEED = 0.012;
    const BRAND_COLOR = '#0D5F4A'; // Your button's dark green color

    function init() {
        canvas = document.getElementById('dnaBgCanvas');
        if (!canvas) return;
        ctx = canvas.getContext('2d');

        resize();
        window.addEventListener('resize', resize);

        requestAnimationFrame(loop);
    }

    function resize() {
        const dpr = window.devicePixelRatio || 1;
        canvas.width = canvas.clientWidth * dpr;
        canvas.height = canvas.clientHeight * dpr;
        ctx.scale(dpr, dpr);
    }

    function loop() {
        phase += SPEED;
        draw();
        requestAnimationFrame(loop);
    }

    function draw() {
        const w = canvas.clientWidth;
        const h = canvas.clientHeight;
        ctx.clearRect(0, 0, w, h);

        const nodes = 45; // Density of the helix
        const radius = w * 0.35; // Overall width of the helix cylinder
        const cx = w / 2;

        for (let i = 0; i < nodes; i++) {
            const progress = i / (nodes - 1);
            const y = progress * h;

            // Angle for the twist (controls how many twists fit on screen)
            const angle = phase + progress * Math.PI * 3                                                                                                                                                                                                                                    ;

            // 3D coordinates using true circular math
            // Math.cos controls left/right position
            // Math.sin controls front/back depth
            const x1 = cx + radius * Math.cos(angle);
            const z1 = Math.sin(angle);

            const x2 = cx + radius * Math.cos(angle + Math.PI); // Opposite side of the circle
            const z2 = Math.sin(angle + Math.PI);

            // Calculate sizes based on depth (larger in front, smaller in back)
            const baseSize = 8;
            const variance = 5;
            const r1 = Math.max(1, baseSize + z1 * variance);
            const r2 = Math.max(1, baseSize + z2 * variance);

            // Calculate opacities based on depth (more opaque in front)
            const alpha1 = 0.4 + 0.6 * ((z1 + 1) / 2);
            const alpha2 = 0.4 + 0.6 * ((z2 + 1) / 2);

            // Apply global glow
            ctx.shadowBlur = 12;
            ctx.shadowColor = BRAND_COLOR;

            // 1. Draw the connecting rung first (in the background)
            ctx.beginPath();
            ctx.moveTo(x1, y);
            ctx.lineTo(x2, y);
            ctx.strokeStyle = BRAND_COLOR;
            ctx.globalAlpha = 0.3; // Keep rungs slightly faded
            ctx.lineWidth = 2.5;
            ctx.stroke();

            // 2. Depth sorting: Draw the back node first, then the front node over it
            if (z1 < z2) {
                drawDot(x1, y, r1, alpha1); // Node 1 is in back
                drawDot(x2, y, r2, alpha2); // Node 2 is in front
            } else {
                drawDot(x2, y, r2, alpha2); // Node 2 is in back
                drawDot(x1, y, r1, alpha1); // Node 1 is in front
            }
        }

        // Reset canvas state
        ctx.globalAlpha = 1.0;
        ctx.shadowBlur = 0;
    }

    // Helper function to draw the individual nodes
    function drawDot(x, y, r, alpha) {
        ctx.beginPath();
        ctx.arc(x, y, r, 0, Math.PI * 2);
        ctx.fillStyle = BRAND_COLOR;
        ctx.globalAlpha = alpha;
        ctx.fill();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();