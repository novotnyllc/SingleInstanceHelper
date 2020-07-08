
$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if([string]::IsNullOrEmpty($Env:SignClientSecret)){
	Write-Host "Client Secret not found, not signing packages"
	return;
}

dotnet tool install --tool-path . SignClient

# Setup Variables we need to pass into the sign client tool

$appSettings = "$currentDirectory\appsettings.json"
$fileList = "$currentDirectory\filelist.txt"

$nupkgs = gci $Env:ArtifactDirectory\*.nupkg -recurse | Select -ExpandProperty FullName

foreach ($nupkg in $nupkgs){
	Write-Host "Submitting $nupkg for signing"

	.\SignClient 'sign' -c $appSettings -i $nupkg -f $fileList -r $Env:SignClientUser -s $Env:SignClientSecret -n 'SingleInstanceHelper' -d 'SingleInstanceHelper' -u 'https://github.com/novotnyllc/SingleInstanceHelper'

	Write-Host "Finished signing $nupkg"
}

Write-Host "Sign-package complete"