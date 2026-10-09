# Clarity Belongs no longer stores Web Deploy credentials in a repository-local settings file.
#
# Software Belongs, AutoPilot IT, and Clarity Belongs all use the same
# DPAPI-encrypted PSCredential:
#
#   %LOCALAPPDATA%\SoftwareBelongs\Publish\webdeploy-credential.xml
#
# Create or refresh it with:
#
#   powershell -ExecutionPolicy Bypass -File C:\Projects\SoftwareBelongs\publish\setup-credential.ps1
#
# Stable Clarity deployment settings (server, IIS site, base URL) live in
# scripts\publish.ps1 and contain no password.
