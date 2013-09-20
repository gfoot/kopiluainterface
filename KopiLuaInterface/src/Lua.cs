
using System.Collections.Generic;
using KopiLuaDll;

namespace LuaInterface 
{

	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Reflection;
    using System.Threading;

    /*
	 * Main class of LuaInterface
	 * Object-oriented wrapper to Lua API
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 * 
	 * // steffenj: important changes in Lua class:
	 * - removed all Open*Lib() functions 
	 * - all libs automatically open in the Lua class constructor (just assign nil to unwanted libs)
	 * */
	public class Lua : IDisposable
	{

		static string init_luanet =
			"local metatable = {}									\n"+
			"local import_type = luanet.import_type							\n"+
			"local load_assembly = luanet.load_assembly						\n"+
			"											\n"+
			"-- Lookup a .NET identifier component.							\n"+
			"function metatable:__index(key) -- key is e.g. \"Form\"				\n"+
			"    -- Get the fully-qualified name, e.g. \"System.Windows.Forms.Form\"		\n"+
			"    local fqn = ((rawget(self,\".fqn\") and rawget(self,\".fqn\") ..			\n"+
			"		\".\") or \"\") .. key							\n"+
			"											\n"+
			"    -- Try to find either a luanet function or a CLR type				\n"+
			"    local obj = rawget(luanet,key) or import_type(fqn)					\n"+
			"											\n"+
			"    -- If key is neither a luanet function or a CLR type, then it is simply		\n"+
			"    -- an identifier component.							\n"+
			"    if obj == nil then									\n"+
			"		-- It might be an assembly, so we load it too.				\n"+
			"        load_assembly(fqn)								\n"+
			"        obj = { [\".fqn\"] = fqn }							\n"+
			"        setmetatable(obj, metatable)							\n"+
			"    end										\n"+
			"											\n"+
			"    -- Cache this lookup								\n"+
			"    rawset(self, key, obj)								\n"+
			"    return obj										\n"+
			"end											\n"+
			"											\n"+
			"-- A non-type has been called; e.g. foo = System.Foo()					\n"+
			"function metatable:__call(...)								\n"+
			"    error(\"No such type: \" .. rawget(self,\".fqn\"), 2)				\n"+
			"end											\n"+
			"											\n"+
			"-- This is the root of the .NET namespace						\n"+
			"luanet[\".fqn\"] = false								\n"+
			"setmetatable(luanet, metatable)							\n"+
			"											\n"+
			"-- Preload the mscorlib assembly							\n"+
			"luanet.load_assembly(\"mscorlib\")							\n";

		readonly public KopiLua.Lua.lua_State luaState;
		ObjectTranslator translator;

        KopiLua.Lua.lua_CFunction panicCallback, lockCallback, unlockCallback;

        /// <summary>
        /// Used to ensure multiple .net threads all get serialized by this single lock for access to the lua stack/objects
        /// </summary>
        object luaLock = new object();

