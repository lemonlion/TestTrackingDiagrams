namespace TestTrackingDiagrams.Reports;

public static class DiagramContextMenu
{
    public static string GetStyles() => """
        .diagram-ctx-menu {
            position: fixed;
            background: #fff;
            border: 1px solid #ccc;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
            padding: 4px 0;
            z-index: 20001;
            font: 13px -apple-system, 'Segoe UI', sans-serif;
            min-width: 200px;
        }
        .diagram-ctx-menu div {
            padding: 6px 24px;
            cursor: pointer;
            white-space: nowrap;
        }
        .diagram-ctx-menu div:hover {
            background: #e8f0fe;
        }
        .diagram-ctx-menu hr {
            margin: 4px 0;
            border: none;
            border-top: 1px solid #e0e0e0;
        }
        """;

    public static string GetInlineSvgStyles() => """
        .plantuml-inline-svg svg,
        .plantuml-browser svg {
            max-width: 100%;
            height: auto;
        }
        .plantuml-browser {
            overflow-x: auto;
            padding-left: 1em;
        }
        """;

    public static string GetCollapsibleNotesStyles() => """
        .details-radio { display: inline-flex; align-items: center; gap: 0.3em; }
        .headers-radio { display: inline-flex; align-items: center; gap: 0.3em; margin-left: 1.5em; }
        .details-radio-label { font-weight: bold; margin-right: 0.3em; font-size: 13px; }
        .details-radio-btn {
            padding: 0.25em 0.6em;
            border: 1px solid rgb(180, 180, 180);
            border-radius: 0.4em;
            background: white;
            cursor: pointer;
            font-size: 0.85em;
        }
        .details-radio-btn:hover { background: rgb(230, 240, 255); border-color: rgb(100, 150, 255); }
        .details-radio-btn.details-active { background: rgb(66, 133, 244); color: white; border-color: rgb(66, 133, 244); }
        .truncate-lines-select {
            padding: 0.2em 0.3em;
            border: 1px solid rgb(180, 180, 180);
            border-radius: 0.4em;
            font-size: 0.85em;
            margin-left: 0.3em;
        }
        .truncate-lines-label { font-size: 0.85em; color: rgb(100, 100, 100); margin-left: 0.2em; }
        .note-toggle-icon { user-select: none; -webkit-user-select: none; }
        """;

    public static string GetInternalFlowPopupStyles() => """
        .iflow-overlay {
            position: fixed;
            inset: 0;
            background: rgba(0,0,0,0.4);
            z-index: 20000;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .iflow-popup {
            background: #fff;
            border-radius: 8px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.25);
            max-width: 90vw;
            max-height: 85vh;
            overflow: auto;
            padding: 20px;
            position: relative;
            font: 14px/1.5 -apple-system, 'Segoe UI', sans-serif;
        }
        .iflow-popup-close {
            position: absolute;
            top: 8px;
            right: 12px;
            cursor: pointer;
            font-size: 20px;
            color: #666;
            background: none;
            border: none;
            padding: 4px 8px;
        }
        .iflow-popup-close:hover {
            color: #000;
        }
        .iflow-popup h3 {
            margin: 0 0 12px 0;
            font-size: 15px;
            color: #333;
        }
        .iflow-popup .iflow-no-data {
            color: #888;
            font-style: italic;
            padding: 20px 0;
        }
        .iflow-popup .iflow-diagram {
            min-height: 60px;
        }
        .iflow-call-tree {
            list-style: none;
            padding-left: 0;
            font: 13px/1.6 monospace;
        }
        .iflow-call-tree ul {
            list-style: none;
            padding-left: 20px;
            border-left: 1px solid #ddd;
        }
        .iflow-call-tree li { padding: 2px 0; }
        .iflow-source { color: #888; font-size: 11px; }
        .iflow-duration { color: #666; font-size: 12px; }
        .iflow-toggle { margin-top: 8px; margin-bottom: 8px; }
        .iflow-toggle-btn {
            padding: 4px 14px;
            border: 1px solid #ccc;
            background: #f5f5f5;
            cursor: pointer;
            font-size: 13px;
            border-radius: 4px;
            margin-right: 4px;
        }
        .iflow-toggle-btn:hover { background: #e8f0fe; }
        .iflow-toggle-active { background: #4285f4; color: #fff; border-color: #4285f4; }
        .iflow-toggle-active:hover { background: #3367d6; }
        .iflow-flame { padding: 4px 0; }
        .iflow-flame-bar {
            position: relative;
            height: 22px;
            border-radius: 3px;
            margin: 1px 0;
            overflow: hidden;
            font: 11px/22px -apple-system, 'Segoe UI', sans-serif;
            color: #333;
            cursor: default;
            border: 1px solid rgba(0,0,0,0.08);
        }
        .iflow-flame-label {
            padding: 0 4px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            display: block;
        }
        .iflow-boundary-marker {
            position: absolute;
            top: 0;
            bottom: 0;
            width: 0;
            border-left: 1px dashed rgba(0,0,0,0.3);
            z-index: 1;
            pointer-events: none;
        }
        .iflow-boundary-marker:hover { border-left-color: rgba(0,0,0,0.6); pointer-events: auto; }
        .whole-test-flow { margin-top: 8px; padding-top: 4px; }
        .whole-test-flow > summary { cursor: pointer; font-weight: 600; color: #555; }
        .diagram-toggle { margin-top: 8px; margin-bottom: 8px; padding-left: 1em; padding-right: 1em; display: flex; align-items: center; }
        .diagram-toggle-btn {
            padding: 4px 14px;
            border: 1px solid #ccc;
            background: #f5f5f5;
            cursor: pointer;
            font-size: 13px;
            border-radius: 4px;
            margin-right: 4px;
        }
        .diagram-toggle-btn:hover { background: #e8f0fe; }
        .diagram-toggle-active { background: #4285f4; color: #fff; border-color: #4285f4; }
        .diagram-toggle-active:hover { background: #3367d6; }
        .diagram-toggle-spacer { flex: 1; }
        .collapse-all-notes-btn, .toggle-headers-btn {
            padding: 4px 14px;
            border: 1px solid #ccc;
            background: #f5f5f5;
            cursor: pointer;
            font-size: 13px;
            border-radius: 4px;
            margin-right: 1em;
        }
        .collapse-all-notes-btn:hover, .toggle-headers-btn:hover { background: #e8f0fe; }
        .span-count-warning { color: #b30000; font-size: 12px; font-style: italic; margin-left: 8px; }
        .iflow-test-band { border-bottom: 1px solid #eee; padding: 4px 0; }
        .iflow-test-band-label { font: 11px/1.4 monospace; color: #888; padding: 2px 0; }
        .iflow-sequential-tests { padding: 0; }
        .iflow-rel-list { list-style: none; padding: 0; margin: 16px 0; }
        .iflow-rel-list li { padding: 6px 12px; border: 1px solid #e0e0e0; border-radius: 4px; margin: 4px 0; cursor: pointer; font: 13px/1.5 -apple-system, 'Segoe UI', sans-serif; }
        .iflow-rel-list li:hover { background: #f0f4ff; border-color: #4285f4; }
        .iflow-rel-summary-table { width: 100%; border-collapse: collapse; font: 12px/1.5 -apple-system, 'Segoe UI', sans-serif; margin-top: 12px; }
        .iflow-rel-summary-table th { text-align: left; padding: 4px 8px; border-bottom: 2px solid #ddd; color: #555; }
        .iflow-rel-summary-table td { padding: 4px 8px; border-bottom: 1px solid #eee; cursor: pointer; }
        .iflow-rel-summary-table tr:hover td { background: #f0f4ff; }
        svg a.iflow-link-hover, svg text.iflow-link-hover { cursor: default; }
        svg a.iflow-link-hover:hover, svg text.iflow-link-hover:hover { cursor: pointer; }
        svg a.iflow-link-hover:hover text { fill: #0000EE; text-decoration: underline; }
        svg text.iflow-link-hover:hover { fill: #0000EE; text-decoration: underline; }
        """;

    public static string GetInternalFlowConfigScript(InternalFlowHasDataBehavior hasDataBehavior) =>
        $"<script>window.__iflowConfig = {{ hasDataBehavior: '{(hasDataBehavior == InternalFlowHasDataBehavior.ShowLinkOnHover ? "showLinkOnHover" : "showLink")}' }};</script>";

    private const string PlantUmlJsCdnBase = "https://cdn.jsdelivr.net/gh/lemonlion/plantuml-js-plantuml_limit_size_16384@v1.2026.3beta6-patched";

