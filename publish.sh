#!/bin/ash
target_dir="$1"
if [ -z "$target_dir" -o ! -d "$target_dir" ]; then
    echo "You must specify a target directory, and optionally a build type (debug or release)"
    exit 1
fi

type="$2"
if [ -z "$type" ]; then
    type="Release"
fi
modules="KopiLua KopiLuaDll KopiLuaInterface DummyLuaInterface"
files=""
for module in $modules; do
    files="$files $module/bin/$type/$module.dll"
done

echo "Copying these files to $target_dir:"
ls -l $files
cp -f $files $target_dir
