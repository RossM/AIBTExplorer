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
        
        private Token[] lineTokens;
        private int currentTokenIndex;
        private int lineIndex;
        private int fileIndex;

        public struct FileData
        {
            public List<string> RawLines, OriginalLines;
            public List<int> OriginalLineNumbers; 
            public string FileName;
        }

        public struct Token
        {
            public string Text;
            public int StartCharNumber;
        }

        public BehaviorTree ReadData(IEnumerable<string> paths)
        {
            var cancelledLines = new List<string>();

            foreach (var path in paths)
            {
                var file = new FileData
                {
                    FileName = path,
                    RawLines = new List<string>(),
                    OriginalLines = new List<string>(),
                    OriginalLineNumbers = new List<int>()
                };

                int originalLineNumber = 0;

                using (var aiConfig = File.OpenText(path))
                {
                    while (!aiConfig.EndOfStream)
                    {
                        file.OriginalLineNumbers.Add(originalLineNumber);

                        var line = aiConfig.ReadLine();
                        var rawLine = line;
                        originalLineNumber++;

                        while (line != null && line.EndsWith(@"\\") && !aiConfig.EndOfStream)
                        {
                            line = line.Substring(0, line.Length - 2);
                            var newLine = aiConfig.ReadLine();
                            line += newLine;
                            rawLine += "\n" + newLine;
                            originalLineNumber++;
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
            for (fileIndex = 0; fileIndex < files.Count; fileIndex++)
            {
                var file = files[fileIndex];
                for (lineIndex = 0; lineIndex < file.RawLines.Count; lineIndex++)
                {
                    var rawLine = file.RawLines[lineIndex];
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
                        section = string.Join("", lineTokens.Select(t => t.Text));
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
                    node.OriginalLineNumber = file.OriginalLineNumbers[file.OriginalLines.Count - 1];
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
                return currentTokenIndex >= lineTokens.Length ? null : lineTokens[currentTokenIndex].Text;
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

        static IEnumerable<Token> LexConfig(string line)
        {
            string token = "";

            for (int lineChar = 0; lineChar < line.Length; )
            {
                int startChar = lineChar;
                if (line[lineChar] == ';')
                    yield break;
                else if (IsTokenChar(line[lineChar]))
                {
                    while (lineChar < line.Length && IsTokenChar(line[lineChar]))
                        token += line[lineChar++];
                    yield return new Token { Text = token, StartCharNumber = startChar };
                    token = "";
                }
                else if (line[lineChar] == '"')
                {
                    lineChar++;
                    while (lineChar < line.Length && line[lineChar] != '"')
                        token += line[lineChar++];
                    lineChar++;
                    yield return new Token { Text = token, StartCharNumber = startChar };
                    token = "";
                }
                else
                {
                    if (!char.IsWhiteSpace(line[lineChar]))
                        yield return new Token { Text = line[lineChar].ToString(), StartCharNumber = startChar };
                    lineChar++;
                }
            }
        }

    }
}
