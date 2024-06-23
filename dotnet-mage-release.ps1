# Load current Git tag
$tag = $(git describe --tags)
$version = $tag.Split('-')[0].TrimStart('v')
$version = "$version.0"

Write-Output "Tag: $tag"
Write-Output "Version: $version"

# $name = "Heroes Profile Uploader"
$publisher = "Heroes Profile"
$appName = "Heroesprofile.Uploader"
$projDir = ".\Heroesprofile.Uploader.Windows"

$outDir = "$projDir\bin\publish"
if (Test-Path $outDir) {
    Remove-Item -Path $outDir -Recurse
}

dotnet tool install --global microsoft.dotnet.mage --version 8.0.0

dotnet publish "${projDir}" -c Release -r win-x64 --self-contained false -o "${outDir}"

# Remove PDB files
Get-ChildItem -Path "${outDir}" -Filter *.pdb -Recurse | Remove-Item -Force

dotnet mage -ClearApplicationCache

dotnet mage -al "${appName}.exe" -TargetDirectory "${outDir}"

dotnet mage -New Application `
    -FromDirectory "${outDir}" `
    -ToFile "${outDir}\${appName}.exe.manifest" `
    -Name "${appName}" `
    -Version $version `
    -IconFile "icon.ico"

dotnet mage -New Deployment `
    -Install true `
    -Publisher "${publisher}" `
    -Version $version `
    -Name "${appName}" `
    -AppManifest "${outDir}\${appName}.exe.manifest" `
    -ToFile "${outDir}\${appName}.application" `
    -ProviderUrl "https://heroesreplay.github.io/HeroesProfile.Uploader/${appName}.application" `
    -SupportURL "https://www.heroesprofile.com/Contact"


# dotnet mage -Verify "${outDir}\${appName}.application"
# dotnet mage -Verify "${outDir}\${appName}.manifest"
