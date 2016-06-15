using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIBTViewer
{
    public class BehaviorTree
    {
        public Dictionary<string, Behavior> Tree = new Dictionary<string, Behavior>();

        public IEnumerable<Behavior> Roots()
        {
            return Tree.Values.Where(b => !b.Parent.Any());
        }

        public BehaviorTree(Dictionary<string, Behavior> tree)
        {
            Tree = tree;
        }
    }
}
