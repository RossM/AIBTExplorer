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
            HashSet<string> excludedCharacterTemplates = new HashSet<string>();

            for (int index = 0; index < btPath.Path.Count; index++)
            {
                var behavior = btPath.Path[index];
                bool isLast = index == btPath.Path.Count - 1;
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
                                excludedCharacterTemplates.Add(GetCharacterTemplate(child));
                            }
                        }
                    }
                    else
                    {
                        if (isLast && ((characterTemplate != null && characterTemplate != newTemplate) || excludedCharacterTemplates.Contains(newTemplate)))
                            return false;

                        if (characterTemplate == null)
                            characterTemplate = newTemplate;
                    }
                }

                if (!isLast)
                {
                    if (behavior.NodeType.ToLowerInvariant() == "selector" && !behavior.TypeLink.Contains(btPath.Path[index + 1]))
                    {
                        foreach (var child in behavior.ChildLink)
                        {
                            if (child == btPath.Path[index + 1])
                                break;

                            ShouldShowHandleSequence(child, knownFalse, knownTrue);
                        }
                    }

                    if (behavior.NodeType.ToLowerInvariant() == "sequence" && !behavior.TypeLink.Contains(btPath.Path[index + 1]))
                    {
                        foreach (var child in behavior.ChildLink)
                        {
                            if (child == btPath.Path[index + 1])
                                break;

                            ShouldShowHandleSequence(child, knownTrue, knownFalse);
                        }
                    }
                }
                else
                {
                    while (true)
                    {
                        bool escape = true;

                        if (behavior.TypeLink.Count > 0)
                            return true;

                        if (!ShouldShowHandleSequence(behavior, knownTrue, knownFalse)) return false;

                        if (behavior.NodeType.ToLowerInvariant() == "selector")
                        {
                            foreach (var child in behavior.ChildLink)
                            {
                                var newChild = GetReplacementNode(child, characterTemplate) ?? child;

                                if (newChild.Annotations.Contains("HasAction"))
                                    break;

                                if (!ShouldShowHandleSequence(newChild, knownFalse, knownTrue)) return false;
                            }
                        }

                        if (behavior.NodeType.ToLowerInvariant() == "sequence")
                        {
                            foreach (var child in behavior.ChildLink)
                            {
                                var newChild = GetReplacementNode(child, characterTemplate) ?? child;

                                if (newChild.Annotations.Contains("HasAction"))
                                {
                                    behavior = child;
                                    escape = false;
                                    break;
                                }

                                if (!ShouldShowHandleSequence(newChild, knownTrue, knownFalse)) return false;
                            }
                        }

                        if (escape)
                            return true;
                    }
                }
            }

            return true;
        }

        private static bool ShouldShowHandleSequence(Behavior child, HashSet<string> knownTrue, HashSet<string> knownFalse)
        {
            if (knownFalse.Contains(child.Key))
                return false;

            if (child.NodeType.ToLowerInvariant() == "inverter" && child.ChildLink.Count > 0 &&
                knownTrue.Contains(child.ChildLink[0].Key))
            {
                return false;
            }

            knownTrue.Add(child.Key);

            if (child.NodeType.ToLowerInvariant() == "inverter" && child.ChildLink.Count > 0)
                knownFalse.Add(child.ChildLink[0].Key);

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

            return GetReplacementNode(nextBehavior, characterTemplate);
        }

        private static Behavior GetReplacementNode(Behavior nextBehavior, string characterTemplate)
        {
            if (characterTemplate != null && nextBehavior.BehaviorName.StartsWith("::"))
            {
                var maybeReplacement =
                    nextBehavior.TypeLink.FirstOrDefault(
                        b => b.Key.StartsWith(characterTemplate + "::"));
                return maybeReplacement;
            }

            return null;
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
