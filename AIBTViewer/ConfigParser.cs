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
        public List<string> OriginalLines = new List<string>(); 

        public BehaviorTree ReadData()
        {
            using (
                var aiConfig =
                    File.OpenText(@"C:\Program Files (x86)\Steam\SteamApps\common\XCOM 2\XComGame\Config\DefaultAI.ini")
                )
            {
                string section = "";

                while (!aiConfig.EndOfStream)
                {
                    var line = aiConfig.ReadLine();
                    var rawLine = line;

                    while (line.EndsWith(@"\\") && !aiConfig.EndOfStream)
                    {
                        line = line.Substring(0, line.Length - 2);
                        var newLine = aiConfig.ReadLine();
                        line += newLine;
                        rawLine += "\n" + newLine;
                    }

                    var tokens = LexConfig(line).ToArray();
                    var tokensLower = tokens.Select(s => s.ToLowerInvariant()).ToArray();

                    if (tokens.Length == 0)
                    {
                        OriginalLines.Add(rawLine);
                        continue;
                    }

                    if (tokens[0] == "[")
                    {
                        OriginalLines.Add(rawLine);
                        section = string.Join("", tokens);
                        continue;
                    }

                    if (tokensLower[0] == "behaviors")
                    {
                        var node = new Behavior();

                        int i = 3;

                        while (i < tokens.Length)
                        {
                            int index;
                            switch (tokensLower[i])
                            {
                                case "behaviorname":
                                    node.BehaviorName = tokens[i + 2];
                                    i += 4;
                                    break;

                                case "nodetype":
                                    node.NodeType = tokens[i + 2];
                                    i += 4;
                                    break;

                                case "child":
                                    index = int.Parse(tokens[i + 2]);
                                    while (node.Child.Count < index + 1)
                                        node.Child.Add("");
                                    node.Child[index] = tokens[i + 5];
                                    i += 7;
                                    break;

                                case "param":
                                    index = int.Parse(tokens[i + 2]);
                                    while (node.Param.Count < index + 1)
                                        node.Param.Add("");
                                    node.Param[index] = tokens[i + 5];
                                    i += 7;
                                    break;

                                default:
                                    i++;
                                    break;
                            }
                        }

                        node.RawText = rawLine;
                        OriginalLines.Add(string.Format("%{0}", node.BehaviorName));

                        BT[node.BehaviorName.ToLowerInvariant()] = node;
                    }
                }
            }

            return new BehaviorTree(BT);
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
