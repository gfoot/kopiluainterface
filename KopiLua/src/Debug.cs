namespace KopiLua
{
    public partial class Lua
    {

        static private void DefaultWriteLine(string format, params object[] arg)
        {
            System.Console.WriteLine(format, arg);
        }

        public delegate void WriteLineFunc(string format, params object[] arg);
        static public WriteLineFunc WriteLine = DefaultWriteLine;
        
        //[System.Diagnostics.ConditionalAttribute("DEBUG")]
        static public void Assert(bool condition)
        {
            if (condition) return;
            WriteLine("Assert fail");
            throw new System.Exception();
        }

        //[System.Diagnostics.ConditionalAttribute("DEBUG")]
        static public void Assert(bool condition, string message)
        {
            if (condition) return;
            WriteLine("Assert fail - {0}", message);
            throw new System.Exception();
        }

        //[System.Diagnostics.ConditionalAttribute("DEBUG")]
        static public void Assert(bool condition, string message, string detail)
        {
            if (condition) return;
            WriteLine("Assert fail - {0}", message);
            WriteLine("    detail: {0}", detail);
            throw new System.Exception();
        }

        //[System.Diagnostics.ConditionalAttribute("DEBUG")]
        static public void Assert(bool condition, string message, string detail, params object[] arg)
        {
            if (condition) return;
            WriteLine("Assert fail - {0}", message);
            WriteLine("    detail: " + detail, arg);
            throw new System.Exception();
        }
    }
}
