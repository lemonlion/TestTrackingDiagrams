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
                }
                
                .test-execution-summary h2 {
                    margin-top: 0;
                }
                
                .test-execution-summary table td {
                    padding: 0.25em;
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
                
                #searchbar {
                    margin: 1em;
                    margin-left: 0;
                    padding: 0.5em;
                    border-radius: 0.5em;
                    width: 25em;
                }
                
                #filters {
                    margin-left: 1em;
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
        """;
}
