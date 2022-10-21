using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Haley.Models
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class DescriptionNameValueAttribute : Attribute
    {
        public string Key { get; private set; }
        public string Value { get; private set; }
        public DescriptionNameValueAttribute() { Key = string.Empty; Value = string.Empty; }
        public DescriptionNameValueAttribute(string key, string value) { Key = key; Value = value; }
    }
}
