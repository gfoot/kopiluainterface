using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lua511;

namespace LLStressTest
{
    class LLStressTest
    {
        static void Main(string[] args)
        {
            var L = LuaDLL.luaL_newstate();
            LuaDLL.lua_dostring(L, "tab = {}");
            while (true)
            {
                for (int i = 0; i < 1000; ++i)
                {
                    LuaDLL.lua_dostring(L, "otab = tab");
                    LuaDLL.lua_dostring(L, "tab = {}");
                    LuaDLL.lua_dostring(L, "tab[0] = otab");
                    LuaDLL.lua_newuserdata(L, 4000);
                    LuaDLL.lua_remove(L, -1);
                }
                Console.WriteLine("...");
            }
        }
    }
}
