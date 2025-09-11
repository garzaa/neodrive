alias butler="D:/Program\ Files/butler-windows-amd64/butler.exe"
alias 7z="C:/Program\ Files/7-Zip/7z.exe"
alias gh="C:/Program\ Files/GitHub\ CLI/gh.exe"

function itchrelease() {
    for i in win-exe gnu-linux win32-exe osx; do
        butler push ./demos/neodrive-$i sevencrane/neodrive:$i
    done
}

function zip() {
    for i in win-exe gnu-linux win32-exe osx; do
        7z a ./demos/zips/neodrive-$i.zip ./demos/neodrive-$i 
    done
}

function gitrelease() {
    TAG=$(cat ProjectSettings/ProjectSettings.asset | grep bundleVersion | cut -d':' -f2 | xargs)
    echo $TAG
    gh release create $TAG Demos/zips/*.zip  --generate-notes
}

set -x
itchrelease
zip
gitrelease

set +x