    public static string GetPlantUmlBrowserRenderScript() => $$"""
        <script src="{{PlantUmlJsCdnBase}}/viz-global.js"></script>
        <script src="{{PlantUmlJsCdnBase}}/plantuml.js"></script>
        <script>
            plantumlLoad();
            document.addEventListener('DOMContentLoaded', function() {
                var renderQueue = [];
                var rendering = false;
                function extractIflowMap(source) {
                    var map = {};
                    var re = /\[\[#(iflow-[^\s\]]+)\s+([^\]]+)\]\]/g;
                    var m;
                    while ((m = re.exec(source)) !== null) {
                        var key = m[2].split('\\n').join('').replace(/\s+/g, '');
                        map[key] = m[1];
                    }
                    return map;
                }
                function processQueue() {
                    if (rendering || renderQueue.length === 0) return;
                    rendering = true;
                    var item = renderQueue.shift();
                    var lines = item.source.split('\n');
                    var mo = new MutationObserver(function() {
                        mo.disconnect();
                        bindIflowLinks(item.el, item.source);
                        if (window._makeNotesCollapsible) window._makeNotesCollapsible(item.el);
                        rendering = false;
                        processQueue();
                    });
                    mo.observe(item.el, { childList: true, subtree: true });
                    try {
                        window.plantuml.render(lines, item.el.id);
                    } catch(e) {
                        mo.disconnect();
                        rendering = false;
                        var msg = (e && e.message) ? e.message : String(e);
                        if (msg.indexOf('too large') >= 0) {
                            item.el.innerHTML = '<div style="color:#c00;padding:1em;border:1px solid #c00;border-radius:6px;margin:0.5em 0;">'
                                + '<strong>Diagram too large for client-side rendering.</strong><br>'
                                + 'Use <code>PlantUmlRendering.Server</code> or <code>PlantUmlRendering.Local</code> for large diagrams.'
                                + '<details style="margin-top:0.5em"><summary>Raw PlantUML</summary><pre style="white-space:pre-wrap">'
                                + item.source.replace(/</g,'&lt;') + '</pre></details></div>';
                        } else {
                            item.el.textContent = 'Render error: ' + msg;
                        }
                        processQueue();
                    }
                }
                window._iflowBindLinks = function(container, source) { bindIflowLinks(container, source); };
                function bindIflowLinks(container, source) {
                    if (!container) return;
                    var iflowData = window.__iflowSegments || {};
                    var config = window.__iflowConfig || {};
                    var hoverOnly = config.hasDataBehavior === 'showLinkOnHover';
                    var bound = 0;
                    container.querySelectorAll('a').forEach(function(a) {
                        var href = a.getAttribute('xlink:href') || a.getAttribute('href') || '';
                        if (href.indexOf('#iflow-') !== 0) return;
                        var segId = href.substring(1);
                        if (!iflowData[segId]) return;
                        if (hoverOnly) {
                            a.removeAttribute('xlink:href');
                            a.removeAttribute('href');
                            a.classList.add('iflow-link-hover');
                        } else {
                            a.style.cursor = 'pointer';
                        }
                        a.addEventListener('click', function(ev) {
                            ev.preventDefault();
                            ev.stopPropagation();
                            if (window._iflowShowPopup) window._iflowShowPopup(segId);
                        });
                        bound++;
                    });
                    if (bound > 0) return;
                    if (!source) return;
                    var iflowMap = extractIflowMap(source);
                    if (Object.keys(iflowMap).length === 0) return;
                    var allTexts = Array.from(container.querySelectorAll('text'));
                    var blueIndices = new Set();
                    allTexts.forEach(function(t, idx) {
                        if ((t.getAttribute('fill') || '').toLowerCase() === '#0000ff') {
                            blueIndices.add(idx);
                            t.setAttribute('fill', '#000000');
                            t.removeAttribute('text-decoration');
                        }
                    });
                    var groups = [];
                    var curGrp = [];
                    var sorted = Array.from(blueIndices).sort(function(a, b) { return a - b; });
                    for (var gi = 0; gi < sorted.length; gi++) {
                        if (curGrp.length === 0 || sorted[gi] === curGrp[curGrp.length - 1] + 1) {
                            curGrp.push(sorted[gi]);
                        } else {
                            groups.push(curGrp);
                            curGrp = [sorted[gi]];
                        }
                    }
                    if (curGrp.length > 0) groups.push(curGrp);
                    groups.forEach(function(group) {
                        var combined = group.map(function(idx) { return allTexts[idx].textContent; }).join('');
                        var key = combined.replace(/\s+/g, '');
                        var segId = iflowMap[key] || null;
                        if (!segId || !iflowData[segId]) return;
                        var groupEls = group.map(function(idx) { return allTexts[idx]; });
                        groupEls.forEach(function(textEl) {
                            textEl.style.pointerEvents = 'all';
                            if (hoverOnly) {
                                textEl.style.cursor = 'default';
                                textEl.addEventListener('mouseenter', function() {
                                    groupEls.forEach(function(el) {
                                        el.setAttribute('fill', '#0000FF');
                                        el.setAttribute('text-decoration', 'underline');
                                        el.style.cursor = 'pointer';
                                    });
                                });
                                textEl.addEventListener('mouseleave', function() {
                                    groupEls.forEach(function(el) {
                                        el.setAttribute('fill', '#000000');
                                        el.removeAttribute('text-decoration');
                                        el.style.cursor = 'default';
                                    });
                                });
                            } else {
                                textEl.setAttribute('fill', '#0000FF');
                                textEl.setAttribute('text-decoration', 'underline');
                                textEl.style.cursor = 'pointer';
                            }
                            textEl.addEventListener('click', function(ev) {
                                ev.preventDefault();
                                ev.stopPropagation();
                                if (window._iflowShowPopup) window._iflowShowPopup(segId);
                            });
                        });
                        bound++;
                    });
                }
                var observer = new IntersectionObserver(function(entries) {
                    entries.forEach(function(entry) {
                        if (!entry.isIntersecting) return;
                        var el = entry.target;
                        if (el.dataset.rendered) return;
                        el.dataset.rendered = '1';
                        observer.unobserve(el);
                        var source = el.getAttribute('data-plantuml');
                        if (source) {
                            if (window._preProcessSource) source = window._preProcessSource(el, source);
                            renderQueue.push({ el: el, source: source });
                            processQueue();
                        } else if (el.hasAttribute('data-plantuml-z')) {
                            decompressGzipBase64(el.getAttribute('data-plantuml-z')).then(function(decoded) {
                                el.setAttribute('data-plantuml', decoded);
                                var src = decoded;
                                if (window._preProcessSource) src = window._preProcessSource(el, decoded);
                                renderQueue.push({ el: el, source: src });
                                processQueue();
                            }).catch(function() { el.textContent = 'Decompression error'; });
                        }
                    });
                }, { rootMargin: '200px' });
                function decompressGzipBase64(base64) {
                    var raw = atob(base64);
                    var bytes = new Uint8Array(raw.length);
                    for (var i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
                    var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'));
                    return new Response(stream).text();
                }
                window.decompressGzipBase64 = decompressGzipBase64;
                document.querySelectorAll('.plantuml-browser').forEach(function(el) {
                    observer.observe(el);
                });
                // Preload first scenario's diagrams immediately
                var firstScenario = document.querySelector('.scenario');
                if (firstScenario) {
                    firstScenario.querySelectorAll('.plantuml-browser').forEach(function(el) {
                        if (el.dataset.rendered) return;
                        el.dataset.rendered = '1';
                        observer.unobserve(el);
                        var source = el.getAttribute('data-plantuml');
                        if (source) {
                            if (window._preProcessSource) source = window._preProcessSource(el, source);
                            renderQueue.push({ el: el, source: source });
                        } else if (el.hasAttribute('data-plantuml-z')) {
                            decompressGzipBase64(el.getAttribute('data-plantuml-z')).then(function(decoded) {
                                el.setAttribute('data-plantuml', decoded);
                                var src = decoded;
                                if (window._preProcessSource) src = window._preProcessSource(el, decoded);
                                renderQueue.push({ el: el, source: src });
                                processQueue();
                            }).catch(function() { el.textContent = 'Decompression error'; });
                        }
                    });
                    processQueue();
                    // Also render first scenario's flame charts
                    if (window._renderFlameCharts) window._renderFlameCharts(firstScenario);
                }
            });
        </script>
        """;

    public static string GetMermaidScript() => """
        <script type="module">
            import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
            mermaid.initialize({ startOnLoad: true, securityLevel: 'loose' });
        </script>
        """;

