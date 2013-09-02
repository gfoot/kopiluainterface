using KopiLua;

namespace KopiLuaDll
{
	/*
	 * Lua types for the API, returned by lua_type function
	 */
	public enum LuaTypes 
	{
		LUA_TNONE=-1,
		LUA_TNIL=0,
		LUA_TNUMBER=3,
		LUA_TSTRING=4,
		LUA_TBOOLEAN=1,
		LUA_TTABLE=5,
		LUA_TFUNCTION=6,
		LUA_TUSERDATA=7,
		LUA_TLIGHTUSERDATA=2
	};


	/*
	 * Lua Garbage Collector options (param "what")
	 */
	public enum LuaGCOptions
	{
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7
	};



	/*
	 * Special stack indexes
	 */
	public enum LuaIndexes 
	{
		LUA_REGISTRYINDEX=-10000,
		LUA_ENVIRONINDEX=-10001,	
		LUA_GLOBALSINDEX=-10002
	};


#if false
	/*
	 * Structure used by the chunk reader
	 */
	// [ StructLayout( LayoutKind.Sequential )]
	public ref struct ReaderInfo
	{
		public string chunkData;
		public bool finished;
	};


	/*
	 * Delegate for chunk readers used with lua_load
	 */
	public delegate string LuaChunkReader(Lua.lua_State luaState, ReaderInfo ^data, uint size);
#endif


	public class LuaDll 
	{
        // Not sure of the purpose of this, but I'm keeping it -kevinh
        static object tag = 0;

		// steffenj: BEGIN additional Lua API functions new in Lua 5.1

		public static int lua_gc(Lua.lua_State luaState, LuaGCOptions what, int data)
		{
			return Lua.lua_gc(luaState, (int) what, data);
		}

		public static string lua_typename(Lua.lua_State luaState, LuaTypes type)
		{
			return Lua.lua_typename(luaState, (int) type).ToString();
		}

		public static string luaL_typename(Lua.lua_State luaState, int stackPos)
		{
			return lua_typename(luaState, lua_type(luaState, stackPos));
		}

		public static void luaL_error(Lua.lua_State luaState, string message)
		{
			Lua.luaL_error(luaState, message);
		}

        public static void luaL_where(Lua.lua_State luaState, int level)
		{
			Lua.luaL_where(luaState, level);
		}


		// Not yet wrapped
		// static string luaL_gsub(Lua.lua_State luaState, string str, string pattern, string replacement);

#if false
		// the functions below are still untested
		static void lua_getfenv(Lua.lua_State luaState, int stackPos);
		static int lua_isfunction(Lua.lua_State luaState, int stackPos);
		static int lua_islightuserdata(Lua.lua_State luaState, int stackPos);
		static int lua_istable(Lua.lua_State luaState, int stackPos);
		static int lua_isuserdata(Lua.lua_State luaState, int stackPos);
		static int lua_lessthan(Lua.lua_State luaState, int stackPos1, int stackPos2);
		static int lua_rawequal(Lua.lua_State luaState, int stackPos1, int stackPos2);
		static int lua_setfenv(Lua.lua_State luaState, int stackPos);
		static void lua_setfield(Lua.lua_State luaState, int stackPos, string name);
		static int luaL_callmeta(Lua.lua_State luaState, int stackPos, string name);
		// steffenj: END additional Lua API functions new in Lua 5.1
#endif

		public static Lua.lua_State luaL_newstate()
		{
			return Lua.luaL_newstate();
		}

		// Not yet wrapped
		// static void lua_close(Lua.lua_State luaState);

		public static void luaL_openlibs(Lua.lua_State luaState)
		{
			Lua.luaL_openlibs(luaState);
		}

		// Not yet wrapped
		// static int lua_objlen(Lua.lua_State luaState, int stackPos);

		public static int luaL_loadstring(Lua.lua_State luaState, string chunk)
		{
			return Lua.luaL_loadstring(luaState, chunk);
		}

		public static int luaL_dostring(Lua.lua_State luaState, string chunk)
		{
			int result = luaL_loadstring(luaState, chunk);
			if (result != 0)
				return result;

			return lua_pcall(luaState, 0, -1, 0);
		}

		/// <summary>DEPRECATED - use luaL_dostring(Lua.lua_State luaState, string chunk) instead!</summary>
		public static int lua_dostring(Lua.lua_State luaState, string chunk)
		{
			return luaL_dostring(luaState, chunk);
		}

		public static void lua_createtable(Lua.lua_State luaState, int narr, int nrec)
		{
			Lua.lua_createtable(luaState, narr, nrec);
		}

