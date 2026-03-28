namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public static class Stylesheets
{
    public static string GetHideStyleSheet() =>
        @"
            .hide {
                display: none;
            }";


    public static string GetPlantUmlStyleSheet() => @"
            .raw-plantuml {
                border: solid 1px;
                padding-left: 10px;
                padding-right: 10px;
             }

            summary {
                cursor: pointer;
            }

            details {
                margin-top: 1em;
                margin-bottom: 1em;
            }            
";

    public static string GetFilterFreeTextStyleSheet() => @"
            .filterFreeTextPanel input
            {
                padding: 0.3em;
                width: 45em;
                margin-top: 0.5em;
            }       
";


        
}