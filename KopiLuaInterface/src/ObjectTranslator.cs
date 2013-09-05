using KopiLuaDll;

namespace LuaInterface 
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;

    /*
	 * Passes objects from the CLR to Lua and vice-versa
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class ObjectTranslator 
	{
		internal CheckType typeChecker;

        // object # to object (FIXME - it should be possible to get object address as an object #)
        public readonly Dictionary<int, object> objects = new Dictionary<int, object>();
        // object to object #
        public readonly Dictionary<object, int> objectsBackMap = new Dictionary<object, int>();
        internal Lua interpreter;
		private MetaFunctions metaFunctions;
		private List<Assembly> assemblies;
		private KopiLua.Lua.lua_CFunction registerTableFunction,unregisterTableFunction,getMethodSigFunction,
			getConstructorSigFunction,importTypeFunction,loadAssemblyFunction;

        internal EventHandlerContainer pendingEvents = new EventHandlerContainer();

		public ObjectTranslator(Lua interpreter,KopiLua.Lua.lua_State luaState) 
		{
			this.interpreter=interpreter;
			typeChecker=new CheckType(this);
			metaFunctions=new MetaFunctions(this);
			assemblies=new List<Assembly>();

			importTypeFunction=new KopiLua.Lua.lua_CFunction(this.importType);
			loadAssemblyFunction=new KopiLua.Lua.lua_CFunction(this.loadAssembly);
			registerTableFunction=new KopiLua.Lua.lua_CFunction(this.registerTable);
			unregisterTableFunction=new KopiLua.Lua.lua_CFunction(this.unregisterTable);
			getMethodSigFunction=new KopiLua.Lua.lua_CFunction(this.getMethodSignature);
			getConstructorSigFunction=new KopiLua.Lua.lua_CFunction(this.getConstructorSignature);

			createLuaObjectList(luaState);
			createIndexingMetaFunction(luaState);
			createBaseClassMetatable(luaState);
			createClassMetatable(luaState);
			createFunctionMetatable(luaState);
			setGlobalFunctions(luaState);
		}

		/*
		 * Sets up the list of objects in the Lua side
		 */
		private void createLuaObjectList(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.lua_pushstring(luaState,"luaNet_objects");
			LuaDll.lua_newtable(luaState);
			LuaDll.lua_newtable(luaState);
			LuaDll.lua_pushstring(luaState,"__mode");
			LuaDll.lua_pushstring(luaState,"v");
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_setmetatable(luaState,-2);
			LuaDll.lua_settable(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
		}
		/*
		 * Registers the indexing function of CLR objects
		 * passed to Lua
		 */
		private void createIndexingMetaFunction(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.lua_pushstring(luaState,"luaNet_indexfunction");
			LuaDll.luaL_dostring(luaState,MetaFunctions.luaIndexFunction);	// steffenj: lua_dostring renamed to luaL_dostring
			//LuaDLL.lua_pushstdcallcfunction(luaState,indexFunction);
            LuaDll.lua_rawset(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
		}
		/*
		 * Creates the metatable for superclasses (the base
		 * field of registered tables)
		 */
		private void createBaseClassMetatable(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.luaL_newmetatable(luaState,"luaNet_searchbase");
			LuaDll.lua_pushstring(luaState,"__gc");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__tostring");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__index");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.baseIndexFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__newindex");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_settop(luaState,-2);
		}
		/*
		 * Creates the metatable for type references
		 */
		private void createClassMetatable(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.luaL_newmetatable(luaState,"luaNet_class");
			LuaDll.lua_pushstring(luaState,"__gc");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__tostring");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__index");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.classIndexFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__newindex");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.classNewindexFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__call");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.callConstructorFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_settop(luaState,-2);
		}
		/*
		 * Registers the global functions used by LuaInterface
		 */
		private void setGlobalFunctions(KopiLua.Lua.lua_State luaState)
		{
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.indexFunction);
			LuaDll.lua_setglobal(luaState,"get_object_member");
			LuaDll.lua_pushstdcallcfunction(luaState,importTypeFunction);
			LuaDll.lua_setglobal(luaState,"import_type");
			LuaDll.lua_pushstdcallcfunction(luaState,loadAssemblyFunction);
			LuaDll.lua_setglobal(luaState,"load_assembly");
			LuaDll.lua_pushstdcallcfunction(luaState,registerTableFunction);
			LuaDll.lua_setglobal(luaState,"make_object");
			LuaDll.lua_pushstdcallcfunction(luaState,unregisterTableFunction);
			LuaDll.lua_setglobal(luaState,"free_object");
			LuaDll.lua_pushstdcallcfunction(luaState,getMethodSigFunction);
			LuaDll.lua_setglobal(luaState,"get_method_bysig");
			LuaDll.lua_pushstdcallcfunction(luaState,getConstructorSigFunction);
			LuaDll.lua_setglobal(luaState,"get_constructor_bysig");
		}
		/*
		 * Creates the metatable for delegates
		 */
		private void createFunctionMetatable(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.luaL_newmetatable(luaState,"luaNet_function");
			LuaDll.lua_pushstring(luaState,"__gc");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_pushstring(luaState,"__call");
			LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.execDelegateFunction);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_settop(luaState,-2);
		}
		/*
		 * Passes errors (argument e) to the Lua interpreter
		 */
		internal void throwError(KopiLua.Lua.lua_State luaState,object e) 
		{
            // If the argument is a mere string, we are free to add extra info to it (as opposed to some private C# exception object or somesuch, which we just pass up)
            if (e is string)
            {
                // We use this to remove anything pushed by luaL_where
                int oldTop = LuaDll.lua_gettop(luaState);

                // Stack frame #1 is our C# wrapper, so not very interesting to the user
                // Stack frame #2 must be the lua code that called us, so that's what we want to use
                LuaDll.luaL_where(luaState, 2);
                object[] curlev = popValues(luaState, oldTop);
                // Debug.WriteLine(curlev);

                if (curlev.Length > 0)
                    e = curlev[0].ToString() + e;
            }

			push(luaState,e);
			LuaDll.lua_error(luaState);
		}
		/*
		 * Implementation of load_assembly. Throws an error
		 * if the assembly is not found.
		 */
		private int loadAssembly(KopiLua.Lua.lua_State luaState) 
		{
			string assemblyName=LuaDll.lua_tostring(luaState,1);
			try 
			{
                if (assemblyName == "System.Windows.Forms")
                    assemblyName = "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                if (assemblyName == "System.Drawing")
                    assemblyName = "System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                if (assemblyName == "UnityEngine")
                    assemblyName = "UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

                Assembly assembly = null;

                try
                {
                    assembly = Assembly.Load(assemblyName);

                    // If we couldn't find it based on a name, see if we can use it as a filename and find it
                    if (assembly == null)
                        assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));
                }
                catch (Exception)
                {
                    // ignore - it might not even be a filename
                }

				if(assembly!=null && !assemblies.Contains(assembly))
					assemblies.Add(assembly);
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
			}
			return 0;
		}
        
        internal Type FindType(string className)
        {
            if (className.StartsWith("out ") || className.StartsWith("ref "))
            {
                var type = FindType(className.Substring(4));
                if (type != null)
                    return type.MakeByRefType();
                return null;
            }

			foreach(Assembly assembly in assemblies) 
			{
				Type klass=assembly.GetType(className);
				if(klass!=null) 
				{
					return klass;
				}
			}
            return null;
        }
            
		/*
		 * Implementation of import_type. Returns nil if the
		 * type is not found.
		 */
		private int importType(KopiLua.Lua.lua_State luaState) 
		{
			string className=LuaDll.lua_tostring(luaState,1);
            Type klass=FindType(className);
            if(klass!=null)
				pushType(luaState,klass);
            else
    			LuaDll.lua_pushnil(luaState);
            return 1;
        }
		/*
		 * Implementation of make_object. Registers a table (first
		 * argument in the stack) as an object subclassing the
		 * type passed as second argument in the stack.
		 */
		private int registerTable(KopiLua.Lua.lua_State luaState) 
		{
			if(LuaDll.lua_type(luaState,1)==LuaTypes.LUA_TTABLE) 
			{
				LuaTable luaTable=getTable(luaState,1);
                string superclassName = LuaDll.lua_tostring(luaState, 2);
                if (superclassName != null)
                {
                    Type klass = FindType(superclassName);
                    if (klass != null)
                    {
                        // Creates and pushes the object in the stack, setting
                        // it as the  metatable of the first argument
                        object obj = CodeGeneration.Instance.GetClassInstance(klass, luaTable);
                        pushObject(luaState, obj, "luaNet_metatable");
                        LuaDll.lua_newtable(luaState);
                        LuaDll.lua_pushstring(luaState, "__index");
                        LuaDll.lua_pushvalue(luaState, -3);
                        LuaDll.lua_settable(luaState, -3);
                        LuaDll.lua_pushstring(luaState, "__newindex");
                        LuaDll.lua_pushvalue(luaState, -3);
                        LuaDll.lua_settable(luaState, -3);
                        LuaDll.lua_setmetatable(luaState, 1);
                        // Pushes the object again, this time as the base field
                        // of the table and with the luaNet_searchbase metatable
                        LuaDll.lua_pushstring(luaState, "base");
                        int index = addObject(obj);
                        pushNewObject(luaState, obj, index, "luaNet_searchbase");
                        LuaDll.lua_rawset(luaState, 1);
                    }
                    else
                        throwError(luaState, "register_table: can not find superclass '" + superclassName + "'");
                }
                else
                    throwError(luaState, "register_table: superclass name can not be null");
			} 
			else throwError(luaState,"register_table: first arg is not a table");
			return 0;
		}
		/*
		 * Implementation of free_object. Clears the metatable and the
		 * base field, freeing the created object for garbage-collection
		 */
		private int unregisterTable(KopiLua.Lua.lua_State luaState) 
		{
			try 
			{
				if(LuaDll.lua_getmetatable(luaState,1)!=0) 
				{
					LuaDll.lua_pushstring(luaState,"__index");
					LuaDll.lua_gettable(luaState,-2);
					object obj=getRawNetObject(luaState,-1);
					if(obj==null) throwError(luaState,"unregister_table: arg is not valid table");
					FieldInfo luaTableField=obj.GetType().GetField("__luaInterface_luaTable");
					if(luaTableField==null) throwError(luaState,"unregister_table: arg is not valid table");
					luaTableField.SetValue(obj,null);
					LuaDll.lua_pushnil(luaState);
					LuaDll.lua_setmetatable(luaState,1);
					LuaDll.lua_pushstring(luaState,"base");
					LuaDll.lua_pushnil(luaState);
					LuaDll.lua_settable(luaState,1);
				} 
				else throwError(luaState,"unregister_table: arg is not valid table");
			} 
			catch(Exception e) 
			{
				throwError(luaState,e.Message);
			}
			return 0;
		}
		/*
		 * Implementation of get_method_bysig. Returns nil
		 * if no matching method is not found.
		 */
		private int getMethodSignature(KopiLua.Lua.lua_State luaState) 
		{
			IReflect klass; object target;
			int udata=LuaDll.luanet_checkudata(luaState,1,"luaNet_class");
			if(udata!=-1) 
			{
				klass=(IReflect)objects[udata];
				target=null;
			}
			else 
			{
				target=getRawNetObject(luaState,1);
				if(target==null) 
				{
					throwError(luaState,"get_method_bysig: first arg is not type or object reference");
					LuaDll.lua_pushnil(luaState);
					return 1;
				}
				klass=target.GetType();
			}
			string methodName=LuaDll.lua_tostring(luaState,2);
			Type[] signature=new Type[LuaDll.lua_gettop(luaState)-2];
            for (int i = 0; i < signature.Length; i++)
            {
                string typeName = LuaDll.lua_tostring(luaState, i + 3);
                signature[i] = FindType(typeName);
                if (signature[i] == null)
                    throwError(luaState, string.Format("Type not found: {0}", typeName));
            }
		    try 
			{
				MethodInfo method=klass.GetMethod(methodName,BindingFlags.Public | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy,null,signature,null);
				pushFunction(luaState,new KopiLua.Lua.lua_CFunction((new LuaMethodWrapper(this,target,klass,method)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaDll.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Implementation of get_constructor_bysig. Returns nil
		 * if no matching constructor is found.
		 */
		private int getConstructorSignature(KopiLua.Lua.lua_State luaState) 
		{
			IReflect klass=null;
			int udata=LuaDll.luanet_checkudata(luaState,1,"luaNet_class");
			if(udata!=-1) 
			{
				klass=(IReflect)objects[udata];
			}
			if(klass==null) 
			{
				throwError(luaState,"get_constructor_bysig: first arg is invalid type reference");
			}
			Type[] signature=new Type[LuaDll.lua_gettop(luaState)-1];
			for(int i=0;i<signature.Length;i++)
				signature[i]=FindType(LuaDll.lua_tostring(luaState,i+2));
			try 
			{
				ConstructorInfo constructor=klass.UnderlyingSystemType.GetConstructor(signature);
				pushFunction(luaState,new KopiLua.Lua.lua_CFunction((new LuaMethodWrapper(this,null,klass,constructor)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaDll.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Pushes a type reference into the stack
		 */
		internal void pushType(KopiLua.Lua.lua_State luaState, Type t) 
		{
			pushObject(luaState,new ProxyType(t),"luaNet_class");
		}
		/*
		 * Pushes a delegate into the stack
		 */
		internal void pushFunction(KopiLua.Lua.lua_State luaState, KopiLua.Lua.lua_CFunction func) 
		{
			pushObject(luaState,func,"luaNet_function");
		}
		/*
		 * Pushes a CLR object into the Lua stack as an userdata
		 * with the provided metatable
		 */
		internal void pushObject(KopiLua.Lua.lua_State luaState, object o, string metatable) 
		{
			int index = -1;
			// Pushes nil
			if(o==null) 
			{
				LuaDll.lua_pushnil(luaState);
				return;
			}

			// Object already in the list of Lua objects? Push the stored reference.
            bool found = objectsBackMap.TryGetValue(o, out index);
			if(found) 
			{
				LuaDll.luaL_getmetatable(luaState,"luaNet_objects");
				LuaDll.lua_rawgeti(luaState,-1,index);

                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep 
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
                // object here
                // did we find a non nil object in our table? if not, we need to call collect object
                LuaTypes type = LuaDll.lua_type(luaState, -1);
                if (type != LuaTypes.LUA_TNIL)
                {
                    LuaDll.lua_remove(luaState, -2);     // drop the metatable - we're going to leave our object on the stack

                    return;
                }

                // MetaFunctions.dumpStack(this, luaState);
                LuaDll.lua_remove(luaState, -1);    // remove the nil object value
                LuaDll.lua_remove(luaState, -1);    // remove the metatable

                collectObject(o, index);            // Remove from both our tables and fall out to get a new ID
			}
            index = addObject(o);

			pushNewObject(luaState,o,index,metatable);
		}


		/*
		 * Pushes a new object into the Lua stack with the provided
		 * metatable
		 */
		private void pushNewObject(KopiLua.Lua.lua_State luaState,object o,int index,string metatable) 
		{
			if(metatable=="luaNet_metatable") 
			{
				// Gets or creates the metatable for the object's type
				LuaDll.luaL_getmetatable(luaState,o.GetType().AssemblyQualifiedName);

				if(LuaDll.lua_isnil(luaState,-1))
				{
					LuaDll.lua_settop(luaState,-2);
					LuaDll.luaL_newmetatable(luaState,o.GetType().AssemblyQualifiedName);
					LuaDll.lua_pushstring(luaState,"cache");
					LuaDll.lua_newtable(luaState);
					LuaDll.lua_rawset(luaState,-3);
					LuaDll.lua_pushlightuserdata(luaState,LuaDll.luanet_gettag());
					LuaDll.lua_pushnumber(luaState,1);
					LuaDll.lua_rawset(luaState,-3);
					LuaDll.lua_pushstring(luaState,"__index");
					LuaDll.lua_pushstring(luaState,"luaNet_indexfunction");
					LuaDll.lua_rawget(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
					LuaDll.lua_rawset(luaState,-3);
					LuaDll.lua_pushstring(luaState,"__gc");
					LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
					LuaDll.lua_rawset(luaState,-3);
					LuaDll.lua_pushstring(luaState,"__tostring");
					LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
					LuaDll.lua_rawset(luaState,-3);
					LuaDll.lua_pushstring(luaState,"__newindex");
					LuaDll.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
					LuaDll.lua_rawset(luaState,-3);
				}
			}
			else
			{
				LuaDll.luaL_getmetatable(luaState,metatable);
			}

			// Stores the object index in the Lua list and pushes the
			// index into the Lua stack
			LuaDll.luaL_getmetatable(luaState,"luaNet_objects");
			LuaDll.luanet_newudata(luaState,index);
			LuaDll.lua_pushvalue(luaState,-3);
			LuaDll.lua_remove(luaState,-4);
			LuaDll.lua_setmetatable(luaState,-2);
			LuaDll.lua_pushvalue(luaState,-1);
			LuaDll.lua_rawseti(luaState,-3,index);
			LuaDll.lua_remove(luaState,-2);
		}
		/*
		 * Gets an object from the Lua stack with the desired type, if it matches, otherwise
		 * returns null.
		 */
		internal object getAsType(KopiLua.Lua.lua_State luaState,int stackPos,Type paramType) 
		{
			ExtractValue extractor=typeChecker.checkType(luaState,stackPos,paramType);
			if(extractor!=null) return extractor(luaState,stackPos);
			return null;
		}


        /// <summary>
        /// Given the Lua int ID for an object remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
		internal void collectObject(int udata) 
		{
			object o;
            bool found = objects.TryGetValue(udata, out o);

            // The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
            if (found)
            {
                // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

                objects.Remove(udata);
                objectsBackMap.Remove(o);
            }
		}


        /// <summary>
        /// Given an object reference, remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
        void collectObject(object o, int udata)
        {
            // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

            objects.Remove(udata);
            objectsBackMap.Remove(o);
        }


        /// <summary>
        /// We want to ensure that objects always have a unique ID
        /// </summary>
        int nextObj = 0;

        int addObject(object obj)
        {
            // New object: inserts it in the list
            int index = nextObj++;

            // Debug.WriteLine("Adding " + obj.ToString() + " @ " + index);

            objects[index] = obj;
            objectsBackMap[obj] = index;

            return index;
        }



		/*
		 * Gets an object from the Lua stack according to its Lua type.
		 */
		internal object getObject(KopiLua.Lua.lua_State luaState,int index) 
		{
			LuaTypes type=LuaDll.lua_type(luaState,index);
			switch(type) 
			{
				case LuaTypes.LUA_TNUMBER:
				{
					return LuaDll.lua_tonumber(luaState,index);
				} 
				case LuaTypes.LUA_TSTRING: 
				{
					return LuaDll.lua_tostring(luaState,index);
				} 
				case LuaTypes.LUA_TBOOLEAN:
				{
					return LuaDll.lua_toboolean(luaState,index);
				} 
				case LuaTypes.LUA_TTABLE: 
				{
					return getTable(luaState,index);
				} 
				case LuaTypes.LUA_TFUNCTION:
				{
					return getFunction(luaState,index);
				} 
				case LuaTypes.LUA_TUSERDATA:
				{
					int udata=LuaDll.luanet_tonetobject(luaState,index);
					if(udata!=-1)
						return objects[udata];
					else
						return getUserData(luaState,index);
				}
				default:
					return null;
			}
		}
		/*
		 * Gets the table in the index positon of the Lua stack.
		 */
		internal LuaTable getTable(KopiLua.Lua.lua_State luaState,int index) 
		{
			LuaDll.lua_pushvalue(luaState,index);
			return new LuaTable(LuaDll.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the userdata in the index positon of the Lua stack.
		 */
		internal LuaUserData getUserData(KopiLua.Lua.lua_State luaState,int index) 
		{
			LuaDll.lua_pushvalue(luaState,index);
			return new LuaUserData(LuaDll.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the function in the index positon of the Lua stack.
		 */
		internal LuaFunction getFunction(KopiLua.Lua.lua_State luaState,int index) 
		{
			LuaDll.lua_pushvalue(luaState,index);
			return new LuaFunction(LuaDll.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as Lua functions.
		 */
		internal object getNetObject(KopiLua.Lua.lua_State luaState,int index) 
		{
			int idx=LuaDll.luanet_tonetobject(luaState,index);
			if(idx!=-1)
				return objects[idx];
			else
				return null;
		}
		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as is.
		 */
		internal object getRawNetObject(KopiLua.Lua.lua_State luaState,int index) 
		{
			int udata=LuaDll.luanet_rawnetobj(luaState,index);
			if(udata!=-1) 
			{
				return objects[udata];
			}
			return null;
		}
		/*
		 * Pushes the entire array into the Lua stack and returns the number
		 * of elements pushed.
		 */
		internal int returnValues(KopiLua.Lua.lua_State luaState, object[] returnValues) 
		{
			if(LuaDll.lua_checkstack(luaState,returnValues.Length+5)) 
			{
				for(int i=0;i<returnValues.Length;i++) 
				{
					push(luaState,returnValues[i]);
				}
				return returnValues.Length;
			} else
				return 0;
		}
		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array.
		 */
		internal object[] popValues(KopiLua.Lua.lua_State luaState,int oldTop) 
		{
			int newTop=LuaDll.lua_gettop(luaState);
			if(oldTop==newTop) 
			{
				return null;
			} 
			else 
			{
				ArrayList returnValues=new ArrayList();
				for(int i=oldTop+1;i<=newTop;i++) 
				{
					returnValues.Add(getObject(luaState,i));
				}
				LuaDll.lua_settop(luaState,oldTop);
				return returnValues.ToArray();
			}
		}
		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array, casting
		 * them to the provided types.
		 */
		internal object[] popValues(KopiLua.Lua.lua_State luaState,int oldTop,Type[] popTypes) 
		{
			int newTop=LuaDll.lua_gettop(luaState);
			if(oldTop==newTop) 
			{
				return null;
			} 
			else 
			{
				int iTypes;
				ArrayList returnValues=new ArrayList();
				if(popTypes[0] == typeof(void))
					iTypes=1;
				else
					iTypes=0;
				for(int i=oldTop+1;i<=newTop;i++) 
				{
					returnValues.Add(getAsType(luaState,i,popTypes[iTypes]));
					iTypes++;
				}
				LuaDll.lua_settop(luaState,oldTop);
				return returnValues.ToArray();
			}
		}

        // kevinh - the following line doesn't work for remoting proxies - they always return a match for 'is'
		// else if(o is ILuaGeneratedType) 
        static bool IsILua(object o)
        {
            if(o is ILuaGeneratedType)
            {
                // Make sure we are _really_ ILuaGenerated
                Type typ = o.GetType();

                return (typ.GetInterface("ILuaGeneratedType") != null);
            }
            else
                return false;
        }

		/*
		 * Pushes the object into the Lua stack according to its type.
		 */
		internal void push(KopiLua.Lua.lua_State luaState, object o) 
		{
			if(o==null) 
			{
				LuaDll.lua_pushnil(luaState);
			}
			else if(o is sbyte || o is byte || o is short || o is ushort ||
				o is int || o is uint || o is long || o is float ||
				o is ulong || o is decimal || o is double) 
			{
				double d=Convert.ToDouble(o);
				LuaDll.lua_pushnumber(luaState,d);
			}
			else if(o is char)
			{
				double d = (char)o;
				LuaDll.lua_pushnumber(luaState,d);
			}
			else if(o is string)
			{
				string str=(string)o;
				LuaDll.lua_pushstring(luaState,str);
			}
			else if(o is bool)
			{
				bool b=(bool)o;
				LuaDll.lua_pushboolean(luaState,b);
			}
			else if(IsILua(o))
			{
				(((ILuaGeneratedType)o).__luaInterface_getLuaTable()).push(luaState);
			}
			else if(o is LuaTable) 
			{
				((LuaTable)o).push(luaState);
			} 
			else if(o is KopiLua.Lua.lua_CFunction) 
			{
				pushFunction(luaState,(KopiLua.Lua.lua_CFunction)o);
			} 
			else if(o is LuaFunction)
			{
				((LuaFunction)o).push(luaState);
			}
			else 
			{
				pushObject(luaState,o,"luaNet_metatable");
			}
		}
		/*
		 * Checks if the method matches the arguments in the Lua stack, getting
		 * the arguments if it does.
		 */
		internal bool matchParameters(KopiLua.Lua.lua_State luaState,MethodBase method,ref MethodCache methodCache) 
		{
			return metaFunctions.matchParameters(luaState,method,ref methodCache);
		}
	}
}