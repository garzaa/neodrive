code "C:\Users\Adrian\AppData\Local\Unity\Editor\Editor.log"
echo "use revert file to pick up new changes"

echo "building project"
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" \
    -quit \
    -batchmode \
    -executeMethod ProjectBuilder.BuildWindows
echo "done"
