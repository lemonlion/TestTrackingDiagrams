$ErrorActionPreference = 'Stop'

function Get-DiagramUrl([string]$Text) {
    # PlantUML custom encoding: deflate + custom base64 (0-9A-Za-z-_)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $ms = [System.IO.MemoryStream]::new()
    $ds = [System.IO.Compression.DeflateStream]::new($ms, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    $ds.Write($bytes, 0, $bytes.Length)
    $ds.Dispose()
    $compressed = $ms.ToArray()
    $ms.Dispose()

    $lookup = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"
    $sb = [System.Text.StringBuilder]::new()
    for ($i = 0; $i -lt $compressed.Length; $i += 3) {
        $b1 = [int]$compressed[$i]
        $b2 = if (($i + 1) -lt $compressed.Length) { [int]$compressed[$i + 1] } else { 0 }
        $b3 = if (($i + 2) -lt $compressed.Length) { [int]$compressed[$i + 2] } else { 0 }
        [void]$sb.Append($lookup[($b1 -shr 2) -band 0x3F])
        [void]$sb.Append($lookup[(($b1 -band 0x3) -shl 4) -bor (($b2 -shr 4) -band 0xF)])
        [void]$sb.Append($lookup[(($b2 -band 0xF) -shl 2) -bor (($b3 -shr 6) -band 0x3)])
        [void]$sb.Append($lookup[$b3 -band 0x3F])
    }
    return "https://www.plantuml.com/plantuml/svg/$($sb.ToString())"
}

# Common participant block for consistent layout across strips
$p = @"
skinparam ParticipantPadding 10
skinparam sequenceArrowThickness 1
skinparam roundcorner 5
actor "Merchant Gateway Service" as mgs
entity "CosmosDB" as cosmosDB
actor "Caller" as caller
entity "Transaction Service" as ts
entity "Service Bus" as sb
"@

# Activity diagram common skinparam
$actSkin = @"
skinparam ActivityBackgroundColor #EBF5FB
skinparam ActivityBorderColor #2E86C1
skinparam ActivityFontSize 11
"@

# --- STRIP 1: Read fiservProductMapping + OK ---
$strip1 = @"
@startuml
$p
mgs -> cosmosDB: 1. Read: /fiservProductMapping
cosmosDB --> mgs: 2. OK
@enduml
"@

$act1 = @"
@startuml
$actSkin
start
:FiservProductMappingRepository\n.GetByPartitionKeyAsync();
:JsonSerializer.Deserialize\n<FiservProductMapping>(response);
if (Mapping exists?) then (yes)
  :MappingValidator\n.ValidateSchema(mapping);
  :InMemoryCache\n.Set("fiserv-mapping", mapping);
else (no)
  :throw MappingNotFoundException();
  stop
endif
stop
@enduml
"@

# --- STRIP 2: Upsert credit-scores + Created ---
$strip2 = @"
@startuml
$p
mgs -> cosmosDB: 3. Upsert: /account-credit-scores-v2
cosmosDB --> mgs: 4. Created
@enduml
"@

$act2 = @"
@startuml
$actSkin
start
:CreditScoreWriter\n.ConfirmPersistence(response);
:ETagValidator\n.ValidateConcurrency(eTag);
:AccountCreditScoreState\n.UpdateFromCosmosResponse();
:TelemetryClient\n.TrackEvent("CreditScoreUpserted");
stop
@enduml
"@

# --- STRIP 3: POST authorisation from caller ---
$strip3 = @"
@startuml
$p
caller -> mgs: 5. POST: /merchant-gateway-service/\nmerchant/{customerRef}/authorisation
note left
  Amount: 10 GBP
  sca-token: sca
  context-merchantId: 0611fc34...
end note
@enduml
"@

$act3 = @"
@startuml
$actSkin
start
:AuthorisationController\n.PostAsync(request);
:RequestValidator\n.ValidateModel(request);
:MerchantContextProvider\n.GetMerchantContext(headers);
:ScaTokenValidator.Validate(scaToken);
fork
  :AccountResolver\n.ResolveAccountId(customerRef);
fork again
  :SpendOptionResolver\n.ResolveDefaults(merchantId);
end fork
:AuthorisationOrchestrator\n.BeginOrchestration(context);
stop
@enduml
"@

# --- STRIP 4: GET available-credit + OK ---
$strip4 = @"
@startuml
$p
mgs -> ts: 6. GET: /account/{id}/available-credit
note left
  Authorization: Bearer ***
  context-tenantid: cfb938bc...
end note
ts --> mgs: 7. OK
note right
  AvailableCredit: 1500
  Purchase: 1500, Instalment: 1400
end note
@enduml
"@

$act4 = @"
@startuml
$actSkin
start
:AvailableCreditResponseParser\n.ParseResponse(responseBody);
:SpendLimitEvaluator\n.EvaluateByType("Purchase", amount);
if (Amount <= AvailableCredit?) then (yes)
  :EligibilityResult.Eligible();
else (no)
  :EligibilityResult\n.InsufficientCredit();
endif
:ClientSessionValidator\n.ValidateSessionId(headers);
note right
  Missing client-session-id
  triggers rejection
end note
:RejectionReasonResolver\n.Resolve(\n"ClientSessionIdNotProvided");
stop
@enduml
"@

# --- STRIP 5: Send to Service Bus + Responded ---
$strip5 = @"
@startuml
$p
mgs -> sb: 8. Send: /mgs-outbox
note left
  OrchestrationId: c6e6698e...
  Method: Authorisation
  Reason: ClientSessionIdNotProvided
end note
sb --> mgs: 9. Responded
@enduml
"@

$act5 = @"
@startuml
$actSkin
start
:OutboxConfirmationHandler\n.HandleResponse(busResponse);
:OrchestrationStateManager\n.TransitionTo("Rejected");
:ErrorResponseFactory\n.CreateValidationError(reason);
:RequestMetricsCollector\n.RecordRejection(orchestrationId);
:HttpResponseBuilder\n.BuildBadRequest(errors);
stop
@enduml
"@

# --- STRIP 6: Bad Request response (final, not expandable) ---
$strip6 = @"
@startuml
$p
mgs --> caller: 10. Bad Request (400)
note right
  type: validation
  error: ClientSessionIdNotProvided
end note
@enduml
"@

# Generate all URLs
$diagrams = @(
    @{ Name = 'strip1'; Text = $strip1 }
    @{ Name = 'act1';   Text = $act1 }
    @{ Name = 'strip2'; Text = $strip2 }
    @{ Name = 'act2';   Text = $act2 }
    @{ Name = 'strip3'; Text = $strip3 }
    @{ Name = 'act3';   Text = $act3 }
    @{ Name = 'strip4'; Text = $strip4 }
    @{ Name = 'act4';   Text = $act4 }
    @{ Name = 'strip5'; Text = $strip5 }
    @{ Name = 'act5';   Text = $act5 }
    @{ Name = 'strip6'; Text = $strip6 }
)

$urls = @{}
foreach ($d in $diagrams) {
    $urls[$d.Name] = Get-DiagramUrl $d.Text
    Write-Host "Encoded $($d.Name) - URL length: $($urls[$d.Name].Length)"
}

# Quick sanity check on URL lengths
$maxLen = ($urls.Values | Measure-Object Length -Maximum).Maximum
Write-Host "`nMax URL length: $maxLen (should be under 2000 for browser compatibility)"

# Build the markdown
$md = @"
# Sequence Diagram with Interactive Activity Drilldown

> **Click any sequence strip below** to expand and see the internal activity diagram for what the Merchant Gateway Service does at that point in the flow.

<details>
<summary><img src="$($urls['strip1'])" alt="1-2: Read fiservProductMapping + OK"></summary>

**Internal Activity: Processing Product Mapping**

<img src="$($urls['act1'])" alt="Activity: Product mapping processing">

</details>

<details>
<summary><img src="$($urls['strip2'])" alt="3-4: Upsert credit scores + Created"></summary>

**Internal Activity: After Credit Score Persistence**

<img src="$($urls['act2'])" alt="Activity: Credit score persistence">

</details>

<details>
<summary><img src="$($urls['strip3'])" alt="5: POST authorisation received"></summary>

**Internal Activity: Processing Authorisation Request**

<img src="$($urls['act3'])" alt="Activity: Process authorisation request">

</details>

<details>
<summary><img src="$($urls['strip4'])" alt="6-7: GET available credit + OK"></summary>

**Internal Activity: Evaluating Available Credit**

<img src="$($urls['act4'])" alt="Activity: Evaluate available credit">

</details>

<details>
<summary><img src="$($urls['strip5'])" alt="8-9: Service Bus send + Responded"></summary>

**Internal Activity: Finalising Rejection**

<img src="$($urls['act5'])" alt="Activity: Finalise rejection">

</details>

<img src="$($urls['strip6'])" alt="10: Bad Request response">
"@

$outputPath = "C:\git\TestTrackingDiagrams\poc-interactive-diagrams.md"
[System.IO.File]::WriteAllText($outputPath, $md, [System.Text.UTF8Encoding]::new($false))
Write-Host "`nGenerated: $outputPath" -ForegroundColor Cyan
Write-Host "File size: $((Get-Item $outputPath).Length) bytes"
Write-Host "`nPaste the contents into a GitHub issue, PR description, or gist to test."
