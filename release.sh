alias butler="D:/Program\ Files/butler-windows-amd64/butler.exe"
alias 7z="C:/Program\ Files/7-Zip/7z.exe"

function itchrelease() {
    for i in win-exe; do
        butler push ./demos/neodrive-$i sevencrane/neodrive:$i
    done
}

function zip() {
    for i in win-exe; do
        7z a ./demos/zips/neodrive-$i.zip ./demos/$i 
    done
}

set -x
itchrelease
zip

set +x
