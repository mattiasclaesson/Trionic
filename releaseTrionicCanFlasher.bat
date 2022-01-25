call SetupCANFlasher\version.bat
devenv TrionicCanFlasher.sln /Rebuild Release /project SetupCANFlasher

pushd SetupCANFlasher\bin\Release\
"C:\md5sum.exe" TrionicCANFlasher.msi >> TrionicCANFlasher.md5
popd

mkdir z:\TrionicCANFlasher\%SetupCANFlasher.version%
xcopy SetupCANFlasher\version.bat z:\TrionicCANFlasher\%SetupCANFlasher.version%\
xcopy SetupCANFlasher\bin\Release\TrionicCANFlasher.msi z:\TrionicCANFlasher\%SetupCANFlasher.version%\
xcopy SetupCANFlasher\bin\Release\TrionicCANFlasher.md5 z:\TrionicCANFlasher\%SetupCANFlasher.version%\

echo ^<?xml version="1.0" encoding="utf-8"?^>  > z:\TrionicCANFlasher\version.xml
echo ^<canflasher version="%SetupCANFlasher.version%"/^> >> z:\TrionicCANFlasher\version.xml

git tag SetupCANFlasher_v%SetupCANFlasher.version%
git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%