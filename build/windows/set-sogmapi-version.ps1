#
#
# This is the PowerShell equivalent of ../unix/set-sogmapi-version.sh.
# When making changes, both scripts should be updated.
#
#


. "$PSScriptRoot\lib\in-place-regex.ps1"

# get version number
$version=$args[0]
if (!$version) {
    $version = Read-Host "SoGMAPI release version (like '4.0.0')"
}

# move to SoGMAPI root
cd "$PSScriptRoot/../.."

# apply changes
In-Place-Regex -Path "build/common.targets" -Search "<Version>.+</Version>" -Replace "<Version>$version</Version>"
In-Place-Regex -Path "src/SoGMAPI/Constants.cs" -Search "RawApiVersion = `".+?`";" -Replace "RawApiVersion = `"$version`";"
ForEach ($modName in "ConsoleCommands","ErrorHandler","SaveBackup") {
    In-Place-Regex -Path "src/SoGMAPI.Mods.$modName/manifest.json" -Search "`"(Version|MinimumApiVersion)`": `".+?`"" -Replace "`"`$1`": `"$version`""
}
