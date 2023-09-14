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

namespace Haley.Utils
{
    public static class ObjectMapping {
        public static void Map<TTarget>(this Dictionary<string, object> source, ref TTarget target, MappingInfo mapping_info = default(MappingInfo)) where TTarget : class, new() {
            if (source == null || source.Count == 0) return; //dont' process
            object targetObj = target;
            target = source.Map(ref targetObj, mapping_info) as TTarget;
        }

        public static TTarget Map<TTarget>(this Dictionary<string, object> source, MappingInfo mapping_info = default(MappingInfo)) where TTarget : class, new() {
            return Map(source, typeof(TTarget), mapping_info) as TTarget;
        }

        public static object Map(this Dictionary<string, object> source, Type target_type, MappingInfo mapping_info = default(MappingInfo)) {
            var target = Activator.CreateInstance(target_type);
            target = Map(source, ref target, mapping_info);
            return target;
        }
        
        public static object Map(this Dictionary<string, object> source,ref object target, MappingInfo mapping_info = default(MappingInfo)) {
            if (source == null || source.Count == 0) return target; //dont' process

            var targetProps = target.GetType()
                .GetProperties()? //Getall props
                .RemoveIgnored(((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject), mapping_info.IncludeIgnoredMembers)
                //.Where(p =>
                //!Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)))?
                .ToList(); //This gives properties which are not ignored.

            foreach (var prop in targetProps) {
                MapSingleProp(source, prop,ref target, mapping_info); //Sending datacols to save processing time.
            }
            return target;
        }
        public static TTarget MapProperties<TSource, TTarget>(this TSource source, MappingInfo mapping_info = default(MappingInfo))
           where TSource : class
           where TTarget : class {
            return source.MapProperties(typeof(TTarget), mapping_info) as TTarget;
        }

        public static object MapProperties<TSource>(this TSource source, Type target_type, MappingInfo mapping_info = default(MappingInfo))
           where TSource : class {
            var target = Activator.CreateInstance(target_type);
            return source.MapProperties(target, mapping_info);
        }

        public static TTarget MapProperties<TSource,TTarget>(this TSource source, TTarget target, MappingInfo mapping_info = default(MappingInfo))
        where TSource : class
        where TTarget : class{
            object newTarget = target;
            return source.MapProperties(newTarget,mapping_info) as TTarget;
        }

