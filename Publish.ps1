# Define paths and server details
$sourcePath = "C:\ClickOnce\Deployment\"  # The ClickOnce publish folder
$serverUrl = "https://nt1.unoeuro.com:8172/msdeploy.axd"
$siteName = "appx.mdk-photo.com"
$userName = "mdk-photo.com"
$password = "byzj42rl"

# Optional: Zip the published files if needed
$packagePath = "C:\ClickOnce\DeploymentPackage.zip"
Compress-Archive -Path $sourcePath\* -DestinationPath $packagePath -Force

# Web Deploy command
& "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" `
    -source:package="$packagePath" `
    -dest:auto,ComputerName="$serverUrl",UserName="$userName",Password="$password",IncludeAcls="False",AuthType="Basic" `
    -setParam:name='IIS Web Application Name',value="$siteName" `
    -allowUntrusted `
    -verbose