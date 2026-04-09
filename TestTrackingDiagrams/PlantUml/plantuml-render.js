// PlantUML Node.js renderer for TestTrackingDiagrams CI summaries
// Usage: node plantuml-render.js <viz-global.js-path> <plantuml.js-path>
// stdin: PlantUML source code (one diagram)
// stdout: SVG output
//
// Both viz-global.js and plantuml.js are designed for browsers (<script> tags).
// We load them via vm.runInThisContext to simulate browser <script> tag loading,
// avoiding CJS module wrapping that breaks their UMD/global interactions.
//
// CRITICAL: Viz.js compiles Graphviz WASM asynchronously. We pre-compile it and
// cache the instance so plantuml.js gets a ready-to-use Viz.

'use strict';

var vm = require('vm');
var fs = require('fs');
var path = require('path');
var urlModule = require('url');

// --- Minimal DOM polyfills for plantuml.js (TeaVM-compiled) ---

class MockElement {
    constructor(tag) {
        this.tagName = (tag || 'DIV').toUpperCase();
        this.id = '';
        this.innerHTML = '';
        this.outerHTML = '';
        this.textContent = '';
        this.style = {};
        this.childNodes = [];
        this.children = [];
        this.parentNode = null;
        this.ownerDocument = null;
        this.namespaceURI = null;
        this._attributes = {};
    }
    setAttribute(name, value) { this._attributes[name] = value; }
    getAttribute(name) { return this._attributes[name] || null; }
    removeAttribute(name) { delete this._attributes[name]; }
    hasAttribute(name) { return name in this._attributes; }
    appendChild(child) {
        if (typeof child === 'object' && child !== null) {
            child.parentNode = this;
            this.childNodes.push(child);
            this.children.push(child);
        }
        return child;
    }
    removeChild(child) {
        this.childNodes = this.childNodes.filter(c => c !== child);
        this.children = this.children.filter(c => c !== child);
        return child;
    }
    insertBefore(newChild, refChild) { return this.appendChild(newChild); }
    replaceChild(newChild, oldChild) { this.removeChild(oldChild); return this.appendChild(newChild); }
    cloneNode() { return new MockElement(this.tagName); }
    querySelector(sel) {
        if (sel && sel.startsWith('#')) return global._mockElements[sel.slice(1)] || null;
        return null;
    }
    querySelectorAll() { return []; }
    getElementsByTagName() { return []; }
    getElementsByClassName() { return []; }
    addEventListener() {}
    removeEventListener() {}
    dispatchEvent() {}
    getBoundingClientRect() { return { x: 0, y: 0, width: 100, height: 100, top: 0, left: 0, bottom: 100, right: 100 }; }
    getBBox() {
        // PlantUML calls getBBox() on SVG text elements for layout measurement.
        var fontSize = parseFloat(this.style && this.style.fontSize) || 14;
        var text = this.textContent || '';
        return { x: 0, y: 0, width: text.length * fontSize * 0.6, height: fontSize * 1.2 };
    }
    getContext(type) {
        // PlantUML calls canvas.getContext('2d') for text measurement.
        if (type === '2d') {
            var ctx = {
                font: '10px sans-serif',
                measureText: function(text) {
                    var fontSize = parseFloat(ctx.font) || 10;
                    return { width: text.length * fontSize * 0.6 };
                },
                fillText: function() {},
                clearRect: function() {},
                fillRect: function() {},
                strokeRect: function() {},
                beginPath: function() {},
                closePath: function() {},
                moveTo: function() {},
                lineTo: function() {},
                stroke: function() {},
                fill: function() {},
                save: function() {},
                restore: function() {},
                scale: function() {},
                translate: function() {},
                rotate: function() {},
                arc: function() {},
                createLinearGradient: function() { return { addColorStop: function() {} }; },
                fillStyle: '',
                strokeStyle: '',
                lineWidth: 1,
                canvas: this
            };
            return ctx;
        }
        return null;
    }
    focus() {}
    blur() {}
}

