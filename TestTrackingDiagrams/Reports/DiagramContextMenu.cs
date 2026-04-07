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
        }
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
        .diagram-toggle { margin-top: 8px; margin-bottom: 8px; padding-left: 1em; }
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

    private const string PlantUmlJsCdnBase = "https://cdn.jsdelivr.net/gh/lemonlion/plantuml-js-plantuml_limit_size_8192@v1.2026.3beta6-patched";

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
                            renderQueue.push({ el: el, source: source });
                            processQueue();
                        } else if (el.hasAttribute('data-plantuml-z')) {
                            decompressGzipBase64(el.getAttribute('data-plantuml-z')).then(function(decoded) {
                                el.setAttribute('data-plantuml', decoded);
                                renderQueue.push({ el: el, source: decoded });
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
                document.querySelectorAll('.plantuml-browser').forEach(function(el) {
                    observer.observe(el);
                });
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

            function getSvg(container) {
                return container.querySelector('svg');
            }

            function getSource(container) {
                return container.getAttribute('data-plantuml') || container.getAttribute('data-mermaid-source') || '';
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
                // 3. Fall back to first visible rect fill (skip transparent rects)
                var rects = svg.querySelectorAll('rect');
                for (var i = 0; i < rects.length; i++) {
                    var rect = rects[i];
                    var fo = rect.getAttribute('fill-opacity');
                    if (fo !== null && parseFloat(fo) === 0) continue;
                    var rstyle = rect.getAttribute('style') || '';
                    var fom = rstyle.match(/fill-opacity\s*:\s*([^;]+)/);
                    if (fom && parseFloat(fom[1]) === 0) continue;
                    var fill = rect.getAttribute('fill');
                    if (fill) {
                        if (fill === 'none' || fill === 'transparent') continue;
                        // Skip 8-digit hex with zero alpha (e.g. #00000000 from plantuml-js)
                        if (/^#[0-9a-fA-F]{8}$/.test(fill) && fill.slice(7).toLowerCase() === '00') continue;
                        return fill;
                    }
                    var fm = rstyle.match(/fill\s*:\s*([^;]+)/);
                    if (fm && fm[1].trim() !== 'none' && fm[1].trim() !== 'transparent') return fm[1].trim();
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
                    ctx.fillStyle = bg;
                    ctx.fillRect(0, 0, canvas.width, canvas.height);
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
                    }
                    menu.appendChild(createSeparator());
                    menu.appendChild(createMenuItem('Save as PNG', function() {
                        svgToCanvas(svg, function(canvas) {
                            canvas.toBlob(function(blob) {
                                var a = document.createElement('a');
                                a.href = URL.createObjectURL(blob);
                                a.download = 'diagram.png';
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
                                a.download = 'diagram.png';
                                a.click();
                                URL.revokeObjectURL(a.href);
                            }, 'image/png');
                        });
                    }));
                    menu.appendChild(createMenuItem('Save as SVG', function() {
                        var blob = new Blob([serializeSvg(svg)], { type: 'image/svg+xml' });
                        var a = document.createElement('a');
                        a.href = URL.createObjectURL(blob);
                        a.download = 'diagram.svg';
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
                                a.download = 'diagram.png';
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
                            try {
                                var lines = el.getAttribute('data-plantuml').split('\n');
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
}
