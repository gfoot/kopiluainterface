#!/bin/ash
target_dir="$1"
root=`dirname "$0"`
if [ -z "$target_dir" -o ! -d "$target_dir" ]; then
    echo "You must specify a target directory, and optionally a build type (debug or release)"
    exit 1
fi

type="$2"
if [ -z "$type" ]; then
    type="Release"
fi
modules="KopiLua KopiLuaDll KopiLuaInterface"
files=""
for module in $modules; do
    files="$files $root/$module/bin/$type/$module.dll"
    files="$files $root/$module/bin/$type/$module.pdb"
done

echo "Copying these files to $target_dir:"
ls -l $files
cp -f $files $target_dir

echo "Generating MDB files..."
unityeditorexe=`cat "/proc/registry/HKEY_CURRENT_USER/Software/Unity Technologies/Unity Editor 3.x/Location/@"`
unityeditordir=`cygpath -a "$unityeditorexe"/..`
pdb2mdb="${unityeditordir}Data/MonoBleedingEdge/lib/mono/4.0/pdb2mdb.exe"
cd "$target_dir"
for module in $modules; do
	echo $module.dll
	"$pdb2mdb" $module.dll
done

