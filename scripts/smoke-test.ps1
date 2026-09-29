param(
    [string]$BaseUrl = "http://localhost:5028",
    [int]$GenerationTimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

function Invoke-GetJson {
    param([string]$Uri)
    Invoke-RestMethod -Uri $Uri -Method Get -TimeoutSec 60
}

function Wait-UntilReady {
    $deadline = (Get-Date).AddSeconds(60)
    while ($true) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -UseBasicParsing -TimeoutSec 30
            if ($response.StatusCode -eq 200) { return }
        } catch { }

        if ((Get-Date) -gt $deadline) {
            throw "The API did not report readiness within 60 seconds (check database connectivity)."
        }
        Start-Sleep -Seconds 3
    }
}

try {
    Write-Host "== TecAssist.NET end-to-end smoke test =="
    Write-Host "Target: $BaseUrl"

    Wait-UntilReady
    Write-Host "[ok] /health/ready reported healthy"

    $content = @'
The TecAssist Industries TA-2200 voltage regulator ships with firmware revision 7.3.1. In Q3 2026, all 412 delivered units passed the IEC 61010 voltage compliance test at an average measured output of 229.4V (nominal 220V, tolerance +/- 10 percent). Two prototypes initially failed the leakage current test at 0.42mA and were reworked before delivery. The recommended calibration interval for the TA-2200 is 90 days. The extended warranty SKU is TA-2200-XW-36 and covers 36 months.
'@

    $documentPayload = @{
        title = "TA-2200 Q3 Compliance Report"
        content = $content
        sourceType = "text"
    } | ConvertTo-Json

    $document = Invoke-RestMethod -Uri "$BaseUrl/api/documents/text" -Method Post -Body $documentPayload -ContentType "application/json" -TimeoutSec 60
    Write-Host "[ok] document accepted: $($document.id) (status: $($document.status))"

    $deadline = (Get-Date).AddSeconds(180)
    while ($document.status -eq "pending" -or $document.status -eq "processing") {
        if ((Get-Date) -gt $deadline) {
            throw "The document did not finish ingesting within 180 seconds (status: $($document.status))."
        }
        Start-Sleep -Seconds 4
        $document = Invoke-GetJson "$BaseUrl/api/documents/$($document.id)"
        Write-Host "     ingestion status: $($document.status) (chunks: $($document.chunkCount))"
    }

    if ($document.status -ne "ready") {
        throw "Ingestion ended with status '$($document.status)'."
    }
    Write-Host "[ok] document ready with $($document.chunkCount) chunk(s)"

    $conversationPayload = @{ title = "Smoke test conversation" } | ConvertTo-Json
    $conversation = Invoke-RestMethod -Uri "$BaseUrl/api/conversations" -Method Post -Body $conversationPayload -ContentType "application/json" -TimeoutSec 60
    Write-Host "[ok] conversation created: $($conversation.id)"

    $messagePayload = @{
        content = "What firmware revision ships with the TA-2200, and how many delivered units passed the Q3 voltage compliance test?"
    } | ConvertTo-Json

    Write-Host "[..] asking the model (non-streaming; may take a while)..."
    $reply = Invoke-RestMethod -Uri "$BaseUrl/api/conversations/$($conversation.id)/messages" -Method Post -Body $messagePayload -ContentType "application/json" -TimeoutSec $GenerationTimeoutSeconds

    Write-Host "[ok] model answer:"
    Write-Host ""
    Write-Host $reply.answer
    Write-Host ""
    Write-Host "[ok] citations: $($reply.citations.Count)"
    foreach ($citation in $reply.citations) {
        Write-Host ("     - {0} (score {1})" -f $citation.documentTitle, $citation.score.ToString("N3"))
    }

    $messages = Invoke-GetJson "$BaseUrl/api/conversations/$($conversation.id)/messages"
    Write-Host "[ok] message history persisted: $($messages.Count) message(s)"

    Invoke-RestMethod -Uri "$BaseUrl/api/conversations/$($conversation.id)" -Method Delete -TimeoutSec 60 | Out-Null
    Invoke-RestMethod -Uri "$BaseUrl/api/documents/$($document.id)" -Method Delete -TimeoutSec 60 | Out-Null
    Write-Host "[ok] test conversation and document cleaned up"

    Write-Host "== SMOKE TEST PASSED =="
    exit 0
}
catch {
    Write-Host "== SMOKE TEST FAILED =="
    Write-Host $_.Exception.Message
    exit 1
}