		public static void lua_newtable(Lua.lua_State luaState)
		{
			lua_createtable(luaState, 0, 0);
		}

		public static int luaL_dofile(Lua.lua_State luaState, string fileName)
		{
			int result = Lua.luaL_loadfile(luaState, fileName);
			if (result != 0)
				return result;

			return Lua.lua_pcall(luaState, 0, -1, 0);
		}

		public static void lua_getglobal(Lua.lua_State luaState, string name) 
		{
			lua_pushstring(luaState, name);
			Lua.lua_gettable(luaState, (int) LuaIndexes.LUA_GLOBALSINDEX);
		}

		public static void lua_setglobal(Lua.lua_State luaState, string name)
		{
			lua_pushstring(luaState,name);
			lua_insert(luaState,-2);
			lua_settable(luaState, (int) LuaIndexes.LUA_GLOBALSINDEX);
		}

		public static void lua_settop(Lua.lua_State luaState, int newTop)
		{
			Lua.lua_settop(luaState, newTop);
		}

		public static void lua_pop(Lua.lua_State luaState, int amount)
		{
			lua_settop(luaState, -(amount) - 1);
		}

		public static void lua_insert(Lua.lua_State luaState, int newTop)
		{
			Lua.lua_insert(luaState, newTop);
		}

		public static void lua_remove(Lua.lua_State luaState, int index)
		{
			Lua.lua_remove(luaState, index);
		}

		public static void lua_gettable(Lua.lua_State luaState, int index)
		{
			Lua.lua_gettable(luaState, index);
		}


		public static void lua_rawget(Lua.lua_State luaState, int index)
		{
			Lua.lua_rawget(luaState, index);
		}


		public static void lua_settable(Lua.lua_State luaState, int index)
		{
			Lua.lua_settable(luaState, index);
		}


		public static void lua_rawset(Lua.lua_State luaState, int index)
		{
			Lua.lua_rawset(luaState, index);
		}


		public static void lua_setmetatable(Lua.lua_State luaState, int objIndex)
		{
			Lua.lua_setmetatable(luaState, objIndex);
		}


		public static int lua_getmetatable(Lua.lua_State luaState, int objIndex)
		{
			return Lua.lua_getmetatable(luaState, objIndex);
		}


		public static int lua_equal(Lua.lua_State luaState, int index1, int index2)
		{
			return Lua.lua_equal(luaState, index1, index2);
		}


		public static void lua_pushvalue(Lua.lua_State luaState, int index)
		{
			Lua.lua_pushvalue(luaState, index);
		}


		public static void lua_replace(Lua.lua_State luaState, int index)
		{
			Lua.lua_replace(luaState, index);
		}

		public static int lua_gettop(Lua.lua_State luaState)
		{
			return Lua.lua_gettop(luaState);
		}


		public static LuaTypes lua_type(Lua.lua_State luaState, int index)
		{
			return (LuaTypes) Lua.lua_type(luaState, index);
		}

		public static bool lua_isnil(Lua.lua_State luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes.LUA_TNIL;
		}

		public static bool lua_isnumber(Lua.lua_State luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes.LUA_TNUMBER;
		}

		public static bool lua_isboolean(Lua.lua_State luaState, int index) 
		{
			return lua_type(luaState,index)==LuaTypes.LUA_TBOOLEAN;
		}

		public static int luaL_ref(Lua.lua_State luaState, int registryIndex)
		{
			return Lua.luaL_ref(luaState, registryIndex);
		}

