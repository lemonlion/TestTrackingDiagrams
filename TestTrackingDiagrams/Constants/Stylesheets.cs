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
                    margin-bottom: 1em;
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
                }

                .filters {
                    display: flex;
                    flex-direction: column;
                    gap: 0.5em;
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
                    width: 5em;
                    padding: 0.25em 0.4em;
                    border: 1px solid rgb(180, 180, 180);
                    border-radius: 0.4em;
                    font-size: 0.85em;
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
                    margin-bottom: 0.5em;
                }

                .scenario-focused {
                    outline: 2px solid rgb(66, 133, 244);
                    outline-offset: 2px;
                }
        """;
}
