// See https://aka.ms/new-console-template for more information
using Haley.Utils;
using System.Text.Json;


var jsonArray1 = "[4,3,5,2,2]";
jsonArray1.IsValidJson();

var jsonArray33 = " [4,3,5,2,2] ";
jsonArray33.IsValidJson();
//jsonArray33.IsJsonArray();

try
{
    var another = jsonArray1.Trim().Substring(1, jsonArray1.Length - 2);
    var answer = another.Split(',');
    var answer1 = JsonSerializer.Deserialize<string[]>(jsonArray1);
} catch (Exception ex)
{
}
var jsonArray2 = "{\"mpId\": 1311, \"mpName\": \"md_name\", \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Name\", \"isPredefined\": \"1\", \"shId\": 0, \"displayMpId\": 0, \"dbName\": null, \"tagGroupId\": 0}";
jsonArray2.IsValidJson();

try
{
    var answer2 = JsonSerializer.Deserialize<string[]>(jsonArray2);
} catch (Exception ex)
{
}


var jsonArray3 = "[{\"spId\": 1922, \"shId\": 2, \"mpId\": 1311, \"mpName\": \"md_name\", \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Title\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Name of the Contract\", \"graph\": null, \"tagGroup\": null},{\"spId\": 1923, \"shId\": 2, \"mpId\": 1332, \"mpName\": \"md_code\", \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Code\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Contract code\", \"graph\": null, \"tagGroup\": null},{\"spId\": 1924, \"shId\": 2, \"mpId\": 1338, \"mpName\": \"md_contract_distribution\", \"mpDataType\": \"t_bool\", \"mpKind\": \"attribute\", \"displayName\": \"Has Distribution\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Does this contract have user distribution associated with it in CDE\", \"graph\": null, \"tagGroup\": null}]";
jsonArray3.IsValidJson();

try
{
    var answer3 = JsonSerializer.Deserialize<string[]>(jsonArray3);
} catch (Exception ex)
{
}

var jsonArray4 = "[{\"spId\": 1922, \"shId\": 2, \"mpId\": 1311, \"mpName\": \"md_name\", \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Title\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Name of the Contract\", \"graph\": null, \"tagGroup\": null},{\"spId\": 1923, \"shId\": 2, \"mpId\": 1332, \"mpName\": , \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Code\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Contract code\", \"graph\": null, \"tagGroup\": null},{\"spId\": 1924, \"shId\": 2, \"mpId\": 1338, \"mpName\": \"md_contract_distribution\", \"mpDataType\": \"t_bool\", \"mpKind\": \"attribute\", \"displayName\": \"Has Distribution\", \"isPredefined\": \"1\", \"isVisible\": \"1\", \"isOptional\": \"0\", \"isArray\": \"0\", \"order\": 0, \"description\": \"Does this contract have user distribution associated with it in CDE\", \"graph\": null, \"tagGroup\": null}]";
jsonArray4.IsValidJson();

var jsonArray5 = "{\"mpId\": 1311, \"mpName\": \"md_name\", \"mpDataType\": \"t_string\", \"mpKind\": \"user_value\", \"displayName\": \"Name\", \"isPredefined\": \"1\", \"shId\": \"[4,6,3,3]\", \"displayMpId\": 0, \"dbName\": null, \"tagGroupId\": 0}";
jsonArray5.IsValidJson();