        public static object MapProperties<TSource>(this TSource source, object target, MappingInfo mapping_info = default(MappingInfo))
        where TSource : class
        {
            try {
                //Sometimes target can be null. We can set it default.
                if (target == null || source == null) {
                    return target; //Then there is nothing to map.
                }

                var sourceProperties = source.GetType().GetProperties().Select(p => p); //We don't have to worry about source properites being readonly. We are merely going to get it.
                var targetProperties = target.GetType().GetProperties().Where(p => p.CanWrite); //Only take properties which are not readonly.

                if (sourceProperties == null || sourceProperties?.Count() == 0 || targetProperties == null || targetProperties?.Count() == 0) return target;

                #region Remove Ignored Properties 
                if (!mapping_info.IncludeIgnoredMembers) {
                    //Remove properties in source which has "Both" & "FromThis" ignore properties
                    sourceProperties = sourceProperties.RemoveIgnored(((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.FromThisObject), false); //where if we ignore an object in source to be copied.
                    targetProperties = targetProperties.RemoveIgnored(((mode) => mode == IgnoreMappingMode.Both || mode == IgnoreMappingMode.ToThisObject), false); //where if we ignore an object in source to be copied.
                }
                #endregion

                foreach (var targetProp in targetProperties) {
                    //Getting only for the target (not for the source).
                    var possibleNameMatches = PopulateTargetNames(targetProp, mapping_info);
                    if (possibleNameMatches == null || possibleNameMatches.Count() == 0) continue; //Don't proceed with processing 
                    object targetValue = null;

                    foreach (var sourceProp in sourceProperties) {
                        object sourcePropValue = null;
                        var _sourceName = sourceProp.Name; //SOURCE NAME IS ALWAYS THE PROPERTY NAME DIRECTLY

                        if (possibleNameMatches.Any(p => p.Equals(_sourceName, mapping_info.ComparisonMethod))) //In any case, names should match
                        {
                            sourcePropValue = sourceProp.GetValue(source);

                            if (mapping_info.Converter != null && mapping_info.Converter.Invoke(targetProp, sourcePropValue, out object _convertedval)) {
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
        static object MapSingleProp(this Dictionary<string, object> source, PropertyInfo prop,ref object target, MappingInfo mapping_info) {
            var possibleNameMatches = PopulateTargetNames(prop, mapping_info);
            if (possibleNameMatches == null || possibleNameMatches.Count() == 0) return target; //Don't proceed with this target property

            //Now we need to find out if the source has any value with either the original prop name or the alternative name. If it is found and it matches, we get that value.

            foreach (var _name in possibleNameMatches) {
                var dic_key = source.Keys.FirstOrDefault(p => p.Equals(_name, mapping_info.ComparisonMethod));
                //REMEMBER KEY IS VERY IMPORTANT IN DICTIONARY. IT IS CASE SENSITIVE
                if (!string.IsNullOrWhiteSpace(_name)
                    && !string.IsNullOrWhiteSpace(dic_key)) {
                    //if a match is found.
                    var sourceValue = source[dic_key];
                    if (sourceValue != null) {
                        FillSingleProp(prop, target, sourceValue, mapping_info);
                        break;//Don't check other possible names.
                    }
                }
            }
            return target;
        }
        internal static object FillSingleProp(PropertyInfo prop,object target, object source_value, MappingInfo mapping_info = default(MappingInfo)) {
            try {
                Type _propType = prop.PropertyType;
                //Intercept using type parser.
                if (mapping_info.Converter != null) {
                    if (!mapping_info.Converter.Invoke(prop, source_value, out object converted_value)) return target; //It type parse returns false, don't fill this value
                    if (converted_value != null && converted_value.GetType() == prop.PropertyType) {
                        prop.SetValue(target, converted_value, null);
                        return target;
                    }
                }

                if (source_value == null) {
                    prop.SetValue(source_value, null, null); //Directly set the input source_value as the property value.
                    return target;
                }

                //STRING
                if (_propType == typeof(string)) {
                    prop.SetValue(target, source_value.ToString().Trim(), null);
                }
                //BOOL
                else if (_propType == typeof(bool) || _propType == typeof(bool?)) {
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
                //INT
                else if (_propType == typeof(int)
                         || _propType == typeof(int?)) {
                    int.TryParse(source_value.ToString(), out int _int_value);
                    prop.SetValue(target, _int_value, null);
                }
                //DOUBLE
                else if (_propType == typeof(double) || _propType == typeof(double?)) {
                    double.TryParse(source_value.ToString(), out double dbl_value);
                    prop.SetValue(target, dbl_value, null);
                }
                //LONG
                else if (_propType == typeof(long) || _propType == typeof(long?)) {
                    long.TryParse(source_value.ToString(), out long lng_value);
                    prop.SetValue(target, lng_value, null);
                }
                //ENUM
                else if (typeof(Enum).IsAssignableFrom(_propType)) {
                    foreach (var item in Enum.GetValues(_propType)) {
                        var isMatch = item?.ToString().Equals(source_value.ToString(), StringComparison.OrdinalIgnoreCase);
                       if (isMatch.HasValue && isMatch.Value){
                            prop.SetValue(target, item, null);
                            break;
                        }
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
        internal static List<string> PopulateTargetNames(PropertyInfo prop,MappingInfo mapping_info) {
            List<string> possibleNameMatches = new List<string>();
            //Check if this property has to be ignored.
            if (mapping_info.IgnoredTargetNames != null && mapping_info.IgnoredTargetNames.Any(p => p.Equals(prop.Name, mapping_info.ComparisonMethod))) return possibleNameMatches; //Don't consider this target name.

            //DO NOT SORT. LET US TAKE THE PROPERTY NAME AS THE PRIORITY
            possibleNameMatches.Add(prop.Name); //Add default property name. 
            List<string> othernames = new List<string>();
            List<string> info_source = new List<string>();
            string json_name = string.Empty;

            //Other names.
            var otherNamesAttribute = prop.GetCustomAttribute<OtherNamesAttribute>();
            if (otherNamesAttribute != null) {
                //It means that the target property has other names attribute defined and it might hold some values.
                othernames.AddRange(otherNamesAttribute.AlternativeNames);
            }

            //JsonPropertyName
            var jsonNameAttribute = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonNameAttribute != null) {
                json_name = jsonNameAttribute.Name;
            }

            if (mapping_info.TargetAlternateNames.ContainsKey(prop.Name)) {
                info_source = mapping_info.TargetAlternateNames[prop.Name];
            }

            switch (mapping_info.NameMapping) {
                case TargetNameLookup.AttributesAndMappingInfo:
                    possibleNameMatches.AddRange(info_source); //First priority (not sorted)
                    possibleNameMatches.Add(json_name); //Second priority
                    possibleNameMatches.AddRange(othernames); //Last priority as specified (not sorted)
                    break;
                case TargetNameLookup.AttributesOnly:
                    possibleNameMatches.Add(json_name); //Second priority
                    possibleNameMatches.AddRange(othernames); //Last priority as specified (not sorted)
                    break;
                case TargetNameLookup.MappingInfoOnly:
                    possibleNameMatches.AddRange(info_source); //First priority (not sorted)
                    break;
                case TargetNameLookup.OtherNameAttributeOnly:
                    possibleNameMatches.AddRange(othernames); //Last priority as specified (not sorted)
                    break;
                case TargetNameLookup.JsonPropNameAttributeOnly:
                    possibleNameMatches.Add(json_name); //Second priority
                    break;
                default:
                    break;
            }
           
            return possibleNameMatches?.Where(p=> !string.IsNullOrWhiteSpace(p))?.Distinct()?.ToList(); //remove nulls and empty
        }
        internal static IEnumerable<PropertyInfo> RemoveIgnored(this IEnumerable<PropertyInfo> source, Func<IgnoreMappingMode, bool> ignoreValidator,bool should_bypass) {
            try {
                if (should_bypass) return source; //Don't ignore anything.
                List<PropertyInfo> _toremoveSource = new List<PropertyInfo>();
                //Filter source. (Using Linq increases process time. So going with foreach)
                foreach (var prop in source.Where(p => Attribute.IsDefined(p, typeof(IgnoreMappingAttribute)) || Attribute.IsDefined(p,typeof(JsonIgnoreAttribute)))) {

                    //Json ignore attribute gives same effect as the IgnoreMapping with Mode = IgnoreBoth.

                    //Check IgnoreMapping attribute
                    var mapignoreAtrbte = prop.GetCustomAttribute<IgnoreMappingAttribute>();
                    if (mapignoreAtrbte != null) {
                        var _mode = mapignoreAtrbte.Mode;
                        if (ignoreValidator.Invoke(_mode)) {
                            _toremoveSource.Add(prop); //Only if both or from this object is ignored.
                            //If we have ignored this property, no need to check further for another validation.
                            continue;
                        }
                    }

                    //Check JsonIgnore attribute
                    var jsonIgnoreAtrbte = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                    if (jsonIgnoreAtrbte != null) {
                        _toremoveSource.Add(prop); //if we have that attribute, no need to further check if it has any properties.
                    }
                }

                return source.Except(_toremoveSource);

            } catch (Exception) {
                return source; //Incase of exception, don't remove anything, return all values.
            }
        }
    }
}