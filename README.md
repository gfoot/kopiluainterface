KopiLuaInteface {version} ({date})
==================================


This is a combination of LuaInterface 2.0.1 with KopiLua 0.1.  The idea is
to provide a pure C# Lua suite for maximum portability in environments like
Unity and XNA.


Licence
-------

I believe everything here was originally published under the MIT licence, so
that applies to the combination too.  See the original COPYRIGHT files in
the KopiLua and KopiLuaInterface directories.


Modifications
-------------

Both packages have been modified rather a lot, and I've lost track of what I
changed and why.  Broadly speaking, though, LuaInterface needed changing to
make it work with KopiLua - issues like the distinction between a C function
and a C# function being redundant.  I also disabled some code that's not
suitable for use in Unity's web player.

KopiLua itself had some bugs - I remember specifically some issues with
userdata, and some lua_assert calls had lost the '!' from their expressions.


Out of date
-----------

Note that LuaInterface is now up to version 2.0.3, so this snapshot is out
of date.  KopiLua has also been added to github and extended there by a
third party, and it's possible that the most recent version there would be a
better base than what I have used so far.

I will try to consolidate my changes better, and see if they can be made
less intrusive, so they can more easily be applied to newer versions.


Building
--------

KopiLua won't build without certain #defines, so you can't just put its C#
files into a Unity project - you need to build it to a DLL in Visual Studio
and put the DLL into your Unity project.


Unity example and pre-built DLLs
--------------------------------

The Unity directory contains a Unity demo app, including the three compiled
Kopi DLLs.


Contact
-------

If you have questions about the combination of LuaInterface with KopiLua,
you can contact me:

    george.foot@gmail.com

However, bear in mind that I'm not a Lua expert and I'm not the original
author of either package.  Certainly for documentation you should look to
the original packages online.

