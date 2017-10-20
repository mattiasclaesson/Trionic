call SetupCANFlasher\version.bat
devenv TrionicCanFlasher.sln /Rebuild Release /project SetupCANFlasher

pushd SetupCANFlasher\bin\Release\
"C:\Program Files (x86)\hashutils-1.3.0-redist\bin.x86-32\md5sum.exe" TrionicCANFlasher.msi >> TrionicCANFlasher.md5
popd

mkdir z:\TrionicCANFlasher\%SetupCANFlasher.version%
xcopy SetupCANFlasher\version.bat z:\TrionicCANFlasher\%SetupCANFlasher.version%\
xcopy SetupCANFlasher\bin\Release\TrionicCANFlash.msi z:\TrionicCANFlasher\%SetupCANFlasher.version%\
xcopy SetupCANFlasher\bin\Release\TrionicCANFlash.md5 z:\TrionicCANFlasher\%SetupCANFlasher.version%\

echo ^<?xml version="1.0" encoding="utf-8"?^>  > z:\TrionicCANFlasher\version.xml
echo ^<canflasher version="%SetupCANFlasher.version%"/^> >> z:\TrionicCANFlasher\version.xml

echo ----------------------------------------------------
git changes
echo ----------------------------------------------------

git tag SetupCANFlasher_v%SetupCANFlasher.version%
git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%