// Serialize a MockElement tree to an SVG/HTML string.
function serializeElement(el) {
    if (!el || typeof el !== 'object') return '';
    if (el.nodeType === 3) return el.textContent || el.data || '';
    var tag = (el.tagName || 'div').toLowerCase();
    var attrs = '';
    if (el._attributes) {
        for (var k in el._attributes) {
            attrs += ' ' + k + '="' + el._attributes[k] + '"';
        }
    }
    var children = '';
    if (el.childNodes && el.childNodes.length > 0) {
        for (var i = 0; i < el.childNodes.length; i++) {
            children += serializeElement(el.childNodes[i]);
        }
    }
    var text = (!el.childNodes || el.childNodes.length === 0) ? (el.textContent || '') : '';
    return '<' + tag + attrs + '>' + text + children + '</' + tag + '>';
}

var mockDocument = {
    getElementById: function(id) {
        return global._mockElements[id] || null;
    },
    querySelector: function(sel) {
        if (sel && sel.startsWith('#')) return global._mockElements[sel.slice(1)] || null;
        return null;
    },
    createElement: function(tag) {
        var el = new MockElement(tag);
        el.ownerDocument = mockDocument;
        return el;
    },
    createElementNS: function(ns, tag) {
        var el = new MockElement(tag);
        el.ownerDocument = mockDocument;
        el.namespaceURI = ns;
        return el;
    },
    createTextNode: function(text) {
        return { nodeType: 3, nodeValue: text, textContent: text, data: text };
    },
    createDocumentFragment: function() { return new MockElement('fragment'); },
    createEvent: function() { return { initEvent: function() {} }; },
    body: new MockElement('body'),
    head: new MockElement('head'),
    documentElement: new MockElement('html'),
    addEventListener: function() {},
    removeEventListener: function() {},
    querySelectorAll: function() { return []; },
    implementation: { createHTMLDocument: function() { return mockDocument; } },
    currentScript: null,
    baseURI: 'about:blank'
};

// --- Arguments ---

var vizPath = process.argv[2];
var plantumlPath = process.argv[3];

if (!vizPath || !plantumlPath) {
    process.stderr.write('Usage: node plantuml-render.js <viz-global.js> <plantuml.js>\n');
    process.exit(1);
}

// --- Set up browser-like globals ---

global.self = global;
global.window = global;
global.document = mockDocument;
// Node.js v24+ makes navigator read-only on globalThis
try { global.navigator = { userAgent: 'node', platform: 'node' }; }
catch (_) { Object.defineProperty(global, 'navigator', { value: { userAgent: 'node', platform: 'node' }, configurable: true, writable: true }); }
global.HTMLElement = MockElement;
global.SVGElement = MockElement;
global.Element = MockElement;
global.Node = MockElement;
global.DOMParser = class {
    parseFromString(str) {
        var el = new MockElement('div');
        el.innerHTML = str;
        el._svgContent = str;
        return {
            documentElement: el,
            firstChild: el,
            querySelector: function() { return el; },
            querySelectorAll: function() { return [el]; }
        };
    }
};
global.XMLSerializer = class {
    serializeToString(node) {
        return node.outerHTML || node.innerHTML || node._svgContent || '';
    }
};
global._mockElements = {};

process.on('unhandledRejection', function(err) {
    process.stderr.write('UNHANDLED REJECTION: ' + (err && err.stack || err) + '\n');
});
process.on('uncaughtException', function(err) {
    process.stderr.write('UNCAUGHT EXCEPTION: ' + (err && err.stack || err) + '\n');
    process.exit(1);
});

// --- Load scripts like <script> tags (no CJS module wrapping) ---

function loadScript(filePath) {
    var code = fs.readFileSync(filePath, 'utf8');
    vm.runInThisContext(code, { filename: filePath });
}

// Read stdin in parallel with initialization
var inputResolve;
var inputPromise = new Promise(function(resolve) { inputResolve = resolve; });
var input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', function(chunk) { input += chunk; });
process.stdin.on('end', function() { inputResolve(input); });

// --- Phase 1: Load viz-global.js ---
var vizScript = new MockElement('script');
vizScript.src = urlModule.pathToFileURL(path.resolve(vizPath)).href;
mockDocument.currentScript = vizScript;
mockDocument.baseURI = urlModule.pathToFileURL(process.cwd()).href + '/';

