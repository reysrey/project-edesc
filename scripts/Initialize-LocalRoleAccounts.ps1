param(
    [string]$Password
)

$ErrorActionPreference = 'Stop'
# This script only provisions development accounts in the repository's local DB.
# It does not add account management endpoints or alter backend source code.
$repositoryPath = Split-Path -Parent $PSScriptRoot
$seedPath = Join-Path $repositoryPath 'src/Infrastructure/Data/ApplicationDbContextInitialiser.cs'
if (-not $Password) {
    $seedSource = Get-Content -Raw -LiteralPath $seedPath
    $passwordMatch = [regex]::Match($seedSource, 'CreateAsync\(adminUser, "([^"]+)"')
    if (-not $passwordMatch.Success) { throw 'Pass -Password explicitly; local seed password was not found.' }
    $Password = $passwordMatch.Groups[1].Value
}
$composeProject = docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' kaizenboard-db
if ($LASTEXITCODE -ne 0 -or $composeProject.Trim() -ne 'project-edesc') {
    throw 'Expected local project-edesc PostgreSQL container kaizenboard-db. Start docker compose first.'
}
function Invoke-LocalSql([string]$Sql) {
    $result = $Sql | docker exec -i kaizenboard-db psql -U kaizen -d KaizenBoardDb -qAt -v ON_ERROR_STOP=1
    if ($LASTEXITCODE -ne 0) { throw 'Local database command failed.' }
    return $result
}
$accounts = @(
    @{ Email = 'employee@localhost'; Name = 'Сотрудник IdeaHub'; Role = 'User'; Label = 'Сотрудник' },
    @{ Email = 'moderator@localhost'; Name = 'Модератор IdeaHub'; Role = 'AdminL2'; Label = 'Модератор' }
)
foreach ($account in $accounts) {
    # Emails and role names below are fixed script-owned fixtures, never user input.
    $email = $account.Email
    $existing = Invoke-LocalSql "SELECT count(*) FROM `"AspNetUsers`" WHERE `"NormalizedEmail`" = upper('$email');"
    if ([int]$existing -eq 0) {
        $body = @{ email = $email; password = $Password } | ConvertTo-Json -Compress
        Invoke-RestMethod -Uri 'http://localhost:5000/api/Users/register' -Method Post -ContentType 'application/json' -Body $body | Out-Null
        $state = 'created with supplied/local seed password'
    } else {
        $state = 'already exists; existing password preserved'
    }
    $roleName = $account.Role
    $name = $account.Name
    $label = $account.Label
    $otherRoles = Invoke-LocalSql "SELECT count(*) FROM `"AspNetUserRoles`" ur JOIN `"AspNetUsers`" u ON u.`"Id`"=ur.`"UserId`" JOIN `"AspNetRoles`" r ON r.`"Id`"=ur.`"RoleId`" WHERE u.`"NormalizedEmail`"=upper('$email') AND r.`"Name`" <> '$roleName';"
    if ([int]$otherRoles -ne 0) { throw "Fixture $email has another role; no existing permissions were removed. Use a separate local database." }
    Invoke-LocalSql @"
INSERT INTO "AspNetUserRoles" ("UserId","RoleId")
SELECT u."Id", r."Id" FROM "AspNetUsers" u CROSS JOIN "AspNetRoles" r
WHERE u."NormalizedEmail"=upper('$email') AND r."Name"='$roleName'
ON CONFLICT DO NOTHING;
UPDATE "AspNetUsers" SET "FullName"='$name', "Position"='$label'
WHERE "NormalizedEmail"=upper('$email');
"@ | Out-Null
    Write-Output "$email — $label ($roleName): $state"
}
Write-Output 'Leader: use the existing local admin@localhost account (AdminL1). Sign out before testing another account, or use separate browser profiles.'