    public static string GetContextMenuScript() => """
        <script>
        (function() {
            var menu = null;

            function findDiagramContainer(el) {
                while (el) {
                    if (el.dataset && el.dataset.diagramType) return el;
                    el = el.parentElement;
                }
                return null;
            }

            function getDiagramFilename(container, ext) {
                var scenario = container.closest('details.scenario');
                var baseName = 'diagram';
                if (scenario) {
                    var summary = scenario.querySelector(':scope > summary');
                    if (summary) {
                        var clone = summary.cloneNode(true);
                        clone.querySelectorAll('button, a, .endpoint, .label, .duration-badge').forEach(function(e) { e.remove(); });
                        baseName = (clone.textContent || '').trim();
                    }
                }
                baseName = baseName.toLowerCase()
                    .replace(/[\[\]]/g, '')
                    .replace(/["']/g, '')
                    .replace(/[^a-z0-9]+/g, '-')
                    .replace(/^-+|-+$/g, '');
                if (!baseName) baseName = 'diagram';
                var containers = scenario ? Array.from(scenario.querySelectorAll('[data-diagram-type]')) : [];
                if (containers.length > 1) {
                    var idx = containers.indexOf(container);
                    baseName += '-dg' + (idx + 1);
                }
                return baseName + '.' + ext;
            }

            function getSvg(container) {
                return container.querySelector('svg');
            }

            function getSource(container) {
                return container.getAttribute('data-plantuml') || container.getAttribute('data-mermaid-source') || '';
            }

            async function getSourceAsync(container) {
                var src = container.getAttribute('data-plantuml') || container.getAttribute('data-mermaid-source');
                if (src) return src;
                if (container.hasAttribute('data-plantuml-z')) {
                    var decoded = await decompressGzipBase64(container.getAttribute('data-plantuml-z'));
                    container.setAttribute('data-plantuml', decoded);
                    return decoded;
                }
                return '';
            }

            function getTypeLabel(container) {
                var t = container.getAttribute('data-diagram-type');
                return t === 'mermaid' ? 'Mermaid' : 'PlantUML';
            }

            function serializeSvg(svg) {
                return new XMLSerializer().serializeToString(svg);
            }

            function getBackgroundColor(svg) {
                // 1. Check SVG inline style (PlantUML sets background here)
                var svgStyle = svg.getAttribute('style') || '';
                var bgMatch = svgStyle.match(/background\s*:\s*([^;]+)/);
                if (bgMatch) {
                    var val = bgMatch[1].trim();
                    if (val && val !== 'none' && val !== 'transparent') return val;
                }
                // 2. Check computed style
                var computed = window.getComputedStyle(svg).backgroundColor;
                if (computed && computed !== 'rgba(0, 0, 0, 0)' && computed !== 'transparent') return computed;
                // 3. Only consider rects that cover the full SVG area (true background rects)
                var svgW = svg.width.baseVal.value || svg.getBoundingClientRect().width;
                var svgH = svg.height.baseVal.value || svg.getBoundingClientRect().height;
                var svgArea = svgW * svgH;
                if (svgArea > 0) {
                    var rects = svg.querySelectorAll('rect');
                    for (var i = 0; i < rects.length; i++) {
                        var rect = rects[i];
                        var rw = parseFloat(rect.getAttribute('width') || 0);
                        var rh = parseFloat(rect.getAttribute('height') || 0);
                        if ((rw * rh) / svgArea < 0.9) continue;
                        var fo = rect.getAttribute('fill-opacity');
                        if (fo !== null && parseFloat(fo) === 0) continue;
                        var rstyle = rect.getAttribute('style') || '';
                        var fom = rstyle.match(/fill-opacity\s*:\s*([^;]+)/);
                        if (fom && parseFloat(fom[1]) === 0) continue;
                        var fill = rect.getAttribute('fill');
                        if (fill) {
                            if (fill === 'none' || fill === 'transparent') continue;
                            if (/^#[0-9a-fA-F]{8}$/.test(fill) && fill.slice(7).toLowerCase() === '00') continue;
                            return fill;
                        }
                        var fm = rstyle.match(/fill\s*:\s*([^;]+)/);
                        if (fm && fm[1].trim() !== 'none' && fm[1].trim() !== 'transparent') return fm[1].trim();
                    }
                }
                return '#ffffff';
            }

            function svgToCanvas(svg, callback) {
                var svgData = serializeSvg(svg);
                var url = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
                var img = new Image();
                var scale = 2;
                img.onload = function() {
                    var canvas = document.createElement('canvas');
                    canvas.width = img.naturalWidth * scale;
                    canvas.height = img.naturalHeight * scale;
                    var ctx = canvas.getContext('2d');
                    ctx.scale(scale, scale);
                    ctx.drawImage(img, 0, 0);
                    callback(canvas);
                };
                img.src = url;
            }

            function svgToCanvasWithBg(svg, callback) {
                var bg = getBackgroundColor(svg);
                var clone = svg.cloneNode(true);
                var vb = clone.getAttribute('viewBox');
                var bx = '0', by = '0', bw, bh;
                if (vb) {
                    var parts = vb.split(/[\s,]+/);
                    bx = parts[0]; by = parts[1]; bw = parts[2]; bh = parts[3];
                } else {
                    bw = clone.getAttribute('width') || svg.getBoundingClientRect().width;
                    bh = clone.getAttribute('height') || svg.getBoundingClientRect().height;
                }
                var bgRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
                bgRect.setAttribute('x', bx);
                bgRect.setAttribute('y', by);
                bgRect.setAttribute('width', bw);
                bgRect.setAttribute('height', bh);
                bgRect.setAttribute('fill', bg);
                clone.insertBefore(bgRect, clone.firstChild);
                var svgData = serializeSvg(clone);
                var url = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
                var img = new Image();
                var scale = 2;
                img.onload = function() {
                    var canvas = document.createElement('canvas');
                    canvas.width = img.naturalWidth * scale;
                    canvas.height = img.naturalHeight * scale;
                    var ctx = canvas.getContext('2d');
                    ctx.scale(scale, scale);
                    ctx.drawImage(img, 0, 0);
                    callback(canvas);
                };
                img.src = url;
            }

            function htmlToCanvas(element, callback) {
                var rect = element.getBoundingClientRect();
                var scale = 2;
                var canvas = document.createElement('canvas');
                canvas.width = rect.width * scale;
                canvas.height = rect.height * scale;
                var ctx = canvas.getContext('2d');
                ctx.scale(scale, scale);
                var svgNs = 'http://www.w3.org/2000/svg';
                var fo = '<foreignObject width="' + rect.width + '" height="' + rect.height + '">'
                    + '<body xmlns="http://www.w3.org/1999/xhtml" style="margin:0">'
                    + element.outerHTML + '</body></foreignObject>';
                var svgMarkup = '<svg xmlns="' + svgNs + '" width="' + rect.width + '" height="' + rect.height + '">' + fo + '</svg>';
                var url = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgMarkup)));
                var img = new Image();
                img.onload = function() { ctx.drawImage(img, 0, 0); callback(canvas); };
                img.onerror = function() { callback(canvas); };
                img.src = url;
            }

            function closeMenu() {
                if (menu) { menu.remove(); menu = null; }
            }

            function createMenuItem(label, action) {
                var item = document.createElement('div');
                item.textContent = label;
                item.addEventListener('click', function(e) {
                    e.stopPropagation();
                    closeMenu();
                    action();
                });
                return item;
            }

            function createSeparator() {
                return document.createElement('hr');
            }

            function extractCallerPayloads(source) {
                if (!source) return '';
                var lines = source.split('\n');
                var callerAlias = null;
                for (var i = 0; i < lines.length; i++) {
                    var m = lines[i].match(/^\s*actor\s+"[^"]*"\s+as\s+(\S+)/);
                    if (m) { callerAlias = m[1]; break; }
                }
                if (!callerAlias) return '';
                var payloads = [];
                var inNote = false;
                var noteLines = [];
                var afterCallerRequest = false;
                for (var i = 0; i < lines.length; i++) {
                    var line = lines[i];
                    if (!inNote) {
                        if (line.match(new RegExp('^\\s*' + callerAlias.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s+->\\s+'))) {
                            afterCallerRequest = true;
                        } else if (line.match(/^\s*\S+\s+-->\s+/)) {
                            afterCallerRequest = false;
                        }
                        if (afterCallerRequest && line.match(/^\s*note\s+left/)) {
                            inNote = true;
                            noteLines = [];
                        }
                    } else {
                        if (line.match(/^\s*end\s+note/)) {
                            inNote = false;
                            afterCallerRequest = false;
                            var body = noteLines
                                .filter(function(l) { return !l.match(/^\s*\$color\(gray\)\[/); })
                                .join('\n').trim();
                            if (body) payloads.push(body);
                        } else {
                            noteLines.push(line);
                        }
                    }
                }
                return payloads.join('\n\n');
            }

            function showToast(message) {
                var existing = document.querySelector('.diagram-ctx-toast');
                if (existing) existing.remove();
                var toast = document.createElement('div');
                toast.className = 'diagram-ctx-toast';
                toast.textContent = message;
                toast.style.cssText = 'position:fixed;bottom:20px;left:50%;transform:translateX(-50%);background:#333;color:#fff;padding:10px 20px;border-radius:6px;font:13px -apple-system,sans-serif;z-index:30000;opacity:1;transition:opacity 0.5s';
                document.body.appendChild(toast);
                setTimeout(function() { toast.style.opacity = '0'; }, 2500);
                setTimeout(function() { toast.remove(); }, 3000);
            }

            document.addEventListener('contextmenu', function(e) {
                var container = findDiagramContainer(e.target);
                if (!container) return;
                e.preventDefault();
                closeMenu();

                var diagramType = container.getAttribute('data-diagram-type');
                var svg = getSvg(container);
                var isHtmlContent = !svg && (diagramType === 'flamechart' || diagramType === 'calltree');

                // Need either an SVG or a recognized HTML content type
                if (!svg && !isHtmlContent) return;

                var source = getSource(container);
                var typeLabel = getTypeLabel(container);

                menu = document.createElement('div');
                menu.className = 'diagram-ctx-menu';

                // Check if right-click is on a note
                if (svg && window._findNoteGroups) {
                    var noteGroups = window._findNoteGroups(svg);
                    var clickedNoteIdx = -1;
                    for (var ni = 0; ni < noteGroups.length; ni++) {
                        var grp = noteGroups[ni];
                        var els = grp.paths.concat(grp.texts);
                        var found = false;
                        for (var ei = 0; ei < els.length; ei++) {
                            if (els[ei] === e.target || els[ei].contains(e.target)) {
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            // Check by coordinate — handles transparent hoverRect overlays
                            try {
                                var bbox = window._getNoteBBox(grp);
                                var pt = svg.createSVGPoint();
                                pt.x = e.clientX; pt.y = e.clientY;
                                var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                                if (svgPt.x >= bbox.x && svgPt.x <= bbox.x + bbox.width &&
                                    svgPt.y >= bbox.y && svgPt.y <= bbox.y + bbox.height) {
                                    found = true;
                                }
                            } catch(ex) {}
                        }
                        if (found) { clickedNoteIdx = ni; break; }
                    }
                    if (clickedNoteIdx >= 0) {
                        // Get full original content from the original source
                        var origSrc = container._noteOriginalSource || getSource(container);
                        var noteBlocks = window._parseNoteBlocks(origSrc);
                        var noteText;
                        if (noteBlocks[clickedNoteIdx]) {
                            noteText = noteBlocks[clickedNoteIdx].contentLines.map(function(l) {
                                return l.replace(/^\s*\$color\(gray\)/, '');
                            }).join('\n').trim();
                        } else {
                            noteText = noteGroups[clickedNoteIdx].texts.map(function(t) { return t.textContent; }).join('\n');
                        }
                        menu.appendChild(createMenuItem('Copy box text', function() {
                            navigator.clipboard.writeText(noteText);
                        }));
                        menu.appendChild(createSeparator());
                    }
                }

                var selectedText = (window.getSelection() || '').toString().trim();
                if (selectedText) {
                    menu.appendChild(createMenuItem('Copy Highlighted Text', function() {
                        navigator.clipboard.writeText(selectedText);
                    }));
                    menu.appendChild(createSeparator());
                }

                if (svg) {
                    // Full SVG menu
                    menu.appendChild(createMenuItem('Copy as PNG', function() {
                        svgToCanvas(svg, function(canvas) {
                            canvas.toBlob(function(blob) {
                                navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Copy as PNG (no transparency)', function() {
                        svgToCanvasWithBg(svg, function(canvas) {
                            canvas.toBlob(function(blob) {
                                navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Copy as SVG', function() {
                        navigator.clipboard.writeText(serializeSvg(svg));
                    }));
                    if (source) {
                        menu.appendChild(createMenuItem('Copy ' + typeLabel + ' source', function() {
                            navigator.clipboard.writeText(source);
                        }));
                        var origSource = container._noteOriginalSource || source;
                        if (origSource !== source) {
                            menu.appendChild(createMenuItem('Copy original ' + typeLabel + ' source', function() {
                                navigator.clipboard.writeText(origSource);
                            }));
                        }
                    }
                    menu.appendChild(createSeparator());
                    menu.appendChild(createMenuItem('Save as PNG', function() {
                        svgToCanvas(svg, function(canvas) {
                            canvas.toBlob(function(blob) {
                                var a = document.createElement('a');
                                a.href = URL.createObjectURL(blob);
                                a.download = getDiagramFilename(container, 'png');
                                a.click();
                                URL.revokeObjectURL(a.href);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Save as PNG (no transparency)', function() {
                        svgToCanvasWithBg(svg, function(canvas) {
                            canvas.toBlob(function(blob) {
                                var a = document.createElement('a');
                                a.href = URL.createObjectURL(blob);
                                a.download = getDiagramFilename(container, 'png');
                                a.click();
                                URL.revokeObjectURL(a.href);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Save as SVG', function() {
                        var blob = new Blob([serializeSvg(svg)], { type: 'image/svg+xml' });
                        var a = document.createElement('a');
                        a.href = URL.createObjectURL(blob);
                        a.download = getDiagramFilename(container, 'svg');
                        a.click();
                        URL.revokeObjectURL(a.href);
                    }));
                    menu.appendChild(createSeparator());
                    menu.appendChild(createMenuItem('Open as image in new tab', function() {
                        var svgData = serializeSvg(svg);
                        var b64 = btoa(unescape(encodeURIComponent(svgData)));
                        var html = '<html><body style="margin:0;display:flex;justify-content:center;align-items:start;min-height:100vh;background:#f5f5f5"><img src="data:image/svg+xml;base64,' + b64 + '" style="max-width:100%"></body></html>';
                        var blob = new Blob([html], { type: 'text/html' });
                        window.open(URL.createObjectURL(blob));
                    }));
                    if (source) {
                        menu.appendChild(createMenuItem('Open ' + typeLabel + ' source in new tab', function() {
                            var blob = new Blob([source], { type: 'text/plain' });
                            window.open(URL.createObjectURL(blob));
                        }));
                        var origSource2 = container._noteOriginalSource || source;
                        if (origSource2 !== source) {
                            menu.appendChild(createMenuItem('Open original ' + typeLabel + ' source in new tab', function() {
                                var blob = new Blob([origSource2], { type: 'text/plain' });
                                window.open(URL.createObjectURL(blob));
                            }));
                        }
                    }
                } else {
                    // HTML content (flame chart, call tree) — PNG only
                    menu.appendChild(createMenuItem('Copy as PNG', function() {
                        htmlToCanvas(container, function(canvas) {
                            canvas.toBlob(function(blob) {
                                navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Save as PNG', function() {
                        htmlToCanvas(container, function(canvas) {
                            canvas.toBlob(function(blob) {
                                var a = document.createElement('a');
                                a.href = URL.createObjectURL(blob);
                                a.download = getDiagramFilename(container, 'png');
                                a.click();
                                URL.revokeObjectURL(a.href);
                            }, 'image/png');
                        });
                    }));
                }

                if (source && diagramType === 'plantuml') {
                    var payloads = extractCallerPayloads(source);
                    if (payloads) {
                        menu.appendChild(createMenuItem('Copy All Caller Request Payloads', function() {
                            navigator.clipboard.writeText(payloads);
                            showToast('Copied ' + payloads.split('\n\n').length + ' request payload(s)');
                        }));
                    }
                }

                menu.appendChild(createSeparator());
                menu.appendChild(createMenuItem('Show default browser menu', function() {
                    showToast('To use the browser menu, right-click outside the diagram area.');
                }));

                document.body.appendChild(menu);

                var rect = menu.getBoundingClientRect();
                var x = e.clientX;
                var y = e.clientY;
                if (x + rect.width > window.innerWidth) x = window.innerWidth - rect.width - 4;
                if (y + rect.height > window.innerHeight) y = window.innerHeight - rect.height - 4;
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                menu.style.left = x + 'px';
                menu.style.top = y + 'px';
            });

            document.addEventListener('click', function(e) {
                if (menu && !menu.contains(e.target)) closeMenu();
            });
            document.addEventListener('keydown', function(e) {
                if (e.key === 'Escape') closeMenu();
            });
            document.addEventListener('scroll', closeMenu, true);
        })();
        </script>
        """;

