@echo off
echo Downloading cover images...
if not exist "exe\covers" mkdir "exe\covers"
for /l %%i in (0,1,5) do (
    echo Downloading cover_default%%i.webp...
    curl -L -o "exe\covers\cover_default%%i.webp" "https://i.animepahe.pw/uploads/defaults/cover_default%%i.webp"
)
echo Done.
pause
