using System;
using System.Collections.Generic;
using System.Text;

using LuaInterface;

namespace SimpleTest
{
    class Foo
    {
        public double m = 3;

        public double multiply(double x)
        {
            return x * m;
        }
    };

    class Bar
    {
        public double x;
    };

    class Program
    {
        static void Main(string[] args)
        {
            Lua lua = new Lua();
            lua["x"] = 3;
            lua.DoString("y=x");
            Console.WriteLine("y={0}", lua["y"]);

            {
                object[] retVals = lua.DoString("return 1,'hello'");
                Console.WriteLine("{0},{1}", retVals[0], retVals[1]);
            }

            {
                KopiLua.Lua.lua_pushcfunction(lua.luaState, Func);
                KopiLua.Lua.lua_setglobal(lua.luaState, "func");
                Console.WriteLine("registered 'func'");

                double result = (double)lua.DoString("return func(1,2,3)")[0];
                Console.WriteLine("{0}", result);
            }

            {
                Bar bar = new Bar();
                bar.x = 2;
                lua["bar"] = bar;
                Console.WriteLine("'bar' registered");

                object o = lua["bar"];
                Console.WriteLine("'bar' read back as {0}", o);
                Console.WriteLine(o == bar ? "same" : "different");
                Console.WriteLine("LuaInterface says bar.x = {0}", lua["bar.x"]);

                double result = (double)lua.DoString("return bar.x")[0];
                Console.WriteLine("lua says bar.x = {0}", result);

                lua.DoString("bar.x = 4");
                Console.WriteLine("now bar.x = {0}", bar.x);
            }

            {
                Foo foo = new Foo();
                lua.RegisterFunction("multiply", foo, foo.GetType().GetMethod("multiply"));
                Console.WriteLine("registered 'multiply'");

                double result = (double)lua.DoString("return multiply(3)")[0];
                Console.WriteLine("{0}", result);
            }

            Console.ReadLine();
        }

        static int Func(KopiLua.Lua.lua_State L)
        {
            int n = KopiLua.Lua.lua_gettop(L);
            KopiLua.Lua.lua_pushnumber(L, n * 2);
            return 1;
        }
    }
}
