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
            z-index: 10000;
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

    public static string GetPlantUmlBrowserRenderScript() => """
        <script src="https://plantuml.github.io/plantuml/js-plantuml/viz-global.js"></script>
        <script src="https://plantuml.github.io/plantuml/js-plantuml/plantuml.js"></script>
        <script>
            plantumlLoad();
            document.addEventListener('DOMContentLoaded', function() {
                var renderQueue = [];
                var rendering = false;
                function processQueue() {
                    if (rendering || renderQueue.length === 0) return;
                    rendering = true;
                    var item = renderQueue.shift();
                    var lines = item.source.split('\n');
                    var mo = new MutationObserver(function() {
                        mo.disconnect();
                        rendering = false;
                        processQueue();
                    });
                    mo.observe(item.el, { childList: true, subtree: true });
                    window.plantuml.render(lines, item.el.id);
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
            var skipNext = false;

            function findDiagramContainer(el) {
                while (el) {
                    if (el.classList && (el.classList.contains('plantuml-browser') || el.classList.contains('mermaid')))
                        return el;
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

            document.addEventListener('contextmenu', function(e) {
                if (skipNext) { skipNext = false; return; }
                var container = findDiagramContainer(e.target);
                if (!container) return;
                var svg = getSvg(container);
                if (!svg) return;
                e.preventDefault();
                closeMenu();

                var source = getSource(container);
                var typeLabel = getTypeLabel(container);

                menu = document.createElement('div');
                menu.className = 'diagram-ctx-menu';

                menu.appendChild(createMenuItem('Copy as PNG', function() {
                    svgToCanvas(svg, function(canvas) {
                        canvas.toBlob(function(blob) {
                            navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
                        }, 'image/png');
                    });
                }));
                menu.appendChild(createMenuItem('Copy as SVG', function() {
                    navigator.clipboard.writeText(serializeSvg(svg));
                }));
                menu.appendChild(createMenuItem('Copy ' + typeLabel + ' source', function() {
                    navigator.clipboard.writeText(source);
                }));
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
                menu.appendChild(createMenuItem('Open ' + typeLabel + ' source in new tab', function() {
                    var blob = new Blob([source], { type: 'text/plain' });
                    window.open(URL.createObjectURL(blob));
                }));
                menu.appendChild(createSeparator());
                menu.appendChild(createMenuItem('Show default browser menu', function() {
                    skipNext = true;
                    var evt = new MouseEvent('contextmenu', {
                        bubbles: true, clientX: e.clientX, clientY: e.clientY
                    });
                    e.target.dispatchEvent(evt);
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

    public const string SearchExtensionJs = """
                                    // Also search diagram source from data attributes
                                    let diagramEls = scenarios[i].querySelectorAll('[data-plantuml],[data-mermaid-source]');
                                    for (let d = 0; d < diagramEls.length; d++) {
                                        var src = diagramEls[d].getAttribute('data-plantuml')
                                               || diagramEls[d].getAttribute('data-mermaid-source');
                                        if (src) text += ' ' + src.toLowerCase();
                                    }
                                    """;
}
