using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIBTViewer
{
    class Analyzer
    {
        public List<String> Errors = new List<string>(); 

        public void Analyze(BehaviorTree behaviorTree)
        {
            BT = behaviorTree;

            LinkBehaviors();

            AddAnnotations();
        }

        private void AddAnnotations()
        {
            var behaviors = BT.Tree.Values.ToArray();

            foreach (var behavior in behaviors)
            {
                var typeLower = behavior.NodeType != null ? behavior.NodeType.ToLowerInvariant() : "unknown";
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

        private void LinkBehaviors()
        {
            var behaviors = BT.Tree.Values.ToArray();

            foreach (var behavior in behaviors)
            {
                foreach (var childName in behavior.Child)
                {
                    Behavior child;
                    if (!BT.Tree.TryGetValue(childName.ToLowerInvariant(), out child))
                    {
                        child = new Behavior { BehaviorName = childName };
                        if (child.BehaviorName.ToLowerInvariant().StartsWith("addtotargetscore_") ||
                            child.BehaviorName.ToLowerInvariant().StartsWith("addtoalertdatascore_"))
                            child.NodeType = "Action";
                        else
                        {
                            AnalysisError(behavior, string.Format("Missing behavior \"{0}\"", childName));
                            child.NodeType = "?";
                        }
                        BT.Tree.Add(childName.ToLowerInvariant(), child);
                    }
                    child.Parent.Add(behavior);
                    behavior.ChildLink.Add(child);
                }

                if (behavior.BehaviorName.Contains("::"))
                {
                    var typelessName =
                        behavior.BehaviorName.Substring(behavior.BehaviorName.IndexOf("::", StringComparison.InvariantCulture));
                    Behavior typelessNode;
                    if (BT.Tree.TryGetValue(typelessName.ToLowerInvariant(), out typelessNode) && typelessNode != behavior)
                    {
                        typelessNode.TypeLink.Add(behavior);
                        behavior.Parent.Add(typelessNode);
                    }
                }
            }
        }

        static readonly List<string> UpdateAbilities = new List<string>
        {
            "updatebesttarget", "updatebestalertdata",
            "settargetstack", "setpotentialtargetstack", "setalertdatastack",
            "setnexttarget", "setnextalertdata"
        };

        private void AnalysisError(Behavior behavior, string error)
        {
            Errors.Add(string.Format("{0} - {1} [{2}] : {3}",
                behavior.FileName,
                behavior.OriginalLineNumber + 1,
                behavior.BehaviorName,
                error));
        }

        private BehaviorTree BT;

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
                    if (behavior.TypeLink.Contains(btPath.Path[index + 1]))
                        continue;

                    switch (behavior.NodeType.ToLowerInvariant())
                    {
                        case "selector":
                            foreach (var child in behavior.ChildLink)
                            {
                                if (child == btPath.Path[index + 1])
                                    break;

                                ShouldShowHandleSequence(child, knownFalse, knownTrue);
                            }
                            break;

                        case "sequence":
                            foreach (var child in behavior.ChildLink)
                            {
                                if (child == btPath.Path[index + 1])
                                    break;

                                ShouldShowHandleSequence(child, knownTrue, knownFalse);
                            }
                            break;
                    }
                }
                else
                {
                    return ShouldShow(behavior, knownTrue, knownFalse, characterTemplate);
                }
            }

            return true;
        }

        private static bool ShouldShow(Behavior behavior, HashSet<string> knownTrue, HashSet<string> knownFalse, string characterTemplate)
        {
            if (behavior.TypeLink.Count > 0)
                return true;

            if (!ShouldShowHandleSequence(behavior, knownTrue, knownFalse)) return false;

            switch (behavior.NodeType.ToLowerInvariant())
            {
                case "selector":
                    foreach (var child in behavior.ChildLink)
                    {
                        var newChild = GetReplacementNode(child, characterTemplate) ?? child;

                        if (newChild.Annotations.Contains("HasAction"))
                            return true;

                        if (!ShouldShowHandleSequence(newChild, knownFalse, knownTrue)) return false;
                    }
                    break;

                case "sequence":
                    foreach (var child in behavior.ChildLink)
                    {
                        var newChild = GetReplacementNode(child, characterTemplate) ?? child;

                        if (newChild.Annotations.Contains("HasAction"))
                            return ShouldShow(child, knownTrue, knownFalse, characterTemplate);

                        if (!ShouldShowHandleSequence(newChild, knownTrue, knownFalse)) return false;
                    }
                    break;
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
            if (characterTemplate == null) 
                return null;
            
            if (nextBehavior.BehaviorName.StartsWith("::"))
            {
                return nextBehavior.TypeLink.FirstOrDefault(
                    b => b.Key.StartsWith(characterTemplate + "::"));
            }

            return null;
        }

        private static string GetCharacterTemplate(Behavior behavior)
        {
            return behavior.BehaviorName.Substring(0,
                behavior.BehaviorName.IndexOf("::", StringComparison.InvariantCulture)).ToLowerInvariant();
        }
    }
}
