param(
    [string]$BaseUrl = "http://localhost:5028",
    [int]$GenerationTimeoutSeconds = 300,
    [string]$SupabaseUrl,
    [string]$AnonKey,
    [string]$Email,
    [string]$Password,
    [string]$BearerToken
)

$ErrorActionPreference = "Stop"

$headers = @{}
if (-not $BearerToken -and $SupabaseUrl -and $AnonKey -and $Email -and $Password) {
    $body = @{ grant_type = "password"; email = $Email; password = $Password } | ConvertTo-Json
    $auth = Invoke-RestMethod -Uri "$SupabaseUrl/auth/v1/token?grant_type=password" -Method Post -ContentType "application/json" -Headers @{ apikey = $AnonKey; Authorization = "Bearer $AnonKey" } -Body $body -TimeoutSec 60
    $BearerToken = $auth.access_token
    Write-Host "[ok] signed in to Supabase as $Email"
}
if ($BearerToken) {
    $headers["Authorization"] = "Bearer $BearerToken"
}

function Invoke-GetJson {
    param([string]$Uri)
    Invoke-RestMethod -Uri $Uri -Method Get -Headers $headers -TimeoutSec 60
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

    $document = Invoke-RestMethod -Uri "$BaseUrl/api/documents/text" -Method Post -Headers $headers -Body $documentPayload -ContentType "application/json" -TimeoutSec 60
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
    $conversation = Invoke-RestMethod -Uri "$BaseUrl/api/conversations" -Method Post -Headers $headers -Body $conversationPayload -ContentType "application/json" -TimeoutSec 60
    Write-Host "[ok] conversation created: $($conversation.id)"

    $messagePayload = @{
        content = "What firmware revision ships with the TA-2200, and how many delivered units passed the Q3 voltage compliance test?"
    } | ConvertTo-Json

    Write-Host "[..] asking the model (SSE streaming; may take a while)..."
    $raw = Invoke-WebRequest -Uri "$BaseUrl/api/conversations/$($conversation.id)/messages" -Method Post -Headers $headers -Body $messagePayload -ContentType "application/json" -TimeoutSec $GenerationTimeoutSeconds -UseBasicParsing

    $answer = New-Object System.Text.StringBuilder
    $citationCount = 0
    $tokenCount = 0
    $assistantMessageId = $null

    $frames = $raw.Content -split "`r?`n`r?`n"
    foreach ($frame in $frames) {
        if (-not $frame.Trim()) { continue }

        $eventName = $null
        $dataLine = $null
        foreach ($line in ($frame -split "`r?`n")) {
            if ($line.StartsWith("event: ")) { $eventName = $line.Substring(7).Trim() }
            elseif ($line.StartsWith("data: ")) { $dataLine = $line.Substring(6) }
        }

        if ($null -eq $dataLine) { continue }
        $data = $dataLine | ConvertFrom-Json

        switch ($eventName) {
            "token" { [void]$answer.Append($data.text); $tokenCount++ }
            "citation" { $citationCount++ }
            "done" { $assistantMessageId = $data.assistantMessageId }
            "error" { throw "The stream reported an error: $($data.message)" }
        }
    }

    if ($tokenCount -eq 0) { throw "The model streamed no tokens." }
    if (-not $assistantMessageId) { throw "The stream did not complete (no done frame)." }

    Write-Host "[ok] streamed $tokenCount token frame(s) with $citationCount citation frame(s)"
    Write-Host "[ok] model answer:"
    Write-Host ""
    Write-Host $answer.ToString()
    Write-Host ""

    $messages = Invoke-GetJson "$BaseUrl/api/conversations/$($conversation.id)/messages"
    Write-Host "[ok] message history persisted: $($messages.Count) message(s)"

    Invoke-RestMethod -Uri "$BaseUrl/api/conversations/$($conversation.id)" -Method Delete -Headers $headers -TimeoutSec 60 | Out-Null
    Invoke-RestMethod -Uri "$BaseUrl/api/documents/$($document.id)" -Method Delete -Headers $headers -TimeoutSec 60 | Out-Null
    Write-Host "[ok] test conversation and document cleaned up"

    Write-Host "== SMOKE TEST PASSED =="
    exit 0
}
catch {
    Write-Host "== SMOKE TEST FAILED =="
    Write-Host $_.Exception.Message
    exit 1
}
