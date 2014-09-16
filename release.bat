call SetupCANFlash\version.bat
devenv Trionic.sln /Rebuild Release /project SetupCANFlash
mkdir C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%
xcopy SetupCANFlash\version.bat C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\
xcopy SetupCANFlash\Release\TrionicCANFlash.msi C:\users\mattias\Dropbox\public\TrionicCANFlasher\%SetupCANFlash.version%\

git tag TrionicCanFlasher_v%TrionicCANFlasher.version%
git tag TrionicCANLib_v%TrionicCANLib.version%
git tag SetupCANFlash_v%SetupCANFlash.version%