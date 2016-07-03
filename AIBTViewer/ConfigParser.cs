using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIBTViewer;

namespace AIBTViewer
{
    public class ConfigParser
    {
        public Dictionary<string, Behavior> BT = new Dictionary<string, Behavior>();
        public List<FileData> files = new List<FileData>();
        
        private string[] lineTokens;
        private int currentTokenIndex;

        public struct FileData
        {
            public List<string> RawLines, OriginalLines;
            public string FileName;
        }

        public BehaviorTree ReadData(IEnumerable<string> paths)
        {
            var cancelledLines = new List<string>();

            foreach (var path in paths)
            {
                var file = new FileData { FileName = path, RawLines = new List<string>(), OriginalLines = new List<string>() };

                using (var aiConfig = File.OpenText(path))
                {
                    while (!aiConfig.EndOfStream)
                    {
                        var line = aiConfig.ReadLine();
                        var rawLine = line;

                        while (line != null && line.EndsWith(@"\\") && !aiConfig.EndOfStream)
                        {
                            line = line.Substring(0, line.Length - 2);
                            var newLine = aiConfig.ReadLine();
                            line += newLine;
                            rawLine += "\n" + newLine;
                        }

                        file.RawLines.Add(rawLine);
                    }
                }

                files.Add(file);
            }

            foreach (var file in files)
            {
                foreach (var rawLine in file.RawLines)
                {
                    if (rawLine.StartsWith("-"))
                    {
                        cancelledLines.Add(rawLine.Substring(1));
                    }
                }
            }

            string section = "";
            foreach (var file in files)
            {
                foreach (var rawLine in file.RawLines)
                {
                    var line = rawLine.Replace("\\\\\n", "");

                    file.OriginalLines.Add(rawLine);

                    if (line.StartsWith("-"))
                        continue;
                    if (line.StartsWith("+"))
                        line = line.Substring(1);

                    if (cancelledLines.Contains(line))
                        continue;

                    lineTokens = LexConfig(line).ToArray();
                    currentTokenIndex = 0;

                    if (lineTokens.Length == 0)
                        continue;

                    if (CurrentToken == "[")
                    {
                        section = string.Join("", lineTokens);
                        continue;
                    }

                    if (!String.Equals(section, "[XComGame.X2AIBTBehaviorTree]",
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (CurrentToken.ToLowerInvariant() != "behaviors") 
                        continue;

                    var node = new Behavior();

                    EatToken("behaviors");
                    EatToken("=");
                    EatToken("(");

                    while (currentTokenIndex < lineTokens.Length)
                    {
                        int index;
                        switch (CurrentToken.ToLowerInvariant())
                        {
                            case "behaviorname":
                                EatToken();
                                EatToken("=");

                                node.BehaviorName = CurrentToken;
                                EatToken();

                                break;

                            case "nodetype":
                                EatToken();
                                EatToken("=");

                                node.NodeType = CurrentToken;
                                EatToken();

                                break;

                            case "child":
                                EatToken();
                                EatToken("[");
                                
                                index = int.Parse(CurrentToken);
                                EatToken();

                                while (node.Child.Count < index + 1)
                                    node.Child.Add("");
                                
                                EatToken("]");
                                EatToken("=");

                                node.Child[index] = CurrentToken;
                                EatToken();

                                break;

                            case "param":
                                EatToken();
                                EatToken("[");

                                index = int.Parse(CurrentToken);
                                EatToken();

                                while (node.Param.Count < index + 1)
                                    node.Param.Add("");
                                
                                EatToken("]");
                                EatToken("=");

                                node.Param[index] = CurrentToken;
                                EatToken();
                                
                                break;

                            default:
                                // Unexpected token!
                                EatToken();
                                break;
                        }

                        if (CurrentToken == ")")
                        {
                            EatToken();
                            break;
                        }

                        EatToken(",", ")");

                        // Allow an optional "," before the closing ")"
                        if (CurrentToken == ")")
                        {
                            EatToken();
                            break;
                        }
                    }

                    node.RawText = rawLine;
                    node.FileName = file.FileName;
                    file.OriginalLines[file.OriginalLines.Count - 1] = string.Format("%[{0}]", node.BehaviorName);

                    BT[node.Key] = node;
                }
            }

            return new BehaviorTree(BT);
        }

        string CurrentToken
        {
            get
            {
                return currentTokenIndex >= lineTokens.Length ? null : lineTokens[currentTokenIndex];
            }
        }

        void EatToken()
        {
            currentTokenIndex++;
        }

        void EatToken(params string[] expected)
        {
            currentTokenIndex++;
        }

        static bool IsTokenChar(char c)
        {
            if (char.IsWhiteSpace(c)) return false;
            if (";[]=,()\"".Contains(c)) return false;
            return true;
        }

        static IEnumerable<String> LexConfig(string line)
        {
            string token = "";

            for (int i = 0; i < line.Length; )
            {
                if (line[i] == ';')
                    yield break;
                else if (IsTokenChar(line[i]))
                {
                    while (i < line.Length && IsTokenChar(line[i]))
                        token += line[i++];
                    yield return token;
                    token = "";
                }
                else if (line[i] == '"')
                {
                    i++;
                    while (i < line.Length && line[i] != '"')
                        token += line[i++];
                    i++;
                    yield return token;
                    token = "";
                }
                else
                {
                    if (!char.IsWhiteSpace(line[i]))
                        yield return line[i].ToString();
                    i++;
                }
            }
        }

    }
}
