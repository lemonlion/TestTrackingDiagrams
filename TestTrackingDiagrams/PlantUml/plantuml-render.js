// PlantUML Node.js renderer for TestTrackingDiagrams CI summaries
// Usage: node plantuml-render.js <viz-global.js-path> <plantuml.js-path>
// stdin: PlantUML source code (one diagram)
// stdout: SVG output
//
// Both viz-global.js and plantuml.js are designed for browsers (<script> tags).
// We load them via vm.runInThisContext to simulate browser <script> tag loading,
// avoiding CJS module wrapping that breaks their UMD/global interactions.

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

// Catch silently swallowed Promise rejections
process.on('unhandledRejection', function(err) {
    process.stderr.write('UNHANDLED REJECTION: ' + (err && err.stack || err) + '\n');
});

// --- Load scripts like <script> tags (no CJS module wrapping) ---
// vm.runInThisContext runs code in the V8 global context.
// Unlike require(), it does NOT wrap code in (function(exports,require,module,...){}).
// This means module/exports/require/__filename are NOT available inside the script,
// so UMD wrappers (like viz-global.js) take the browser/global path instead of CJS.

function loadScript(filePath, label) {
    process.stderr.write('Loading ' + label + '...\n');
    var code = fs.readFileSync(filePath, 'utf8');
    vm.runInThisContext(code, { filename: filePath });
    process.stderr.write(label + ' loaded.\n');
}

// Set document.currentScript to a mock <script> element pointing to viz-global.js.
// viz-global.js reads currentScript.src at load time to resolve its WASM URL.
// The WASM is base64-embedded so the exact URL doesn't matter for loading,
// but the code requires a valid value.
var vizScript = new MockElement('script');
vizScript.src = urlModule.pathToFileURL(path.resolve(vizPath)).href;
mockDocument.currentScript = vizScript;
mockDocument.baseURI = urlModule.pathToFileURL(process.cwd()).href + '/';

loadScript(vizPath, 'viz-global.js');
process.stderr.write('  Viz=' + typeof globalThis.Viz + ', instance=' + typeof (globalThis.Viz && globalThis.Viz.instance) + '\n');

// Browsers null currentScript between scripts
mockDocument.currentScript = null;

loadScript(plantumlPath, 'plantuml.js');
process.stderr.write('  plantumlLoad=' + typeof globalThis.plantumlLoad + ', plantuml=' + typeof globalThis.plantuml + '\n');

// --- Read stdin and render ---

var input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', function(chunk) { input += chunk; });
process.stdin.on('end', function() {
    var targetId = '_render_target';
    var target = new MockElement('div');
    target.id = targetId;
    target.ownerDocument = mockDocument;
    global._mockElements[targetId] = target;

    // Intercept innerHTML setter to capture SVG output
    var svgResult = '';
    Object.defineProperty(target, 'innerHTML', {
        get: function() { return svgResult; },
        set: function(value) { svgResult = value; },
        configurable: true
    });

    var loadFn = globalThis.plantumlLoad;
    if (!loadFn) {
        process.stderr.write('ERROR: plantumlLoad not found on global scope\n');
        process.exit(1);
    }

    loadFn([], function() {
        var renderer = globalThis.plantuml;
        if (!renderer || typeof renderer.render !== 'function') {
            process.stderr.write('ERROR: plantuml.render not available after plantumlLoad\n');
            process.exit(1);
        }

        var lines = input.trim().split('\n');
        process.stderr.write('Rendering ' + lines.length + ' lines...\n');

        try {
            renderer.render(lines, targetId);
        } catch (e) {
            process.stderr.write('ERROR during render: ' + (e.stack || e.message) + '\n');
            process.exit(1);
        }

        // Check synchronous result first
        if (svgResult && svgResult.indexOf('<svg') !== -1) {
            process.stdout.write(svgResult);
            process.exit(0);
        }

        // Poll for async result (Viz WASM init + render is async)
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
                    process.stderr.write('Partial content: ' + svgResult.substring(0, 500) + '\n');
                }
                process.exit(1);
            }
            setTimeout(check, 50);
        };
        setTimeout(check, 50);
    });
});
