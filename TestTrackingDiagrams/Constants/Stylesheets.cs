namespace TestTrackingDiagrams;

public class Stylesheets
{
    public const string HtmlReportStyleSheet =
        """
               body {
                   font-family: sans-serif;
               }
        
               .raw-plantuml {
                   border: solid 1px;
                   padding-left: 1em;
                   padding-right: 1em;
                   margin-left: 1em;
                   margin-right: 1em;
                }
        
               summary {
                   cursor: pointer;
               }
        
               details {
                   margin-top: 1em;
                   margin-bottom: 1em;
               }
               
               .feature {
                    margin-top: 1em;
                    margin-bottom: 1em;
                    background-color: rgb(224, 224, 224);
                    padding: 1em;
                    border-radius: 10px;
                    content-visibility: auto;
                    contain-intrinsic-size: auto 500px;
                }
                
                .scenario {
                    margin-top: 1em;
                    margin-bottom: 1em;
                    background-color: white;
                    padding: 1em;
                    border-radius: 10px;
                }
                
                .example-diagrams {
                    border-radius: 1em;
                    border: 1px solid;
                    border-color: rgb(224, 224, 224);
                }

                [data-diagram-type]:not(:has(svg)):not(:has(img)) {
                    padding: 1em;
                }
        
                .example-diagrams > summary {
                    background-color: white;
                    padding: 1em;
                    border-radius: 1em;
                }
                
                .example-image {
                    padding: 1em;
                    padding-top: 0;
                    padding-bottom: 0;
                }
        
                body > details > details > details.example {
                    background-color: white;
                    padding: 1em;
                    margin-top: 0;
                }
                
                .h2 {
                    font-size: 1.5em;
                    font-weight: bold;
                }
                
                .h3 {
                    font-size: 1.1em;
                    font-weight: bold;
                }
        
                .h4 {
                    font-size: 1em;
                    font-weight: bold;
                }
                
                .column-header {
                    font-weight: bold;
                }
                
                .test-execution-summary {
                    background-color: rgb(224, 224, 224);
                    border-radius: 1em;
                    padding: 1em;
                    flex-shrink: 0;
                }
                
                .test-execution-summary h2 {
                    margin-top: 0;
                }
                
                .test-execution-summary table td {
                    padding: 0.25em;
                }

                .header-row {
                    display: flex;
                    gap: 1em;
                    align-items: stretch;
                }

                .summary-chart {
                    width: 120px;
                    height: 120px;
                    flex-shrink: 0;
                    align-self: center;
                }

                .ci-metadata {
                    align-self: center;
                    flex-shrink: 0;
                }

                .ci-metadata table {
                    font-size: 0.85em;
                }

                .filtering-box {
                    flex: 1;
                    min-width: 0;
                    background-color: rgb(224, 224, 224);
                    border-radius: 1em;
                    padding: 1em;
                }

                .filtering-box h2 {
                    margin-top: 0;
                    margin-bottom: 0;
                }

                .filtering-box-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 0.5em;
                }

                .filtering-box-export {
                    display: flex;
                    gap: 0.3em;
                }

                .filters {
                    display: flex;
                    flex-direction: column;
                    gap: 1em;
                }

                .filter-search {
                    width: 100%;
                }

                #searchbar {
                    padding: 0.5em;
                    border-radius: 0.5em;
                    width: 100%;
                    box-sizing: border-box;
                    border: 1px solid rgb(180, 180, 180);
                }

                .filter-row {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 0.5em;
                    align-items: center;
                }

                .failure-result {
                    padding: 1em;
                    border: 1px solid;
                    border-color: rgb(224, 224, 224);
                    border-radius: 1em;
                    color: rgb(191,0,0);
                    background-color: rgb(255,236,242);
                }

                .example-diagrams[open] > pre {
                    border-bottom-left-radius: 0;
                    border-bottom-right-radius: 0;
                }

                .failure-result pre {
                    overflow-x: scroll;
                    padding: 1em;
                }

                .failed {
                    color: rgb(191,0,0);
                }

                .skipped {
                    color: #949494;
                }
                
                span.label {
                    background-color: rgb(200, 200, 200);
                    color: white;
                    padding: 0.3em;
                    border-radius: 0.3em;
                    font-size: 0.7em;
                    white-space: nowrap;
                    font-weight: normal;
                    display: inline-block;
                }
                
                .endpoint {
                    font-size: 0.8em;
                    font-weight: normal;
                    font-style: italic;
                    margin-top: 0.2em;
                    color: rgb(110, 110, 110);
                    float: right;
                }
                
                .search-hidden {
                    display: none !important;
                }
                
                .dep-hidden {
                    display: none !important;
                }
                
                .status-hidden {
                    display: none !important;
                }
                
                .hp-hidden {
                    display: none !important;
                }
                
                .happy-path-filters, .dependency-filters, .status-filters {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    gap: 0.3em;
                }

                .happy-path-filters {
                    margin-left: 1em;
                }
                
                .happy-path-filters-label, .dependency-filters-label, .status-filters-label {
                    font-weight: bold;
                    margin-right: 0.3em;
                }
                
                .happy-path-toggle, .dependency-toggle, .status-toggle {
                    padding: 0.25em 0.6em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: white;
                    cursor: pointer;
                    font-size: 0.85em;
                }
                
                .happy-path-toggle:hover, .dependency-toggle:hover, .status-toggle:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }
                
                .happy-path-toggle.happy-path-active, .dependency-toggle.dependency-active, .status-toggle.status-active {
                    background: rgb(66, 133, 244);
                    color: white;
                    border-color: rgb(66, 133, 244);
                }

                .dep-mode-toggle {
                    padding: 0.15em 0.5em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: rgb(240, 240, 240);
                    cursor: pointer;
                    font-size: 0.75em;
                    font-weight: bold;
                    min-width: 2.5em;
                    text-align: center;
                }

                .dep-mode-toggle:hover {
                    background: rgb(220, 230, 245);
                    border-color: rgb(100, 150, 255);
                }

                .cat-mode-toggle {
                    padding: 0.15em 0.5em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: rgb(240, 240, 240);
                    cursor: pointer;
                    font-size: 0.75em;
                    font-weight: bold;
                    min-width: 2.5em;
                    text-align: center;
                }

                .cat-mode-toggle:hover {
                    background: rgb(220, 230, 245);
                    border-color: rgb(100, 150, 255);
                }

                .duration-badge {
                    font-size: 0.75em;
                    font-weight: normal;
                    padding: 0.15em 0.5em;
                    border-radius: 0.3em;
                    margin-left: 0.5em;
                    display: inline-block;
                    white-space: nowrap;
                }

                .duration-fast {
                    background: rgb(220, 245, 220);
                    color: rgb(30, 100, 30);
                }

                .duration-moderate {
                    background: rgb(255, 243, 205);
                    color: rgb(140, 100, 0);
                }

                .duration-slow {
                    background: rgb(255, 220, 220);
                    color: rgb(191, 0, 0);
                }

                .duration-filters {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    gap: 0.3em;
                }

                .duration-filters-label {
                    font-weight: bold;
                    margin-right: 0.3em;
                }

                #duration-threshold {
                    width: 7em;
                    padding: 0.25em 0.4em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    font-size: 0.85em;
                }

                .duration-filters-unit {
                    font-size: 0.85em;
                    color: rgb(100, 100, 100);
                }

                #custom-duration-wrap {
                    display: inline-flex;
                    align-items: center;
                    gap: 0.3em;
                }

                .percentile-btn {
                    padding: 0.25em 0.6em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: white;
                    cursor: pointer;
                    font-size: 0.85em;
                }

                .percentile-btn:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }

                .percentile-btn.percentile-active {
                    background: rgb(66, 133, 244);
                    color: white;
                    border-color: rgb(66, 133, 244);
                }

                .collapse-expand-all {
                    padding: 0.25em 0.6em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: white;
                    cursor: pointer;
                    font-size: 0.85em;
                }

                .collapse-expand-all:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }

                .jump-to-failure {
                    position: fixed;
                    bottom: 1.5em;
                    right: 1.5em;
                    padding: 0.5em 1em;
                    background: rgb(191, 0, 0);
                    color: white;
                    border: none;
                    border-radius: 0.5em;
                    cursor: pointer;
                    font-size: 0.9em;
                    z-index: 1000;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                }

                .jump-to-failure:hover {
                    background: rgb(160, 0, 0);
                }

                .failure-counter {
                    font-size: 0.85em;
                    margin-left: 0.3em;
                    opacity: 0.9;
                }

                .copy-scenario-name {
                    font-size: 0.7em;
                    padding: 0.1em 0.4em;
                    margin-left: 0.4em;
                    border: 1px solid rgb(200, 200, 200);
                    border-radius: 0.3em;
                    background: rgb(245, 245, 245);
                    cursor: pointer;
                    opacity: 0.5;
                    vertical-align: middle;
                }

                .copy-scenario-name:hover {
                    opacity: 1;
                    background: rgb(230, 240, 255);
                }

                .scenario-link {
                    font-size: 0.7em;
                    padding: 0.1em 0.4em;
                    margin-left: 0.3em;
                    border: 1px solid rgb(200, 200, 200);
                    border-radius: 0.3em;
                    background: rgb(245, 245, 245);
                    cursor: pointer;
                    opacity: 0.5;
                    text-decoration: none;
                    color: inherit;
                    vertical-align: middle;
                }

                .scenario-link:hover {
                    opacity: 1;
                    background: rgb(230, 240, 255);
                }

                .export-filtered {
                    display: flex;
                    align-items: center;
                    gap: 0.3em;
                }

                .expand-row {
                    display: flex;
                    gap: 0.5em;
                    margin-bottom: 0.5em;
                }

                .export-btn {
                    padding: 0.25em 0.6em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: white;
                    cursor: pointer;
                    font-size: 0.85em;
                }

                .export-btn:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }

                .toolbar-row {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 0.5em;
                    align-items: center;
                    justify-content: space-between;
                    margin-top: 1em;
                    margin-bottom: 0.5em;
                }

                .toolbar-left {
                    display: flex;
                    gap: 0.5em;
                    align-items: center;
                }

                .toolbar-right {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 0.5em;
                    align-items: center;
                    margin-right: 2em;
                }

                .scenario-focused {
                    outline: 2px solid rgb(66, 133, 244);
                    outline-offset: 2px;
                }

                .scenario-steps {
                    margin: 0.5em 0 1em 0;
                    padding: 0.5em 1em;
                    border-left: 3px solid rgb(200, 200, 200);
                }

                .step {
                    margin: 0.3em 0;
                    line-height: 1.6;
                    padding-left: 0;
                }

                .step:not(.step-collapsible)::before {
                    content: '';
                    display: inline-block;
                    width: 0.3em;
                }

                .step-number {
                    font-family: monospace;
                    color: #888;
                    font-size: 0.8em;
                    margin-right: 0.3em;
                }

                .step-status {
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    width: 1.2em;
                    height: 1.2em;
                    border-radius: 50%;
                    color: white;
                    font-size: 0.9em;
                    font-weight: bold;
                    margin-right: 0.3em;
                    margin-left: 0.5em;
                    margin-top: -0.1em;
                    vertical-align: middle;
                    user-select: none;
                }

                .step-status.passed { background: #1daf26; }
                .step-status.failed { background: #cc0000; }
                .step-status.skipped { background: #949494; }
                .step-status.bypassed { background: #2e7bff; }
                .step-status.skipped-after-failure { background: #fbc800; color: black; }
                .step-status.passed-bypassed { background: #2e7bff; }
                .step-status.passed-skipped { background: #949494; }

                .step-keyword {
                    font-weight: bold;
                    color: rgb(100, 100, 100);
                }

                .step-text {
                    color: rgb(50, 50, 50);
                }

                .step-duration {
                    font-size: 0.8em;
                    color: rgb(130, 130, 130);
                }

                .step-comment {
                    font-style: italic;
                    color: rgb(100, 100, 100);
                    margin-left: 1.5em;
                    font-size: 0.9em;
                }

                .step-attachment {
                    display: inline-block;
                    margin-left: 1.5em;
                    font-size: 0.85em;
                    color: rgb(66, 133, 244);
                }

                .step-docstring {
                    background: #f5f5f5;
                    border: 1px solid #ddd;
                    padding: 0.5em;
                    margin: 0.3em 0 0.3em 2em;
                    font-size: 0.85em;
                    overflow-x: auto;
                }

                .sub-steps {
                    margin-left: 1.5em;
                    border-left: 2px solid rgb(220, 220, 220);
                    padding-left: 0.8em;
                }

                .step-collapsible {
                    margin: 0.15em 0;
                    padding-left: 0;
                }

                .step-collapsible > summary {
                    cursor: pointer;
                    list-style: none;
                    padding-left: 0;
                }

                .step-collapsible > summary::-webkit-details-marker {
                    display: none;
                }

                .step-collapsible > summary::before {
                    content: '\25B6';
                    display: inline-block;
                    width: 0.5em;
                    font-size: 0.6em;
                    text-align: center;
                    transition: transform 0.15s ease;
                    vertical-align: middle;
                }

                details.step-collapsible[open] > summary::before {
                    transform: rotate(90deg);
                }

                .feature-description {
                    font-style: italic;
                    color: rgb(100, 100, 100);
                    margin: 0.3em 0 0.8em 0;
                    white-space: pre-line;
                }

                .rule {
                    margin-left: 1em;
                    border-left: 3px solid #2e7bff;
                    padding-left: 0.5em;
                    margin-bottom: 0.5em;
                }

                .h2-5 {
                    font-size: 1.15em;
                    font-weight: 600;
                    color: #333;
                }

                .examples-table {
                    border-collapse: collapse;
                    margin: 0.5em 0 0.5em 1em;
                    font-size: 0.9em;
                }

                .examples-table th, .examples-table td {
                    border: 1px solid #ddd;
                    padding: 0.3em 0.6em;
                    text-align: left;
                }

                .examples-table thead {
                    background: #f0f0f0;
                }

                .examples-row-passed { background: #f0fff0; }
                .examples-row-failed { background: #fff0f0; cursor: pointer; }
                .examples-row-skipped { background: #fff8e1; }
                .examples-row-bypassed { background: #f0f0ff; }
                .examples-row-expandable:hover { background: #ffe0e0; }
                .examples-row-expanded { background: #ffe0e0; }
                .examples-detail-row td { padding: 0.5em 1em; background: #fff5f5; }

                .category-filters {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    gap: 0.3em;
                }

                .category-filters-label {
                    font-weight: bold;
                    margin-right: 0.3em;
                }

                .category-toggle {
                    padding: 0.25em 0.6em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    background: white;
                    cursor: pointer;
                    font-size: 0.85em;
                }

                .category-toggle:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }

                .category-toggle.category-active {
                    background: rgb(66, 133, 244);
                    color: white;
                    border-color: rgb(66, 133, 244);
                }

                .cat-hidden {
                    display: none !important;
                }

                .features-summary-details {
                    margin-top: 1em;
                    margin-bottom: 1em;
                    background-color: rgb(224, 224, 224);
                    padding: 1em;
                    border-radius: 10px;
                }
                .features-summary-details summary {
                    cursor: pointer;
                }

                .features-summary-table-wrapper {
                    background: white;
                    border-radius: 8px;
                    padding: 1em;
                    margin-top: 0.5em;
                }

                .feature-summary-table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-bottom: 1.5em;
                    font-size: 0.9em;
                }
                .feature-summary-table th, .feature-summary-table td {
                    border: 1px solid #ddd;
                    padding: 6px 10px;
                    text-align: left;
                }
                .feature-summary-table th {
                    background: #f5f5f5;
                    cursor: pointer;
                    user-select: none;
                }
                .feature-summary-table th:hover {
                    background: #e8e8e8;
                }
                .feature-summary-table tr.failed td {
                    background: #fff0f0;
                }

                .step-param-inline {
                    padding: 1px 4px;
                    border-radius: 3px;
                    font-family: monospace;
                    font-size: 0.9em;
                    margin-left: 4px;
                }
                .param-success { background: #d4edda; }
                .param-failure { background: #f8d7da; }
                .param-exception { background: #f5c6cb; }
                .param-not-provided { background: #fff3cd; }
                .param-na { background: #e9ecef; }

                .step-param-table {
                    margin: 4px 0 4px 24px;
                }
                .step-param-table table {
                    border-collapse: collapse;
                    font-size: 0.85em;
                }
                .step-param-table th, .step-param-table td {
                    border: 1px solid #ccc;
                    padding: 3px 8px;
                }
                .step-param-table th {
                    background: #f5f5f5;
                }
                .step-param-table th.key {
                    font-weight: bold;
                    text-decoration: underline;
                }

                .step-param-combined-table {
                    margin: 8px 0 4px 24px;
                }
                .step-param-combined-table table {
                    border-collapse: collapse;
                    font-size: 0.85em;
                }
                .step-param-combined-table th, .step-param-combined-table td {
                    border: 1px solid #ccc;
                    padding: 3px 8px;
                }
                .step-param-combined-table th {
                    background: #f5f5f5;
                }
                .step-param-combined-table th.key {
                    font-weight: bold;
                    text-decoration: underline;
                }
                .step-param-combined-table th.combined-separator,
                .step-param-combined-table td.combined-separator {
                    border-left: 2px solid #888;
                    border-right: 2px solid #888;
                    background: #f0f0f0;
                    text-align: center;
                    font-weight: bold;
                    padding: 3px 4px;
                    width: 1em;
                }
                .row-surplus td:first-child { color: green; }
                .row-missing td:first-child { color: red; }

                .step-param-tree {
                    margin: 4px 0 4px 24px;
                    font-family: monospace;
                    font-size: 0.85em;
                }
                .tree-node {
                    padding: 1px 0;
                }
                .tree-node-name {
                    font-weight: bold;
                }
                .tree-children {
                    margin-left: 16px;
                }

                /* Error Diff */
                .error-diff {
                    display: flex;
                    flex-direction: column;
                    gap: 0.3em;
                    margin: 0.8em 0;
                    font-family: monospace;
                    font-size: 0.9em;
                }
                .diff-expected, .diff-actual {
                    display: flex;
                    gap: 0.6em;
                    align-items: baseline;
                    padding: 0.4em 0.6em;
                    border-radius: 0.4em;
                    overflow-x: auto;
                }
                .diff-expected {
                    background: rgb(255, 235, 235);
                }
                .diff-actual {
                    background: rgb(235, 255, 235);
                }
                .diff-label {
                    font-weight: bold;
                    white-space: nowrap;
                    min-width: 5.5em;
                }
                .diff-del {
                    background: rgb(255, 180, 180);
                    text-decoration: line-through;
                    border-radius: 2px;
                    padding: 0 1px;
                }
                .diff-ins {
                    background: rgb(180, 255, 180);
                    border-radius: 2px;
                    padding: 0 1px;
                }

                /* Failure Clusters */
                .failure-clusters {
                    margin: 1em 0;
                    border: 2px solid rgb(224, 160, 160);
                    border-radius: 0.8em;
                    background: rgb(255, 248, 248);
                    padding: 0;
                }
                .failure-clusters > summary {
                    padding: 0.6em 1em;
                    font-weight: bold;
                    color: rgb(160, 0, 0);
                    font-size: 1.1em;
                }
                .failure-cluster {
                    margin: 0 1em 0.8em 1em;
                    border: 1px solid rgb(230, 200, 200);
                    border-radius: 0.5em;
                    background: white;
                }
                .failure-cluster > summary {
                    padding: 0.5em 0.8em;
                    font-weight: 600;
                    color: rgb(140, 0, 0);
                    font-size: 0.95em;
                }
                .failure-cluster-count {
                    background: rgb(191, 0, 0);
                    color: white;
                    border-radius: 1em;
                    padding: 0.15em 0.5em;
                    font-size: 0.8em;
                    margin-left: 0.4em;
                }
                .failure-cluster-scenarios {
                    list-style: none;
                    margin: 0;
                    padding: 0 1em 0.5em 1em;
                }
                .failure-cluster-scenarios li {
                    padding: 0.25em 0;
                    border-bottom: 1px solid rgb(240, 230, 230);
                }
                .failure-cluster-scenarios li:last-child {
                    border-bottom: none;
                }
                .failure-cluster-scenario-link {
                    color: rgb(66, 133, 244);
                    text-decoration: none;
                    cursor: pointer;
                }
                .failure-cluster-scenario-link:hover {
                    text-decoration: underline;
                }

                /* Scenario Timeline / Gantt */
                .scenario-timeline {
                    margin: 1em 0;
                    border: 1px solid rgb(200, 200, 200);
                    border-radius: 0.8em;
                    padding: 1em;
                    background: rgb(250, 250, 250);
                    overflow-x: auto;
                }
                .timeline-header {
                    font-weight: bold;
                    font-size: 1.05em;
                    margin-bottom: 0.6em;
                }
                .timeline-info {
                    cursor: help;
                    font-weight: normal;
                    font-size: 0.95em;
                    opacity: 0.55;
                    vertical-align: top;
                    line-height: 1;
                }
                .timeline-info:hover { opacity: 1; }
                .timeline-row {
                    display: flex;
                    align-items: center;
                    margin: 0.2em 0;
                    gap: 0.5em;
                }
                .timeline-label {
                    min-width: 200px;
                    max-width: 200px;
                    font-size: 0.8em;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    white-space: nowrap;
                    text-align: right;
                    padding-right: 0.4em;
                }
                .timeline-track {
                    flex: 1;
                    position: relative;
                    height: 1.2em;
                    background: rgb(240, 240, 240);
                    border-radius: 3px;
                }
                .timeline-bar {
                    position: absolute;
                    height: 100%;
                    border-radius: 3px;
                    min-width: 2px;
                    top: 0;
                }
                .timeline-bar-passed {
                    background: rgb(34, 139, 34);
                }
                .timeline-bar-failed {
                    background: rgb(191, 0, 0);
                }
                .timeline-bar-skipped {
                    background: rgb(148, 148, 148);
                }
                .timeline-bar-bypassed {
                    background: rgb(255, 165, 0);
                }
                .timeline-duration {
                    font-size: 0.75em;
                    color: rgb(100, 100, 100);
                    min-width: 3.5em;
                }
                .timeline-toggle {
                    background: rgb(245, 245, 245);
                    border: 1px solid rgb(200, 200, 200);
                    border-radius: 0.3em;
                    padding: 0.3em 0.7em;
                    cursor: pointer;
                    font-size: 0.85em;
                }
                .timeline-toggle:hover {
                    background: rgb(230, 240, 255);
                    border-color: rgb(100, 150, 255);
                }
                .timeline-toggle-active {
                    background: rgb(66, 133, 244);
                    color: white;
                    border-color: rgb(66, 133, 244);
                }
                .timeline-toggle-active:hover {
                    background: rgb(50, 110, 220);
                }
        """;

