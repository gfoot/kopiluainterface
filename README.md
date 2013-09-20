KopiLuaInteface
===============

This is a combination of LuaInterface 2.0.1 with KopiLua 0.1.  The idea is
to provide a pure C# Lua suite for maximum portability in environments like
Unity and XNA.

Note that KopiLua is in a submodule along with a test suite, so after 
cloning KopiLuaInterface you'll need to "git submodule update --init" 
to fill in the KopiLua directory.

What is KopiLua?
----------------

KopiLua is a pure C# Lua implementation - mostly a direct transliteration 
of the standard C implementation.  If you're happy to use the C-style API
and write your own interfacing code on top of it then you can use KopiLua 
on its own, without KopiLuaInterface.

See the documentation in the KopiLua directory for more information.

What is KopiLuaInterface?
-------------------------

KopiLuaInterface is a version of LuaInterface altered to run on top of 
KopiLua.  LuaInterface provides very flexible and user-friendly object
oriented C#/Lua interfacing, making it very easy to provide your Lua 
code with access to C# data and methods, and vice versa.  It is powerful 
and elegant.

Licence
-------

I believe everything here was originally published under the MIT licence, so
that applies to the combination too.  See the original COPYRIGHT files in
the KopiLua and KopiLuaInterface directories.

Modifications
-------------

Both packages have been modified rather a lot, and I've lost track of what I
changed and why.  I hope to figure this out and document it better when I 
get a chance.

Broadly speaking, though, LuaInterface needed changing to
make it work with KopiLua - issues like the distinction between a C function
and a C# function being redundant, and namespacing changes.  I also disabled 
some code that's not suitable for use in Unity's web player.

LuaInterface had a nasty finalizer bug that was worked around upstream by
making various classes disposable, and requiring the user to actually dispose
them.  I fixed that a different way, which still removes the finalizers but
doesn't require users to control the lifetimes.

I've also extended luanet.get_method_bysig to support methods with out and ref
arguments.

KopiLua itself had some bugs - I remember specifically some issues with
userdata, and some lua_assert calls had lost the '!' from their expressions.

Out of date
-----------

Note that LuaInterface is now up to version 2.0.3, so this snapshot is out
of date.  KopiLua has also been added to github and forked there by several
third parties, and it's possible that I ought to merge from the newer
versions.

Sometime I hope to consolidate my changes better, and see if they can be made
less intrusive, so they can more easily be applied to newer versions.

Building With Visual Studio
---------------------------

Note again that KopiLua is in a submodule, so if you see an empty KopiLua 
subdirectory then you probably need to "git submodule update --init".

It's all meant to be built in _Visual Studio_, so just open KLI.sln and build 
everything, preferably in _Release_ configuration for now.  There are a few 
random test apps to check various aspects of the system.  _StressTest_ is 
broken, but the others should work; they tend to wait for you to press Enter 
at the end so the console window doesn't disappear.

For use in Unity, you need to copy all three DLLs (KopiLua, KopiLuaDll, and 
KopiLuaInterface) into your Unity project's Assets folder.  You can also 
copy the PDB files and use _pdb2mdb_ to generate MDB files, so that Unity and 
MonoDevelop can understand the debug info.

The easiest way to do that is using the _publish.sh_ script, if you have 
_Cygwin_.  If not, you'll need to do it by hand - in particular locating 
pdb2mdb is pretty much impossible in a batch file.

Building Without Visual Studio
------------------------------

If you want to put all the source files in your Unity project instead, it
may be a struggle, as KopiLua requires a lot of #defines to work properly.  
However new Unity versions do openly support setting custom defines, in the 
player settings, so you may be able to get this to work.

I have tried loading the solution into Unity's MonoDevelop, with mixed results
- it mostly seems to work, but Unity's MonoDevelop has some limitations.  I
also tried to install a more canonical MonoDevelop, but couldn't get anything
to work properly.

Unity example and pre-built DLLs
--------------------------------

For a demo app, see http://gfootweb.webspace.virginmedia.com/LuaDemo/.  You
can download the source zip which also contains precompiled DLLs.

Contact
-------

If you have questions about the combination of LuaInterface with KopiLua,
you can contact me:

    george.foot@gmail.com

However, bear in mind that I'm not a Lua expert and I'm not the original
author of either package.  Certainly for documentation you should look to
the original packages online.

