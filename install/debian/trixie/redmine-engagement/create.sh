#!/bin/bash
set -e

WORKDIR="/tmp/custom-packages/$(uuid)"
PACKDIR="$WORKDIR/package"
PROGDIR="$PACKDIR/opt/redmine-engagement"
CTRLDIR="$PACKDIR/DEBIAN"

mkdir -p "$PROGDIR"
mkdir -p "$CTRLDIR"

cp -a control "$CTRLDIR/"

cp -a ../../../../BaseLibrary/bin/Release/*.dll "$PROGDIR/"
cp -a ../../../../SiteLibrary/bin/Release/*.dll "$PROGDIR/"
cp -a ../../../../QuaesturApi/bin/Release/*.dll "$PROGDIR/"
cp -a ../../../../RedmineApi/bin/Release/*.dll "$PROGDIR/"
cp -a ../../../../RedmineEngagement/bin/Release/*.dll "$PROGDIR/"
cp -a ../../../../RedmineEngagement/bin/Release/*.exe "$PROGDIR/"

DATETIME="$(find "$PACKDIR" -type f -printf '%TY%Tm%Td%TH%TM\n' | sort | tail -n 1)"

chown -R user:user "$PACKDIR"
find "$PACKDIR" -type d | xargs -I '{}' chmod 0755 {}
find "$PACKDIR" -type f | xargs -I '{}' chmod 0644 {}

PACKAGE_CONTENT_HASH="$(tar --sort=name --owner=root:0 --group=root:0 --mtime='UTC 1970-01-01' -C $WORKDIR -c package | openssl sha256 -binary | hex)"
PACKAGE_VERSION="$(package-version version $PACKAGE_CONTENT_HASH)"
sed -i "s/{{VERSION}}/$PACKAGE_VERSION/g" "$CTRLDIR/control"

PACKAGE_NAME="$(cat control | head -n 1 | cut -d ' ' -f 2)"
PACKAGE_FULLNAME="${PACKAGE_NAME}_${PACKAGE_VERSION}_amd64"
find "$PACKDIR" -exec touch -h -t $DATETIME {} \;
touch -t $DATETIME "$PACKDIR"
pushd "$WORKDIR" > /dev/null
dpkg-deb --build --root-owner-group package
ar -x package.deb 2> /dev/null
rm package.deb
ar -qD package.deb debian-binary control.tar.xz data.tar.xz 2> /dev/null
popd > /dev/null
cp "$WORKDIR/package.deb" "$1/$PACKAGE_FULLNAME.deb"
touch -t $DATETIME "$1/$PACKAGE_FULLNAME.deb"
rm -rf "$WORKDIR"

