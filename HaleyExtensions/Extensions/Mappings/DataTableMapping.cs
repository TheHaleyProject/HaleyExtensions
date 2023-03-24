using Haley.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Haley.Enums;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Diagnostics.SymbolStore;
using System.Dynamic;

namespace Haley.Utils
{
    public static class DataTableMapping {
        public static IEnumerable<TTarget> Map<TTarget>(this DataTable source,MappingInfo mapping_info = default(MappingInfo)) where TTarget : class, new() {
            var dataCols = source.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var targetProps = (typeof(TTarget))
                .GetProperties()?
                .RemoveIgnored(((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject),mapping_info.IncludeIgnoredMembers)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            List<TTarget> _targets = new List<TTarget>();
            foreach (DataRow row in source.Rows) {
                TTarget target = new TTarget();
                foreach (var prop in targetProps) {
                    MapSingleProp(row, prop, ref target, mapping_info, dataCols); //Sending datacols to save processing time.
                }
                _targets.Add(target);
            }

            return _targets;
        }

        public static TTarget Map<TTarget>(this DataRow source, MappingInfo mapping_info = default(MappingInfo)) where TTarget : class, new() //Should be a class and also should have a parameter less new constructor.
        {
            TTarget _target = new TTarget();
            Map(source, ref _target, mapping_info);
            return _target;
        }

        public static void Map<TTarget>(this DataRow source,ref TTarget target, MappingInfo mapping_info = default(MappingInfo)) where TTarget : class, new() {

            var data_columns = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var properties = (typeof(TTarget))
                .GetProperties()?
                .RemoveIgnored(((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject), mapping_info.IncludeIgnoredMembers)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            foreach (var prop in properties) {
                MapSingleProp(source, prop, ref target, mapping_info, data_columns); //Sending datacols to save processing time.
            }
        }

        private static void MapSingleProp<TTarget>(this DataRow source, PropertyInfo prop,ref TTarget target, MappingInfo mapping_info, List<string> sourceColumnNames = null) where TTarget:class, new() {
            var possibleNameMatches = ObjectMapping.PopulateTargetNames(prop,mapping_info);
            if (possibleNameMatches == null || possibleNameMatches.Count() == 0) return; //Don't proceed with processing 

            if (sourceColumnNames == null) {
                sourceColumnNames = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            }
            //Now we need to find out if the datarow has any property with either the original prop name or the alternative name. If it is found and it matches, we get that value.

            foreach (var _name in possibleNameMatches) {
                //get the ky
                var col_key = sourceColumnNames.FirstOrDefault(p => p.Equals(_name, mapping_info.ComparisonMethod));
                if (!string.IsNullOrWhiteSpace(_name)
                    && !string.IsNullOrWhiteSpace(col_key)) {
                    //if a match is found.
                    var sourceValue = source[col_key];
                    if (sourceValue != DBNull.Value && sourceValue != null) {
                        ObjectMapping.FillSingleProp(prop,target, sourceValue, mapping_info);
                        break;
                    }
                }
            }
        }

        public static List<dynamic> ToDynamic(this DataTable source) {
            var result = new List<dynamic>();
            foreach (DataRow row in source.Rows) {
                dynamic dyn = new ExpandoObject();
                result.Add(dyn);
                foreach (DataColumn column in source.Columns) {
                    var dic = (IDictionary<string, object>)dyn; //Casting the dynamic as a dictionary of string/object
                    dic[column.ColumnName] = row[column]; //Adding to this dictionary will reflect in the actual dynamic object
                }
            }
            return result;
        }
    }
}