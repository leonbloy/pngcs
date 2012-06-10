#!/bin/sh

# Build distribution (ready for zip) in directory pngcs_nnn  below the script dir.
# The directory shuld not exist 
# This script dir should be located in inside the main solution 
# (eg c:\Users\h\Documents\Visual Studio 2010\Projects\pngcs\dist\build_dist.sh )

DIRS=`dirname $0`

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

cp -r "$DIRS/.." "$DIRNEW/src"

rm -rf "$DIRNEW/src/.git"
cp "$DIRNEW/src/Ar.Com.Hjg.Pngcs/bin/Release/Pngcs.dll" "$DIRNEW/"
cp "$DIRNEW/src/COPYING.txt" "$DIRNEW/"
cp "$DIRNEW/src/readme.txt" "$DIRNEW/"
find "$DIRNEW" -name bin -type d -exec /bin/rm -rf '{}' \;
find "$DIRNEW" -name obj -type d -exec /bin/rm -rf '{}' \;
find "$DIRNEW" -name '*.zip' -exec /bin/rm -f '{}' \;
find "$DIRNEW" -name '*.sh' -exec /bin/rm -f '{}' \;
/bin/rm -f "$DIRNEW/src/pngcs.suo" 

echo "Output in $DIRNEW. Rename it to your version number and zip it" 

