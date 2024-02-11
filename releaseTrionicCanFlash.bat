call SetupCANFlash\version.bat
devenv TrionicCanFlasher.sln /Rebuild Release /project SetupCANFlash

pushd SetupCANFlash\Release\
"C:\md5sum.exe" TrionicCANFlash.msi >> TrionicCANFlash.md5
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

git tag SetupCANFlash_v%SetupCANFlash.version%
git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%

git push --tags

gh release create TrionicCanFlasher_v%TrionicCANFlasher.version% --generate-notes --verify-tag
gh release upload TrionicCanFlasher_v%TrionicCANFlasher.version% SetupT8SuitePro\Release\TrionicCANFlash.zip
gh release upload TrionicCanFlasher_v%TrionicCANFlasher.version% SetupT8SuitePro\Release\TrionicCANFlash.msi
gh release upload TrionicCanFlasher_v%TrionicCANFlasher.version% SetupT8SuitePro\Release\TrionicCANFlash.md5