    public static string GetInternalFlowPopupScript() => """
        <script>
        (function() {
            var iflowData = window.__iflowSegments || {};

            function showPopup(segmentId) {
                var existing = document.querySelector('.iflow-overlay');
                if (existing) existing.remove();

                var overlay = document.createElement('div');
                overlay.className = 'iflow-overlay';

                var popup = document.createElement('div');
                popup.className = 'iflow-popup';

                var closeBtn = document.createElement('button');
                closeBtn.className = 'iflow-popup-close';
                closeBtn.innerHTML = '&times;';
                closeBtn.onclick = function() { overlay.remove(); };
                popup.appendChild(closeBtn);

                var segment = iflowData[segmentId];
                if (segment && segment.content) {
                    var header = document.createElement('h3');
                    header.textContent = segment.title || 'Internal Flow';
                    popup.appendChild(header);

                    var diagramDiv = document.createElement('div');
                    diagramDiv.className = 'iflow-diagram';
                    diagramDiv.innerHTML = segment.content;
                    popup.appendChild(diagramDiv);

                    // Render flame chart from data if available
                    if (segment.flameData && window._renderPopupFlameCharts) {
                        window._renderPopupFlameCharts(diagramDiv, segment.flameData);
                    }

                    if (window.plantuml && diagramDiv.querySelector('.plantuml-browser')) {
                        diagramDiv.querySelectorAll('.plantuml-browser').forEach(function(el) {
                            function renderEl(source) {
                                try {
                                    var lines = source.split('\n');
                                    if (lines.length > 3000) {
                                        el.innerHTML = '<div style="color:#c00;padding:1em;border:1px solid #c00;border-radius:6px">' +
                                            '<strong>Activity diagram too large for browser rendering (' + lines.length + ' lines).</strong><br>' +
                                            'Use <code>CallTree</code> style for large relationship flows.</div>';
                                        return;
                                    }
                                    window.plantuml.render(lines, el.id);
                                    setTimeout(function() {
                                        var text = el.textContent || '';
                                        if (text.indexOf('RuntimeException') >= 0 || text.indexOf('RangeError') >= 0) {
                                            el.innerHTML = '<div style="color:#c00;padding:1em;border:1px solid #c00;border-radius:6px">' +
                                                '<strong>Activity diagram too large for browser rendering.</strong><br>' +
                                                'Use <code>CallTree</code> style for large relationship flows.</div>';
                                        }
                                    }, 100);
                                } catch(e) {
                                    el.textContent = 'Activity diagram too large for browser rendering. Use CallTree style instead.';
                                    el.style.color = '#c00';
                                }
                            }
                            var source = el.getAttribute('data-plantuml');
                            if (source) {
                                renderEl(source);
                            } else if (el.hasAttribute('data-plantuml-z')) {
                                decompressGzipBase64(el.getAttribute('data-plantuml-z')).then(function(decoded) {
                                    el.setAttribute('data-plantuml', decoded);
                                    renderEl(decoded);
                                }).catch(function() { el.textContent = 'Decompression error'; });
                            }
                        });
                    }
                } else {
                    var noData = document.createElement('div');
                    noData.className = 'iflow-no-data';
                    noData.textContent = segment && segment.message
                        ? segment.message
                        : 'No internal flow data available for this segment.';
                    popup.appendChild(noData);
                }

                overlay.appendChild(popup);

                // Wire up toggle buttons if present
                var toggleBtns = popup.querySelectorAll('.iflow-toggle-btn');
                if (toggleBtns.length) {
                    toggleBtns.forEach(function(btn) {
                        btn.addEventListener('click', function() {
                            var view = btn.getAttribute('data-view');
                            var container = popup.querySelector('.iflow-diagram');
                            if (!container) return;
                            toggleBtns.forEach(function(b) { b.classList.remove('iflow-toggle-active'); });
                            btn.classList.add('iflow-toggle-active');
                            var main = container.querySelector('.iflow-view-main');
                            var flame = container.querySelector('.iflow-view-flame');
                            if (main) main.style.display = view === 'main' ? '' : 'none';
                            if (flame) flame.style.display = view === 'flame' ? '' : 'none';
                        });
                    });
                }

                overlay.addEventListener('click', function(e) {
                    if (e.target === overlay) overlay.remove();
                });
                document.body.appendChild(overlay);
            }

            // Expose for direct binding from the render script
            window._iflowShowPopup = showPopup;

            // Fallback: document-level click handler (capture phase for IKVM/server SVG compatibility)
            document.addEventListener('click', function(e) {
                var el = e.target;
                while (el && el !== document) {
                    if (el.localName === 'a') {
                        var href = el.getAttribute('xlink:href') || el.getAttribute('href') || '';
                        if (href.indexOf('#iflow-') === 0) {
                            e.preventDefault();
                            e.stopPropagation();
                            showPopup(href.substring(1));
                            return;
                        }
                    }
                    el = el.parentNode;
                }
            }, true);

            document.addEventListener('keydown', function(e) {
                if (e.key === 'Escape') {
                    var overlay = document.querySelector('.iflow-overlay');
                    if (overlay) overlay.remove();
                }
            });
        })();
        </script>
        """;

