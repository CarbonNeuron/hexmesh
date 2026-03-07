// src/HexMesh.Server/wwwroot/js/hexRenderer.js

const SQRT3 = Math.sqrt(3);
const MIN_SCALE = 0.1;
const MAX_SCALE = 5.0;
const CHUNK_SIZE = 16;

let app = null;
let worldContainer = null;
let hexSize = 20;
let hexGraphics = new Map(); // key: "q,r" -> Graphics
let dotNetRef = null;
let subscribedChunks = new Set();
let isDragging = false;
let lastPointer = { x: 0, y: 0 };

export async function initialize(canvasId, dotNetReference, hexSizeParam) {
    dotNetRef = dotNetReference;
    hexSize = hexSizeParam || 20;

    const container = document.getElementById(canvasId);
    if (!container) return;

    app = new PIXI.Application();
    await app.init({
        resizeTo: container,
        background: 0x1a1a2e,
        antialias: true,
        resolution: window.devicePixelRatio || 1,
        autoDensity: true,
    });
    container.appendChild(app.canvas);

    worldContainer = new PIXI.Container();
    app.stage.addChild(worldContainer);

    // Center the world in the viewport
    worldContainer.x = app.screen.width / 2;
    worldContainer.y = app.screen.height / 2;

    setupInteraction(container);
    updateChunkSubscriptions();
}

function setupInteraction(container) {
    const canvas = app.canvas;

    canvas.addEventListener('pointerdown', (e) => {
        isDragging = true;
        lastPointer = { x: e.clientX, y: e.clientY };
        canvas.setPointerCapture(e.pointerId);
    });

    canvas.addEventListener('pointermove', (e) => {
        if (!isDragging) return;
        const dx = e.clientX - lastPointer.x;
        const dy = e.clientY - lastPointer.y;
        worldContainer.x += dx;
        worldContainer.y += dy;
        lastPointer = { x: e.clientX, y: e.clientY };
        onViewportChanged();
    });

    canvas.addEventListener('pointerup', (e) => {
        isDragging = false;
    });

    canvas.addEventListener('wheel', (e) => {
        e.preventDefault();
        const scaleFactor = e.deltaY < 0 ? 1.1 : 0.9;
        const newScale = Math.max(MIN_SCALE, Math.min(MAX_SCALE, worldContainer.scale.x * scaleFactor));

        // Zoom toward mouse position
        const rect = canvas.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const worldBefore = {
            x: (mouseX - worldContainer.x) / worldContainer.scale.x,
            y: (mouseY - worldContainer.y) / worldContainer.scale.y,
        };

        worldContainer.scale.set(newScale);

        worldContainer.x = mouseX - worldBefore.x * newScale;
        worldContainer.y = mouseY - worldBefore.y * newScale;

        onViewportChanged();
    }, { passive: false });

    // Handle resize
    const resizeObserver = new ResizeObserver(() => {
        app.resize();
        onViewportChanged();
    });
    resizeObserver.observe(container);
}

let viewportDebounceTimer = null;

function onViewportChanged() {
    if (viewportDebounceTimer) clearTimeout(viewportDebounceTimer);
    viewportDebounceTimer = setTimeout(() => {
        updateChunkSubscriptions();
    }, 50);
}

function getVisibleChunks() {
    const scale = worldContainer.scale.x;
    const w = app.screen.width;
    const h = app.screen.height;

    const left = -worldContainer.x / scale;
    const top = -worldContainer.y / scale;
    const right = (w - worldContainer.x) / scale;
    const bottom = (h - worldContainer.y) / scale;

    const margin = hexSize * 2;
    const minQ = pixelToHexQ(left - margin, top - margin);
    const maxQ = pixelToHexQ(right + margin, bottom + margin);
    const minR = pixelToHexR(left - margin, top - margin);
    const maxR = pixelToHexR(right + margin, bottom + margin);

    const minCQ = floorDiv(minQ, CHUNK_SIZE);
    const maxCQ = floorDiv(maxQ, CHUNK_SIZE);
    const minCR = floorDiv(minR, CHUNK_SIZE);
    const maxCR = floorDiv(maxR, CHUNK_SIZE);

    const chunks = new Set();
    for (let cq = minCQ; cq <= maxCQ; cq++) {
        for (let cr = minCR; cr <= maxCR; cr++) {
            chunks.add(`${cq},${cr}`);
        }
    }
    return chunks;
}

