# Tests de la demande d'organisateur et du back-office admin.
# Prerequis : API sur http://localhost:5178, Keycloak lance, conteneur Docker billetterie-db.
# Comptes : orgaa (organisateur valide), orgab (organisateur valide), utilisateurdetest (client),
#           superadmin (role Admin).
# Avant de lancer :
#   $env:TEST_PWD  = "mot_de_passe_des_comptes_de_test"
#   $env:ADMIN_PWD = "mot_de_passe_de_superadmin"
# Le script remet a zero la demande du client de test (utilisateurdetest) en base : il est rejouable.
# Il ne touche jamais a orgaa / orgab.

if (-not $env:TEST_PWD)  { Write-Host "Definis d'abord `$env:TEST_PWD"  -ForegroundColor Red; return }
if (-not $env:ADMIN_PWD) { Write-Host "Definis d'abord `$env:ADMIN_PWD" -ForegroundColor Red; return }

$api      = "http://localhost:5178/api"
$evts     = "$api/evenements"
$orgUrl   = "$api/organisateurs"
$adminUrl = "$api/admin/organisateurs"

function Get-Token($user, $pwd) {
    (Invoke-RestMethod -Uri "http://localhost:8080/realms/billetterie/protocol/openid-connect/token" -Method Post `
        -Body @{client_id="billetterie-front"; grant_type="password"; username=$user; password=$pwd}).access_token
}

function Get-Email($token) {
    $p = $token.Split('.')[1].Replace('-','+').Replace('_','/')
    while ($p.Length % 4) { $p += '=' }
    ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($p)) | ConvertFrom-Json).email
}

function Appel($methode, $url, $token, $corps) {
    $h = @{}
    if ($token) { $h.Authorization = "Bearer $token" }
    $p = @{ Uri=$url; Method=$methode; Headers=$h; UseBasicParsing=$true; ContentType="application/json; charset=utf-8" }
    if ($null -ne $corps) { $p.Body = [Text.Encoding]::UTF8.GetBytes(($corps | ConvertTo-Json -Depth 6)) }
    $json = $null
    try {
        $r = Invoke-WebRequest @p
        $code = [int]$r.StatusCode
        if ($r.Content) { try { $json = $r.Content | ConvertFrom-Json } catch {} }
    } catch {
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        else { $code = "ERREUR : " + $_.Exception.Message }
    }
    [pscustomobject]@{ Code = $code; Json = $json }
}

$script:ok = 0; $script:ko = 0
function Verifier($nom, $attendu, $obtenu) {
    if ("$attendu" -eq "$obtenu") { $script:ok++; Write-Host ("OK     {0} (attendu {1})" -f $nom, $attendu) -ForegroundColor Green }
    else { $script:ko++; Write-Host ("ECHEC  {0} : attendu {1}, obtenu {2}" -f $nom, $attendu, $obtenu) -ForegroundColor Red }
}

function Sql($requete) {
    $requete | docker exec -i billetterie-db sh -c 'psql -U "$POSTGRES_USER" -d billetterie -t -A'
}

# Remet a zero le compte client : supprime ses evenements puis sa ligne Organisateur
function Reset-Demande($email) {
    Sql "DELETE FROM ""Evenements"" WHERE ""OrganisateurId"" IN (SELECT o.""Id"" FROM ""Organisateurs"" o JOIN ""Utilisateurs"" u ON u.""Id"" = o.""UtilisateurId"" WHERE u.""Email"" = '$email');" | Out-Null
    Sql "DELETE FROM ""Organisateurs"" WHERE ""UtilisateurId"" IN (SELECT ""Id"" FROM ""Utilisateurs"" WHERE ""Email"" = '$email');" | Out-Null
}

function Id-Demande($tokenAdmin, $email) {
    $liste = Appel Get "$adminUrl`?statut=0" $tokenAdmin $null
    (@($liste.Json) | Where-Object { $_.email -eq $email } | Select-Object -First 1).id
}

# --- Preparation ---
$tA     = Get-Token "orgaa" $env:TEST_PWD
$tC     = Get-Token "utilisateurdetest" $env:TEST_PWD
$tAdmin = Get-Token "superadmin" $env:ADMIN_PWD
foreach ($t in @($tA, $tC, $tAdmin)) { Appel Post "$api/utilisateurs/sync" $t $null | Out-Null }

$emailC = Get-Email $tC
$emailA = Get-Email $tA
$futur  = (Get-Date).AddMonths(2).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$corpsEvt = @{ titre="Evenement admin test"; description="x"; lieu="Paris"; dateDebut=$futur
               dateFin=([datetime]$futur).AddHours(3).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") }

Reset-Demande $emailC

# --- Demande du client (etat de depart des tests admin) ---
Write-Host "`n--- Demande du client ---"
Verifier "D0  demande du client" 201 (Appel Post "$orgUrl/demande" $tC $null).Code

# --- Liste des demandes ---
Write-Host "`n--- Admin : liste des demandes ---"
Verifier "A1  liste sans token" 401 (Appel Get $adminUrl $null $null).Code
Verifier "A2  liste en tant que client" 403 (Appel Get $adminUrl $tC $null).Code
Verifier "A2a liste en tant qu'organisateur" 403 (Appel Get $adminUrl $tA $null).Code

$a3 = Appel Get $adminUrl $tAdmin $null
Verifier "A3  liste en tant qu'admin" 200 $a3.Code

$attente = Appel Get "$adminUrl`?statut=0" $tAdmin $null
Verifier "A4  filtre EnAttente" 200 $attente.Code
Verifier "A4a la demande du client apparait" 1 (@($attente.Json | Where-Object { $_.email -eq $emailC }).Count)
Verifier "A4b seulement des demandes EnAttente" 0 (@($attente.Json | Where-Object { $_.statutValidation -ne 0 }).Count)

$valides = Appel Get "$adminUrl`?statut=1" $tAdmin $null
Verifier "A5  orgaa apparait parmi les Valide" 1 (@($valides.Json | Where-Object { $_.email -eq $emailA }).Count)
Verifier "A5a orgaa absent du filtre EnAttente" 0 (@($attente.Json | Where-Object { $_.email -eq $emailA }).Count)
Verifier "A6  statut invalide" 400 (Appel Get "$adminUrl`?statut=abc" $tAdmin $null).Code

$premier = @($a3.Json)[0]
Verifier "A7  pas de keycloakId expose" $null $premier.keycloakId
Verifier "A7a pas d'id d'admin expose" $null $premier.valideParUtilisateurId
Verifier "A8  l'id de la demande est fourni" $true ([bool]$premier.id)

# --- Controle d'acces sur valider / refuser ---
Write-Host "`n--- Admin : controle d'acces ---"
$idDemande = Id-Demande $tAdmin $emailC
Verifier "V0  id de la demande retrouve" $true ([bool]$idDemande)

Verifier "V1  valider sans token" 401 (Appel Post "$adminUrl/$idDemande/valider" $null $null).Code
Verifier "V1a refuser sans token" 401 (Appel Post "$adminUrl/$idDemande/refuser" $null $null).Code
Verifier "V2  valider en tant que client" 403 (Appel Post "$adminUrl/$idDemande/valider" $tC $null).Code
Verifier "V2a valider en tant qu'organisateur" 403 (Appel Post "$adminUrl/$idDemande/valider" $tA $null).Code
Verifier "V2b refuser en tant que client" 403 (Appel Post "$adminUrl/$idDemande/refuser" $tC $null).Code
Verifier "V3  valider un id inexistant" 404 (Appel Post "$adminUrl/$([guid]::NewGuid())/valider" $tAdmin $null).Code
Verifier "V3a refuser un id inexistant" 404 (Appel Post "$adminUrl/$([guid]::NewGuid())/refuser" $tAdmin $null).Code
Verifier "V4  la demande est restee EnAttente (0)" 0 (Appel Get "$orgUrl/ma-demande" $tC $null).Json.statutValidation
Verifier "V4a le client n'est pas encore organisateur" 403 (Appel Post $evts $tC $corpsEvt).Code

# --- Scenario refus ---
Write-Host "`n--- Admin : refus ---"
Verifier "R1  refuser la demande" 204 (Appel Post "$adminUrl/$idDemande/refuser" $tAdmin $null).Code
$ma = Appel Get "$orgUrl/ma-demande" $tC $null
Verifier "R1a statut Refuse (2)" 2 $ma.Json.statutValidation
Verifier "R1b date de validation renseignee" $true ([bool]$ma.Json.dateValidation)
Verifier "R1c client refuse : pas d'evenement" 403 (Appel Post $evts $tC $corpsEvt).Code
Verifier "R2  refuser une seconde fois" 409 (Appel Post "$adminUrl/$idDemande/refuser" $tAdmin $null).Code
Verifier "R2a valider une demande refusee" 409 (Appel Post "$adminUrl/$idDemande/valider" $tAdmin $null).Code
Verifier "R3  un refuse ne peut pas redemander" 409 (Appel Post "$orgUrl/demande" $tC $null).Code

# --- Scenario validation (on repart d'une demande neuve) ---
Write-Host "`n--- Admin : validation ---"
Reset-Demande $emailC
Verifier "S0  nouvelle demande du client" 201 (Appel Post "$orgUrl/demande" $tC $null).Code
$idDemande = Id-Demande $tAdmin $emailC

Verifier "S1  valider la demande" 204 (Appel Post "$adminUrl/$idDemande/valider" $tAdmin $null).Code
$ma = Appel Get "$orgUrl/ma-demande" $tC $null
Verifier "S1a statut Valide (1)" 1 $ma.Json.statutValidation
Verifier "S1b date de validation renseignee" $true ([bool]$ma.Json.dateValidation)
Verifier "S1c le client peut maintenant creer un evenement" 201 (Appel Post $evts $tC $corpsEvt).Code

Verifier "S2  valider une seconde fois" 409 (Appel Post "$adminUrl/$idDemande/valider" $tAdmin $null).Code
Verifier "S2a refuser une demande deja validee" 409 (Appel Post "$adminUrl/$idDemande/refuser" $tAdmin $null).Code
Verifier "S2b le statut est reste Valide (1)" 1 (Appel Get "$orgUrl/ma-demande" $tC $null).Json.statutValidation

# Audit : qui a valide, et quand (verification en base)
$audit = (Sql "SELECT (""ValideParUtilisateurId"" IS NOT NULL AND ""DateValidation"" IS NOT NULL) FROM ""Organisateurs"" WHERE ""Id"" = '$idDemande';") -join ""
Verifier "S3  audit en base : valide par + date renseignes" "t" $audit.Trim()

$parAdmin = (Sql "SELECT u.""Email"" FROM ""Organisateurs"" o JOIN ""Utilisateurs"" u ON u.""Id"" = o.""ValideParUtilisateurId"" WHERE o.""Id"" = '$idDemande';") -join ""
Verifier "S3a l'admin enregistre est bien superadmin" (Get-Email $tAdmin) $parAdmin.Trim()

# --- Un admin ne traite pas sa propre demande ---
Write-Host "`n--- Admin : auto-validation interdite ---"
$emailAdmin = Get-Email $tAdmin
Reset-Demande $emailAdmin

Verifier "X0  l'admin fait sa propre demande" 201 (Appel Post "$orgUrl/demande" $tAdmin $null).Code
$idAdmin = Id-Demande $tAdmin $emailAdmin
Verifier "X0a sa demande apparait dans la liste" $true ([bool]$idAdmin)

Verifier "X1  valider sa propre demande" 403 (Appel Post "$adminUrl/$idAdmin/valider" $tAdmin $null).Code
Verifier "X1a refuser sa propre demande" 403 (Appel Post "$adminUrl/$idAdmin/refuser" $tAdmin $null).Code
Verifier "X2  sa demande est restee EnAttente (0)" 0 (Appel Get "$orgUrl/ma-demande" $tAdmin $null).Json.statutValidation
Verifier "X2a aucune trace de validation en base" "f" ((Sql "SELECT (""ValideParUtilisateurId"" IS NOT NULL OR ""DateValidation"" IS NOT NULL) FROM ""Organisateurs"" WHERE ""Id"" = '$idAdmin';") -join "").Trim()

# Le reste du back-office fonctionne toujours pour les autres demandes
Verifier "X3  l'admin peut toujours traiter la demande d'un autre (client : deja Valide, donc 409)" 409 (Appel Post "$adminUrl/$idDemande/valider" $tAdmin $null).Code

# Nettoyage : le client de test redevient un simple client
Reset-Demande $emailC

Write-Host ("`nResultat : {0} OK, {1} ECHEC" -f $script:ok, $script:ko) -ForegroundColor $(if ($script:ko -eq 0) { "Green" } else { "Red" })
Write-Host "Note : a la fin du script, utilisateurdetest est organisateur valide (il est remis a zero au prochain lancement)." -ForegroundColor DarkGray