    public static string GetToggleScript() => """
        <script>
        document.addEventListener('click', function(e) {
            var btn = e.target.closest ? e.target.closest('.iflow-toggle-btn') : null;
            if (!btn) return;
            var toggle = btn.closest('.iflow-toggle');
            if (!toggle) return;
            var container = toggle.parentElement;
            if (!container) return;
            var view = btn.getAttribute('data-view');
            toggle.querySelectorAll('.iflow-toggle-btn').forEach(function(b) {
                b.classList.toggle('iflow-toggle-active', b === btn);
            });
            container.querySelectorAll('.iflow-view').forEach(function(v) {
                v.style.display = 'none';
            });
            var target = container.querySelector('.iflow-view-' + view);
            if (target) {
                target.style.display = '';
                window._renderFlameCharts(target);
            }
        });
        document.addEventListener('click', function(e) {
            var btn = e.target.closest ? e.target.closest('.diagram-toggle-btn') : null;
            if (!btn) return;
            var toggle = btn.closest('.diagram-toggle');
            if (!toggle) return;
            var container = toggle.parentElement;
            if (!container) return;
            var dtype = btn.getAttribute('data-dtype');
            toggle.querySelectorAll('.diagram-toggle-btn').forEach(function(b) {
                b.classList.toggle('diagram-toggle-active', b === btn);
            });
            container.querySelectorAll('.diagram-view').forEach(function(v) {
                v.style.display = 'none';
            });
            var target = container.querySelector('.diagram-view-' + dtype);
            if (target) {
                target.style.display = '';
                if (window._renderFlameCharts) window._renderFlameCharts(target);
            }
        });
        </script>
        """;

    /// <summary>
    /// Client-side JavaScript that renders flame charts from compact JSON data.
    /// Flame chart elements with a <c>data-flame</c> attribute or inside popups
    /// with <c>flameData</c> are rendered on demand instead of being pre-rendered
    /// as HTML on the server, dramatically reducing report file size.
    /// </summary>
    public static string GetFlameChartRenderScript() => """
        <script>
        (function() {
            function renderFlameData(container, data) {
                if (!data || !data.s || !data.f || data.f.length === 0) return;
                if (container.dataset.flameRendered) return;
                container.dataset.flameRendered = '1';

                var sources = data.s;
                var hasMarkers = data.m && data.m.length > 0;
                if (hasMarkers) container.style.position = 'relative';

                var html = [];
                if (hasMarkers) {
                    for (var mi = 0; mi < data.m.length; mi++) {
                        var m = data.m[mi];
                        html.push('<div class="iflow-boundary-marker" style="left:' + m[0].toFixed(2) + '%" title="' + escHtml(m[1]) + '"></div>');
                    }
                }
                for (var i = 0; i < data.f.length; i++) {
                    var sp = data.f[i];
                    var srcIdx = sp[0], name = sp[1], leftPct = sp[2], widthPct = sp[3], depth = sp[4], durMs = sp[5];
                    var source = sources[srcIdx];
                    var hue = Math.abs(hashCode(source)) % 360;
                    var lightness = 70 + Math.min(depth * 5, 20);
                    var durText = durMs >= 1 ? ' (' + durMs + 'ms)' : '';
                    html.push('<div class="iflow-flame-bar" style="margin-left:' + leftPct.toFixed(2)
                        + '%;width:' + widthPct.toFixed(2) + '%;background:hsl(' + hue + ', 60%, ' + lightness + '%)" title="['
                        + escHtml(source) + '] ' + escHtml(name) + durText + '"><span class="iflow-flame-label">'
                        + escHtml(name) + durText + '</span></div>');
                }
                container.innerHTML = html.join('');
            }

            function renderSequentialFlameData(container, data) {
                if (!data || !data.s || !data.b || data.b.length === 0) return;
                if (container.dataset.flameRendered) return;
                container.dataset.flameRendered = '1';
                var sources = data.s;
                var html = [];
                for (var bi = 0; bi < data.b.length; bi++) {
                    var band = data.b[bi];
                    html.push('<div class="iflow-test-band"><div class="iflow-test-band-label">' + escHtml(band.id) + '</div>');
                    for (var i = 0; i < band.f.length; i++) {
                        var sp = band.f[i];
                        var srcIdx = sp[0], name = sp[1], leftPct = sp[2], widthPct = sp[3], depth = sp[4], durMs = sp[5];
                        var source = sources[srcIdx];
                        var hue = Math.abs(hashCode(source)) % 360;
                        var lightness = 70 + Math.min(depth * 5, 20);
                        var durText = durMs >= 1 ? ' (' + durMs + 'ms)' : '';
                        html.push('<div class="iflow-flame-bar" style="margin-left:' + leftPct.toFixed(2)
                            + '%;width:' + widthPct.toFixed(2) + '%;background:hsl(' + hue + ', 60%, ' + lightness + '%)" title="['
                            + escHtml(source) + '] ' + escHtml(name) + durText + '"><span class="iflow-flame-label">'
                            + escHtml(name) + durText + '</span></div>');
                    }
                    html.push('</div>');
                }
                container.innerHTML = html.join('');
            }

            function escHtml(s) {
                return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
            }

            function hashCode(s) {
                var h = 0;
                for (var i = 0; i < s.length; i++) {
                    h = ((h << 5) - h + s.charCodeAt(i)) | 0;
                }
                return h;
            }

            // Decompress gzip+base64 data (used for whole-test-flow compressed attributes)
            function decompressBase64(base64) {
                var raw = atob(base64);
                var bytes = new Uint8Array(raw.length);
                for (var i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
                var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'));
                return new Response(stream).text();
            }

            // Render all data-flame elements within a container (or document)
            function renderFlameCharts(root) {
                var els = (root || document).querySelectorAll('.iflow-flame[data-flame]');
                for (var i = 0; i < els.length; i++) {
                    if (els[i].dataset.flameRendered) continue;
                    try {
                        var data = JSON.parse(els[i].getAttribute('data-flame'));
                        renderFlameData(els[i], data);
                    } catch(e) {}
                }
                // Handle compressed flame data (whole-test-flow)
                var zEls = (root || document).querySelectorAll('.iflow-flame[data-flame-z]');
                for (var i = 0; i < zEls.length; i++) {
                    if (zEls[i].dataset.flameRendered) continue;
                    (function(el) {
                        el.dataset.flameRendered = '1';
                        decompressBase64(el.getAttribute('data-flame-z')).then(function(json) {
                            el.dataset.flameRendered = '';
                            var data = JSON.parse(json);
                            renderFlameData(el, data);
                        }).catch(function() {});
                    })(zEls[i]);
                }
                var seqEls = (root || document).querySelectorAll('.iflow-sequential-tests[data-flame]');
                for (var i = 0; i < seqEls.length; i++) {
                    if (seqEls[i].dataset.flameRendered) continue;
                    try {
                        var data = JSON.parse(seqEls[i].getAttribute('data-flame'));
                        renderSequentialFlameData(seqEls[i], data);
                    } catch(e) {}
                }
            }

            // Render flame charts from flameData property in popup segments
            function renderPopupFlameCharts(container, flameData) {
                if (!flameData) return;
                var el = container.querySelector('.iflow-flame[data-diagram-type="flamechart"]');
                if (el) renderFlameData(el, flameData);
            }

            // Expose globally
            window._renderFlameCharts = renderFlameCharts;
            window._renderPopupFlameCharts = renderPopupFlameCharts;

            // Auto-render visible data-flame elements on page load
            document.addEventListener('DOMContentLoaded', function() {
                renderFlameCharts(document);

                // Render on details expand
                document.addEventListener('toggle', function(e) {
                    if (e.target.tagName === 'DETAILS' && e.target.open) {
                        renderFlameCharts(e.target);
                    }
                }, true);
            });
        })();
        </script>
        """;

