using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;

namespace StressTest
{
    class StressTest
    {
        static void Main(string[] args)
        {
            Main2();
        }

        static void Main1()
        {
            UnityEngine.GameObject cube = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
            float t = UnityEngine.Time.realtimeSinceStartup;
            UnityEngine.Quaternion q = UnityEngine.Quaternion.AngleAxis(t * 50, UnityEngine.Vector3.up);
            cube.transform.rotation = q;
        }

        static void Main2()
        {
            Lua L = new Lua();
//            L.DoString("UnityEngine = luanet.UnityEngine");
//            L.DoString("print(UnityEngine)");
//            L.DoString("cubetype = UnityEngine.PrimitiveType.Cube");
//            L.DoString("print(cubetype)");
//            L.DoString("gotype = UnityEngine.GameObject");
//            L.DoString("print(gotype)");
//            L.DoString("CP = gotype.CreatePrimitive");
//            L.DoString("print(CP)");
//            L.DoString("cube2 = UnityEngine.GameObject.CP2()");
//            L.DoString("print(cube2)");
//            L.DoString("cube = CP(cubetype)");
//            L.DoString("cube = luanet.UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube)");
            L.DoString("luanet.import_type(UnityEngine.GameObject)()");
            L.DoString("luanet.UnityEngine.GameObject.CP2()");
            
            while (true)
            {
                L.DoString("t = UnityEngine.Time.realtimeSinceStartup");
                L.DoString("q = UnityEngine.Quaternion.AngleAxis(t*50, UnityEngine.Vector3.up)");
                L.DoString("cube.transform.rotation = q");
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}


namespace UnityEngine
{
    public enum PrimitiveType
    {
        Cube
    };

    public class GameObject
    {
        static public GameObject CreatePrimitive(PrimitiveType type)
        {
            return new GameObject();
        }

        static public GameObject CP2()
        {
            return new GameObject();
        }

        public Transform transform = new Transform();
    }

    public class Time
    {
        public static float realtimeSinceStartup { get { return 0.0f; } }
    }

    public class Quaternion
    {
        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            return new Quaternion();
        }
    }

    public class Vector3
    {
        public static Vector3 up;
    }

    public class Transform
    {
        public Quaternion rotation { get; set; }
    }
}