function pixelToHexQ(x, y) {
    return Math.floor((SQRT3 / 3 * x - 1 / 3 * y) / hexSize);
}

function pixelToHexR(x, y) {
    return Math.floor((2 / 3 * y) / hexSize);
}

function floorDiv(a, b) {
    return Math.floor(a / b);
}

async function updateChunkSubscriptions() {
    if (!dotNetRef) return;

    const visible = getVisibleChunks();

    const toSubscribe = [];
    const toUnsubscribe = [];

    for (const key of visible) {
        if (!subscribedChunks.has(key)) {
            toSubscribe.push(key);
        }
    }
    for (const key of subscribedChunks) {
        if (!visible.has(key)) {
            toUnsubscribe.push(key);
        }
    }

    if (toSubscribe.length === 0 && toUnsubscribe.length === 0) return;

    subscribedChunks = visible;

    for (const key of toUnsubscribe) {
        removeChunkGraphics(key);
    }

    try {
        await dotNetRef.invokeMethodAsync('OnChunkSubscriptionsChanged',
            toSubscribe.map(parseChunkKey),
            toUnsubscribe.map(parseChunkKey)
        );
    } catch (e) {
        console.error('Failed to update chunk subscriptions:', e);
    }
}

function parseChunkKey(key) {
    const [q, r] = key.split(',').map(Number);
    return { q, r };
}

function chunkKey(q, r) {
    return `${q},${r}`;
}

function removeChunkGraphics(chunkKeyStr) {
    const toRemove = [];
    for (const [key, gfx] of hexGraphics) {
        if (gfx._chunkKey === chunkKeyStr) {
            toRemove.push(key);
        }
    }
    for (const key of toRemove) {
        const gfx = hexGraphics.get(key);
        worldContainer.removeChild(gfx);
        gfx.destroy();
        hexGraphics.delete(key);
    }
}

function hexToPixel(q, r) {
    const x = hexSize * (SQRT3 * q + SQRT3 / 2 * r);
    const y = hexSize * (3 / 2 * r);
    return { x, y };
}

function drawHex(q, r, color, chunkQ, chunkR) {
    const key = `${q},${r}`;
    let gfx = hexGraphics.get(key);

    if (gfx) {
        gfx.clear();
    } else {
        gfx = new PIXI.Graphics();
        const pos = hexToPixel(q, r);
        gfx.x = pos.x;
        gfx.y = pos.y;
        gfx._chunkKey = chunkKey(chunkQ, chunkR);
        worldContainer.addChild(gfx);
        hexGraphics.set(key, gfx);
    }

    const points = [];
    for (let i = 0; i < 6; i++) {
        const angle = (Math.PI / 180) * (60 * i - 30);
        points.push(hexSize * Math.cos(angle));
        points.push(hexSize * Math.sin(angle));
    }

    gfx.poly(points, true);
    gfx.fill({ color: color });
    gfx.stroke({ width: 1, color: 0x2a2a4a });
}

function removeHex(q, r) {
    const key = `${q},${r}`;
    const gfx = hexGraphics.get(key);
    if (gfx) {
        worldContainer.removeChild(gfx);
        gfx.destroy();
        hexGraphics.delete(key);
    }
}

export function applyDeltas(deltas) {
    for (const delta of deltas) {
        if (delta.cleared) {
            removeHex(delta.q, delta.r);
        } else {
            drawHex(delta.q, delta.r, delta.color, delta.chunkQ, delta.chunkR);
        }
    }
}

export function loadChunkData(chunkQ, chunkR, cells) {
    for (const cell of cells) {
        drawHex(cell.q, cell.r, cell.color, chunkQ, chunkR);
    }
}

export function dispose() {
    if (app) {
        app.destroy(true);
        app = null;
    }
    hexGraphics.clear();
    subscribedChunks.clear();
}