    public static string GetFocusModeScript() => """
        <style>
            .focus-dimmed { opacity: 0.15; transition: opacity 0.3s; }
            .focus-active { opacity: 1; transition: opacity 0.3s; }
        </style>
        <script>
        (function() {
            document.addEventListener('DOMContentLoaded', function() {
                var focused = null;

                function getNodeAlias(el) {
                    // Walk up to find a group (<g>) that contains a rect or polygon (C4 system/person)
                    var g = el.closest ? el.closest('g') : null;
                    if (!g) return null;
                    // Look for text elements inside the group to identify the node
                    var texts = g.querySelectorAll('text');
                    if (texts.length === 0) return null;
                    return texts[0].textContent.trim();
                }

                function getRelationships(svg) {
                    // Parse relationship edges: look for <g> containing <path> or <line> + <text>
                    var edges = [];
                    svg.querySelectorAll('g').forEach(function(g) {
                        var path = g.querySelector('path, line, polyline');
                        var textEls = g.querySelectorAll('text');
                        if (path && textEls.length > 0) {
                            edges.push({ group: g, label: textEls[0].textContent.trim() });
                        }
                    });
                    return edges;
                }

                function focusNode(nodeName, svgContainer) {
                    if (!svgContainer) return;
                    var svg = svgContainer.querySelector('svg');
                    if (!svg) return;

                    if (focused === nodeName) {
                        // Unfocus - remove all dim/active classes
                        svg.querySelectorAll('.focus-dimmed, .focus-active').forEach(function(el) {
                            el.classList.remove('focus-dimmed', 'focus-active');
                        });
                        focused = null;
                        return;
                    }

                    focused = nodeName;
                    // Dim everything, then highlight related elements
                    var allGroups = svg.querySelectorAll('g');
                    allGroups.forEach(function(g) {
                        g.classList.remove('focus-dimmed', 'focus-active');
                        // Check if this group contains the focused node name
                        var texts = g.querySelectorAll('text');
                        var isRelated = false;
                        texts.forEach(function(t) {
                            if (t.textContent.trim() === nodeName) isRelated = true;
                        });
                        if (isRelated) {
                            g.classList.add('focus-active');
                        } else {
                            g.classList.add('focus-dimmed');
                        }
                    });
                }

                // Expose for testing and external use
                window.focusNode = focusNode;

                // Bind click handlers to SVG nodes after render
                var mo = new MutationObserver(function(mutations) {
                    mutations.forEach(function(m) {
                        m.addedNodes.forEach(function(node) {
                            if (node.tagName === 'svg' || (node.querySelector && node.querySelector('svg'))) {
                                var svg = node.tagName === 'svg' ? node : node.querySelector('svg');
                                if (!svg) return;
                                svg.querySelectorAll('rect, polygon, ellipse').forEach(function(shape) {
                                    shape.style.cursor = 'pointer';
                                    shape.addEventListener('click', function(e) {
                                        var name = getNodeAlias(e.target);
                                        if (name) focusNode(name, svg.parentElement);
                                    });
                                });
                            }
                        });
                    });
                });
                document.querySelectorAll('.plantuml-browser').forEach(function(el) {
                    mo.observe(el, { childList: true, subtree: true });
                });
            });
        })();
        </script>
        """;

