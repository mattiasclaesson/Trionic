call SetupCANFlash\version.bat
devenv Trionic.sln /Rebuild Release /project SetupCANFlash

pushd SetupCANFlash\Release\
"C:\Program Files (x86)\hashutils-1.3.0-redist\bin.x86-32\md5sum.exe" TrionicCANFlash.msi >> TrionicCANFlash.md5
"C:\Program Files\7-Zip\7z.exe" a -tzip TrionicCANFlash.zip TrionicCANFlash.* setup.exe
popd

mkdir C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%
xcopy SetupCANFlash\version.bat C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.msi C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.md5 C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.zip C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\setup.exe C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\


echo ^<?xml version="1.0" encoding="utf-8"?^>  > C:\users\mattias\Dropbox\public\TrionicCANFlasher\version.xml
echo ^<canflasher version="%SetupCANFlash.version%"/^> >> C:\users\mattias\Dropbox\public\TrionicCANFlasher\version.xml

echo ----------------------------------------------------
git changes
echo ----------------------------------------------------
git tag SetupCANFlash_v%SetupCANFlash.version%
git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%