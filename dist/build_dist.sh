#!/bin/sh

# Build distribution (ready for zip) in directory pngcs_nnn  below the script dir.
# The directory shuld not exist 
# This script dir should be located in inside the main solution 
# (eg c:\Users\h\Documents\Visual Studio 2010\Projects\pngcs\dist\build_dist.sh )

# http://stackoverflow.com/questions/59895/can-a-bash-script-tell-what-directory-its-stored-in
DIRS="$( cd "$( dirname "$0" )" && pwd )"
# no spaces!
DIRNEW="/C/temp/pngcs_nnn"

if [ -d "$DIRNEW" ]; then
    echo "Directory $DIRNEW already exists. Please remove it".
    exit 1
fi
mkdir "$DIRNEW"
mkdir "$DIRNEW/src"
if [ -d "$DIRNEW" ]; then
 echo "dir $DIRNEW created"
else
 echo "??"
 exit
fi

cd "$DIRS"
cp -r ../* $DIRNEW/src/
cd $DIRNEW

mkdir dotnet20
mkdir dotnet45

mv src/Hjg.Pngcs/bin/Release/Pngcs.dll dotnet20/
mv src/Hjg.Pngcs/bin/Release45/Pngcs.dll dotnet45/Pngcs45.dll
cp src/*.txt .
mv src/*.dll dotnet20
mv src/docs .

find . -name bin -type d -exec /bin/rm -rf '{}' \;
find . -name obj -type d -exec /bin/rm -rf '{}' \;
find . -name '*.zip' -exec /bin/rm -f '{}' \;
find . -name '*.sh' -exec /bin/rm -f '{}' \;
/bin/rm -f src/pngcs.suo
/bin/rm -rf src/dist/

cd "$DIRS"

echo "Did you updated you version number?"

pwd

grep AssemblyVersion ../Hjg.Pngcs/Properties/AssemblyInfo.cs  | grep -v '^/'

echo "Output in $DIRNEW. Rename the directory with your version number and zip it" 

