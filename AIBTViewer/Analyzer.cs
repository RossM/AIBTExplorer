using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIBTViewer
{
    class Analyzer
    {
        private BehaviorTree BT;

        public void Analyze(BehaviorTree behaviorTree)
        {
            BT = behaviorTree;

            var behaviors = behaviorTree.Tree.Values.ToArray();
            foreach (var behavior in behaviors)
            {
                foreach (var childName in behavior.Child)
                {
                    Behavior child;
                    if (!behaviorTree.Tree.TryGetValue(childName.ToLowerInvariant(), out child))
                    {
                        child = new Behavior();
                        child.BehaviorName = childName;
                        if (child.BehaviorName.ToLowerInvariant().StartsWith("addtotargetscore_") || child.BehaviorName.ToLowerInvariant().StartsWith("addtoalertdatascore_"))
                            child.NodeType = "Action";
                        else
                            child.NodeType = "?";
                        behaviorTree.Tree.Add(childName.ToLowerInvariant(), child);
                    }
                    child.Parent.Add(behavior);
                    behavior.ChildLink.Add(child);
                }

                if (behavior.BehaviorName.Contains("::"))
                {
                    var typelessName = behavior.BehaviorName.Substring(behavior.BehaviorName.IndexOf("::", StringComparison.InvariantCulture));
                    Behavior typelessNode;
                    if (behaviorTree.Tree.TryGetValue(typelessName.ToLowerInvariant(), out typelessNode) && typelessNode != behavior)
                    {
                        typelessNode.TypeLink.Add(behavior);
                        behavior.Parent.Add(typelessNode);
                    }
                }
            }

            behaviors = behaviorTree.Tree.Values.ToArray();

            foreach (var behavior in behaviors)
            {
                var typeLower = behavior.NodeType.ToLowerInvariant();
                var nameLower = behavior.BehaviorName.ToLowerInvariant();

                if (typeLower == "action")
                    PropagateAnnotation(behavior, "HasAction");

                if (nameLower.StartsWith("selectability-"))
                    PropagateAnnotation(behavior, "HasSelectAbility");

                if (UpdateAbilities.Contains(nameLower) || nameLower.StartsWith("settargetstack-"))
                    PropagateAnnotation(behavior, "HasUpdateBestTarget");

                if (typeLower == "condition" || typeLower == "statcondition")
                    PropagateAnnotationLastChildOnly(behavior, "ConditionValued");
            }
        }

        List<string> UpdateAbilities = new List<string>
        {
            "updatebesttarget", "updatebestalertdata",
            "settargetstack", "setalertdatastack",
            "setnexttarget", "setnextalertdata"
        };

        private static void PropagateAnnotation(Behavior behavior, string annotation)
        {
            var queue = new Stack<Behavior>();
            queue.Push(behavior);
            while (queue.Count > 0)
            {
                var node = queue.Pop();
                if (!node.Annotations.Contains(annotation))
                {
                    node.Annotations.Add(annotation);
                    foreach (var parent in node.Parent)
                        queue.Push(parent);
                }
            }
        }

        private static void PropagateAnnotationLastChildOnly(Behavior behavior, string annotation)
        {
            var queue = new Stack<Behavior>();
            queue.Push(behavior);
            while (queue.Count > 0)
            {
                var node = queue.Pop();
                if (!node.Annotations.Contains(annotation))
                {
                    node.Annotations.Add(annotation);
                    foreach (var parent in node.Parent)
                    {
                        if (parent.ChildLink.Count > 0 && parent.ChildLink[parent.ChildLink.Count - 1] == node)
                            queue.Push(parent);
                    }
                }
            }
        }
    }
}
