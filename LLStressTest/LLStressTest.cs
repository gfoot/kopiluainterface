using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KopiLuaDll;

namespace LLStressTest
{
    class LLStressTest
    {
        static void Main(string[] args)
        {
            var L = LuaDll.luaL_newstate();
            LuaDll.lua_dostring(L, "tab = {}");
            while (true)
            {
                for (int i = 0; i < 1000; ++i)
                {
                    LuaDll.lua_dostring(L, "otab = tab");
                    LuaDll.lua_dostring(L, "tab = {}");
                    LuaDll.lua_dostring(L, "tab[0] = otab");
                    LuaDll.lua_newuserdata(L, 4000);
                    LuaDll.lua_remove(L, -1);
                }
                Console.WriteLine("...");
            }
        }
    }
}
