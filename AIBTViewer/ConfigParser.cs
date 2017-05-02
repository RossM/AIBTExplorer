using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIBTViewer;

namespace AIBTViewer
{
    public class ConfigParser
    {
        public Dictionary<string, Behavior> BT = new Dictionary<string, Behavior>();
        public List<FileData> files = new List<FileData>();

        public List<string> Errors = new List<string>(); 
        
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
            public int StartCharNumber, StartLineNumber;
        }

        public BehaviorTree ReadData(IEnumerable<string> paths)
        {
            var cancelledLines = new List<string>();
            var removedBehaviors = new List<string>();
            var newBehaviors = new Dictionary<string, Behavior>();

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

                try
                {
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
                catch (System.IO.IOException)
                {
                    // Ignore
                }
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
                    var line = rawLine;

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

                    string property = CurrentToken.ToLowerInvariant();
                    Behavior node;
                    switch (property)
                    {
                        case "behaviors":
                            if (!String.Equals(section, "[XComGame.X2AIBTBehaviorTree]",
                                StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }

                            EatToken("behaviors");
                            EatToken("=");

                            node = ParseBehavior();

                            node.RawText = rawLine;
                            node.FileName = file.FileName;
                            node.OriginalLineNumber = file.OriginalLineNumbers[file.OriginalLines.Count - 1];
                            file.OriginalLines[file.OriginalLines.Count - 1] = string.Format("%[{0}]", node.BehaviorName);

                            if (node.BehaviorName != null)
                                BT[node.Key] = node;
                            break;

                        case "newbehaviors":
                            if (!String.Equals(section, "[LW_Overhaul.UIScreenListener_Shell]",
                                StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }

                            EatToken("newbehaviors");
                            EatToken("=");

                            node = ParseBehavior();

                            node.RawText = rawLine;
                            node.FileName = file.FileName;
                            node.OriginalLineNumber = file.OriginalLineNumbers[file.OriginalLines.Count - 1];
                            file.OriginalLines[file.OriginalLines.Count - 1] = string.Format("%[{0}]", node.BehaviorName);

                            if (node.BehaviorName != null)
                                newBehaviors[node.Key] = node;
                            break;

                        case "behaviorremovals":
                            if (!String.Equals(section, "[LW_Overhaul.UIScreenListener_Shell]",
                                StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }

                            EatToken("behaviorremovals");
                            EatToken("=");

                            removedBehaviors.Add(CurrentToken);
                            EatToken();
                            break;
                        
                        default:
                            continue;
                    }
                }
            }

            foreach (var behaviorName in removedBehaviors)
            {
                BT.Remove(behaviorName);
            }

            foreach (var newBehavior in newBehaviors)
            {
                BT[newBehavior.Key] = newBehavior.Value;
            }

            return new BehaviorTree(BT);
        }

        private Behavior ParseBehavior()
        {
            var node = new Behavior();

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

                    case "intent":
                        EatToken();
                        EatToken("=");
                        EatToken();

                        break;

                    default:
                        // Unexpected token!
                        ParseError("field name");
                        EatToken();

                        while (currentTokenIndex < lineTokens.Length && CurrentToken != "," && CurrentToken != ")")
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
            return node;
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
            if (currentTokenIndex >= lineTokens.Length)
            {
                ParseError(string.Format("\"{0}\"", expected[0]));
                return;
            }
            
            if (expected.All(s => !String.Equals(s, CurrentToken, StringComparison.InvariantCultureIgnoreCase)))
            {
                ParseError(string.Format("\"{0}\"", expected[0]));
            }

            currentTokenIndex++;
        }

        private void ParseError(string expected)
        {
            string prevToken = currentTokenIndex > 0
                ? string.Format("\"{0}\"", lineTokens[currentTokenIndex - 1].Text)
                : "beginning of line";
            string curToken = currentTokenIndex < lineTokens.Length
                ? string.Format("\"{0}\"", lineTokens[currentTokenIndex].Text)
                : "end of line";

            int startLineNumber;
            if (currentTokenIndex >= 0 && currentTokenIndex < lineTokens.Length)
                startLineNumber = lineTokens[currentTokenIndex].StartLineNumber;
            else if (currentTokenIndex >= 1)
                startLineNumber = lineTokens[currentTokenIndex - 1].StartLineNumber;
            else
                startLineNumber = files[fileIndex].OriginalLineNumbers[lineIndex];

            int startCharNumber;
            if (currentTokenIndex >= 0 && currentTokenIndex < lineTokens.Length)
                startCharNumber = lineTokens[currentTokenIndex].StartCharNumber;
            else if (currentTokenIndex >= 1)
                startCharNumber = lineTokens[currentTokenIndex - 1].StartCharNumber +
                                  lineTokens[currentTokenIndex - 1].Text.Length;
            else
                startCharNumber = 0;

            Errors.Add(string.Format("{0} - ({1},{2}) : Expected {3} after {4} but got {5} instead",
                files[fileIndex].FileName,
                startLineNumber + 1,
                startCharNumber + 1,
                expected,
                prevToken,
                curToken));
        }

        static bool IsTokenChar(char c)
        {
            if (char.IsWhiteSpace(c)) return false;
            if (";[]=,()\"".Contains(c)) return false;
            return true;
        }

        IEnumerable<Token> LexConfig(string line)
        {
            string token = "";

            int curLine = files[fileIndex].OriginalLineNumbers[lineIndex];
            int curChar = 0;

            for (int lineChar = 0; lineChar < line.Length; )
            {
                int startChar = curChar;
                int startLine = curLine;

                if (lineChar + 2 < line.Length && line[lineChar] == '\\' && line[lineChar + 1] == '\\' &&
                    line[lineChar + 2] == '\n')
                {
                    lineChar += 3;
                    curChar = 0;
                    curLine++;
                    continue;
                }
                
                if (line[lineChar] == ';')
                    yield break;
                
                if (IsTokenChar(line[lineChar]))
                {
                    while (lineChar < line.Length && IsTokenChar(line[lineChar]))
                    {
                        token += line[lineChar++];
                        curChar++;
                    }
                    yield return new Token { Text = token, StartCharNumber = startChar, StartLineNumber = startLine };
                    token = "";
                    continue;
                }
                
                if (line[lineChar] == '"')
                {
                    lineChar++;
                    curChar++;
                    while (lineChar < line.Length && line[lineChar] != '"')
                    {
                        token += line[lineChar++];
                        curChar++;
                    }
                    lineChar++;
                    curChar++;
                    yield return new Token { Text = token, StartCharNumber = startChar, StartLineNumber = startLine };
                    token = "";
                    continue;
                }

                if (!char.IsWhiteSpace(line[lineChar]))
                    yield return new Token { Text = line[lineChar].ToString(), StartCharNumber = startChar, StartLineNumber = startLine };
                lineChar++;
                curChar++;
            }
        }

    }
}
