// PlantUML Node.js renderer for TestTrackingDiagrams CI summaries
// Usage: node plantuml-render.js <viz-global.js-path> <plantuml.js-path>
// stdin: PlantUML source code (one diagram)
// stdout: SVG output

'use strict';

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
    querySelector() { return null; }
    querySelectorAll() { return []; }
    getElementsByTagName() { return []; }
    getElementsByClassName() { return []; }
    addEventListener() {}
    removeEventListener() {}
    dispatchEvent() {}
    getBoundingClientRect() { return { x: 0, y: 0, width: 100, height: 100, top: 0, left: 0, bottom: 100, right: 100 }; }
    focus() {}
    blur() {}
}

var mockDocument = {
    getElementById: function(id) { return global._mockElements[id] || null; },
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
    querySelector: function() { return null; },
    querySelectorAll: function() { return []; },
    implementation: { createHTMLDocument: function() { return mockDocument; } }
};

// --- Module paths ---

var vizPath = process.argv[2];
var plantumlPath = process.argv[3];

if (!vizPath || !plantumlPath) {
    process.stderr.write('Usage: node plantuml-render.js <viz-global.js> <plantuml.js>\n');
    process.exit(1);
}

// --- Phase 1: Load Viz.js WITHOUT browser mocks ---
// viz-global.js (Viz.js 3.x / Emscripten) detects the environment at load time:
//   - If document is undefined: takes the Node.js CJS path (uses __filename for WASM URL)
//   - If document is defined: takes the browser path (uses document.currentScript.src)
// By NOT setting global.document yet, viz-global.js correctly detects Node.js
// and resolves the embedded base64 WASM without needing URL or fetch polyfills.
process.stderr.write('Phase 1: Loading viz-global.js...\n');
var vizModule = require(vizPath);

// viz-global.js uses UMD: in CJS mode it populates module.exports, not globalThis.Viz.
// plantuml.js expects Viz to be global, so expose it.
if (!globalThis.Viz) globalThis.Viz = {};
Object.assign(globalThis.Viz, vizModule);
process.stderr.write('Phase 1: Viz loaded. instance=' + typeof vizModule.instance + '\n');

// --- Phase 2: Wait for WASM compilation, read stdin in parallel ---
var inputResolve;
var inputPromise = new Promise(function(resolve) { inputResolve = resolve; });
var input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', function(chunk) { input += chunk; });
process.stdin.on('end', function() { inputResolve(input); });

process.stderr.write('Phase 2: Waiting for WASM init...\n');
Promise.all([vizModule.instance(), inputPromise]).then(function(results) {
    var plantUml = results[1];
    process.stderr.write('Phase 2: WASM ready, stdin received (' + plantUml.length + ' chars)\n');

    // --- Phase 3: Set up browser mocks for plantuml.js ---
    global.self = global;
    global.window = global;
    global.document = mockDocument;
    global.navigator = { userAgent: 'node', platform: 'node' };
    global.HTMLElement = MockElement;
    global.SVGElement = MockElement;
    global.Element = MockElement;
    global.Node = MockElement;
    global.DOMParser = class {
        parseFromString(str) {
            var el = new MockElement('div');
            el.innerHTML = str;
            el._svgContent = str;
            var doc = {
                documentElement: el,
                firstChild: el,
                querySelector: function() { return el; },
                querySelectorAll: function() { return [el]; }
            };
            return doc;
        }
    };
    global.XMLSerializer = class {
        serializeToString(node) {
            return node.outerHTML || node.innerHTML || node._svgContent || '';
        }
    };
    global._mockElements = {};

    // --- Phase 4: Load plantuml.js ---
    process.stderr.write('Phase 3: Loading plantuml.js...\n');
    var plantumlModule = require(plantumlPath);
    process.stderr.write('Phase 3: plantuml.js loaded. plantumlLoad=' + typeof plantumlModule.plantumlLoad + '\n');

    // --- Phase 5: Render ---
    var targetId = '_render_target';
    var target = new MockElement('div');
    target.id = targetId;
    target.ownerDocument = mockDocument;
    global._mockElements[targetId] = target;

    // Intercept innerHTML setter to capture SVG
    var svgResult = '';
    Object.defineProperty(target, 'innerHTML', {
        get: function() { return svgResult; },
        set: function(value) {
            svgResult = value;
        },
        configurable: true
    });

    plantumlModule.plantumlLoad([], function() {
        var renderer = global.plantuml || (global.self && global.self.plantuml);
        if (!renderer || typeof renderer.render !== 'function') {
            process.stderr.write('ERROR: plantuml.render not available after plantumlLoad\n');
            process.exit(1);
        }

        var lines = plantUml.trim().split('\n');
        process.stderr.write('Phase 5: Rendering ' + lines.length + ' lines...\n');

        try {
            renderer.render(lines, targetId);
        } catch (e) {
            process.stderr.write('ERROR during render: ' + e.message + '\n');
            process.exit(1);
        }

        // Check synchronous result first
        if (svgResult && svgResult.indexOf('<svg') !== -1) {
            process.stdout.write(svgResult);
            process.exit(0);
        }

        // Poll for async result
        var attempts = 0;
        var maxAttempts = 200; // 10 seconds max
        var check = function() {
            if (svgResult && svgResult.indexOf('<svg') !== -1) {
                process.stdout.write(svgResult);
                process.exit(0);
            }
            if (++attempts > maxAttempts) {
                process.stderr.write('ERROR: Timed out waiting for SVG render (' + maxAttempts * 50 + 'ms)\n');
                if (svgResult) {
                    process.stderr.write('Partial content: ' + svgResult.substring(0, 500) + '\n');
                }
                process.exit(1);
            }
            setTimeout(check, 50);
        };
        setTimeout(check, 50);
    });
}).catch(function(err) {
    process.stderr.write('ERROR: Initialization failed: ' + (err.stack || err.message || err) + '\n');
    process.exit(1);
});
