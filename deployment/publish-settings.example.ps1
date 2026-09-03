# Copy this file to publish-settings.local.ps1 and fill in the password.
# The local file is ignored by Git and must never be committed.

$DeployServer = "https://69.164.255.30:8172/msdeploy.axd"
$DeployUsername = ""
$DeployPassword = "CHANGE_ME"
$AllowUntrusted = $true

$SiteName = "ClarityBelongs"
$BaseUrl = "https://claritybelongs.com/"
