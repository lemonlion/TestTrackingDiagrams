// PlantUML Node.js renderer for TestTrackingDiagrams CI summaries
// Usage: node plantuml-render.js <viz-global.js-path> <plantuml.js-path>
// stdin: PlantUML source code (one diagram)
// stdout: SVG output
//
// Both viz-global.js and plantuml.js are designed for browsers (<script> tags).
// We load them via vm.runInThisContext to simulate browser <script> tag loading,
// avoiding CJS module wrapping that breaks their UMD/global interactions.
//
// CRITICAL: Viz.js compiles Graphviz WASM asynchronously. plantuml.render() calls
// Viz synchronously and produces nothing if WASM isn't compiled yet. We MUST wait
// for Viz.instance() to resolve before calling plantuml.render().

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
    focus() {}
    blur() {}
}

var mockDocument = {
    getElementById: function(id) {
        process.stderr.write('  [DOM] getElementById("' + id + '")\n');
        return global._mockElements[id] || null;
    },
    querySelector: function(sel) {
        process.stderr.write('  [DOM] querySelector("' + sel + '")\n');
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

// Catch silently swallowed errors
process.on('unhandledRejection', function(err) {
    process.stderr.write('UNHANDLED REJECTION: ' + (err && err.stack || err) + '\n');
});
process.on('uncaughtException', function(err) {
    process.stderr.write('UNCAUGHT EXCEPTION: ' + (err && err.stack || err) + '\n');
    process.exit(1);
});

// --- Load scripts like <script> tags (no CJS module wrapping) ---

function loadScript(filePath, label) {
    process.stderr.write('Loading ' + label + '...\n');
    var code = fs.readFileSync(filePath, 'utf8');
    vm.runInThisContext(code, { filename: filePath });
    process.stderr.write(label + ' loaded.\n');
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

loadScript(vizPath, 'viz-global.js');
process.stderr.write('  Viz=' + typeof globalThis.Viz + ', instance=' + typeof (globalThis.Viz && globalThis.Viz.instance) + '\n');
mockDocument.currentScript = null;

// --- Phase 2: Wait for WASM to compile BEFORE loading plantuml.js ---
// plantuml.render() calls Viz synchronously. If WASM isn't compiled yet,
// the render produces no output. We must ensure WASM is ready first.
process.stderr.write('Waiting for Viz WASM compilation...\n');
var vizReady = globalThis.Viz.instance().then(function(viz) {
    process.stderr.write('Viz WASM ready. Testing...\n');
    var testSvg = viz.renderString('digraph { a -> b }', { format: 'svg' });
    if (testSvg && testSvg.indexOf('<svg') !== -1) {
        process.stderr.write('Viz test render OK (' + testSvg.length + ' chars)\n');
    } else {
        process.stderr.write('Viz test render FAILED: ' + (testSvg || '(empty)').substring(0, 200) + '\n');
        process.exit(1);
    }
    return viz;
}).catch(function(err) {
    process.stderr.write('Viz WASM FAILED: ' + (err && err.stack || err) + '\n');
    process.exit(1);
});

// --- Phase 3: Once WASM is ready, load plantuml.js and render ---
Promise.all([vizReady, inputPromise]).then(function(results) {
    var plantUml = results[1];
    process.stderr.write('stdin received (' + plantUml.length + ' chars)\n');

    // Load plantuml.js AFTER Viz WASM is compiled
    loadScript(plantumlPath, 'plantuml.js');
    process.stderr.write('  plantumlLoad=' + typeof globalThis.plantumlLoad + '\n');

    var loadFn = globalThis.plantumlLoad;
    if (!loadFn) {
        process.stderr.write('ERROR: plantumlLoad not found\n');
        process.exit(1);
    }

    // Set up render target
    var targetId = '_render_target';
    var target = new MockElement('div');
    target.id = targetId;
    target.ownerDocument = mockDocument;
    global._mockElements[targetId] = target;

    // Intercept innerHTML setter to capture SVG output
    var svgResult = '';
    Object.defineProperty(target, 'innerHTML', {
        get: function() { return svgResult; },
        set: function(value) {
            process.stderr.write('  [innerHTML set] ' + (value ? value.substring(0, 80) + '...' : '(empty)') + '\n');
            svgResult = value;
        },
        configurable: true
    });

    loadFn([], function() {
        var renderer = globalThis.plantuml;
        process.stderr.write('plantumlLoad callback: plantuml=' + typeof renderer +
            ', render=' + typeof (renderer && renderer.render) + '\n');

        if (!renderer || typeof renderer.render !== 'function') {
            process.stderr.write('ERROR: plantuml.render not available\n');
            process.exit(1);
        }

        var lines = plantUml.trim().split('\n');
        process.stderr.write('Rendering ' + lines.length + ' lines...\n');

        try {
            renderer.render(lines, targetId);
        } catch (e) {
            process.stderr.write('ERROR during render: ' + (e.stack || e.message) + '\n');
            process.exit(1);
        }

        process.stderr.write('render() returned. svgResult=' + (svgResult ? svgResult.length + ' chars' : 'empty') + '\n');

        // Check synchronous result
        if (svgResult && svgResult.indexOf('<svg') !== -1) {
            process.stdout.write(svgResult);
            process.exit(0);
        }

        // Poll for async result
        var attempts = 0;
        var maxAttempts = 400; // 20 seconds max
        var check = function() {
            if (svgResult && svgResult.indexOf('<svg') !== -1) {
                process.stdout.write(svgResult);
                process.exit(0);
            }
            if (++attempts > maxAttempts) {
                process.stderr.write('ERROR: Timed out waiting for SVG render (' + maxAttempts * 50 + 'ms)\n');
                if (svgResult) {
                    process.stderr.write('Partial: ' + svgResult.substring(0, 500) + '\n');
                }
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
