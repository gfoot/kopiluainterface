using System;
using System.Collections.Generic;
using System.Text;
using Lua511;

namespace DummyLuaInterface
{
    public class Lua
    {
        KopiLua.Lua.lua_State luaState;

        static string luaIndexFunction = "local function index(obj,name)\n" +
            "  local meta=getmetatable(obj)\n" +
            "  local cached=meta.cache[name]\n" +
            "  if cached~=nil  then\n" +
            "    return cached\n" +
            "  else\n" +
            "    local value,isFunc=get_object_member(obj,name)\n" +
            "    if isFunc then\n" +
            "      meta.cache[name]=value\n" +
            "    end\n" +
            "    return value\n" +
            "  end\n" +
            "end\n" +
            "return index";

        public Lua()
        {
            luaState = LuaDLL.luaL_newstate();	// steffenj: Lua 5.1.1 API change (lua_open is gone)
            //LuaDLL.luaopen_base(luaState);	// steffenj: luaopen_* no longer used
            LuaDLL.luaL_openlibs(luaState);		// steffenj: Lua 5.1.1 API change (luaopen_base is gone, just open all libs right here)

            LuaDLL.lua_pushstring(luaState, "luaNet_indexfunction");
            LuaDLL.luaL_dostring(luaState, luaIndexFunction);	// steffenj: lua_dostring renamed to luaL_dostring
            //LuaDLL.lua_pushstdcallcfunction(luaState,indexFunction);
            KopiLua.Lua.WriteLine("type: {0}", LuaDLL.lua_type(luaState, -1));
            KopiLua.Lua.Assert(LuaDLL.lua_type(luaState, -1) == LuaTypes.LUA_TFUNCTION, "luaNet_indexfunction ought to have been a function");

            KopiLua.Lua.Assert(false);
        }
    }
}
