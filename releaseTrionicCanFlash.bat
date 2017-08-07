call SetupCANFlash\version.bat
devenv TrionicCanFlasher.sln /Rebuild Release /project SetupCANFlash

pushd SetupCANFlash\Release\
"C:\Program Files (x86)\hashutils-1.3.0-redist\bin.x86-32\md5sum.exe" TrionicCANFlash.msi >> TrionicCANFlash.md5
"C:\Program Files\7-Zip\7z.exe" a -tzip TrionicCANFlash.zip TrionicCANFlash.* setup.exe
popd

mkdir z:\TrionicCANFlasher\%SetupCANFlash.version%
xcopy SetupCANFlash\version.bat z:\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.msi z:\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.md5 z:\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.zip z:\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\setup.exe z:\TrionicCANFlasher\%SetupCANFlash.version%\


echo ^<?xml version="1.0" encoding="utf-8"?^>  > z:\TrionicCANFlasher\version.xml
echo ^<canflasher version="%SetupCANFlash.version%"/^> >> z:\TrionicCANFlasher\version.xml

echo ----------------------------------------------------
git changes
echo ----------------------------------------------------

git tag SetupCANFlash_v%SetupCANFlash.version%
git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%