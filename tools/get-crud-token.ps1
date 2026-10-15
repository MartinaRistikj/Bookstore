
$ErrorActionPreference = "Stop"

$clientId = "bookstore-crud-client"
$clientSecret = "bookstore-demo-secret-change-for-production"
$tokenUrl = "http://localhost:8080/realms/bookstore/protocol/openid-connect/token"

try {
    $response = Invoke-RestMethod `
        -Uri $tokenUrl `
        -Method Post `
        -ContentType "application/x-www-form-urlencoded" `
        -Body @{
            grant_type = "client_credentials"
            client_id = $clientId
            client_secret = $clientSecret
            scope = "bookstore.manage"
        }

    $response.access_token | Set-Clipboard

    Write-Host "SUCCESS! CRUD token copied to clipboard."
    Write-Host "Open Swagger and paste it into CrudOAuth."
}
catch {
    Write-Host "Failed to generate CRUD token."
    Write-Host $_.Exception.Message
    exit 1
}