		public Lua() 
		{
			luaState = LuaDll.luaL_newstate();	// steffenj: Lua 5.1.1 API change (lua_open is gone)
			//LuaDLL.luaopen_base(luaState);	// steffenj: luaopen_* no longer used
			LuaDll.luaL_openlibs(luaState);		// steffenj: Lua 5.1.1 API change (luaopen_base is gone, just open all libs right here)
		    LuaDll.lua_pushstring(luaState, "LUAINTERFACE LOADED");
            LuaDll.lua_pushboolean(luaState, true);
            LuaDll.lua_settable(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
			LuaDll.lua_newtable(luaState);
			LuaDll.lua_setglobal(luaState, "luanet");
            LuaDll.lua_pushvalue(luaState, (int)LuaIndexes.LUA_GLOBALSINDEX);
			LuaDll.lua_getglobal(luaState, "luanet");
			LuaDll.lua_pushstring(luaState, "getmetatable");
			LuaDll.lua_getglobal(luaState, "getmetatable");
			LuaDll.lua_settable(luaState, -3);
            LuaDll.lua_replace(luaState, (int)LuaIndexes.LUA_GLOBALSINDEX);
			translator=new ObjectTranslator(this,luaState);
            LuaDll.lua_replace(luaState, (int)LuaIndexes.LUA_GLOBALSINDEX);
			LuaDll.luaL_dostring(luaState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring

            // We need to keep this in a managed reference so the delegate doesn't get garbage collected
            panicCallback = new KopiLua.Lua.lua_CFunction(PanicCallback);
            LuaDll.lua_atpanic(luaState, panicCallback);

            // LuaDLL.lua_atlock(luaState, lockCallback = new KopiLua.Lua.lua_CFunction(LockCallback));
            // LuaDLL.lua_atunlock(luaState, unlockCallback = new KopiLua.Lua.lua_CFunction(UnlockCallback));
        }

    	/*
    	 * CAUTION: LuaInterface.Lua instances can't share the same lua state! 
    	 */
    	public Lua(KopiLua.Lua.lua_State lState)
    	{
        		LuaDll.lua_pushstring(lState, "LUAINTERFACE LOADED");
                LuaDll.lua_gettable(lState, (int)LuaIndexes.LUA_REGISTRYINDEX);
        		if(LuaDll.lua_toboolean(lState,-1)) {
            		LuaDll.lua_settop(lState,-2);
            		throw new LuaException("There is already a LuaInterface.Lua instance associated with this Lua state");
        		} else {
            		LuaDll.lua_settop(lState,-2);
            		LuaDll.lua_pushstring(lState, "LUAINTERFACE LOADED");
            		LuaDll.lua_pushboolean(lState, true);
                    LuaDll.lua_settable(lState, (int)LuaIndexes.LUA_REGISTRYINDEX);
            		this.luaState=lState;
                    LuaDll.lua_pushvalue(lState, (int)LuaIndexes.LUA_GLOBALSINDEX);
					LuaDll.lua_getglobal(lState, "luanet");
					LuaDll.lua_pushstring(lState, "getmetatable");
					LuaDll.lua_getglobal(lState, "getmetatable");
					LuaDll.lua_settable(lState, -3);
                    LuaDll.lua_replace(lState, (int)LuaIndexes.LUA_GLOBALSINDEX);
					translator=new ObjectTranslator(this, this.luaState);
                    LuaDll.lua_replace(lState, (int)LuaIndexes.LUA_GLOBALSINDEX);
					LuaDll.luaL_dostring(lState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring
        		}
    	}

        /// <summary>
        /// Called for each lua_lock call 
        /// </summary>
        /// <param name="luaState"></param>
        /// Not yet used
        int LockCallback(KopiLua.Lua.lua_State luaState)
        {
            // Monitor.Enter(luaLock);

            return 0;
        }

        /// <summary>
        /// Called for each lua_unlock call 
        /// </summary>
        /// <param name="luaState"></param>
        /// Not yet used
        int UnlockCallback(KopiLua.Lua.lua_State luaState)
        {
            // Monitor.Exit(luaLock);

            return 0;
        }

        static int PanicCallback(KopiLua.Lua.lua_State luaState)
        {
            // string desc = LuaDLL.lua_tostring(luaState, 1);
            string reason = String.Format("unprotected error in call to Lua API ({0})", LuaDll.lua_tostring(luaState, -1));

           //        lua_tostring(L, -1);

            throw new LuaException(reason);
        }



        /// <summary>
        /// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
        /// </summary>
        void ThrowExceptionFromError(int oldTop)
        {
            object err = translator.getObject(luaState, -1);
            LuaDll.lua_settop(luaState, oldTop);

            // If the 'error' on the stack is an actual C# exception, just rethrow it.  Otherwise the value must have started
            // as a true Lua error and is best interpreted as a string - wrap it in a LuaException and rethrow.
            Exception thrown = err as Exception;

            if (thrown == null)
            {
                if (err == null)
                    err = "Unknown Lua Error";

                thrown = new LuaException(err.ToString());
            }

            throw thrown;
        }



        /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name="e">null for no pending exception</param>
        internal int SetPendingException(Exception e)
        {
            Exception caughtExcept = e;

            if (caughtExcept != null)
            {
                translator.throwError(luaState, caughtExcept);
                LuaDll.lua_pushnil(luaState);

                return 1;
            }
            else
                return 0;
        }


		/*
		 * Excutes a Lua chunk and returns all the chunk's return
		 * values in an array
		 */
		public object[] DoString(string chunk) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			if(LuaDll.luaL_loadbuffer(luaState,chunk,"chunk")==0) 
			{
                if (LuaDll.lua_pcall(luaState, 0, -1, 0) == 0)
                    return translator.popValues(luaState, oldTop);
                else
                    ThrowExceptionFromError(oldTop);
			} 
			else
                ThrowExceptionFromError(oldTop);

            return null;            // Never reached - keeps compiler happy
		}
		/*
		 * Excutes a Lua file and returns all the chunk's return
		 * values in an array
		 */
		public object[] DoFile(string fileName) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			if(LuaDll.luaL_loadfile(luaState,fileName)==0) 
			{
                if (LuaDll.lua_pcall(luaState, 0, -1, 0) == 0)
                    return translator.popValues(luaState, oldTop);
                else
                    ThrowExceptionFromError(oldTop);
			} 
			else
                ThrowExceptionFromError(oldTop);

            return null;            // Never reached - keeps compiler happy
		}


		/*
		 * Indexer for global variables from the LuaInterpreter
		 * Supports navigation of tables by using . operator
		 */
		public object this[string fullPath]
		{
			get 
			{
				object returnValue=null;
				int oldTop=LuaDll.lua_gettop(luaState);
				string[] path=fullPath.Split(new char[] { '.' });
				LuaDll.lua_getglobal(luaState,path[0]);
				returnValue=translator.getObject(luaState,-1);
				if(path.Length>1) 
				{
					string[] remainingPath=new string[path.Length-1];
					Array.Copy(path,1,remainingPath,0,path.Length-1);
					returnValue=getObject(remainingPath);
				}
				LuaDll.lua_settop(luaState,oldTop);
				return returnValue;
			}
			set 
			{
				int oldTop=LuaDll.lua_gettop(luaState);
				string[] path=fullPath.Split(new char[] { '.' });
				if(path.Length==1) 
				{
					translator.push(luaState,value);
					LuaDll.lua_setglobal(luaState,fullPath);
				} 
				else 
				{
					LuaDll.lua_getglobal(luaState,path[0]);
					string[] remainingPath=new string[path.Length-1];
					Array.Copy(path,1,remainingPath,0,path.Length-1);
					setObject(remainingPath,value);
				}
				LuaDll.lua_settop(luaState,oldTop);
			}
		}
		/*
		 * Navigates a table in the top of the stack, returning
		 * the value of the specified field
		 */
		internal object getObject(string[] remainingPath) 
		{
			object returnValue=null;
			for(int i=0;i<remainingPath.Length;i++) 
			{
				LuaDll.lua_pushstring(luaState,remainingPath[i]);
				LuaDll.lua_gettable(luaState,-2);
				returnValue=translator.getObject(luaState,-1);
				if(returnValue==null) break;	
			}
			return returnValue;    
		}
		/*
		 * Gets a numeric global variable
		 */
		public double GetNumber(string fullPath) 
		{
			return (double)this[fullPath];
		}
		/*
		 * Gets a string global variable
		 */
		public string GetString(string fullPath) 
		{
			return (string)this[fullPath];
		}
		/*
		 * Gets a table global variable
		 */
		public LuaTable GetTable(string fullPath) 
		{
			return (LuaTable)this[fullPath];
		}
		/*
		 * Gets a table global variable as an object implementing
		 * the interfaceType interface
		 */
		public object GetTable(Type interfaceType, string fullPath) 
		{
			return CodeGeneration.Instance.GetClassInstance(interfaceType,GetTable(fullPath));
		}
		/*
		 * Gets a function global variable
		 */
		public LuaFunction GetFunction(string fullPath) 
		{
            object obj=this[fullPath];
			return (obj is KopiLua.Lua.lua_CFunction ? new LuaFunction((KopiLua.Lua.lua_CFunction)obj,this) : (LuaFunction)obj);
		}
		/*
		 * Gets a function global variable as a delegate of
		 * type delegateType
		 */
		public Delegate GetFunction(Type delegateType,string fullPath) 
		{
			return CodeGeneration.Instance.GetDelegate(delegateType,GetFunction(fullPath));
		}
		/*
		 * Calls the object as a function with the provided arguments,
		 * returning the function's returned values inside an array
		 */
		internal object[] callFunction(object function,object[] args) 
		{
            return callFunction(function, args, null);
		}


		/*
		 * Calls the object as a function with the provided arguments and
		 * casting returned values to the types in returnTypes before returning
		 * them in an array
		 */
		internal object[] callFunction(object function,object[] args,Type[] returnTypes) 
		{
			int nArgs=0;
			int oldTop=LuaDll.lua_gettop(luaState);
			if(!LuaDll.lua_checkstack(luaState,args.Length+6))
                throw new LuaException("Lua stack overflow");
			translator.push(luaState,function);
			if(args!=null) 
			{
				nArgs=args.Length;
				for(int i=0;i<args.Length;i++) 
				{
					translator.push(luaState,args[i]);
				}
			}
            int error = LuaDll.lua_pcall(luaState, nArgs, -1, 0);
            if (error != 0)
                ThrowExceptionFromError(oldTop);

            if(returnTypes != null)
			    return translator.popValues(luaState,oldTop,returnTypes);
            else
                return translator.popValues(luaState, oldTop);
		}
		/*
		 * Navigates a table to set the value of one of its fields
		 */
		internal void setObject(string[] remainingPath, object val) 
		{
			for(int i=0; i<remainingPath.Length-1;i++) 
			{
				LuaDll.lua_pushstring(luaState,remainingPath[i]);
				LuaDll.lua_gettable(luaState,-2);
			}
			LuaDll.lua_pushstring(luaState,remainingPath[remainingPath.Length-1]);
			translator.push(luaState,val);
			LuaDll.lua_settable(luaState,-3);
		}
		/*
		 * Creates a new table as a global variable or as a field
		 * inside an existing table
		 */
		public void NewTable(string fullPath) 
		{
			string[] path=fullPath.Split(new char[] { '.' });
			int oldTop=LuaDll.lua_gettop(luaState);
			if(path.Length==1) 
			{
				LuaDll.lua_newtable(luaState);
				LuaDll.lua_setglobal(luaState,fullPath);
			} 
			else 
			{
				LuaDll.lua_getglobal(luaState,path[0]);
				for(int i=1; i<path.Length-1;i++) 
				{
					LuaDll.lua_pushstring(luaState,path[i]);
					LuaDll.lua_gettable(luaState,-2);
				}
				LuaDll.lua_pushstring(luaState,path[path.Length-1]);
				LuaDll.lua_newtable(luaState);
				LuaDll.lua_settable(luaState,-3);
			}
			LuaDll.lua_settop(luaState,oldTop);
		}

		public ListDictionary GetTableDict(LuaTable table)
		{
			ListDictionary dict = new ListDictionary();

			int oldTop = LuaDll.lua_gettop(luaState);
			translator.push(luaState, table);
			LuaDll.lua_pushnil(luaState);
			while (LuaDll.lua_next(luaState, -2) != 0) 
			{
				dict[translator.getObject(luaState, -2)] = translator.getObject(luaState, -1);
				LuaDll.lua_settop(luaState, -2);
			}
			LuaDll.lua_settop(luaState, oldTop);

			return dict;
		}

		/*
		 * Lets go of a previously allocated reference to a table, function
		 * or userdata
		 */
		internal void dispose(int reference) 
		{
			LuaDll.lua_unref(luaState,reference);
		}
		/*
		 * Gets a field of the table corresponding to the provided reference
		 * using rawget (do not use metatables)
		 */
		internal object rawGetObject(int reference,string field) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,reference);
			LuaDll.lua_pushstring(luaState,field);
			LuaDll.lua_rawget(luaState,-2);
			object obj=translator.getObject(luaState,-1);
			LuaDll.lua_settop(luaState,oldTop);
			return obj;
		}
		/*
		 * Gets a field of the table or userdata corresponding to the provided reference
		 */
		internal object getObject(int reference,string field) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,reference);
			object returnValue=getObject(field.Split(new char[] {'.'}));
			LuaDll.lua_settop(luaState,oldTop);
			return returnValue;
		}
		/*
		 * Gets a numeric field of the table or userdata corresponding the the provided reference
		 */
		internal object getObject(int reference,object field) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,reference);
			translator.push(luaState,field);
			LuaDll.lua_gettable(luaState,-2);
			object returnValue=translator.getObject(luaState,-1);
			LuaDll.lua_settop(luaState,oldTop);
			return returnValue;
		}
		/*
		 * Sets a field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, string field, object val) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,reference);
			setObject(field.Split(new char[] {'.'}),val);
			LuaDll.lua_settop(luaState,oldTop);
		}
		/*
		 * Sets a numeric field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, object field, object val) 
		{
			int oldTop=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,reference);
			translator.push(luaState,field);
			translator.push(luaState,val);
			LuaDll.lua_settable(luaState,-3);
			LuaDll.lua_settop(luaState,oldTop);
		}

		/*
		 * Registers an object's method as a Lua function (global or table field)
		 * The method may have any signature
		 */
	    public LuaFunction RegisterFunction(string path, object target,MethodInfo function) 
		{
            // We leave nothing on the stack when we are done
            int oldTop = LuaDll.lua_gettop(luaState);

			LuaMethodWrapper wrapper=new LuaMethodWrapper(translator,target,function.DeclaringType,function);
			translator.push(luaState,new KopiLua.Lua.lua_CFunction(wrapper.call));

            this[path]=translator.getObject(luaState, -1);
            
            LuaFunction f = GetFunction(path);

            LuaDll.lua_settop(luaState, oldTop);

            return f;
		}


		/*
		 * Compares the two values referenced by ref1 and ref2 for equality
		 */
		internal bool compareRef(int ref1, int ref2) 
		{
			int top=LuaDll.lua_gettop(luaState);
			LuaDll.lua_getref(luaState,ref1);
			LuaDll.lua_getref(luaState,ref2);
            int equal=LuaDll.lua_equal(luaState,-1,-2);
			LuaDll.lua_settop(luaState,top);
			return (equal!=0);
		}
        
        internal void pushCSFunction(KopiLua.Lua.lua_CFunction function)
        {
            translator.pushFunction(luaState,function);
        }

        /// <summary>
        /// Dictionary mapping integer reference codes stored in the Lua interpreter's registry to the C# objects 
        /// that own the references, using weak references so we can tell when the C# object no longer exists.
        /// </summary>
        private readonly Dictionary<int, WeakReference> _luaRefToCsObj = new Dictionary<int, WeakReference>();

        /// <summary>
        /// Internally-used list of known-dead lua reference numbers
        /// </summary>
        private readonly List<int> _deadLuaRefs = new List<int>();

        /// <summary>
        /// Granularity for garbage-collection of lua references
        /// </summary>
        private const int LuaRefGcGranularity = 1000;

        /// <summary>
        /// Number of Lua references that must be allocated before we next try to garbage-collect stale references
        /// </summary>
        private int _nextGcNumRefs = LuaRefGcGranularity;

        /// <summary>
        /// Register a new Lua reference and the associated C# object
        /// </summary>
        public void AddLuaRef(int luaRef, object csObject)
        {
            CollectGarbage();
            _luaRefToCsObj[luaRef] = new WeakReference(csObject);
        }

        /// <summary>
        /// Consider garbage-collecting stale Lua references at this point.  We only go ahead if enough Lua 
        /// references have been allocated since the last collection - the overhead in the interpreter per 
        /// reference is very small so we can wait quite a long time before worrying that most of the references
        /// are stale.
        /// </summary>
        private void CollectGarbage()
        {
            // Reduce the reference count target if the actual number of references has somehow decreased
            if (_luaRefToCsObj.Count < _nextGcNumRefs - LuaRefGcGranularity)
                _nextGcNumRefs = _luaRefToCsObj.Count + LuaRefGcGranularity;

            // Don't collect now if we haven't yet reached the reference count target
            if (_luaRefToCsObj.Count < _nextGcNumRefs)
                return;

            // Gather the list of dead refs
            _deadLuaRefs.Clear();
            foreach (var luaRef in _luaRefToCsObj.Keys)
            {
                if (!_luaRefToCsObj[luaRef].IsAlive)
                {
                    _deadLuaRefs.Add(luaRef);
                }
            }

            // Dispose the dead refs and free the entries in _luaRefToCsObj
            foreach (var luaRef in _deadLuaRefs)
            {
                dispose(luaRef);
                _luaRefToCsObj.Remove(luaRef);
            }
            _deadLuaRefs.Clear();

            // Set the reference count target for the next collection
            _nextGcNumRefs = _luaRefToCsObj.Count + LuaRefGcGranularity;
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (translator != null)
            {
                translator.pendingEvents.Dispose();

                translator = null;
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        #endregion
    }

	/*
	 * Wrapper class for Lua tables
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaTable
	{
		internal int reference;
		private Lua interpreter;
		public LuaTable(int reference, Lua interpreter) 
		{
			this.reference=reference;
			this.interpreter=interpreter;
		    interpreter.AddLuaRef(reference, this);
		}
		/*
		 * Indexer for string fields of the table
		 */
		public object this[string field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Indexer for numeric fields of the table
		 */
		public object this[object field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}


		public System.Collections.IEnumerator GetEnumerator()
		{
			return interpreter.GetTableDict(this).GetEnumerator();
		}

		public ICollection Keys 
		{
			get { return interpreter.GetTableDict(this).Keys; }
		}

		public ICollection Values
		{
			get { return interpreter.GetTableDict(this).Values; }
		}

		/*
		 * Gets an string fields of a table ignoring its metatable,
		 * if it exists
		 */
		internal object rawget(string field) 
		{
			return interpreter.rawGetObject(reference,field);
		}

		internal object rawgetFunction(string field) 
		{
			object obj=interpreter.rawGetObject(reference,field);

    		if(obj is KopiLua.Lua.lua_CFunction)
        		return new LuaFunction((KopiLua.Lua.lua_CFunction)obj,interpreter);
    		else
        		return obj;
		}
        
		/*
		 * Pushes this table into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.lua_getref(luaState,reference);
		}
		public override string ToString() 
		{
			return "table";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaTable) 
			{
				LuaTable l=(LuaTable)o;
				return interpreter.compareRef(l.reference,this.reference);
			} else return false;
		}
		public override int GetHashCode() 
		{
			return reference;
		}
	}

	public class LuaFunction 
	{
		private Lua interpreter;
        internal KopiLua.Lua.lua_CFunction function;
		internal int reference;

		public LuaFunction(int reference, Lua interpreter) 
		{
			this.reference=reference;
            this.function=null;
			this.interpreter=interpreter;

		    interpreter.AddLuaRef(reference, this);
		}
        
		public LuaFunction(KopiLua.Lua.lua_CFunction function, Lua interpreter) 
		{
			this.reference=0;
            this.function=function;
			this.interpreter=interpreter;
		}

		/*
		 * Calls the function casting return values to the types
		 * in returnTypes
		 */
		internal object[] call(object[] args, Type[] returnTypes) 
		{
			return interpreter.callFunction(this,args,returnTypes);
		}
		/*
		 * Calls the function and returns its return values inside
		 * an array
		 */
		public object[] Call(params object[] args) 
		{
			return interpreter.callFunction(this,args);
		}
		/*
		 * Pushes the function into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
            if(reference!=0)
			    LuaDll.lua_getref(luaState,reference);
            else
                interpreter.pushCSFunction(function);
		}
		public override string ToString() 
		{
			return "function";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaFunction) 
			{
				LuaFunction l=(LuaFunction)o;
                if(this.reference!=0 && l.reference!=0)
				    return interpreter.compareRef(l.reference,this.reference);
                else
                    return this.function==l.function;
			} 
			else return false;
		}
		public override int GetHashCode() 
		{
            if(reference!=0)
			    return reference;
            else
                return function.GetHashCode();
		}
	}

	public class LuaUserData
	{
		internal int reference;
		private Lua interpreter;
		public LuaUserData(int reference, Lua interpreter) 
		{
			this.reference=reference;
			this.interpreter=interpreter;
		    interpreter.AddLuaRef(reference, this);
		}
		/*
		 * Indexer for string fields of the userdata
		 */
		public object this[string field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Indexer for numeric fields of the userdata
		 */
		public object this[object field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Calls the userdata and returns its return values inside
		 * an array
		 */
		public object[] Call(params object[] args) 
		{
			return interpreter.callFunction(this,args);
		}
		/*
		 * Pushes the userdata into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
			LuaDll.lua_getref(luaState,reference);
		}
		public override string ToString() 
		{
			return "userdata";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaUserData) 
			{
				LuaUserData l=(LuaUserData)o;
				return interpreter.compareRef(l.reference,this.reference);
			} 
			else return false;
		}
		public override int GetHashCode() 
		{
			return reference;
		}
	}

}