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

namespace Haley.Utils
{
    public delegate bool CustomTypeConverter(PropertyInfo target_prop, object source_value, out object converted_value);

    public static class ObjectMapping
    {
        public static IEnumerable<TTarget> Map<TTarget>(this DataTable source, CustomTypeConverter typeParser = null) where TTarget : class, new() {
            return Map<TTarget>(source,StringComparison.InvariantCulture, typeParser);
        }
        public static IEnumerable<TTarget> Map<TTarget>(this DataTable source, StringComparison comparison_method, CustomTypeConverter typeParser = null) where TTarget : class, new() {
            var dataCols = source.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var targetProps = (typeof(TTarget))
                .GetProperties()?
                .RemoveIgnored((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            List<TTarget> _targets = new List<TTarget>();
            foreach (DataRow row in source.Rows) {
                TTarget target = new TTarget();
                foreach (var prop in targetProps) {
                    MapSingleProp(row, prop, ref target, comparison_method, dataCols, typeParser); //Sending datacols to save processing time.
                }
                _targets.Add(target);
            }

            return _targets;
        }

        public static TTarget Map<TTarget>(this DataRow source, CustomTypeConverter typeParser = null) where TTarget : class, new() {
            return Map<TTarget>(source, StringComparison.InvariantCulture, typeParser);
        }

        public static TTarget Map<TTarget>(this DataRow source, StringComparison comparison_method, CustomTypeConverter typeParser = null) where TTarget : class, new() //Should be a class and also should have a parameter less new constructor.
        {
            TTarget _target = new TTarget();
            Map(source, ref _target, comparison_method, typeParser);
            return _target;
        }

        public static void Map<TTarget>(this DataRow source,ref TTarget target, StringComparison comparison_method, CustomTypeConverter typeParser = null) where TTarget : class, new() {

            var data_columns = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            var properties = (typeof(TTarget))
                .GetProperties()?
                .RemoveIgnored((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            foreach (var prop in properties) {
                MapSingleProp(source, prop, ref target, comparison_method, data_columns, typeParser); //Sending datacols to save processing time.
            }
        }

        public static TTarget Map<TTarget>(this JsonNode source, StringComparison comparison_method = StringComparison.InvariantCulture, CustomTypeConverter typeParser = null) 
            where TTarget:class, new()
            {
            TTarget target = new TTarget();
            Map(source, ref target, comparison_method, typeParser);
            return target;
        }

        public static void Map<TTarget>(this JsonNode source, ref TTarget target, StringComparison comparison_method = StringComparison.InvariantCulture, CustomTypeConverter typeParser = null) where TTarget : class, new() {
            if (source == null) return;
            Dictionary<string, object> dic = source.AsObject()?.ToDictionary(p => p.Key, q => q.Value?.GetValue<object>() as object);
            Map(dic, ref target, comparison_method, typeParser);
        }

        public static TTarget Map<TTarget>(this Dictionary<string, object> source, StringComparison comparison_method = StringComparison.InvariantCulture, CustomTypeConverter typeParser = null) where TTarget : class, new() {
            TTarget target = new TTarget(); //new target
            Map(source,ref target, comparison_method, typeParser);
            return target;
        }
        
        public static void Map<TTarget>(this Dictionary<string, object> source,ref TTarget target, StringComparison comparison_method = StringComparison.InvariantCulture, CustomTypeConverter typeParser = null) where TTarget: class, new() {
            if (source == null || source.Count == 0) return; //dont' process

            var targetProps = (typeof(TTarget))
                .GetProperties()? //Getall props
                .RemoveIgnored((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            foreach (var prop in targetProps) {
                MapSingleProp(source, prop,ref target, comparison_method, typeParser); //Sending datacols to save processing time.
            }
        }

        public static TTarget MapProperties<TSource, TTarget>(this TSource source, TTarget target, bool include_ignored_properties = false, bool ignore_case = false, CustomTypeConverter typeParser = null)
              where TSource : class
              where TTarget : class {
            StringComparison comparison_method = StringComparison.InvariantCulture;
            if (ignore_case) {
                comparison_method = StringComparison.InvariantCultureIgnoreCase;
            }
            return MapProperties(source, target, comparison_method, include_ignored_properties,typeParser);
        }
        public static TTarget MapProperties<TSource, TTarget>(this TSource source, TTarget target, StringComparison comparison_method, bool include_ignored_properties = false, CustomTypeConverter typeParser = null)
        where TSource : class
        where TTarget : class {
            try {
                //Sometimes target can be null. We can set it default.
                if (target == null || source == null) {
                    return target; //Then there is nothing to map.
                }

                var sourceProperties = source.GetType().GetProperties().Select(p => p); //We don't have to worry about source properites being readonly. We are merely going to get it.
                var targetProperties = target.GetType().GetProperties().Where(p => p.CanWrite); //Only take properties which are not readonly.

                if (sourceProperties == null || sourceProperties?.Count() == 0 || targetProperties == null || targetProperties?.Count() == 0) return target;

                #region Remove Ignored Properties
                if (!include_ignored_properties) {
                    //Remove properties in source which has "Both" & "FromThis" ignore properties
                    sourceProperties = sourceProperties.RemoveIgnored((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.FromThisObject); //where if we ignore an object in source to be copied.
                    targetProperties = targetProperties.RemoveIgnored((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject); //where if we ignore an object in source to be copied.
                }
                #endregion

                foreach (var targetProp in targetProperties) {
                    //Getting only for the target (not for the source).
                    var possibleNameMatches = GetOtherNames(targetProp);
                    object targetValue = null;

                    foreach (var sourceProp in sourceProperties) {
                        object sourcePropValue = null;
                        var _sourceName = sourceProp.Name;

                        if (possibleNameMatches.Any(p => p.Equals(_sourceName, comparison_method))) //In any case, names should match
                        {
                            sourcePropValue = sourceProp.GetValue(source);

                            if (typeParser != null && typeParser.Invoke(targetProp, sourcePropValue, out object _convertedval)) {
                                //Sometimes before mapping, we might want to do some processing for certain properties. So, using delegate.
                                if (_convertedval != null && _convertedval.GetType() == targetProp.PropertyType) {
                                    //the converted value should definitely match the target prop type.
                                    targetValue = _convertedval;
                                }
                            }

                            //If we still don't get the target value, check if the property directly matches.
                            if (targetValue == null && sourceProp.PropertyType == targetProp.PropertyType) {
                                targetValue = sourcePropValue;
                            }
                        }

                        if (targetValue != null) {
                            break; //Cos, we managed to find a match.
                        }
                    }

                    //If we are not able to find a match, we just ignore it.
                    if (targetValue != null) {
                        targetProp.SetValue(target, targetValue);
                    }
                }
                return target;
            } catch (Exception) {
                return null;
            }
        }
        #region Private Helpers

        #endregion
        private static object FillSingleProp(PropertyInfo prop,object target, object source_value, CustomTypeConverter typeParser = null) {
            try {
                Type _propType = prop.PropertyType;
                //Intercept using type parser.
                if (typeParser != null) {
                    if (!typeParser.Invoke(prop, source_value, out object converted_value)) return target; //It type parse returns false, don't fill this value
                    if (converted_value != null && converted_value.GetType() == prop.PropertyType) {
                        prop.SetValue(target, converted_value, null);
                        return target;
                    }
                }

                //STRING
                if (_propType == typeof(string)) {
                    prop.SetValue(target, source_value.ToString().Trim(), null);
                }
                //BOOL
                else if (_propType == typeof(bool) || _propType == typeof(bool?)) {
                    if (source_value == null) {
                        prop.SetValue(source_value, null, null);
                    } else {
                        bool? boolval = null;

                        if (bool.TryParse(source_value.ToString(), out bool local_val)) {
                            //If successfully converted.
                            boolval = local_val;
                        }
                        if (boolval == null) {
                            switch (source_value.ToString().ToLower()) {
                                case "1":
                                case "true":
                                case "okay":
                                case "success":
                                case "y":
                                case "t":
                                case "yes":
                                    boolval = true;
                                    break;
                                case "0":
                                case "false":
                                case "notokay":
                                case "fail":
                                case "n":
                                case "f":
                                case "no":
                                    boolval = false;
                                    break;
                            }
                        }

                        if (boolval != null) {
                            prop.SetValue(target, boolval, null);
                        }
                    }
                }
                //INT
                else if (_propType == typeof(int)
                         || _propType == typeof(int?)) {
                    if (source_value == null) {
                        prop.SetValue(source_value, null, null);
                    } else {
                        int.TryParse(source_value.ToString(), out int _int_value);
                        prop.SetValue(target, _int_value, null);
                    }
                }
                //DOUBLE
                else if (_propType == typeof(double) || _propType == typeof(double?)) {
                    if (source_value == null) {
                        prop.SetValue(source_value, null, null);
                    } else {
                        double.TryParse(source_value.ToString(), out double dbl_value);
                        prop.SetValue(target, dbl_value, null);
                    }
                }
                //LONG
                else if (_propType == typeof(long) || _propType == typeof(long?)) {
                    if (source_value == null) {
                        prop.SetValue(source_value, null, null);
                    } else {
                        long.TryParse(source_value.ToString(), out long lng_value);
                        prop.SetValue(target, lng_value, null);
                    }
                }
                //DEFAULT
                else {
                    //TRY TO SET AS IT IS. IF IT RESULTS IN EXCEPTION, DO NOTHING/IGNORE.
                    prop.SetValue(target, source_value, null);
                }
                return target;
            } catch (Exception) {
                //dO NOTHING. Use a logger to log at a later stage.
                return target;
            }
        }
        private static List<string> GetOtherNames(PropertyInfo prop) {
            //DO NOT SORT. LET US TAKE THE PROPERTY NAME AS THE PRIORITY
            var possibleNameMatches = new List<string>() { prop.Name }; //Add default property name. 
            var _otherNamesAttribute = prop.GetCustomAttribute<OtherNamesAttribute>();

            if (_otherNamesAttribute != null) {
                //It means that the target property has other names attribute defined and it might hold some values.
                possibleNameMatches.AddRange(_otherNamesAttribute.AlternativeNames);
            }
            return possibleNameMatches;
        }
        private static void MapSingleProp<TTarget>(this DataRow source, PropertyInfo prop,ref TTarget target, StringComparison comparison_method , List<string> sourceColumnNames = null, CustomTypeConverter typeParser = null) where TTarget:class, new() {
            var possibleNameMatches = GetOtherNames(prop);
            if (sourceColumnNames == null) {
                sourceColumnNames = source.Table.Columns.Cast<DataColumn>().Select(p => p.ColumnName)?.ToList(); //All the column names of the datarow.
            }
            //Now we need to find out if the datarow has any property with either the original prop name or the alternative name. If it is found and it matches, we get that value.

            foreach (var _name in possibleNameMatches) {
                //get the ky
                var col_key = sourceColumnNames.FirstOrDefault(p => p.Equals(_name, comparison_method));
                if (!string.IsNullOrWhiteSpace(_name)
                    && !string.IsNullOrWhiteSpace(col_key)) {
                    //if a match is found.
                    var sourceValue = source[col_key];
                    if (sourceValue != DBNull.Value && sourceValue != null) {
                        FillSingleProp(prop,target, sourceValue, typeParser);
                        break;
                    }
                }
            }
        }
        private static void MapSingleProp<TTarget>(this Dictionary<string, object> source, PropertyInfo prop,ref TTarget target, StringComparison comparison_method, CustomTypeConverter typeParser = null) where TTarget:class, new() {
            var possibleNameMatches = GetOtherNames(prop);
            //Now we need to find out if the source has any value with either the original prop name or the alternative name. If it is found and it matches, we get that value.

            foreach (var _name in possibleNameMatches) {
                var dic_key = source.Keys.FirstOrDefault(p => p.Equals(_name, comparison_method));
                //REMEMBER KEY IS VERY IMPORTANT IN DICTIONARY. IT IS CASE SENSITIVE
                if (!string.IsNullOrWhiteSpace(_name)
                    && !string.IsNullOrWhiteSpace(dic_key)) {
                    //if a match is found.
                    var sourceValue = source[dic_key];
                    if (sourceValue != null) {
                        FillSingleProp(prop,target, sourceValue, typeParser);
                        break;//Don't check other possible names.
                    }
                }
            }
        }
        private static IEnumerable<PropertyInfo> RemoveIgnored(this IEnumerable<PropertyInfo> source, Func<IgnoreMappingMode, bool> ignoreValidator) {
            try {
                List<PropertyInfo> _toremoveSource = new List<PropertyInfo>();
                //Filter source. (Using Linq increases process time. So going with foreach)
                foreach (var prop in source.Where(p => Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))) {
                    var _ignoreAttribute = prop.GetCustomAttribute<IgnoreMappingAttribute>();
                    if (_ignoreAttribute == null) continue;
                    var _mode = _ignoreAttribute.Mode;
                    if (ignoreValidator.Invoke(_mode)) {
                        _toremoveSource.Add(prop); //Only if both or from this object is ignored.
                    }
                }
                return source.Except(_toremoveSource);

            } catch (Exception) {
                return null;
            }
        }
    }
}