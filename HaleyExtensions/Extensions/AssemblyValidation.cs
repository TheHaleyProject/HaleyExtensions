using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Haley.Utils
{
    public static class AssemblyValidation
    {
        public static bool IsDebugBuild(this Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }

        public static string GetSignedKey(this Assembly assembly)
        {
            try
            {
                if (assembly == null) return null;
                var _asm_name = assembly.GetName();
                var _key = _asm_name.GetPublicKey();
                return Convert.ToBase64String(_key);
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}