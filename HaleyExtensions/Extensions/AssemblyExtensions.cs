using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using System.Collections;
using System.Diagnostics;
using System.CodeDom;

namespace Haley.Utils
{
    public static class AssemblyExtensions
    {
        //Reason for using a assembly helper is because, we need the assembly helper (which is a marshall object) to support with cross domain loading.

        public static string GetInfo(this Assembly assembly, AssemblyInfo info) {
            try {
                switch (info) {
                    case AssemblyInfo.Title:
                        return assembly.GetInfo<AssemblyTitleAttribute>();
                    case AssemblyInfo.Description:
                        return assembly.GetInfo<AssemblyDescriptionAttribute>();
                    case AssemblyInfo.Version:
                        return assembly.GetName().Version.ToString(); //send version directly
                    case AssemblyInfo.Product:
                        return assembly.GetInfo<AssemblyProductAttribute>();
                    case AssemblyInfo.Copyright:
                        return assembly.GetInfo<AssemblyCopyrightAttribute>();
                    case AssemblyInfo.Company:
                        return assembly.GetInfo<AssemblyCompanyAttribute>();
                    case AssemblyInfo.FileVersion:
                        return assembly.GetInfo<AssemblyFileVersionAttribute>();
                    case AssemblyInfo.Configuration:
                        return assembly.GetInfo<AssemblyConfigurationAttribute>();
                    case AssemblyInfo.Trademark:
                        return assembly.GetInfo<AssemblyTrademarkAttribute>();
                }
                return null;
            } catch (Exception) {
                return null;
            }
        }

        public static string GetBasePath (this Assembly assembly) {
            try {
                return new Uri(assembly.Location).LocalPath;
            } catch (Exception) {
                return null;
            }
        }

        public static string GetBaseDirectory(this Assembly assembly,string parentFolder = null) {
            try {
                var filepath = assembly.GetBasePath();
                if (filepath == null) return null;
                string result = Path.GetDirectoryName(filepath);
                if (!string.IsNullOrWhiteSpace(parentFolder)) {
                    result = Path.Combine(result,parentFolder);
                }
                return result;
            } catch (Exception) {
                return null;
            }
        }

        static string GetInfo<T>(this Assembly assembly) where T : Attribute {
            try {
                object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
                if (attributes.Length > 0) {
                    T target_attribute = (T)attributes[0];
                    switch (typeof(T).Name) {
                        case nameof(AssemblyTitleAttribute):
                            var title = (target_attribute as AssemblyTitleAttribute)?.Title;
                            if (string.IsNullOrWhiteSpace(title)) {
                                title = Path.GetFileNameWithoutExtension(assembly.CodeBase);
                            }
                            return title; //incase title value is empty we get the dll name.
                        case nameof(AssemblyCompanyAttribute):
                            return (target_attribute as AssemblyCompanyAttribute)?.Company;
                        case nameof(AssemblyCopyrightAttribute):
                            return (target_attribute as AssemblyCopyrightAttribute)?.Copyright;
                        case nameof(AssemblyVersionAttribute):
                            return (target_attribute as AssemblyVersionAttribute)?.Version;
                        case nameof(AssemblyFileVersionAttribute):
                            return (target_attribute as AssemblyFileVersionAttribute)?.Version;
                        case nameof(AssemblyProductAttribute):
                            return (target_attribute as AssemblyProductAttribute)?.Product;
                        case nameof(AssemblyDescriptionAttribute):
                            return (target_attribute as AssemblyDescriptionAttribute)?.Description;
                        case nameof(AssemblyTrademarkAttribute):
                            return (target_attribute as AssemblyTrademarkAttribute)?.Trademark;
                        case nameof(AssemblyConfigurationAttribute):
                        return (target_attribute as AssemblyConfigurationAttribute)?.Configuration;
                    }
                }
                return null ;
            } catch (Exception) {
                return null;
            }
        }
    }
}