    public const string VioletThemeStyleSheet =
        """
                .feature { background-color: #DDD6FE; }
                .features-summary-details { background-color: #DDD6FE; }
                .test-execution-summary { background-color: #DDD6FE; }
                .filtering-box { background-color: #DDD6FE; }
                .example-diagrams { border-color: #DDD6FE; }

                .happy-path-toggle.happy-path-active,
                .dependency-toggle.dependency-active,
                .status-toggle.status-active,
                .category-toggle.category-active {
                    background: #8B5CF6;
                    color: white;
                    border-color: #8B5CF6;
                }
                .percentile-btn.percentile-active {
                    background: #8B5CF6;
                    color: white;
                }

                .happy-path-toggle:hover,
                .dependency-toggle:hover,
                .status-toggle:hover,
                .category-toggle:hover {
                    background: #EDE9FE;
                    border-color: #A78BFA;
                }

                .dep-mode-toggle, .cat-mode-toggle { background: #F5F3FF; }
                .scenario-focused { outline-color: #8B5CF6; }
                .step-attachment { color: #8B5CF6; }

                .details-radio-btn.details-active {
                    background: #8B5CF6;
                    color: white;
                    border-color: #8B5CF6;
                }

                .iflow-toggle-active { background: #8B5CF6; color: #fff; border-color: #8B5CF6; }
                .iflow-toggle-active:hover { background: #7C3AED; }
                .diagram-toggle-active { background: #8B5CF6; color: #fff; border-color: #8B5CF6; }
                .iflow-rel-list li:hover { background: #EDE9FE; border-color: #8B5CF6; }

                .step-status.passed { background: #8B5CF6; }
                .step-status.bypassed { background: #DDD6FE; color: #5B21B6; }
                .step-status.passed-bypassed { background: #DDD6FE; color: #5B21B6; }

                .rule { border-left-color: #8B5CF6; }
                span.label { background-color: #C4B5FD; }
                #searchbar { border-color: #C4B5FD; }
                .scenario-steps { border-left-color: #C4B5FD; }
                .sub-steps { border-left-color: #DDD6FE; }
                .feature-summary-table th { background: #F5F3FF; }
                .param-success { background: #EDE9FE; }
                .duration-fast { background: #EDE9FE; color: #5B21B6; }
        """;
}
