using System;
using Haley.Enums;
using Haley.Utils;

namespace Haley.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreMappingAttribute : Attribute
    {
        public IgnoreMappingMode Mode { get; set; }
        public IgnoreMappingAttribute() :this(IgnoreMappingMode.Both) { }
        public IgnoreMappingAttribute(IgnoreMappingMode mode) { Mode = mode; }
    }
}
