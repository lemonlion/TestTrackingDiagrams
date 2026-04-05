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
        .plantuml-inline-svg svg {
            max-width: 100%;
            height: auto;
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
        .iflow-toggle { margin-bottom: 8px; }
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
        .whole-test-flow { margin-top: 8px; }
        .whole-test-flow > summary { cursor: pointer; font-weight: 600; color: #555; }
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
        """;

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
                        var label = m[2].split('\\n')[0].trim();
                        map[label] = m[1];
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
                    var bound = 0;
                    container.querySelectorAll('a').forEach(function(a) {
                        var href = a.getAttribute('xlink:href') || a.getAttribute('href') || '';
                        if (href.indexOf('#iflow-') !== 0) return;
                        a.style.cursor = 'pointer';
                        a.addEventListener('click', function(ev) {
                            ev.preventDefault();
                            ev.stopPropagation();
                            if (window._iflowShowPopup) window._iflowShowPopup(href.substring(1));
                        });
                        bound++;
                    });
                    if (bound > 0) return;
                    if (!source) return;
                    var iflowMap = extractIflowMap(source);
                    var labels = Object.keys(iflowMap);
                    if (labels.length === 0) return;
                    container.querySelectorAll('text').forEach(function(textEl) {
                        var txt = textEl.textContent.replace(/\s+/g, ' ').trim();
                        if (!txt) return;
                        for (var i = 0; i < labels.length; i++) {
                            if (txt === labels[i]) {
                                var segId = iflowMap[labels[i]];
                                textEl.style.cursor = 'pointer';
                                textEl.style.pointerEvents = 'all';
                                textEl.addEventListener('click', function(ev) {
                                    ev.preventDefault();
                                    ev.stopPropagation();
                                    if (window._iflowShowPopup) window._iflowShowPopup(segId);
                                });
                                bound++;
                                break;
                            }
                        }
                    });
                }
                var observer = new IntersectionObserver(function(entries) {
                    entries.forEach(function(entry) {
                        if (!entry.isIntersecting) return;
                        var el = entry.target;
                        if (el.dataset.rendered) return;
                        el.dataset.rendered = '1';
                        observer.unobserve(el);
                        renderQueue.push({ el: el, source: el.getAttribute('data-plantuml') });
                        processQueue();
                    });
                }, { rootMargin: '200px' });
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
                var rect = svg.querySelector('rect');
                if (rect) {
                    var fill = rect.getAttribute('fill');
                    if (fill && fill !== 'none') return fill;
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
                    ctx.fillRect(0, 0, img.naturalWidth, img.naturalHeight);
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

                    if (window.plantuml && diagramDiv.querySelector('.plantuml-browser')) {
                        diagramDiv.querySelectorAll('.plantuml-browser').forEach(function(el) {
                            try {
                                var lines = el.getAttribute('data-plantuml').split('\n');
                                window.plantuml.render(lines, el.id);
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
            if (target) target.style.display = '';
        });
        </script>
        """;
}
