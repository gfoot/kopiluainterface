using System;
using System.Collections.Generic;
using System.Reflection;
using KopiLuaDll;

namespace LuaInterface
{
	/*
	 * Type checking and conversion functions.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class CheckType
	{
		private ObjectTranslator translator;

		ExtractValue extractNetObject;
		Dictionary<long, ExtractValue> extractValues = new Dictionary<long, ExtractValue>();
		
		public CheckType(ObjectTranslator translator) 
		{
            this.translator = translator;

            extractValues.Add(typeof(object).TypeHandle.Value.ToInt64(), new ExtractValue(getAsObject));
            extractValues.Add(typeof(sbyte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsSbyte));
            extractValues.Add(typeof(byte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsByte));
            extractValues.Add(typeof(short).TypeHandle.Value.ToInt64(), new ExtractValue(getAsShort));
            extractValues.Add(typeof(ushort).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUshort));
            extractValues.Add(typeof(int).TypeHandle.Value.ToInt64(), new ExtractValue(getAsInt));
            extractValues.Add(typeof(uint).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUint));
            extractValues.Add(typeof(long).TypeHandle.Value.ToInt64(), new ExtractValue(getAsLong));
            extractValues.Add(typeof(ulong).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUlong));
            extractValues.Add(typeof(double).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDouble));
            extractValues.Add(typeof(char).TypeHandle.Value.ToInt64(), new ExtractValue(getAsChar));
            extractValues.Add(typeof(float).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFloat));
            extractValues.Add(typeof(decimal).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDecimal));
            extractValues.Add(typeof(bool).TypeHandle.Value.ToInt64(), new ExtractValue(getAsBoolean));
            extractValues.Add(typeof(string).TypeHandle.Value.ToInt64(), new ExtractValue(getAsString));
            extractValues.Add(typeof(LuaFunction).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFunction));
            extractValues.Add(typeof(LuaTable).TypeHandle.Value.ToInt64(), new ExtractValue(getAsTable));
            extractValues.Add(typeof(LuaUserData).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUserdata));

            extractNetObject = new ExtractValue(getAsNetObject);		
		}

		/*
		 * Checks if the value at Lua stack index stackPos matches paramType,
		 * returning a conversion function if it does and null otherwise.
		 */
        internal ExtractValue getExtractor(IReflect paramType)
        {
            return getExtractor(paramType.UnderlyingSystemType);
        }
		internal ExtractValue getExtractor(Type paramType) 
		{
			if(paramType.IsByRef) paramType=paramType.GetElementType();
			
			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();

            if(extractValues.ContainsKey(runtimeHandleValue))
	            return extractValues[runtimeHandleValue];
            else
				return extractNetObject;
		}

		internal ExtractValue checkType(KopiLua.Lua.lua_State luaState,int stackPos,Type paramType) 
		{
            LuaTypes luatype = LuaDll.lua_type(luaState, stackPos);

			if(paramType.IsByRef) paramType=paramType.GetElementType();

            Type underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                paramType = underlyingType;     // Silently convert nullable types to their non null requics
            }

			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();
			
			if (paramType.Equals(typeof(object)))
				return extractValues[runtimeHandleValue];

            if (LuaDll.lua_isnumber(luaState, stackPos))
				return extractValues[runtimeHandleValue];

            if (paramType == typeof(bool))
            {
                if (LuaDll.lua_isboolean(luaState, stackPos))
					return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(string))
            {
                if (LuaDll.lua_isstring(luaState, stackPos))
					return extractValues[runtimeHandleValue];
                else if (luatype == LuaTypes.LUA_TNIL)
					return extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaTypes.LUA_TTABLE)
					return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaTypes.LUA_TUSERDATA)
					return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaTypes.LUA_TFUNCTION)
					return extractValues[runtimeHandleValue];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaTypes.LUA_TFUNCTION)
            {
                return new ExtractValue(new DelegateGenerator(translator, paramType).extractGenerated);
            }
            else if (paramType.IsInterface && luatype == LuaTypes.LUA_TTABLE)
            {
                return new ExtractValue(new ClassGenerator(translator, paramType).extractGenerated);
            }
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaTypes.LUA_TNIL)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return extractNetObject;
            }
            else if (LuaDll.lua_type(luaState, stackPos) == LuaTypes.LUA_TTABLE)
            {
                if (LuaDll.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    object obj = translator.getNetObject(luaState, -1);
                    LuaDll.lua_settop(luaState, -2);
                    if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
						return extractNetObject;
                }
                else
					return null;
            }
            else
            {
                object obj = translator.getNetObject(luaState, stackPos);
                if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
					return extractNetObject;
            }

            return null;
		}

		/*
		 * The following functions return the value in the Lua stack
		 * index stackPos as the desired type if it can, or null
		 * otherwise.
		 */
		private object getAsSbyte(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			sbyte retVal=(sbyte)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsByte(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			byte retVal=(byte)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsShort(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			short retVal=(short)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUshort(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			ushort retVal=(ushort)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsInt(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			int retVal=(int)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUint(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			uint retVal=(uint)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsLong(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			long retVal=(long)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUlong(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			ulong retVal=(ulong)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsDouble(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			double retVal=LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsChar(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			char retVal=(char)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsFloat(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			float retVal=(float)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsDecimal(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			decimal retVal=(decimal)LuaDll.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaDll.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsBoolean(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			return LuaDll.lua_toboolean(luaState,stackPos);
		}
		private object getAsString(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			string retVal=LuaDll.lua_tostring(luaState,stackPos);
			if(retVal=="" && !LuaDll.lua_isstring(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsTable(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			return translator.getTable(luaState,stackPos);
		}
		private object getAsFunction(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			return translator.getFunction(luaState,stackPos);
		}
		private object getAsUserdata(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			return translator.getUserData(luaState,stackPos);
		}
		public object getAsObject(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			if(LuaDll.lua_type(luaState,stackPos)==LuaTypes.LUA_TTABLE) 
			{
				if(LuaDll.luaL_getmetafield(luaState,stackPos,"__index")) 
				{
					if(LuaDll.luaL_checkmetatable(luaState,-1)) 
					{
						LuaDll.lua_insert(luaState,stackPos);
						LuaDll.lua_remove(luaState,stackPos+1);
					} 
					else 
					{
						LuaDll.lua_settop(luaState,-2);
					}
				}
			}
			object obj=translator.getObject(luaState,stackPos);
			return obj;
		}
		public object getAsNetObject(KopiLua.Lua.lua_State luaState,int stackPos) 
		{
			object obj=translator.getNetObject(luaState,stackPos);
			if(obj==null && LuaDll.lua_type(luaState,stackPos)==LuaTypes.LUA_TTABLE) 
			{
				if(LuaDll.luaL_getmetafield(luaState,stackPos,"__index")) 
				{
					if(LuaDll.luaL_checkmetatable(luaState,-1)) 
					{
						LuaDll.lua_insert(luaState,stackPos);
						LuaDll.lua_remove(luaState,stackPos+1);
						obj=translator.getNetObject(luaState,stackPos);
					} 
					else 
					{
						LuaDll.lua_settop(luaState,-2);
					}
				}
			}
			return obj;
		}
	}
}
