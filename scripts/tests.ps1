function Get-Token($user) {
    (Invoke-RestMethod -Uri "http://localhost:8080/realms/billetterie/protocol/openid-connect/token" -Method Post -Body @{client_id="billetterie-front"; grant_type="password"; username=$user; password="isima2026#"}).access_token
}

function Appel($methode, $url, $token, $corps) {
    $h = @{}
    if ($token) { $h.Authorization = "Bearer $token" }
    try {
        $p = @{ Uri=$url; Method=$methode; Headers=$h; ContentType="application/json"; UseBasicParsing=$true }
        if ($corps) { $p.Body = ($corps | ConvertTo-Json) }
        (Invoke-WebRequest @p).StatusCode
    } catch { [int]$_.Exception.Response.StatusCode }
}

$base = "http://localhost:5178/api/evenements"
$id = "82155399-8eee-473e-b192-c92aa5e8013f"
$tA = Get-Token "orgaa"; $tB = Get-Token "orgab"; $tC = Get-Token "utilisateurdetest"

$maj = @{ titre="Concert A modifié"; description="Maj"; lieu="Lyon"; dateDebut="2027-12-05T20:00:00Z"; dateFin="2027-12-05T23:00:00Z" }
$neuf = @{ titre="Test"; description="x"; lieu="Paris"; dateDebut="2027-12-01T20:00:00Z"; dateFin="2027-12-01T23:00:00Z" }

"A7  client crée un évènement (403)  : " + (Appel Post $base $tC $neuf)
"C6  orgaa modifie son évènement (200) : " + (Appel Put "$base/$id" $tA $maj)
"C7  orgab modifie celui d'orgaa (403) : " + (Appel Put "$base/$id" $tB $maj)
"C8  id inexistant (404)              : " + (Appel Put "$base/$([guid]::NewGuid())" $tA $maj)
"C3  liste publique sans token (200)  : " + (Appel Get $base $null $null)
"C4  brouillon en public (404)        : " + (Appel Get "$base/$id" $null $null)
"C5  publier sans tarif (409)         : " + (Appel Post "$base/$id/publier" $tA $null)