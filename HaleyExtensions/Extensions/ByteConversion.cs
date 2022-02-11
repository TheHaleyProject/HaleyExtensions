using System;
using System.Reflection;


namespace Haley.Utils
{
    public static class ByteConversion
    {
      public static string GetPublicKey(this byte[] array)
        {
            try
            {
                var snkpair = new StrongNameKeyPair(array);
                var public_key = snkpair.PublicKey;
                return Convert.ToBase64String(public_key);
            }
            catch (Exception)
            {
                return null;
            }
           
        }
    }
}