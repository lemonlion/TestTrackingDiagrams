using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Drives through a real generated HTML report capturing screenshots of every
/// major feature.  Run:
/// <code>dotnet test TestTrackingDiagrams.Tests.Selenium --filter "ShowcaseReportTests"</code>
/// Output goes to the bin SeleniumOutput/showcase-frames/ folder — stitch the
/// numbered PNGs into a GIF with ScreenToGif, ffmpeg, or ImageMagick:
/// <code>magick -delay 15 -loop 0 SeleniumOutput/showcase-frames/*.png showcase.gif</code>
/// </summary>
public class ShowcaseReportTests : IClassFixture<ChromeFixture1280X900>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private readonly string _framesDir;
    private int _frameNumber;

    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ShowcaseReportTests).Assembly.Location)!,
        "SeleniumOutput");

    private static bool Visible => Environment.GetEnvironmentVariable("SHOWCASE_VISIBLE") == "1";

    public ShowcaseReportTests(ChromeFixture1280X900 chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-showcase-" + Guid.NewGuid().ToString("N")[..8]);
        _framesDir = Path.Combine(OutputDir, "showcase-frames");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(_framesDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    // ── Screenshot helpers ──

    private void CaptureFrame(string label)
    {
        _frameNumber++;
        var fileName = $"{_frameNumber:D3}_{label}.png";
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(Path.Combine(_framesDir, fileName));
    }

    private void CaptureFrames(string label, int count, int delayMs = 150)
    {
        for (var i = 0; i < count; i++)
        {
            CaptureFrame($"{label}_{i:D2}");
            Pause(delayMs);
        }
    }

    private static void Pause(int ms)
    {
        Thread.Sleep(ms);
    }

    // ── Selenium helpers ──

    private IWebElement WaitFor(By by, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private void WaitForDiagramSvg(int timeoutSeconds = 30)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        });
    }

    private void TypeSlowly(IWebElement element, string text, int charDelayMs = 60)
    {
        foreach (var ch in text)
        {
            element.SendKeys(ch.ToString());
            Pause(charDelayMs);
        }
    }

    private void ScrollTo(IWebElement element)
    {
        // Use JS animation for reliable smooth scroll across headless & headed modes
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var el = arguments[0];
            var rect = el.getBoundingClientRect();
            var targetY = window.scrollY + rect.top - (window.innerHeight / 2) + (rect.height / 2);
            var startY = window.scrollY;
            var distance = targetY - startY;
            var duration = Math.min(1200, Math.max(400, Math.abs(distance) * 0.6));
            var startTime = performance.now();
            function ease(t) { return t < 0.5 ? 4*t*t*t : 1 - Math.pow(-2*t+2, 3)/2; }
            function step(now) {
                var elapsed = now - startTime;
                var t = Math.min(elapsed / duration, 1);
                window.scrollTo(0, startY + distance * ease(t));
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ", element);
        WaitForScrollIdle();
    }

    private void JsClick(IWebElement element)
    {
        MoveCursorTo(element);
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
    }

    /// <summary>Injects a fake mouse cursor into the page for visual demo.</summary>
    private void InjectFakeCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            if (document.getElementById('_fakeCursor')) return;
            var c = document.createElement('div');
            c.id = '_fakeCursor';
            c.innerHTML = `<svg width='24' height='24' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>
                <path d='M5 3 L5 20 L10 15 L16 21 L19 18 L13 12 L19 12 Z'
                      fill='white' stroke='black' stroke-width='1.5' stroke-linejoin='round'/>
            </svg>`;
            c.style.cssText = 'position:fixed;top:-50px;left:-50px;z-index:99999;pointer-events:none;filter:drop-shadow(1px 2px 2px rgba(0,0,0,0.3));transition:none;';
            document.body.appendChild(c);
        ");
    }

    /// <summary>Animates the fake cursor to an element over ~300ms.</summary>
    private void MoveCursorTo(IWebElement element)
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var el = arguments[0];
            var c = document.getElementById('_fakeCursor');
            if (!c) return;
            var rect = el.getBoundingClientRect();
            var tx = rect.left + Math.min(rect.width * 0.3, 60);
            var ty = rect.top + rect.height / 2;
            // Get current position
            var cx = parseFloat(c.style.left) || -50;
            var cy = parseFloat(c.style.top) || -50;
            var dist = Math.sqrt((tx-cx)*(tx-cx) + (ty-cy)*(ty-cy));
            var duration = Math.min(500, Math.max(150, dist * 0.8));
            var start = performance.now();
            function ease(t) { return t < 0.5 ? 2*t*t : 1 - Math.pow(-2*t+2, 2)/2; }
            function step(now) {
                var t = Math.min((now - start) / duration, 1);
                var p = ease(t);
                c.style.left = (cx + (tx - cx) * p) + 'px';
                c.style.top = (cy + (ty - cy) * p) + 'px';
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ", element);
        Pause(350);
    }

    /// <summary>Hides the fake cursor (e.g. during typing).</summary>
    private void HideCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var c = document.getElementById('_fakeCursor');
            if (c) c.style.display = 'none';
        ");
    }

    /// <summary>Shows the fake cursor again.</summary>
    private void ShowCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var c = document.getElementById('_fakeCursor');
            if (c) c.style.display = '';
        ");
    }

    private void ScrollToTop()
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var startY = window.scrollY;
            var duration = Math.min(1200, Math.max(400, startY * 0.6));
            var startTime = performance.now();
            function ease(t) { return t < 0.5 ? 4*t*t*t : 1 - Math.pow(-2*t+2, 3)/2; }
            function step(now) {
                var elapsed = now - startTime;
                var t = Math.min(elapsed / duration, 1);
                window.scrollTo(0, startY * (1 - ease(t)));
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ");
        WaitForScrollIdle();
    }

    /// <summary>Waits until the page stops scrolling (stable Y for ~150 ms).</summary>
    private void WaitForScrollIdle(int maxMs = 3000)
    {
        var js = (IJavaScriptExecutor)_driver;
        var lastY = Convert.ToDouble(js.ExecuteScript("return window.scrollY;"));
        var stableCount = 0;
        var elapsed = 0;
        const int poll = 25;
        while (elapsed < maxMs)
        {
            Pause(poll);
            elapsed += poll;
            var y = Convert.ToDouble(js.ExecuteScript("return window.scrollY;"));
            if (Math.Abs(y - lastY) < 0.5)
            {
                stableCount++;
                if (stableCount >= 6) return; // 6 × 25 ms = 150 ms of no movement
            }
            else
            {
                stableCount = 0;
            }
            lastY = y;
        }
    }

    // ── Rich test data ──

    private static readonly string OrderDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as orders
        participant "Stock Service" as stock
        database "Cosmos DB" as cosmos
        queue "EventGrid" as events

        test -> orders : POST /api/orders
        note left
        Content-Type: application/json
        {
          "productId": "SKU-42",
          "quantity": 3,
          "customerId": "cust-1234"
        }
        end note

        orders -> stock : PUT /stock/reserve
        note left
        {"sku":"SKU-42","qty":3}
        end note
        stock --> orders : 200 OK

        orders -> cosmos : Create Item
        note left
        Container: orders
        {"id":"ord-7f3a","status":"created"}
        end note
        cosmos --> orders : 201

        orders -> events : OrderCreated
        note left #LightBlue
        Topic: order-events
        {"orderId":"ord-7f3a","type":"OrderCreated"}
        end note

        orders --> test : 201 Created
        note left
        {
          "orderId": "ord-7f3a",
          **"status": "Confirmed"**,
          **"totalAmount": 250.50**,
          "currency": "GBP"
        }
        end note
        @enduml
        """;

    private static readonly string PaymentDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Payment API" as pay
        participant "Stripe Gateway" as stripe
        database "SQL Server" as sql

        test -> pay : POST /api/payments
        note left
        {"orderId":"ord-7f3a","amount":250.50,"currency":"GBP"}
        end note

        pay -> stripe : POST /v1/charges
        note left
        {"amount":25050,"currency":"gbp"}
        end note
        stripe --> pay : 200 OK
        note right
        {"chargeId":"ch_abc123","status":"succeeded"}
        end note

        pay -> sql : INSERT INTO Payments
        sql --> pay : OK

        pay --> test : 200 OK
        note left
        {"paymentId":"pay-001","status":"completed"}
        end note
        @enduml
        """;

    private static readonly string FailureDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as orders
        participant "Stock Service" as stock

        test -> orders : POST /api/orders
        note left
        {"productId":"SKU-99","quantity":1000}
        end note

        orders -> stock : PUT /stock/reserve
        stock --> orders : 409 Conflict
        note right #Pink
        {"error":"Insufficient stock","available":12}
        end note

        orders --> test : 409 Conflict
        note left #Pink
        {"error":"Cannot fulfil order","code":"STOCK_INSUFFICIENT"}
        end note
        @enduml
        """;

    private (Feature[] Features, DiagramAsCode[] Diagrams) CreateShowcaseData()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Management",
                Description = "End-to-end order lifecycle from creation to fulfilment",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "order-1", DisplayName = "Creating an order reserves stock and publishes event",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(342),
                        Categories = ["Smoke", "Orders"],
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the Orders API is running", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(12) },
                            new ScenarioStep { Keyword = "And", Text = "Stock Service has 50 units of SKU-42", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(8) },
                            new ScenarioStep { Keyword = "When", Text = "I create an order for 3 units of SKU-42", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(287) },
                            new ScenarioStep { Keyword = "Then", Text = "the order is confirmed with status Created", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(15) },
                            new ScenarioStep { Keyword = "And", Text = "an OrderCreated event is published", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(20) }
                        ]
                    },
                    new Scenario
                    {
                        Id = "order-2", DisplayName = "Insufficient stock returns 409 Conflict",
                        IsHappyPath = false, Result = ExecutionResult.Failed,
                        Duration = TimeSpan.FromMilliseconds(1204),
                        Categories = ["Orders", "ErrorHandling"],
                        ErrorMessage = "Expected status code 409, but got 500.\nResponse body: {\"error\":\"Internal Server Error\"}",
                        ErrorStackTrace = "   at FluentAssertions.Execution.LateBoundTestFramework.Throw(String message)\n   at OrderTests.Insufficient_stock_returns_409() in /src/Tests/OrderTests.cs:line 87",
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the Orders API is running", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "And", Text = "Stock Service has 12 units of SKU-99", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I create an order for 1000 units of SKU-99", Status = ExecutionResult.Failed },
                            new ScenarioStep { Keyword = "Then", Text = "a 409 Conflict is returned", Status = ExecutionResult.Skipped }
                        ]
                    },
                    new Scenario
                    {
                        Id = "order-3", DisplayName = "Listing orders returns paginated results",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(189),
                        Categories = ["Smoke", "Orders"]
                    },
                    // Parameterized tests — same OutlineId groups them into a table
                    new Scenario
                    {
                        Id = "order-param-1", DisplayName = "Order validation(Amount: 100, Currency: GBP, Region: EU)",
                        OutlineId = "Order validation", IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(34),
                        Categories = ["Orders", "Validation"],
                        ExampleValues = new() { ["Amount"] = "100.00", ["Currency"] = "GBP", ["Region"] = "EU" }
                    },
                    new Scenario
                    {
                        Id = "order-param-2", DisplayName = "Order validation(Amount: 250.50, Currency: USD, Region: US)",
                        OutlineId = "Order validation", IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(28),
                        Categories = ["Orders", "Validation"],
                        ExampleValues = new() { ["Amount"] = "250.50", ["Currency"] = "USD", ["Region"] = "US" }
                    },
                    new Scenario
                    {
                        Id = "order-param-3", DisplayName = "Order validation(Amount: 999.99, Currency: EUR, Region: APAC)",
                        OutlineId = "Order validation", IsHappyPath = false, Result = ExecutionResult.Failed,
                        Duration = TimeSpan.FromMilliseconds(1102),
                        Categories = ["Orders", "Validation"],
                        ExampleValues = new() { ["Amount"] = "999.99", ["Currency"] = "EUR", ["Region"] = "APAC" },
                        ErrorMessage = "Expected 200 OK but got 502 Bad Gateway"
                    },
                    new Scenario
                    {
                        Id = "order-param-4", DisplayName = "Order validation(Amount: 0.01, Currency: JPY, Region: APAC)",
                        OutlineId = "Order validation", IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(31),
                        Categories = ["Orders", "Validation"],
                        ExampleValues = new() { ["Amount"] = "0.01", ["Currency"] = "JPY", ["Region"] = "APAC" }
                    }
                ]
            },
            new Feature
            {
                DisplayName = "Payment Processing",
                Description = "Stripe integration for payment capture and refunds",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "pay-1", DisplayName = "Processing a payment charges Stripe and records transaction",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(456),
                        Categories = ["Smoke", "Payments"],
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the Payment API is running", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I process a payment of £250.50", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "Then", Text = "Stripe is charged successfully", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "And", Text = "the transaction is recorded in SQL Server", Status = ExecutionResult.Passed }
                        ]
                    },
                    new Scenario
                    {
                        Id = "pay-2", DisplayName = "Refund reverses the charge",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(312),
                        Categories = ["Payments"]
                    },
                    new Scenario
                    {
                        Id = "pay-3", DisplayName = "Payment timeout retries three times",
                        IsHappyPath = false, Result = ExecutionResult.Skipped,
                        Duration = TimeSpan.FromMilliseconds(50),
                        Categories = ["Payments", "ErrorHandling"]
                    }
                ]
            },
            new Feature
            {
                DisplayName = "User Authentication",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "auth-1", DisplayName = "Login with valid credentials returns JWT",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(95),
                        Categories = ["Smoke", "Auth"]
                    },
                    new Scenario
                    {
                        Id = "auth-2", DisplayName = "Login with invalid credentials returns 401",
                        IsHappyPath = false, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(78),
                        Categories = ["Auth", "ErrorHandling"]
                    },
                    new Scenario
                    {
                        Id = "auth-3", DisplayName = "Expired token is rejected with 403",
                        IsHappyPath = false, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(62),
                        Categories = ["Auth"]
                    }
                ]
            },
            new Feature
            {
                DisplayName = "Inventory Tracking",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "inv-1", DisplayName = "Stock level decreases after order",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(220),
                        Categories = ["Inventory"]
                    },
                    new Scenario
                    {
                        Id = "inv-2", DisplayName = "Restock event increases available units",
                        IsHappyPath = true, Result = ExecutionResult.Passed,
                        Duration = TimeSpan.FromMilliseconds(145),
                        Categories = ["Inventory"]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("order-1", "", OrderDiagramSource),
            new DiagramAsCode("order-2", "", FailureDiagramSource),
            new DiagramAsCode("order-param-1", "", OrderDiagramSource),
            new DiagramAsCode("order-param-2", "", OrderDiagramSource),
            new DiagramAsCode("order-param-3", "", FailureDiagramSource),
            new DiagramAsCode("order-param-4", "", OrderDiagramSource),
            new DiagramAsCode("pay-1", "", PaymentDiagramSource),
        };

        return (features, diagrams);
    }

    private string GenerateShowcaseReport()
    {
        var (features, diagrams) = CreateShowcaseData();

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow,
            null,
            Path.Combine(_tempDir, "ShowcaseReport.html"),
            "E‑Commerce Service Specifications",
            includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            groupParameterizedTests: true,
            maxParameterColumns: 10,
            showStepNumbers: true);

        var outPath = Path.Combine(OutputDir, "ShowcaseReport.html");
        File.Copy(path, outPath, true);
        return new Uri(path).AbsoluteUri;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  THE SHOWCASE — a single test that drives through every feature
    //  capturing numbered PNGs that stitch into a 60-second GIF.
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating showcase assets, then re-skip afterwards")]
    public void Showcase_drives_through_report_features_capturing_frames()
    {
        // Clean previous run
        foreach (var old in Directory.GetFiles(_framesDir, "*.png"))
            File.Delete(old);

        var reportUrl = GenerateShowcaseReport();
        _driver.Navigate().GoToUrl(reportUrl);
        WaitFor(By.CssSelector("details.feature"));
        // Ensure smooth scrolling is enabled at the CSS level
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.documentElement.style.scrollBehavior='smooth';");
        InjectFakeCursor();
        Pause(500);

        // ── Scene 1: Full report overview (collapsed) ──
        ScrollToTop();
        CaptureFrames("01_overview", 48, 33);

        // ── Scene 2: Expand everything while still at the top ──
        var expandFeaturesBtn = _driver.FindElements(By.CssSelector(".collapse-expand-all"))
            .FirstOrDefault(b => b.Text.Contains("Expand All Features"));
        if (expandFeaturesBtn is not null)
        {
            JsClick(expandFeaturesBtn);
            Pause(300);
        }
        var expandScenariosBtn = _driver.FindElements(By.CssSelector(".collapse-expand-all"))
            .FirstOrDefault(b => b.Text.Contains("Expand All Scenarios"));
        if (expandScenariosBtn is not null)
        {
            JsClick(expandScenariosBtn);
            Pause(400);
        }
        CaptureFrames("02_expanded", 36, 33);

        // ── Scene 3: Test execution summary with pie chart ──
        var summary = _driver.FindElement(By.CssSelector(".test-execution-summary"));
        ScrollTo(summary);
        CaptureFrames("03_summary", 48, 33);

        // ── Scene 4: Scroll down to first scenario's BDD steps ──
        var firstScenario = _driver.FindElement(By.CssSelector("details.scenario"));
        ScrollTo(firstScenario);
        CaptureFrames("04_scenarios_steps", 60, 33);

        // ── Scene 5: Continue down to the sequence diagram ──
        try
        {
            WaitForDiagramSvg(30);
            var diagramContainer = firstScenario.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
            ScrollTo(diagramContainer);
            Pause(500);
            CaptureFrames("05_diagram", 72, 33);
        }
        catch
        {
            try
            {
                var anyDiagram = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
                ScrollTo(anyDiagram);
                Pause(500);
                CaptureFrames("05_diagram", 72, 33);
            }
            catch
            {
                CaptureFrames("05_diagram_loading", 48, 33);
            }
        }

        // ── Scene 6: Failed scenario with error details ──
        var failedScenario = _driver.FindElements(By.CssSelector("details.scenario"))
            .FirstOrDefault(s => s.GetAttribute("data-status") == "Failed");
        if (failedScenario is not null)
        {
            ScrollTo(failedScenario);
            Pause(500);
            CaptureFrames("06_failed_scenario", 84, 33);
        }

        // ── Scene 7: Parameterized test table ──
        var paramTable = _driver.FindElements(By.CssSelector(".param-test-table"));
        if (paramTable.Any())
        {
            ScrollTo(paramTable.First());
            Pause(300);
            CaptureFrames("07_param_table", 48, 33);

            // Click a passed row to show highlighting
            var passedRow = paramTable.First().FindElements(By.CssSelector("tbody tr.row-passed")).FirstOrDefault();
            if (passedRow is not null)
            {
                JsClick(passedRow);
                Pause(400);
                CaptureFrames("07_param_row_passed", 36, 33);
            }

            // Click the failed row — find by CSS class, not index
            var failedRow = paramTable.First().FindElements(By.CssSelector("tbody tr.row-failed")).FirstOrDefault();
            if (failedRow is not null)
            {
                JsClick(failedRow);
                Pause(500);
                CaptureFrames("07_param_row_failed", 60, 33);
            }
        }

        // ── Scene 8: Search — type a query with AND operator ──
        ScrollToTop();
        Pause(200);
        var searchbar = _driver.FindElement(By.Id("searchbar"));
        MoveCursorTo(searchbar);
        searchbar.Clear();
        HideCursor(); // hide cursor during typing

        // Type slowly for visual effect — captures frames during typing
        var query = "$failed && order";
        foreach (var ch in query)
        {
            searchbar.SendKeys(ch.ToString());
            Pause(80);
            if (_frameNumber % 2 == 0) // capture every other frame during typing
                CaptureFrame("08_search_typing");
        }
        Pause(500); // debounce
        CaptureFrames("08_search_result", 48, 33);
        ShowCursor();

        // ── Scene 9: OR search ──
        HideCursor();
        searchbar.Clear();
        searchbar.SendKeys("payment || auth");
        Pause(500);
        CaptureFrames("09_search_or", 36, 33);
        ShowCursor();

        // ── Scene 10: Category filter toggle ──
        searchbar.Clear();
        searchbar.SendKeys(Keys.Backspace); // trigger filter reset
        Pause(300);

        var catToggles = _driver.FindElements(By.CssSelector(".category-toggle"));
        if (catToggles.Any())
        {
            ScrollTo(catToggles.First());
            Pause(200);
            // Click "Smoke" category
            var smokeToggle = catToggles.FirstOrDefault(t => t.Text.Contains("Smoke"));
            if (smokeToggle is not null)
            {
                JsClick(smokeToggle);
                Pause(400);
                CaptureFrames("10_category_smoke", 48, 33);
            }

            // Click "ErrorHandling" as well (AND mode)
            var errorToggle = catToggles.FirstOrDefault(t => t.Text.Contains("Error"));
            if (errorToggle is not null)
            {
                JsClick(errorToggle);
                Pause(400);
            }
            CaptureFrames("10_category_multi", 36, 33);

            // Clear
            var clearBtn = _driver.FindElements(By.CssSelector(".export-btn"))
                .FirstOrDefault(b => b.Text.Contains("Clear"));
            if (clearBtn is not null) JsClick(clearBtn);
            Pause(300);
        }

        // ── Scene 11: Status filter — show only failed ──
        var statusToggles = _driver.FindElements(By.CssSelector(".status-toggle"));
        var failedToggle = statusToggles.FirstOrDefault(t => t.GetAttribute("data-status") == "Failed");
        if (failedToggle is not null)
        {
            ScrollTo(failedToggle);
            JsClick(failedToggle);
            Pause(800);
            CaptureFrames("11_status_failed", 72, 33);

            // Clear
            var clearBtn = _driver.FindElements(By.CssSelector(".export-btn"))
                .FirstOrDefault(b => b.Text.Contains("Clear"));
            if (clearBtn is not null) JsClick(clearBtn);
            Pause(500);
        }

        // ── Scene 12: Happy path toggle ──
        var happyToggle = _driver.FindElements(By.CssSelector(".happy-path-toggle")).FirstOrDefault();
        if (happyToggle is not null)
        {
            ScrollTo(happyToggle);
            JsClick(happyToggle);
            Pause(500);
            CaptureFrames("12_happy_path", 48, 33);

            JsClick(happyToggle); // toggle off
            Pause(300);
        }

        // ── Scene 13: Search help panel ──
        var helpToggle = _driver.FindElements(By.CssSelector(".search-help-toggle")).FirstOrDefault();
        if (helpToggle is not null)
        {
            ScrollTo(helpToggle);
            JsClick(helpToggle);
            Pause(400);
            CaptureFrames("13_search_help", 48, 33);

            JsClick(helpToggle);
            Pause(200);
        }

        // ── Final: scroll back to top for closing shot ──
        ScrollToTop();
        Pause(300);
        CaptureFrames("14_final", 36, 33);

        // Verify we captured a meaningful number of frames
        var frameCount = Directory.GetFiles(_framesDir, "*.png").Length;
        Assert.True(frameCount >= 50, $"Expected at least 50 frames but captured {frameCount}");
    }
}