    public static string GetCollapsibleNotesScript() => """
        <script>
        (function() {
            var SVGNS = 'http://www.w3.org/2000/svg';

            function parseNoteBlocks(source) {
                if (!source) return [];
                var lines = source.split('\n');
                var notes = [];
                for (var i = 0; i < lines.length; i++) {
                    var trimmed = lines[i].trim();
                    if (/^note(?:<<\w+>>)?\s+(left|right)/.test(trimmed)) {
                        var start = i;
                        i++;
                        var contentLines = [];
                        while (i < lines.length && lines[i].trim() !== 'end note') {
                            contentLines.push(lines[i]);
                            i++;
                        }
                        notes.push({ start: start, end: i, contentLines: contentLines });
                    }
                }
                return notes;
            }

            function getNotePreview(contentLines) {
                var nonGray = contentLines.map(function(l) { return l.trim(); })
                    .filter(function(l) { return !l.match(/^\$color\(gray\)/); });
                var raw = nonGray.join(' ').trim();
                if (!raw) return '...';
                if (raw.length <= 60) return raw;
                return raw.substring(0, 60) + '...';
            }

            function hasNoteFill(pathEl) {
                var fill = (pathEl.getAttribute('fill') || '').toLowerCase().trim();
                if (!fill || fill === 'none' || fill === 'transparent') return false;
                if (fill === '#000000' || fill === '#000' || fill === 'black' || fill === 'rgb(0,0,0)') return false;
                if (/^#[0-9a-f]{6}00$/.test(fill)) return false;
                return true;
            }

            function findNoteGroups(svg) {
                var mainG = null;
                for (var i = 0; i < svg.children.length; i++) {
                    if (svg.children[i].tagName === 'g') { mainG = svg.children[i]; break; }
                }
                if (!mainG) return [];
                var children = Array.from(mainG.children);
                var groups = [];
                var ci = 0;
                while (ci < children.length) {
                    if (children[ci].tagName === 'g') { ci++; continue; }
                    if (children[ci].tagName === 'path' && hasNoteFill(children[ci])) {
                        var grp = { paths: [], texts: [] };
                        while (ci < children.length && children[ci].tagName === 'path') {
                            grp.paths.push(children[ci]);
                            ci++;
                        }
                        while (ci < children.length && children[ci].tagName === 'text') {
                            grp.texts.push(children[ci]);
                            ci++;
                        }
                        if (grp.paths.length > 0 && grp.texts.length > 0) {
                            groups.push(grp);
                        }
                    } else {
                        ci++;
                    }
                }
                return groups;
            }

            function getNoteBBox(grp) {
                var minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
                grp.paths.concat(grp.texts).forEach(function(el) {
                    try {
                        var bb = el.getBBox();
                        if (bb.x < minX) minX = bb.x;
                        if (bb.y < minY) minY = bb.y;
                        if (bb.x + bb.width > maxX) maxX = bb.x + bb.width;
                        if (bb.y + bb.height > maxY) maxY = bb.y + bb.height;
                    } catch(e) {}
                });
                return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
            }

            function isLongNote(contentLines) {
                return contentLines && contentLines.length > window._truncateLines;
            }

            // noteStep: 0=collapsed, 1=truncated, 2=expanded (3=truncated on way back down)
            function noteStepState(step) {
                if (step === 0) return 'collapsed';
                if (step === 2) return 'expanded';
                return 'truncated';
            }

            function createNoteButtons(svg, bbox, noteStep, onExpand, onContract, onTruncate, onCycle, contentLines) {
                var size = 12;
                var pad = 3;
                var state = noteStepState(noteStep);
                var longNote = isLongNote(contentLines);
                var buttons = [];

                // Top-right area: contract buttons — shown when expanded or truncated
                if (state === 'expanded' || state === 'truncated') {
                    // For expanded long notes: ▴ arrow to the left of −
                    if (state === 'expanded' && longNote) {
                        var ax = bbox.x + bbox.width - size * 2 - pad * 2;
                        var ay = bbox.y + pad;
                        var ga = document.createElementNS(SVGNS, 'g');
                        ga.setAttribute('class', 'note-toggle-icon');
                        ga.style.cursor = 'pointer';
                        ga.style.opacity = '0';
                        var bgA = document.createElementNS(SVGNS, 'rect');
                        bgA.setAttribute('x', ax); bgA.setAttribute('y', ay);
                        bgA.setAttribute('width', size); bgA.setAttribute('height', size);
                        bgA.setAttribute('rx', '2'); bgA.setAttribute('fill', '#ffffff');
                        bgA.setAttribute('stroke', '#999'); bgA.setAttribute('stroke-width', '0.5');
                        ga.appendChild(bgA);
                        var symA = document.createElementNS(SVGNS, 'text');
                        symA.setAttribute('x', ax + size / 2); symA.setAttribute('y', ay + size - 2.5);
                        symA.setAttribute('text-anchor', 'middle'); symA.setAttribute('font-size', '10');
                        symA.setAttribute('font-family', 'sans-serif'); symA.setAttribute('fill', '#666');
                        symA.style.pointerEvents = 'none';
                        symA.textContent = '\u25B2'; // ▲
                        ga.appendChild(symA);
                        bgA.addEventListener('click', function(ev) { ev.stopPropagation(); onTruncate(); });
                        buttons.push(ga);
                    }
                    // − (minus) button — top-right
                    var ix = bbox.x + bbox.width - size - pad;
                    var iy = bbox.y + pad;
                    var gc = document.createElementNS(SVGNS, 'g');
                    gc.setAttribute('class', 'note-toggle-icon');
                    gc.style.cursor = 'pointer';
                    gc.style.opacity = '0';
                    var bgC = document.createElementNS(SVGNS, 'rect');
                    bgC.setAttribute('x', ix); bgC.setAttribute('y', iy);
                    bgC.setAttribute('width', size); bgC.setAttribute('height', size);
                    bgC.setAttribute('rx', '2'); bgC.setAttribute('fill', '#ffffff');
                    bgC.setAttribute('stroke', '#999'); bgC.setAttribute('stroke-width', '0.5');
                    gc.appendChild(bgC);
                    var symC = document.createElementNS(SVGNS, 'line');
                    symC.setAttribute('x1', ix + 2); symC.setAttribute('y1', iy + size / 2);
                    symC.setAttribute('x2', ix + size - 2); symC.setAttribute('y2', iy + size / 2);
                    symC.setAttribute('stroke', '#666'); symC.setAttribute('stroke-width', '2');
                    symC.setAttribute('stroke-linecap', 'round');
                    symC.style.pointerEvents = 'none';
                    gc.appendChild(symC);
                    bgC.addEventListener('click', function(ev) { ev.stopPropagation(); onContract(); });
                    buttons.push(gc);
                }

                // Bottom-center expand button (▾) — shown when collapsed or truncated
                if (state === 'collapsed' || state === 'truncated') {
                    var expandW = size * 3;
                    var expandH = size;
                    var ex = bbox.x + (bbox.width - expandW) / 2;
                    var ey = bbox.y + bbox.height - expandH - pad;
                    var ge = document.createElementNS(SVGNS, 'g');
                    ge.setAttribute('class', 'note-toggle-icon');
                    ge.style.cursor = 'pointer';
                    ge.style.opacity = '0';
                    var bgE = document.createElementNS(SVGNS, 'rect');
                    bgE.setAttribute('x', ex); bgE.setAttribute('y', ey);
                    bgE.setAttribute('width', expandW); bgE.setAttribute('height', expandH);
                    bgE.setAttribute('rx', '2'); bgE.setAttribute('fill', '#ffffff');
                    bgE.setAttribute('stroke', '#999'); bgE.setAttribute('stroke-width', '0.5');
                    ge.appendChild(bgE);
                    var symE = document.createElementNS(SVGNS, 'text');
                    symE.setAttribute('x', ex + expandW / 2); symE.setAttribute('y', ey + expandH - 2.5);
                    symE.setAttribute('text-anchor', 'middle'); symE.setAttribute('font-size', '10');
                    symE.setAttribute('font-family', 'sans-serif'); symE.setAttribute('fill', '#666');
                    symE.style.pointerEvents = 'none';
                    symE.textContent = '\u25BC'; // ▼
                    ge.appendChild(symE);
                    bgE.addEventListener('click', function(ev) { ev.stopPropagation(); onExpand(); });
                    buttons.push(ge);
                }

                // Bottom-center contract button (▴) — shown when expanded and long note
                if (state === 'expanded' && longNote) {
                    var contractW = size * 3;
                    var contractH = size;
                    var cx = bbox.x + (bbox.width - contractW) / 2;
                    var cy = bbox.y + bbox.height - contractH - pad;
                    var gbc = document.createElementNS(SVGNS, 'g');
                    gbc.setAttribute('class', 'note-toggle-icon');
                    gbc.style.cursor = 'pointer';
                    gbc.style.opacity = '0';
                    var bgBC = document.createElementNS(SVGNS, 'rect');
                    bgBC.setAttribute('x', cx); bgBC.setAttribute('y', cy);
                    bgBC.setAttribute('width', contractW); bgBC.setAttribute('height', contractH);
                    bgBC.setAttribute('rx', '2'); bgBC.setAttribute('fill', '#ffffff');
                    bgBC.setAttribute('stroke', '#999'); bgBC.setAttribute('stroke-width', '0.5');
                    gbc.appendChild(bgBC);
                    var symBC = document.createElementNS(SVGNS, 'text');
                    symBC.setAttribute('x', cx + contractW / 2); symBC.setAttribute('y', cy + contractH - 2.5);
                    symBC.setAttribute('text-anchor', 'middle'); symBC.setAttribute('font-size', '10');
                    symBC.setAttribute('font-family', 'sans-serif'); symBC.setAttribute('fill', '#666');
                    symBC.style.pointerEvents = 'none';
                    symBC.textContent = '\u25B2'; // ▲
                    gbc.appendChild(symBC);
                    bgBC.addEventListener('click', function(ev) { ev.stopPropagation(); onTruncate(); });
                    buttons.push(gbc);
                }

                // Hover detection rect over the whole note
                var hoverRect = document.createElementNS(SVGNS, 'rect');
                hoverRect.setAttribute('x', bbox.x);
                hoverRect.setAttribute('y', bbox.y);
                hoverRect.setAttribute('width', bbox.width);
                hoverRect.setAttribute('height', bbox.height);
                hoverRect.setAttribute('fill', 'transparent');
                hoverRect.style.pointerEvents = 'all';
                hoverRect.addEventListener('mouseenter', function() {
                    buttons.forEach(function(b) { b.style.opacity = '1'; });
                });
                hoverRect.addEventListener('mouseleave', function() {
                    buttons.forEach(function(b) { b.style.opacity = '0'; });
                });
                hoverRect.addEventListener('dblclick', function(ev) {
                    ev.stopPropagation(); ev.preventDefault(); onCycle();
                });
                buttons.forEach(function(b) {
                    b.addEventListener('mouseenter', function() {
                        buttons.forEach(function(bb) { bb.style.opacity = '1'; });
                    });
                });

                // Tooltip for collapsed notes
                if (state === 'collapsed' && contentLines) {
                    var tipLines = contentLines.map(function(l) {
                        return l.replace(/^\s*\$color\(gray\)/, '');
                    });
                    var tipText = tipLines.join('\n').trim();
                    if (tipText) {
                        var displayLines = tipText.split('\n');
                        if (displayLines.length > window._truncateLines) {
                            tipText = displayLines.slice(0, window._truncateLines).join('\n') + '\n...';
                        }
                        var titleEl = document.createElementNS(SVGNS, 'title');
                        titleEl.textContent = tipText;
                        hoverRect.appendChild(titleEl);
                    }
                }

                svg.appendChild(hoverRect);
                buttons.forEach(function(b) { svg.appendChild(b); });
            }

            function makeNotesCollapsible(container) {
                var svg = container.querySelector('svg');
                if (!svg) return;
                var source = container._noteOriginalSource || container.getAttribute('data-plantuml');
                if (!source) return;
                if (!container._noteOriginalSource) container._noteOriginalSource = source;

                var noteBlocks = parseNoteBlocks(container._noteOriginalSource);
                if (noteBlocks.length === 0) return;
                if (!container._noteSteps) container._noteSteps = {};

                var noteGroups = findNoteGroups(svg);

                for (var ni = 0; ni < Math.min(noteGroups.length, noteBlocks.length); ni++) {
                    (function(idx) {
                        var grp = noteGroups[idx];
                        var bbox = getNoteBBox(grp);
                        var step = container._noteSteps[idx] || 0;
                        // Short notes only have steps 0 (collapsed) and 2 (expanded)
                        if (!isLongNote(noteBlocks[idx].contentLines) && step === 1) step = 2;
                        createNoteButtons(svg, bbox, step,
                            function() { setNoteState(container, idx, 2); },
                            function() { setNoteState(container, idx, 0); },
                            function() { setNoteState(container, idx, 1); },
                            function() {
                                var curStep = container._noteSteps[idx] || 0;
                                var long = isLongNote(noteBlocks[idx].contentLines);
                                var nextStep;
                                if (curStep === 2) nextStep = long ? 1 : 0;
                                else if (curStep === 1) nextStep = 0;
                                else nextStep = 2;
                                setNoteState(container, idx, nextStep);
                            },
                            noteBlocks[idx].contentLines);
                    })(ni);
                }
            }

            function buildSourceWithNoteStates(origSource, noteSteps, noteBlocks, hideHeaders) {
                var lines = origSource.split('\n');
                var newLines = [];
                var nIdx = 0;
                var inNote = false;
                var noteMode = 'normal'; // 'normal', 'collapsed', 'truncated'
                var truncateLineCount = 0;
                var justSkippedGray = false;

                for (var i = 0; i < lines.length; i++) {
                    var trimmed = lines[i].trim();
                    if (!inNote && /^note(?:<<\w+>>)?\s+(left|right)/.test(trimmed)) {
                        inNote = true;
                        justSkippedGray = false;
                        newLines.push(lines[i]);
                        var step = noteSteps[nIdx] || 0;
                        var state = noteStepState(step);
                        if (state === 'collapsed') {
                            noteMode = 'collapsed';
                            var preview = getNotePreview(noteBlocks[nIdx].contentLines);
                            newLines.push(preview);
                        } else if (state === 'truncated') {
                            noteMode = 'truncated';
                            truncateLineCount = 0;
                        } else {
                            noteMode = 'normal';
                        }
                        nIdx++;
                        continue;
                    }
                    if (inNote && trimmed === 'end note') {
                        if (noteMode === 'truncated' && truncateLineCount > window._truncateLines) {
                            newLines.push('...');
                        }
                        inNote = false;
                        noteMode = 'normal';
                        justSkippedGray = false;
                        newLines.push(lines[i]);
                        continue;
                    }
                    if (noteMode === 'collapsed') continue;
                    if (noteMode === 'truncated') {
                        if (hideHeaders && /^\$color\(gray\)/.test(trimmed)) { justSkippedGray = true; continue; }
                        if (justSkippedGray && trimmed === '') continue;
                        justSkippedGray = false;
                        truncateLineCount++;
                        if (truncateLineCount <= window._truncateLines) {
                            newLines.push(lines[i]);
                        }
                        continue;
                    }
                    if (inNote && hideHeaders && /^\$color\(gray\)/.test(trimmed)) { justSkippedGray = true; continue; }
                    if (justSkippedGray && trimmed === '') continue;
                    justSkippedGray = false;
                    newLines.push(lines[i]);
                }
                return newLines.join('\n');
            }

            var _svgCache = {};

            function setNoteState(container, noteIdx, targetStep) {
                if (container._noteRendering) return;
                if (!container._noteSteps) container._noteSteps = {};
                if (container._noteSteps[noteIdx] === targetStep) return;
                container._noteSteps[noteIdx] = targetStep;

                var origSource = container._noteOriginalSource;
                var noteBlocks = parseNoteBlocks(origSource);
                var newSource = buildSourceWithNoteStates(origSource, container._noteSteps, noteBlocks, !!container._headersHidden);

                container.setAttribute('data-plantuml', newSource);

                // Check SVG cache — skip plantuml.js re-render if we have a cached result
                if (_svgCache[newSource]) {
                    container.innerHTML = _svgCache[newSource];
                    if (window._iflowBindLinks) window._iflowBindLinks(container, origSource);
                    makeNotesCollapsible(container);
                    return;
                }

                container._noteRendering = true;

                var done = false;
                function afterRender() {
                    if (done) return;
                    done = true;
                    _svgCache[newSource] = container.innerHTML;
                    container._noteRendering = false;
                    if (window._iflowBindLinks) window._iflowBindLinks(container, origSource);
                    makeNotesCollapsible(container);
                }

                var mo = new MutationObserver(function(mutations) {
                    if (!container.querySelector('svg')) return;
                    mo.disconnect();
                    afterRender();
                });
                mo.observe(container, { childList: true, subtree: true });

                try {
                    window.plantuml.render(newSource.split('\n'), container.id);
                } catch(e) {
                    mo.disconnect();
                    container._noteRendering = false;
                }

                var pollCount = 0;
                var poll = setInterval(function() {
                    pollCount++;
                    if (done) { clearInterval(poll); return; }
                    var svg = container.querySelector('svg');
                    if (svg && !svg.querySelector('.note-toggle-icon')) {
                        clearInterval(poll);
                        mo.disconnect();
                        afterRender();
                    }
                    if (pollCount > 20) {
                        clearInterval(poll);
                        mo.disconnect();
                        container._noteRendering = false;
                    }
                }, 250);
            }

            window._makeNotesCollapsible = makeNotesCollapsible;
            window._findNoteGroups = findNoteGroups;
            window._getNoteBBox = getNoteBBox;
            window._parseNoteBlocks = parseNoteBlocks;

            // Global defaults
            window._headersHidden = false;
            window._truncateLines = 20;
            window._detailsDefault = 'expanded';

            // Pre-process source before initial render — applies current report-level defaults
            window._preProcessSource = function(el, source) {
                var noteBlocks = parseNoteBlocks(source);
                if (noteBlocks.length === 0) return source;
                el._noteOriginalSource = source;
                var state = window._detailsDefault;
                // Always initialize _noteSteps so that subsequent headers toggle
                // or details changes see the correct state (step 2 = expanded)
                if (!el._noteSteps) el._noteSteps = {};
                for (var i = 0; i < noteBlocks.length; i++) {
                    var targetStep;
                    if (state === 'expanded') { targetStep = 2; }
                    else if (state === 'truncated') { targetStep = isLongNote(noteBlocks[i].contentLines) ? 1 : 2; }
                    else { targetStep = 0; }
                    el._noteSteps[i] = targetStep;
                }
                if (state !== 'expanded' || window._headersHidden) {
                    el._headersHidden = window._headersHidden;
                    return buildSourceWithNoteStates(source, el._noteSteps, noteBlocks, window._headersHidden);
                }
                return source;
            };

            // Shared serialized render queue — TeaVM engine uses shared global state
            function processRenderQueue(queue, onAllDone) {
                function processNext() {
                    if (queue.length === 0) { if (onAllDone) onAllDone(); return; }
                    var item = queue.shift();
                    var container = item.container;
                    var newSource = buildSourceWithNoteStates(container._noteOriginalSource, container._noteSteps, item.noteBlocks, !!container._headersHidden);
                    container.setAttribute('data-plantuml', newSource);
                    // Check SVG cache first
                    if (_svgCache[newSource]) {
                        container.innerHTML = _svgCache[newSource];
                        if (window._iflowBindLinks) window._iflowBindLinks(container, container._noteOriginalSource);
                        makeNotesCollapsible(container);
                        processNext();
                        return;
                    }
                    container._noteRendering = true;
                    var done = false;
                    function afterRender() {
                        if (done) return;
                        done = true;
                        _svgCache[newSource] = container.innerHTML;
                        container._noteRendering = false;
                        if (window._iflowBindLinks) window._iflowBindLinks(container, container._noteOriginalSource);
                        makeNotesCollapsible(container);
                        processNext();
                    }
                    var mo = new MutationObserver(function() {
                        if (!container.querySelector('svg')) return;
                        mo.disconnect();
                        afterRender();
                    });
                    mo.observe(container, { childList: true, subtree: true });
                    try { window.plantuml.render(newSource.split('\n'), container.id); } catch(e) { mo.disconnect(); container._noteRendering = false; processNext(); }
                    var pollCount = 0;
                    var poll = setInterval(function() {
                        pollCount++;
                        if (done) { clearInterval(poll); return; }
                        var svg = container.querySelector('svg');
                        if (svg && !svg.querySelector('.note-toggle-icon')) { clearInterval(poll); mo.disconnect(); afterRender(); }
                        if (pollCount > 20) { clearInterval(poll); mo.disconnect(); container._noteRendering = false; processNext(); }
                    }, 250);
                }
                processNext();
            }

            function buildDetailsQueue(containers, targetState, force) {
                var queue = [];
                containers.forEach(function(container) {
                    if (!container._noteOriginalSource) container._noteOriginalSource = container.getAttribute('data-plantuml');
                    var noteBlocks = parseNoteBlocks(container._noteOriginalSource);
                    if (noteBlocks.length === 0) return;
                    if (!container._noteSteps) container._noteSteps = {};
                    var needsUpdate = false;
                    for (var i = 0; i < noteBlocks.length; i++) {
                        var targetStep;
                        if (targetState === 'expanded') {
                            targetStep = 2;
                        } else if (targetState === 'truncated') {
                            targetStep = isLongNote(noteBlocks[i].contentLines) ? 1 : 2;
                        } else {
                            targetStep = 0;
                        }
                        if ((container._noteSteps[i] || 0) !== targetStep) {
                            container._noteSteps[i] = targetStep;
                            needsUpdate = true;
                        }
                    }
                    if (needsUpdate || force) queue.push({ container: container, noteBlocks: noteBlocks });
                });
                return queue;
            }

            function buildHeadersQueue(containers, hiding) {
                var queue = [];
                containers.forEach(function(container) {
                    if (!container._noteOriginalSource) container._noteOriginalSource = container.getAttribute('data-plantuml');
                    var noteBlocks = parseNoteBlocks(container._noteOriginalSource);
                    if (noteBlocks.length === 0) return;
                    if (!container._noteSteps) container._noteSteps = {};
                    var wasHidden = !!container._headersHidden;
                    if (wasHidden === hiding) return;
                    container._headersHidden = hiding;
                    queue.push({ container: container, noteBlocks: noteBlocks });
                });
                return queue;
            }

            function syncRadioButtons(parent, targetState) {
                parent.querySelectorAll('.details-radio-btn[data-state]').forEach(function(b) {
                    if (b.getAttribute('data-state') === targetState) b.classList.add('details-active');
                    else b.classList.remove('details-active');
                });
                // Enable/disable dropdown
                parent.querySelectorAll('.truncate-lines-select').forEach(function(sel) {
                    sel.disabled = targetState !== 'truncated';
                });
            }

            // Scenario-level: expand/truncate/collapse details
            window._setAllNotes = function(btn, targetState) {
                var scenario = btn.closest('details.scenario');
                if (!scenario) return;
                syncRadioButtons(scenario, targetState);
                var containers = scenario.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildDetailsQueue(containers, targetState));
            };

            // Report-level: expand/truncate/collapse details for all scenarios
            window._setReportDetails = function(targetState) {
                window._detailsDefault = targetState;
                syncRadioButtons(document.querySelector('.toolbar-right'), targetState);
                // Reset all scenario-level radio buttons too
                document.querySelectorAll('details.scenario').forEach(function(sc) {
                    syncRadioButtons(sc, targetState);
                });
                var containers = document.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildDetailsQueue(containers, targetState));
            };

            // Change truncation line count (report-level)
            window._setTruncateLines = function(sel) {
                window._truncateLines = parseInt(sel.value, 10) || 20;
                // Sync all dropdowns (report + scenario level)
                document.querySelectorAll('.truncate-lines-select').forEach(function(s) {
                    s.value = String(window._truncateLines);
                });
                // Force re-render all containers as truncated (even if already truncated — line count changed)
                var containers = document.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildDetailsQueue(containers, 'truncated', true));
                // Update report + scenario radio buttons to truncated state
                syncRadioButtons(document.querySelector('.toolbar-right'), 'truncated');
                document.querySelectorAll('details.scenario').forEach(function(sc) {
                    syncRadioButtons(sc, 'truncated');
                });
            };

            // Change truncation line count (scenario-level)
            window._setScenarioTruncateLines = function(sel) {
                var scenario = sel.closest('details.scenario');
                if (!scenario) return;
                window._truncateLines = parseInt(sel.value, 10) || 20;
                document.querySelectorAll('.truncate-lines-select').forEach(function(s) {
                    s.value = String(window._truncateLines);
                });
                var containers = scenario.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildDetailsQueue(containers, 'truncated', true));
                syncRadioButtons(scenario, 'truncated');
            };

            function syncHeadersRadio(parent, state) {
                parent.querySelectorAll('.headers-radio-btn').forEach(function(b) {
                    if (b.getAttribute('data-hstate') === state) b.classList.add('details-active');
                    else b.classList.remove('details-active');
                });
            }

            // Report-level: show/hide headers for all scenarios
            window._setReportHeaders = function(state) {
                var hiding = state === 'hidden';
                window._headersHidden = hiding;
                syncHeadersRadio(document.querySelector('.toolbar-right'), state);
                document.querySelectorAll('details.scenario').forEach(function(sc) {
                    syncHeadersRadio(sc, state);
                });
                var containers = document.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildHeadersQueue(containers, hiding));
            };

            // Scenario-level: show/hide headers for one scenario
            window._setScenarioHeaders = function(btn, state) {
                var scenario = btn.closest('details.scenario');
                if (!scenario) return;
                var hiding = state === 'hidden';
                syncHeadersRadio(scenario, state);
                var containers = scenario.querySelectorAll('[data-plantuml]');
                processRenderQueue(buildHeadersQueue(containers, hiding));
            };
        })();
        </script>
        """;
}
