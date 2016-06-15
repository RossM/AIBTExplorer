using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIBTViewer
{
    [DebuggerDisplay("{BehaviorName} [{NodeType}]")]
    public class Behavior
    {
        public string BehaviorName;
        public string NodeType;
        public string RawText;
        public List<string> Child = new List<string>();
        public List<string> Param = new List<string>();
        public List<Behavior> ChildLink = new List<Behavior>();
        public List<Behavior> TypeLink = new List<Behavior>(); 
        public List<Behavior> Parent = new List<Behavior>();
        public string Key { get { return BehaviorName.ToLowerInvariant(); } }

        public List<string> Annotations = new List<string>();
    }
}
