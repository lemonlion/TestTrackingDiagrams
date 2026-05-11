
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
            .filter(function(l) { return !l.match(/^<color:gray>/); });
        var raw = nonGray.join(' ').trim();
        if (!raw) return '';
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

    function isLongNote(contentLines, truncateLines) {
        var limit = truncateLines || window._truncateLines;
        return contentLines && contentLines.length > limit;
    }

    // noteStep: 0=collapsed, 1=truncated, 2=expanded (3=truncated on way back down)
    function noteStepState(step) {
        if (step === 0) return 'collapsed';
        if (step === 2) return 'expanded';
        return 'truncated';
    }

    function createNoteButtons(svg, bbox, noteStep, onExpand, onContract, onTruncate, onCycle, contentLines, grp) {
        var size = 12;
        var topSize = 14;
        var pad = 3;
        var state = noteStepState(noteStep);
        var longNote = isLongNote(contentLines);
        var buttons = [];

        // Top-right area: contract buttons — shown when expanded or truncated
        if (state === 'expanded' || state === 'truncated') {
            // For expanded long notes: ▴ arrow to the left of −
            if (state === 'expanded' && longNote) {
                var ax = bbox.x + bbox.width - topSize * 2 - pad * 2;
                var ay = bbox.y + pad;
                var ga = document.createElementNS(SVGNS, 'g');
                ga.setAttribute('class', 'note-toggle-icon');
                ga.style.cursor = 'pointer';
                ga.style.opacity = '0';
                var bgA = document.createElementNS(SVGNS, 'rect');
                bgA.setAttribute('x', ax); bgA.setAttribute('y', ay);
                bgA.setAttribute('width', topSize); bgA.setAttribute('height', topSize);
                bgA.setAttribute('rx', '2'); bgA.setAttribute('fill', '#ffffff');
                bgA.setAttribute('stroke', '#999'); bgA.setAttribute('stroke-width', '0.5');
                ga.appendChild(bgA);
                var symA = document.createElementNS(SVGNS, 'text');
                symA.setAttribute('x', ax + topSize / 2); symA.setAttribute('y', ay + topSize - 3);
                symA.setAttribute('text-anchor', 'middle'); symA.setAttribute('font-size', '12');
                symA.setAttribute('font-family', 'sans-serif'); symA.setAttribute('fill', '#666');
                symA.style.pointerEvents = 'none';
                symA.textContent = '\u25B2'; // ▲
                ga.appendChild(symA);
                bgA.addEventListener('click', function(ev) { ev.stopPropagation(); onTruncate(); });
                buttons.push(ga);
            }
            // − (minus) button — top-right
            var ix = bbox.x + bbox.width - topSize - pad;
            var iy = bbox.y + pad;
            var gc = document.createElementNS(SVGNS, 'g');
            gc.setAttribute('class', 'note-toggle-icon');
            gc.setAttribute('data-note-btn', 'minus');
            gc.style.cursor = 'pointer';
            gc.style.opacity = '0';
            var bgC = document.createElementNS(SVGNS, 'rect');
            bgC.setAttribute('x', ix); bgC.setAttribute('y', iy);
            bgC.setAttribute('width', topSize); bgC.setAttribute('height', topSize);
            bgC.setAttribute('rx', '2'); bgC.setAttribute('fill', '#ffffff');
            bgC.setAttribute('stroke', '#999'); bgC.setAttribute('stroke-width', '0.5');
            gc.appendChild(bgC);
            var symC = document.createElementNS(SVGNS, 'line');
            symC.setAttribute('x1', ix + 3); symC.setAttribute('y1', iy + topSize / 2);
            symC.setAttribute('x2', ix + topSize - 3); symC.setAttribute('y2', iy + topSize / 2);
            symC.setAttribute('stroke', '#666'); symC.setAttribute('stroke-width', '2');
            symC.setAttribute('stroke-linecap', 'round');
            symC.style.pointerEvents = 'none';
            gc.appendChild(symC);
            bgC.addEventListener('click', function(ev) { ev.stopPropagation(); onContract(); });
            buttons.push(gc);
        }

        // + (plus) button — top-right, shown when collapsed
        if (state === 'collapsed') {
            var px = bbox.x + bbox.width - topSize - pad;
            var py = bbox.y + pad;
            var gp = document.createElementNS(SVGNS, 'g');
            gp.setAttribute('class', 'note-toggle-icon');
            gp.setAttribute('data-note-btn', 'plus');
            gp.style.cursor = 'pointer';
            gp.style.opacity = '0';
            var bgP = document.createElementNS(SVGNS, 'rect');
            bgP.setAttribute('x', px); bgP.setAttribute('y', py);
            bgP.setAttribute('width', topSize); bgP.setAttribute('height', topSize);
            bgP.setAttribute('rx', '2'); bgP.setAttribute('fill', '#ffffff');
            bgP.setAttribute('stroke', '#999'); bgP.setAttribute('stroke-width', '0.5');
            gp.appendChild(bgP);
            var symPH = document.createElementNS(SVGNS, 'line');
            symPH.setAttribute('x1', px + 3); symPH.setAttribute('y1', py + topSize / 2);
            symPH.setAttribute('x2', px + topSize - 3); symPH.setAttribute('y2', py + topSize / 2);
            symPH.setAttribute('stroke', '#666'); symPH.setAttribute('stroke-width', '2');
            symPH.setAttribute('stroke-linecap', 'round');
            symPH.style.pointerEvents = 'none';
            gp.appendChild(symPH);
            var symPV = document.createElementNS(SVGNS, 'line');
            symPV.setAttribute('x1', px + topSize / 2); symPV.setAttribute('y1', py + 3);
            symPV.setAttribute('x2', px + topSize / 2); symPV.setAttribute('y2', py + topSize - 3);
            symPV.setAttribute('stroke', '#666'); symPV.setAttribute('stroke-width', '2');
            symPV.setAttribute('stroke-linecap', 'round');
            symPV.style.pointerEvents = 'none';
            gp.appendChild(symPV);
            bgP.addEventListener('click', function(ev) { ev.stopPropagation(); onExpand(); });
            buttons.push(gp);
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

        // Hover detection via the note's own SVG elements (paths = background, texts = content).
        // This avoids an overlay rect that would block native text selection.
        var _noteHideTimeout;
        function _noteShowButtons() {
            clearTimeout(_noteHideTimeout);
            buttons.forEach(function(b) { b.style.opacity = '1'; });
        }
        function _noteScheduleHide() {
            _noteHideTimeout = setTimeout(function() {
                buttons.forEach(function(b) { b.style.opacity = '0'; });
            }, 300);
        }

        // Note background paths: hover detection + dblclick to cycle state
        if (grp) {
            grp.paths.forEach(function(p) {
                p.addEventListener('mouseenter', _noteShowButtons);
                p.addEventListener('mouseleave', _noteScheduleHide);
                p.addEventListener('dblclick', function(ev) {
                    ev.stopPropagation(); ev.preventDefault(); onCycle();
                });
            });
            // Note text elements: hover detection only (dblclick selects word naturally)
            grp.texts.forEach(function(t) {
                t.addEventListener('mouseenter', _noteShowButtons);
                t.addEventListener('mouseleave', _noteScheduleHide);
            });
        }

        buttons.forEach(function(b) {
            b.addEventListener('mouseenter', _noteShowButtons);
            b.addEventListener('mouseleave', _noteScheduleHide);
        });

        // Tooltip for collapsed notes
        if (state === 'collapsed' && contentLines && grp && grp.paths.length > 0) {
            var tipLines = contentLines.map(function(l) {
                return l.replace(/^\s*<color:gray>/, '');
            });
            var tipText = tipLines.join('\n').trim();
            if (tipText) {
                var displayLines = tipText.split('\n');
                if (displayLines.length > window._truncateLines) {
                    tipText = displayLines.slice(0, window._truncateLines).join('\n') + '\n...';
                }
                var titleEl = document.createElementNS(SVGNS, 'title');
                titleEl.textContent = tipText;
                grp.paths[0].appendChild(titleEl);
            }
        }

        // Transparent hover-detection rect covering the note bounding box.
        // This ensures mouseenter/mouseleave fire reliably even when other
        // SVG elements (arrows, lifeline labels) overlap the note area.
        // Inserted inside mainG BEFORE the note's path elements so that
        // text elements (which follow paths in SVG order) remain on top
        // and native text selection is preserved.
        var hoverRect = document.createElementNS(SVGNS, 'rect');
        hoverRect.setAttribute('x', bbox.x);
        hoverRect.setAttribute('y', bbox.y);
        hoverRect.setAttribute('width', bbox.width);
        hoverRect.setAttribute('height', bbox.height);
        hoverRect.setAttribute('fill', 'transparent');
        hoverRect.setAttribute('stroke', 'none');
        hoverRect.setAttribute('class', 'note-hover-rect');
        hoverRect.style.pointerEvents = 'all';
        hoverRect.addEventListener('mouseenter', _noteShowButtons);
        hoverRect.addEventListener('mouseleave', _noteScheduleHide);
        hoverRect.addEventListener('dblclick', function(ev) {
            ev.stopPropagation(); ev.preventDefault(); onCycle();
        });
        grp.paths[0].parentNode.insertBefore(hoverRect, grp.paths[0]);

        // Insert buttons on top (after hoverRect so they receive clicks)
        buttons.forEach(function(b) { svg.appendChild(b); });
    }

    function makeNotesCollapsible(container) {
        var svg = container.querySelector('svg');
        if (!svg) return;

        // Remove existing toggle icons and hover rects from previous renders
        svg.querySelectorAll('.note-toggle-icon').forEach(function(el) { el.remove(); });
        svg.querySelectorAll('.note-hover-rect').forEach(function(el) { el.remove(); });

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
                    noteBlocks[idx].contentLines, grp);
            })(ni);
        }
    }

    function buildSourceWithNoteStates(origSource, noteSteps, noteBlocks, hideHeaders, truncateLines) {
        var limit = truncateLines || window._truncateLines;
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
                    if (preview) newLines.push(preview);
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
                if (noteMode === 'truncated' && truncateLineCount > limit) {
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
                if (hideHeaders && /^<color:gray>/.test(trimmed)) { justSkippedGray = true; continue; }
                if (justSkippedGray && trimmed === '') continue;
                justSkippedGray = false;
                truncateLineCount++;
                if (truncateLineCount <= limit) {
                    newLines.push(lines[i]);
                }
                continue;
            }
            if (inNote && hideHeaders && /^<color:gray>/.test(trimmed)) { justSkippedGray = true; continue; }
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
        var newSource = buildSourceWithNoteStates(origSource, container._noteSteps, noteBlocks, !!container._headersHidden, container._truncateLines);

        container.setAttribute('data-plantuml', newSource);

        // Check SVG cache — skip plantuml.js re-render if we have a cached result
        if (_svgCache[newSource]) {
            container.innerHTML = _svgCache[newSource];
            if (window._iflowBindLinks) window._iflowBindLinks(container, origSource);
            makeNotesCollapsible(container);
            requestAnimationFrame(function() { if (window._addZoomButton) window._addZoomButton(container); });
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
            requestAnimationFrame(function() { if (window._addZoomButton) window._addZoomButton(container); });
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
    window._truncateLines = 40;
    window._detailsDefault = 'truncated';

    // Pre-process source before initial render — applies current report-level defaults
    window._preProcessSource = function(el, source) {
        var noteBlocks = parseNoteBlocks(source);
        if (noteBlocks.length === 0) return source;
        el._noteOriginalSource = source;
        var state = window._detailsDefault;
        // Always initialize _noteSteps so that subsequent headers toggle
        // or details changes see the correct state (step 2 = expanded)
        if (!el._noteSteps) el._noteSteps = {};
        el._truncateLines = window._truncateLines;
        for (var i = 0; i < noteBlocks.length; i++) {
            var targetStep;
            if (state === 'expanded') { targetStep = 2; }
            else if (state === 'truncated') { targetStep = isLongNote(noteBlocks[i].contentLines, el._truncateLines) ? 1 : 2; }
            else { targetStep = 0; }
            el._noteSteps[i] = targetStep;
        }
        el._headersHidden = window._headersHidden;
        if (state !== 'expanded' || window._headersHidden) {
            return buildSourceWithNoteStates(source, el._noteSteps, noteBlocks, window._headersHidden, el._truncateLines);
        }
        return source;
    };

    // Shared serialized render queue — TeaVM engine uses shared global state
    function processRenderQueue(queue, onAllDone) {
        function processNext() {
            if (queue.length === 0) { if (onAllDone) onAllDone(); return; }
            var item = queue.shift();
            var container = item.container;
            var newSource = buildSourceWithNoteStates(container._noteOriginalSource, container._noteSteps, item.noteBlocks, !!container._headersHidden, container._truncateLines);
            container.setAttribute('data-plantuml', newSource);
            // Check SVG cache first
            if (_svgCache[newSource]) {
                container.innerHTML = _svgCache[newSource];
                if (window._iflowBindLinks) window._iflowBindLinks(container, container._noteOriginalSource);
                makeNotesCollapsible(container);
                requestAnimationFrame(function() { if (window._addZoomButton) window._addZoomButton(container); });
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
                requestAnimationFrame(function() { if (window._addZoomButton) window._addZoomButton(container); });
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
            if (container._headersHidden === undefined) container._headersHidden = window._headersHidden;
            if (container._truncateLines === undefined) container._truncateLines = window._truncateLines;
            var containerLines = container._truncateLines;
            var needsUpdate = false;
            for (var i = 0; i < noteBlocks.length; i++) {
                var targetStep;
                if (targetState === 'expanded') {
                    targetStep = 2;
                } else if (targetState === 'truncated') {
                    targetStep = isLongNote(noteBlocks[i].contentLines, containerLines) ? 1 : 2;
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
            if (container._truncateLines === undefined) container._truncateLines = window._truncateLines;
            if (!container._noteSteps) {
                container._noteSteps = {};
                var state = window._detailsDefault;
                var containerLines = container._truncateLines;
                for (var i = 0; i < noteBlocks.length; i++) {
                    if (state === 'expanded') { container._noteSteps[i] = 2; }
                    else if (state === 'truncated') { container._noteSteps[i] = isLongNote(noteBlocks[i].contentLines, containerLines) ? 1 : 2; }
                    else { container._noteSteps[i] = 0; }
                }
            }
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
        // When switching to truncated, read the scenario's dropdown value
        if (targetState === 'truncated') {
            var sel = scenario.querySelector('.truncate-lines-select');
            if (sel) {
                var scenarioLines = parseInt(sel.value, 10) || window._truncateLines;
                var containers = scenario.querySelectorAll('[data-plantuml]');
                containers.forEach(function(c) { c._truncateLines = scenarioLines; });
            }
        }
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
        // When switching to truncated at report level, sync container truncate lines from global
        if (targetState === 'truncated') {
            document.querySelectorAll('[data-plantuml]').forEach(function(c) {
                c._truncateLines = window._truncateLines;
            });
            // Sync scenario dropdowns to match global value
            document.querySelectorAll('.truncate-lines-select').forEach(function(s) {
                s.value = String(window._truncateLines);
            });
        }
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
        // Update per-container truncate lines
        var containers = document.querySelectorAll('[data-plantuml]');
        containers.forEach(function(c) { c._truncateLines = window._truncateLines; });
        // Force re-render all containers as truncated (even if already truncated — line count changed)
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
        var scenarioLines = parseInt(sel.value, 10) || 20;
        // Store per-container so headers toggle and details toggle preserve the value
        var containers = scenario.querySelectorAll('[data-plantuml]');
        containers.forEach(function(c) { c._truncateLines = scenarioLines; });
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

