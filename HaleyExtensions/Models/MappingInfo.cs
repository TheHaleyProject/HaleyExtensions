using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Haley.Models {

    public delegate bool ValueConverter(PropertyInfo target_prop, object source_value, out object converted_value);

    public struct MappingInfo {
        //Contains information for mapping.
        public Dictionary<string, List<string>> TargetAlternateNames => new Dictionary<string, List<string>>();
        public List<string> IgnoredTargetNames { get; set; }

        public StringComparison ComparisonMethod { get; set; }
        public ValueConverter Converter { get; set; }
        public TargetNameLookup NameMapping { get; set; }
        public bool IncludeIgnoredMembers { get; set; }

        public MappingInfo(TargetNameLookup name_mapping) : this(null, StringComparison.InvariantCulture,name_mapping) {
        }
        public MappingInfo(StringComparison comparison) : this(null, comparison, TargetNameLookup.AttributesAndMappingInfo) {
        }
        public MappingInfo(ValueConverter converter):this(converter,StringComparison.InvariantCulture,TargetNameLookup.AttributesAndMappingInfo) {
        }

        public MappingInfo(ValueConverter converter,StringComparison comparison,TargetNameLookup name_mapping) {
            IgnoredTargetNames = new List<string>();
            ComparisonMethod = comparison;
            Converter = converter;
            NameMapping = name_mapping;
            IncludeIgnoredMembers = false;
        }
    }
}
