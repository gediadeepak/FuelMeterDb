#!/usr/bin/env pwsh
# Test script to verify API endpoints

Write-Host "🔍 Testing FuelMeter API Endpoints..." -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://fuelmeter.api.shrijiitservices.com"

# Test 1: Check if BudgetController exists
Write-Host "1️⃣ Testing Budget Controller availability..." -ForegroundColor Yellow
try {
	$response = Invoke-WebRequest -Uri "$baseUrl/api/budget" -Method GET -UseBasicParsing -SkipCertificateCheck
	Write-Host "✅ Budget API is deployed! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
	Write-Host "❌ Budget API NOT deployed: $($_.Exception.Message)" -ForegroundColor Red
	Write-Host "   You need to deploy the updated API to your server." -ForegroundColor Yellow
}
Write-Host ""

# Test 2: Check if BillEstimation exists
Write-Host "2️⃣ Testing Bill Estimation Controller..." -ForegroundColor Yellow
try {
	$response = Invoke-WebRequest -Uri "$baseUrl/api/billestimation/dashboard" -Method GET -UseBasicParsing -SkipCertificateCheck
	Write-Host "✅ Bill Estimation API is deployed! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
	Write-Host "❌ Bill Estimation API NOT deployed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Check if ExportImport exists
Write-Host "3️⃣ Testing Export/Import Controller..." -ForegroundColor Yellow
try {
	$response = Invoke-WebRequest -Uri "$baseUrl/api/exportimport/export/json" -Method GET -UseBasicParsing -SkipCertificateCheck
	Write-Host "✅ Export/Import API is deployed! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
	Write-Host "❌ Export/Import API NOT deployed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "============================================"
Write-Host "If you see ❌ marks above, you need to:"
Write-Host "1. Upload the contents of FuelMeter.Api/publish folder to your server"
Write-Host "2. Restart your API application on the server"
Write-Host ""
Write-Host "Publish folder location:" -ForegroundColor Yellow
Write-Host "C:\Project\FuelMeterDb\FuelMeter.Api\publish" -ForegroundColor White