loadScript(vizPath);
mockDocument.currentScript = null;

// --- Phase 2: Wait for WASM to compile BEFORE loading plantuml.js ---
var vizReady = globalThis.Viz.instance().then(function(viz) {
    var testSvg = viz.renderString('digraph { a -> b }', { format: 'svg' });
    if (!testSvg || testSvg.indexOf('<svg') === -1) {
        process.stderr.write('Viz test render failed\n');
        process.exit(1);
    }
    // Cache the instance so plantuml.js Viz.instance() calls reuse it
    var origViz = globalThis.Viz;
    globalThis.Viz = new Proxy(origViz, {
        get: function(target, prop) {
            if (prop === 'instance') {
                return function() { return Promise.resolve(viz); };
            }
            return target[prop];
        }
    });
    return viz;
}).catch(function(err) {
    process.stderr.write('Viz WASM init failed: ' + (err && err.stack || err) + '\n');
    process.exit(1);
});

// --- Phase 3: Once WASM is ready, load plantuml.js and render ---
Promise.all([vizReady, inputPromise]).then(function(results) {
    var plantUml = results[1];

    loadScript(plantumlPath);

    var loadFn = globalThis.plantumlLoad;
    if (!loadFn) {
        process.stderr.write('ERROR: plantumlLoad not found after loading plantuml.js\n');
        process.exit(1);
    }

    // Set up render target
    var targetId = '_render_target';
    var target = new MockElement('div');
    target.id = targetId;
    target.ownerDocument = mockDocument;
    global._mockElements[targetId] = target;

    // Track SVG result via innerHTML setter and appendChild
    var svgResult = '';
    Object.defineProperty(target, 'innerHTML', {
        get: function() { return svgResult; },
        set: function(value) { svgResult = value; },
        configurable: true
    });
    var origAppendChild = target.appendChild.bind(target);
    target.appendChild = function(child) {
        origAppendChild(child);
        // PlantUML builds SVG via DOM APIs; serialize the appended tree
        var s = serializeElement(child);
        if (s && s.indexOf('<svg') !== -1) {
            svgResult = s;
        }
        return child;
    };

    loadFn([], function() {
        var renderer = globalThis.plantuml;
        if (!renderer || typeof renderer.render !== 'function') {
            process.stderr.write('ERROR: plantuml.render not available\n');
            process.exit(1);
        }

        var lines = plantUml.replace(/\r\n/g, '\n').trim().split('\n');

        try {
            renderer.render(lines, targetId);
        } catch (e) {
            process.stderr.write('ERROR during render: ' + (e.stack || e.message) + '\n');
            process.exit(1);
        }

        // Check synchronous result
        if (svgResult && svgResult.indexOf('<svg') !== -1) {
            process.stdout.write(svgResult);
            process.exit(0);
        }

        // Poll for async result (plantuml.js renders asynchronously via TeaVM threads)
        var attempts = 0;
        var maxAttempts = 400; // 20 seconds max
        var check = function() {
            // Also check target's children in case appendChild wasn't intercepted
            if (!svgResult || svgResult.indexOf('<svg') === -1) {
                if (target.childNodes.length > 0) {
                    var built = '';
                    for (var i = 0; i < target.childNodes.length; i++) {
                        built += serializeElement(target.childNodes[i]);
                    }
                    if (built && built.indexOf('<svg') !== -1) {
                        svgResult = built;
                    }
                }
            }
            if (svgResult && svgResult.indexOf('<svg') !== -1) {
                process.stdout.write(svgResult);
                process.exit(0);
            }
            if (++attempts > maxAttempts) {
                process.stderr.write('ERROR: Timed out waiting for SVG render (' + maxAttempts * 50 + 'ms)\n');
                process.exit(1);
            }
            setTimeout(check, 50);
        };
        setTimeout(check, 50);
    });
}).catch(function(err) {
    process.stderr.write('FATAL: ' + (err && err.stack || err) + '\n');
    process.exit(1);
});
