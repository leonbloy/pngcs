#!/bin/sh

# Build distribution (ready for zip) in directory pngcs_nnn  below the script dir.
# The directory shuld not exist 
# This script dir should be located in inside the main solution 
# (eg c:\Users\h\Documents\Visual Studio 2010\Projects\pngcs\dist\build_dist.sh )

DIRS=`dirname $0`

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

cd $DIRS
cp -r ../* $DIRNEW/src/
cd $DIRNEW

mv src/Hjg.Pngcs/bin/Release/Pngcs.dll .
cp src/*.txt .
mv src/*.dll .
mv src/docs .

find . -name bin -type d -exec /bin/rm -rf '{}' \;
find . -name obj -type d -exec /bin/rm -rf '{}' \;
find . -name '*.zip' -exec /bin/rm -f '{}' \;
find . -name '*.sh' -exec /bin/rm -f '{}' \;
/bin/rm -f src/pngcs.suo

cd $DIRS

echo "Output in $DIRNEW. Rename it to your version number and zip it" 

