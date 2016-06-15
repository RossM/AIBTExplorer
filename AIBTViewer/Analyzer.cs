using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIBTViewer
{
    class Analyzer
    {
        static public void Analyze(BehaviorTree behaviorTree)
        {
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

        static readonly List<string> UpdateAbilities = new List<string>
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

        static public bool ShouldShow(BTPath btPath)
        {
            string characterTemplate = null;
            HashSet<string> knownTrue = new HashSet<string>();
            HashSet<string> knownFalse = new HashSet<string>();

            for (int index = 0; index < btPath.Path.Count; index++)
            {
                var behavior = btPath.Path[index];
                if (behavior.BehaviorName.Contains("::"))
                {
                    var newTemplate = GetCharacterTemplate(behavior);

                    if (newTemplate == "")
                    {
                        // If this is the child of a '::' node, add knownFalse for the other cases.
                        if (index + 1 < btPath.Path.Count)
                        {
                            foreach (var child in behavior.TypeLink)
                            {
                                if (child == btPath.Path[index + 1])
                                    continue;
                                newTemplate = GetCharacterTemplate(child);
                                knownFalse.Add("characternameis-" + newTemplate);
                            }
                        }
                    }
                    else
                    {
                        if ((characterTemplate != null && characterTemplate != newTemplate) || knownFalse.Contains("characternameis-" + newTemplate))
                            return false;

                        characterTemplate = newTemplate;
                        knownTrue.Add("characternameis-" + newTemplate);
                    }
                }
            }

            return true;
        }

        static public Behavior HideNodes(BTPath btPath, Behavior nextBehavior)
        {
            string characterTemplate = null;

            foreach (var behavior in btPath.Path)
            {
                if (behavior.BehaviorName.Contains("::"))
                {
                    var newTemplate = GetCharacterTemplate(behavior);

                    if (newTemplate != "")
                        characterTemplate = newTemplate;
                }
            }

            while (true)
            {
                if (nextBehavior.BehaviorName.StartsWith("::"))
                {
                    var maybeReplacement =
                        nextBehavior.TypeLink.FirstOrDefault(
                            b => b.Key.StartsWith(characterTemplate + "::"));
                    if (maybeReplacement != null)
                    {
                        nextBehavior = maybeReplacement;
                        continue;
                    }
                }
                return nextBehavior;
            }
        }

        private static string GetCharacterTemplate(Behavior behavior)
        {
            var newTemplate =
                behavior.BehaviorName.Substring(0,
                    behavior.BehaviorName.IndexOf("::", StringComparison.InvariantCulture)).ToLowerInvariant();
            return newTemplate;
        }
    }
}