		public static int lua_ref(Lua.lua_State luaState, int lockRef)
		{
			if(lockRef!=0) 
			{
				return luaL_ref(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
			} 
			else return 0;
		}

		public static void lua_rawgeti(Lua.lua_State luaState, int tableIndex, int index)
		{
			Lua.lua_rawgeti(luaState, tableIndex, index);
		}

		public static void lua_rawseti(Lua.lua_State luaState, int tableIndex, int index)
		{
			Lua.lua_rawseti(luaState, tableIndex, index);
		}


		public static object lua_newuserdata(Lua.lua_State luaState, int size)
		{
			return Lua.lua_newuserdata(luaState, (uint)size);
		}


		public static object lua_touserdata(Lua.lua_State luaState, int index)
		{
			return Lua.lua_touserdata(luaState, index);
		}

		public static void lua_getref(Lua.lua_State luaState, int reference)
		{
			lua_rawgeti(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX,reference);
		}

		// Unwrapped
		// public static void luaL_unref(Lua.lua_State luaState, int registryIndex, int reference);

		public static void lua_unref(Lua.lua_State luaState, int reference) 
		{
			Lua.luaL_unref(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX,reference);
		}

		public static bool lua_isstring(Lua.lua_State luaState, int index)
		{
			return Lua.lua_isstring(luaState, index) != 0;
		}


		public static bool lua_iscfunction(Lua.lua_State luaState, int index)
		{
			return Lua.lua_iscfunction(luaState, index);
		}

		public static void lua_pushnil(Lua.lua_State luaState)
		{
			Lua.lua_pushnil(luaState);
		}



		public static void lua_call(Lua.lua_State luaState, int nArgs, int nResults)
		{
			Lua.lua_call(luaState, nArgs, nResults);
		}

		public static int lua_pcall(Lua.lua_State luaState, int nArgs, int nResults, int errfunc)
		{
			return Lua.lua_pcall(luaState, nArgs, nResults, errfunc);
		}

		// public static int lua_rawcall(Lua.lua_State luaState, int nArgs, int nResults)

		public static Lua.lua_CFunction lua_tocfunction(Lua.lua_State luaState, int index)
		{
			return Lua.lua_tocfunction(luaState, index);
		}

		public static double lua_tonumber(Lua.lua_State luaState, int index)
		{
			return Lua.lua_tonumber(luaState, index);
		}


		public static bool lua_toboolean(Lua.lua_State luaState, int index)
		{
			return Lua.lua_toboolean(luaState, index) != 0;
		}

		// unwrapped
		// was out strLen
		// public static IntPtr lua_tolstring(Lua.lua_State luaState, int index, [Out] int ^ strLen);

		public static string lua_tostring(Lua.lua_State luaState, int index)
		{
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
			LuaTypes t = lua_type(luaState,index);
			
			if(t == LuaTypes.LUA_TNUMBER)
				return string.Format("{0}", lua_tonumber(luaState, index));
			else if(t == LuaTypes.LUA_TSTRING)
			{
				uint strlen;
				return Lua.lua_tolstring(luaState, index, out strlen).ToString();
			}
			else if(t == LuaTypes.LUA_TNIL)
				return null;			// treat lua nulls to as C# nulls
			else
				return "0";	// Because luaV_tostring does this
		}

        public static void lua_atpanic(Lua.lua_State luaState, Lua.lua_CFunction panicf)
		{
			Lua.lua_atpanic(luaState, (Lua.lua_CFunction)panicf);
		}

		public static void lua_pushstdcallcfunction(Lua.lua_State luaState, Lua.lua_CFunction function)
		{
			Lua.lua_pushcfunction(luaState, function);
		}


#if false
		// not yet implemented
        public static void lua_atlock(Lua.lua_State luaState, LuaCSFunction^ lockf)
		{
			IntPtr p = Marshal::GetFunctionPointerForDelegate(lockf);
			Lua.lua_atlock(luaState, (lua_CFunction) p.ToPointer());
		}

        public static void lua_atunlock(Lua.lua_State luaState, LuaCSFunction^ unlockf);
#endif

		public static void lua_pushnumber(Lua.lua_State luaState, double number)
		{
			Lua.lua_pushnumber(luaState, number);
		}

		public static void lua_pushboolean(Lua.lua_State luaState, bool value)
		{
			Lua.lua_pushboolean(luaState, value ? 1 : 0);
		}

#if false
		// Not yet wrapped
		public static void lua_pushlstring(Lua.lua_State luaState, string str, int size)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(str).ToPointer();

			//

			Marshal::FreeHGlobal(IntPtr(cs));
		}
#endif


		public static void lua_pushstring(Lua.lua_State luaState, string str)
		{
			Lua.lua_pushstring(luaState, str);
		}


		public static int luaL_newmetatable(Lua.lua_State luaState, string meta)
		{
			return Lua.luaL_newmetatable(luaState, meta);
		}


		public static void lua_getfield(Lua.lua_State luaState, int stackPos, string meta)
		{
			Lua.lua_getfield(luaState, stackPos, meta);
		}

		public static void luaL_getmetatable(Lua.lua_State luaState, string meta)
		{
			lua_getfield(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX, meta);
		}

		public static object luaL_checkudata(Lua.lua_State luaState, int stackPos, string meta)
		{
			return Lua.luaL_checkudata(luaState, stackPos, meta);
		}

		public static bool luaL_getmetafield(Lua.lua_State luaState, int stackPos, string field)
		{
			return Lua.luaL_getmetafield(luaState, stackPos, field) != 0;
		}

