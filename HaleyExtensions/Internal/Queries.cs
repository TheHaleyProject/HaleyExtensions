using static Haley.Internal.QueryFields;

namespace Haley.Internal {
    internal class QRY_MARIA {
        public static string SCHEMA_EXISTS = $@"select 1 from information_schema.schemata where schema_name = {NAME};";
    }
}