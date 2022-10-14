using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Haley.Models {

    public struct FlattenInfo {
        //all child nodes
        public List<string> ChildNodePaths { get; set; }
        public int Level { get; set; }
        public FlattenMode Mode { get; set; }

        public FlattenInfo(FlattenMode mode, int level = 0) {
            Level = level;
            ChildNodePaths = new List<string>(); //default
            Mode = mode;

        }
        public FlattenInfo(int level = 0):this(FlattenMode.SelectedNodes,level) {
        }
    }
}
