# Tests reservations : creation, paiement, annulation, consultation, expiration.
# Prerequis : API sur http://localhost:5178 (sans point d'arret), Keycloak et conteneur billetterie-db lances.
# Avant de lancer :  $env:TEST_PWD = "mot_de_passe_des_comptes_de_test"

if (-not $env:TEST_PWD) { Write-Host "Definis d'abord `$env:TEST_PWD" -ForegroundColor Red; return }

$api  = "http://localhost:5178/api"
$evts = "$api/evenements"
$resa = "$api/reservations"

function Get-Token($user) {
    (Invoke-RestMethod -Uri "http://localhost:8080/realms/billetterie/protocol/openid-connect/token" -Method Post `
        -Body @{client_id="billetterie-front"; grant_type="password"; username=$user; password=$env:TEST_PWD}).access_token
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

function Demande($typeId, $quantite) { @{ typeBilletId = $typeId; quantite = $quantite } }
function Reserver($token, $lignes) { Appel Post $resa $token @{ lignes = @($lignes) } }

function Dispo($evtId, $typeId) {
    $r = Appel Get "$evts/$evtId/types-billet" $null $null
    (@($r.Json) | Where-Object { $_.id -eq $typeId }).quantiteDisponible
}

function Nouvel-Evenement($token, $titre, $dateDebut) {
    $corps = @{ titre=$titre; description="Test"; lieu="Paris"; dateDebut=$dateDebut
                dateFin=([datetime]$dateDebut).AddHours(3).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") }
    (Appel Post $evts $token $corps).Json.id
}

function Nouveau-Tarif($token, $evtId, $nom, $prix, $quantite) {
    (Appel Post "$evts/$evtId/types-billet" $token @{ nom=$nom; prix=$prix; quantiteTotale=$quantite }).Json.id
}

# Force l'expiration d'une reservation via le conteneur Postgres (pas besoin de DBeaver)
function Forcer-Expiration($reservationId) {
    $sql = "UPDATE ""Reservations"" SET ""DateExpiration"" = now() - interval '1 minute' WHERE ""Id"" = '$reservationId';"
    $sql | docker exec -i billetterie-db sh -c 'psql -U "$POSTGRES_USER" -d billetterie' | Out-Null
}

# --- Preparation ---
$tA = Get-Token "orgaa"
$tB = Get-Token "orgab"
$tC = Get-Token "utilisateurdetest"
$futur = (Get-Date).AddMonths(2).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$passe = "2020-01-01T20:00:00Z"

# Evenement A : publie, futur, Standard 25 EUR x 10 places, VIP 50 EUR x 5 places
$evtA      = Nouvel-Evenement $tA "Concert reservation A" $futur
$standard  = Nouveau-Tarif $tA $evtA "Standard" 25 10
$vip       = Nouveau-Tarif $tA $evtA "VIP" 50 5
Verifier "Prep publier l'evenement A" 204 (Appel Post "$evts/$evtA/publier" $tA $null).Code

# Evenement B : reste en brouillon, avec un tarif
$evtB   = Nouvel-Evenement $tA "Concert brouillon B" $futur
$tarifB = Nouveau-Tarif $tA $evtB "Standard B" 20 10

# Evenement C : publie mais deja commence
$evtC   = Nouvel-Evenement $tA "Concert passe C" $passe
$tarifC = Nouveau-Tarif $tA $evtC "Standard C" 20 10
Verifier "Prep publier l'evenement C" 204 (Appel Post "$evts/$evtC/publier" $tA $null).Code

# ================= RESERVATIONS =================
Write-Host "`n--- Reservations ---"

Verifier "R1  reservation sans token" 401 (Reserver $null @(Demande $standard 1)).Code

$r2 = Reserver $tC @(Demande $standard 2)
$idR2 = $r2.Json.id
Verifier "R2  reservation valide" 201 $r2.Code
Verifier "R2a montant total (2 x 25)" $true ([decimal]$r2.Json.montantTotal -eq 50)
Verifier "R2b nombre de billets" 2 @($r2.Json.billets).Count
Verifier "R2c statut EnAttente (0)" 0 $r2.Json.statutPaiement
Verifier "R2d code unique masque avant paiement" "" (@($r2.Json.billets | Where-Object { $_.codeUnique }) -join "")
Verifier "R2e nom du type de billet rempli" "Standard" @($r2.Json.billets)[0].nomTypeBillet

Verifier "R3  places restantes apres 2 reservees" 8 (Dispo $evtA $standard)

Verifier "R4  meme type deux fois" 400 (Reserver $tC @((Demande $standard 1), (Demande $standard 1))).Code
Verifier "R5  liste de lignes vide" 400 (Reserver $tC @()).Code
Verifier "R6a quantite 0" 400 (Reserver $tC @(Demande $standard 0)).Code
Verifier "R6b quantite 11" 400 (Reserver $tC @(Demande $standard 11)).Code
Verifier "R7  type de billet inexistant" 404 (Reserver $tC @(Demande ([guid]::NewGuid()) 1)).Code

Verifier "R8  9 places demandees, 8 restantes" 409 (Reserver $tC @(Demande $standard 9)).Code
Verifier "R8a stock inchange" 8 (Dispo $evtA $standard)

Verifier "R9  Standard ok + VIP trop demande" 409 (Reserver $tC @((Demande $standard 2), (Demande $vip 10))).Code
Verifier "R9a stock Standard intact (transaction annulee)" 8 (Dispo $evtA $standard)
Verifier "R9b stock VIP intact" 5 (Dispo $evtA $vip)

Verifier "R10 evenement en brouillon" 404 (Reserver $tC @(Demande $tarifB 1)).Code
Verifier "R11 types de deux evenements differents" 400 (Reserver $tC @((Demande $standard 1), (Demande $tarifB 1))).Code
Verifier "R12 evenement deja commence" 409 (Reserver $tC @(Demande $tarifC 1)).Code

Verifier "R13 les 8 dernieres places" 201 (Reserver $tC @(Demande $standard 8)).Code
Verifier "R13a plus aucune place" 0 (Dispo $evtA $standard)
Verifier "R14 une place de plus" 409 (Reserver $tC @(Demande $standard 1)).Code
Verifier "R14a stock reste a 0" 0 (Dispo $evtA $standard)

# ================= PAIEMENT =================
Write-Host "`n--- Paiement ---"

$evtP   = Nouvel-Evenement $tA "Concert paiement" $futur
$tarifP = Nouveau-Tarif $tA $evtP "Standard P" 30 10
Verifier "P0  publier l'evenement P" 204 (Appel Post "$evts/$evtP/publier" $tA $null).Code

$rP = Reserver $tC @(Demande $tarifP 2)
$resaId = $rP.Json.id
Verifier "P1  payer sans token" 401 (Appel Post "$resa/$resaId/payer" $null $null).Code
Verifier "P2  payer la reservation d'un autre" 404 (Appel Post "$resa/$resaId/payer" $tB $null).Code
Verifier "P3  payer un id inexistant" 404 (Appel Post "$resa/$([guid]::NewGuid())/payer" $tC $null).Code

$pay = Appel Post "$resa/$resaId/payer" $tC $null
Verifier "P4  payer sa reservation" 200 $pay.Code
Verifier "P4a statut Paye (1)" 1 $pay.Json.statutPaiement
Verifier "P4b codes visibles, 22 caracteres" 22 (@($pay.Json.billets)[0].codeUnique).Length
Verifier "P5  payer une seconde fois" 409 (Appel Post "$resa/$resaId/payer" $tC $null).Code

# Reservation non payee dont on force l'expiration
$rE = Reserver $tC @(Demande $tarifP 1)
$idE = $rE.Json.id
Forcer-Expiration $idE
Verifier "P6  payer une reservation expiree" 409 (Appel Post "$resa/$idE/payer" $tC $null).Code

# ================= ANNULATION =================
Write-Host "`n--- Annulation ---"

$evtN   = Nouvel-Evenement $tA "Concert annulation" $futur
$tarifN = Nouveau-Tarif $tA $evtN "Standard N" 30 10
Verifier "N0  publier" 204 (Appel Post "$evts/$evtN/publier" $tA $null).Code

$rN = Reserver $tC @(Demande $tarifN 3)
$idN = $rN.Json.id
Verifier "N1  stock apres reservation de 3" 7 (Dispo $evtN $tarifN)

Verifier "N2  annuler sans token" 401 (Appel Post "$resa/$idN/annuler" $null $null).Code
Verifier "N3  annuler la reservation d'un autre" 404 (Appel Post "$resa/$idN/annuler" $tB $null).Code
Verifier "N4  annuler un id inexistant" 404 (Appel Post "$resa/$([guid]::NewGuid())/annuler" $tC $null).Code

$ann = Appel Post "$resa/$idN/annuler" $tC $null
Verifier "N5  annuler sa reservation" 200 $ann.Code
Verifier "N5a statut Annule (2)" 2 $ann.Json.statutPaiement
Verifier "N5b stock rendu" 10 (Dispo $evtN $tarifN)
Verifier "N6  annuler une seconde fois" 409 (Appel Post "$resa/$idN/annuler" $tC $null).Code
Verifier "N6a stock pas rendu deux fois" 10 (Dispo $evtN $tarifN)
Verifier "N7  payer une reservation annulee" 409 (Appel Post "$resa/$idN/payer" $tC $null).Code

$rP2 = Reserver $tC @(Demande $tarifN 2)
$idP2 = $rP2.Json.id
Appel Post "$resa/$idP2/payer" $tC $null | Out-Null
Verifier "N8  annuler une reservation payee" 409 (Appel Post "$resa/$idP2/annuler" $tC $null).Code
Verifier "N8a stock inchange (8)" 8 (Dispo $evtN $tarifN)

# ================= CONSULTATION =================
Write-Host "`n--- Consultation ---"

Verifier "L1  liste sans token" 401 (Appel Get $resa $null $null).Code

$liste = Appel Get $resa $tC $null
Verifier "L2  liste du client" 200 $liste.Code
Verifier "L2a contient la reservation payee" 1 @($liste.Json | Where-Object { $_.id -eq $resaId }).Count
Verifier "L2b la plus recente en premier" $idP2 @($liste.Json)[0].id

$listeB = Appel Get $resa $tB $null
Verifier "L3  liste d'un utilisateur sans reservation" 200 $listeB.Code
Verifier "L3a liste vide" 0 @($listeB.Json | Where-Object { $_ }).Count

$listeA = Appel Get $resa $tA $null
Verifier "L3b orgaa ne voit pas les reservations du client" 0 @($listeA.Json | Where-Object { $_.id -eq $resaId }).Count

Verifier "L4  detail sans token" 401 (Appel Get "$resa/$resaId" $null $null).Code

$d = Appel Get "$resa/$resaId" $tC $null
Verifier "L5  detail de sa reservation payee" 200 $d.Code
Verifier "L5a statut Paye (1)" 1 $d.Json.statutPaiement
Verifier "L5b codes visibles apres paiement" 22 (@($d.Json.billets)[0].codeUnique).Length
Verifier "L5c nom du type de billet rempli" "Standard P" @($d.Json.billets)[0].nomTypeBillet

$dEnAttente = Appel Get "$resa/$idR2" $tC $null
Verifier "L6  detail d'une reservation en attente" 200 $dEnAttente.Code
Verifier "L6a codes masques avant paiement" "" (@($dEnAttente.Json.billets | Where-Object { $_.codeUnique }) -join "")

$dAnn = Appel Get "$resa/$idN" $tC $null
Verifier "L7  detail d'une reservation annulee" 200 $dAnn.Code
Verifier "L7a statut Annule (2)" 2 $dAnn.Json.statutPaiement
Verifier "L7b billets annules en base (2)" 2 @($dAnn.Json.billets)[0].statut

Verifier "L8  detail de la reservation d'un autre" 404 (Appel Get "$resa/$resaId" $tB $null).Code
Verifier "L8a meme chose avec un organisateur" 404 (Appel Get "$resa/$resaId" $tA $null).Code
Verifier "L9  detail d'un id inexistant" 404 (Appel Get "$resa/$([guid]::NewGuid())" $tC $null).Code

Write-Host "`n--- Expiration automatique ---"

function Forcer-Expiration($id) {
    $sql = "UPDATE ""Reservations"" SET ""DateExpiration"" = now() - interval '1 minute' WHERE ""Id"" = '$id';"
    $sql | docker exec -i billetterie-db sh -c 'psql -U "$POSTGRES_USER" -d billetterie' | Out-Null
}

function Statut($id, $token) { (Appel Get "$resa/$id" $token $null).Json.statutPaiement }

$evtE   = Nouvel-Evenement $tA "Concert expiration" $futur
$tarifE = Nouveau-Tarif $tA $evtE "Standard E" 20 10
Verifier "E0  publier l'evenement E" 204 (Appel Post "$evts/$evtE/publier" $tA $null).Code

# A : en attente, sera expiree (3 places)
# B : payee, puis date forcee dans le passe (2 places)
# C : en attente, date non touchee (1 place)
$idA = (Reserver $tC @(Demande $tarifE 3)).Json.id
$idB = (Reserver $tC @(Demande $tarifE 2)).Json.id
$idC = (Reserver $tC @(Demande $tarifE 1)).Json.id
Appel Post "$resa/$idB/payer" $tC $null | Out-Null
Verifier "E0a stock apres 3 reservations (10 - 6)" 4 (Dispo $evtE $tarifE)

Forcer-Expiration $idA
Forcer-Expiration $idB
Write-Host "Attente du service d'expiration (15 s)..."
Start-Sleep -Seconds 70

$tC = Get-Token "utilisateurdetest"   # token frais
Verifier "E1  reservation expiree : statut Expire (3)"       3 (Statut $idA $tC)
Verifier "E2  stock de A rendu (4 + 3 - ... = 7)"             7 (Dispo $evtE $tarifE)
Verifier "E3  payer une reservation expiree"                  409 (Appel Post "$resa/$idA/payer" $tC $null).Code
Verifier "E3a annuler une reservation expiree"                409 (Appel Post "$resa/$idA/annuler" $tC $null).Code
Verifier "E4  reservation payee jamais expiree (Paye = 1)"    1 (Statut $idB $tC)
Verifier "E5  reservation en cours non touchee (0)"           0 (Statut $idC $tC)
Verifier "E6  billets de A annules"                           3 (@((Appel Get "$resa/$idA" $tC $null).Json.billets | Where-Object { $_.statut -eq 2 }) | Measure-Object).Count

Write-Host "`n--- Mes evenements ---"
$evtM = Nouvel-Evenement $tA "Brouillon de orgaa pour M3" $futur

Verifier "M1  sans token" 401 (Appel Get "$evts/mes-evenements" $null $null).Code
Verifier "M2  client simple (pas organisateur)" 403 (Appel Get "$evts/mes-evenements" $tC $null).Code

$mesA = Appel Get "$evts/mes-evenements" $tA $null
Verifier "M3  orgaa voit son brouillon" 1 (@($mesA.Json | Where-Object { $_.id -eq $evtM }).Count)
Verifier "M3a le brouillon a le statut Brouillon (0)" 0 (@($mesA.Json | Where-Object { $_.id -eq $evtM })[0].statut)
Verifier "M3b le brouillon est absent de la liste publique" 0 (@((Appel Get $evts $null $null).Json | Where-Object { $_.id -eq $evtM }).Count)

$mesB = Appel Get "$evts/mes-evenements" $tB $null
Verifier "M4  orgab ne voit pas l'evenement de orgaa" 0 (@($mesB.Json | Where-Object { $_.id -eq $evtM }).Count)
# ================= RESULTAT =================
Write-Host ("`nResultat : {0} OK, {1} ECHEC" -f $script:ok, $script:ko) -ForegroundColor $(if ($script:ko -eq 0) { "Green" } else { "Red" })