		// wrapper not yet implemented
		// public static int lua_load(Lua.lua_State luaState, LuaChunkReader chunkReader, ref ReaderInfo data, string chunkName);

		public static int luaL_loadbuffer(Lua.lua_State luaState, string buff, string name)
		{
			return Lua.luaL_loadbuffer(luaState, buff, (uint)buff.Length, name);
		}

		public static int luaL_loadfile(Lua.lua_State luaState, string filename)
		{
			return Lua.luaL_loadfile(luaState, filename);
		}

		public static void lua_error(Lua.lua_State luaState)
		{
			Lua.lua_error(luaState);
		}


		public static bool lua_checkstack(Lua.lua_State luaState,int extra)
		{
			return Lua.lua_checkstack(luaState, extra) != 0;
		}


		public static int lua_next(Lua.lua_State luaState,int index)
		{
			return Lua.lua_next(luaState, index);
		}




		public static void lua_pushlightuserdata(Lua.lua_State luaState, object udata)
		{
			Lua.lua_pushlightuserdata(luaState, udata);
		}

		public static int luanet_rawnetobj(Lua.lua_State luaState,int obj)
		{
            byte[] bytes = lua_touserdata(luaState, obj) as byte[];
            return fourBytesToInt(bytes);
		}

        // Starting with 5.1 the auxlib version of checkudata throws an exception if the type isn't right
		// Instead, we want to run our own version that checks the type and just returns null for failure
		private static object checkudata_raw(Lua.lua_State L, int ud, string tname)
		{
			object p = Lua.lua_touserdata(L, ud);

			if (p != null) 
			{  /* value is a userdata? */
				if (Lua.lua_getmetatable(L, ud) != 0) 
				{ 
					bool isEqual;

					/* does it have a metatable? */
					Lua.lua_getfield(L, (int) LuaIndexes.LUA_REGISTRYINDEX, tname);  /* get correct metatable */

					isEqual = Lua.lua_rawequal(L, -1, -2) != 0;

					// NASTY - we need our own version of the lua_pop macro
					// lua_pop(L, 2);  /* remove both metatables */
					Lua.lua_settop(L, -(2) - 1);

					if (isEqual)   /* does it have the correct mt? */
						return p;
				}
			}
		  
	        return null;
		}


        public static int luanet_checkudata(Lua.lua_State luaState, int ud, string tname)
		{
			object udata=checkudata_raw(luaState, ud, tname);

            if (udata != null) return fourBytesToInt(udata as byte[]);
		    return -1;
		}


		public static bool luaL_checkmetatable(Lua.lua_State luaState,int index)
		{
			bool retVal=false;

			if(lua_getmetatable(luaState,index)!=0) 
			{
				lua_pushlightuserdata(luaState, tag);
				lua_rawget(luaState, -2);
				retVal = !lua_isnil(luaState, -1);
				lua_settop(luaState, -3);
			}
			return retVal;
		}

		public static object luanet_gettag() 
		{
			return tag;
		}

		public static void luanet_newudata(Lua.lua_State luaState,int val)
		{
			byte[] userdata = lua_newuserdata(luaState, sizeof(int)) as byte[];
            intToFourBytes(val, userdata);
		}

		public static int luanet_tonetobject(Lua.lua_State luaState,int index)
		{
			byte[] udata;

			if(lua_type(luaState,index)==LuaTypes.LUA_TUSERDATA) 
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
				    udata=lua_touserdata(luaState,index) as byte[];
				    if(udata!=null)
                    {
				    	return fourBytesToInt(udata);
                    }
				}

			    udata=checkudata_raw(luaState,index, "luaNet_class") as byte[];
			    if(udata!=null) return fourBytesToInt(udata);

			    udata=checkudata_raw(luaState,index, "luaNet_searchbase") as byte[];
			    if(udata!=null) return fourBytesToInt(udata);

			    udata=checkudata_raw(luaState,index, "luaNet_function") as byte[];
			    if(udata!=null) return fourBytesToInt(udata);
			}
			return -1;
		}

        private static int fourBytesToInt(byte[] bytes)
        {
            return bytes[0] + (bytes[1] << 8) + (bytes[2] << 16) + (bytes[3] << 24);
        }

        private static void intToFourBytes(int val, byte[] bytes)
        {
			// gfoot: is this really a good idea?
            bytes[0] = (byte)val;
            bytes[1] = (byte)(val >> 8);
            bytes[2] = (byte)(val >> 16);
            bytes[3] = (byte)(val >> 24);
        }
	};
}
