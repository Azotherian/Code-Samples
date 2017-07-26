using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace cs4490
{
    class Program
    {
        static void Main(string[] args)
        {
            Lexer lexicalAnalyser = new Lexer();
            lexicalAnalyser.startStream(args[0]);
            Syntaxer syntaxAnalyser = new Syntaxer(lexicalAnalyser);
            try
            {
                syntaxAnalyser.startSyntax(false);
                lexicalAnalyser.startStream(args[0]);
                syntaxAnalyser.restartSyntax();
                syntaxAnalyser.startSyntax(true);
                syntaxAnalyser.setTempOffsets();
                syntaxAnalyser.printSymbolTable();
                //syntaxAnalyser.printQuads();
                //syntaxAnalyser.printStacks();

                //grab symbol table and quad array for tcode
                Dictionary<string, Symbol> symTable = syntaxAnalyser.getSymTable();
                List<Quad> quadArray = syntaxAnalyser.getQuadArray();
                TcodeGen tcodeGenerator = new TcodeGen(symTable, quadArray);

                //start tcode generation
                //tcodeGenerator.generateTCode();
            }
            catch (SyntaxException error) { }
            catch (SemanticException error) { }
            finally
            {
                lexicalAnalyser.closeStream();
            }
            Console.Read();
        }
    }
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base(message)
        {
        }
    }
    public class SemanticException : Exception
    {
        public SemanticException(string message) : base(message)
        {
        }
    }
    public class Token
    {
        private int line_number;
        private string type;
        private string lexeme;
        private string line;

        public Token()
        {
            line_number = 0;
            type = "";
            lexeme = "";
            line = "";
        }
        public Token(int lineNum, string tokType, string tokLex, string fileLine)
        {
            line_number = lineNum;
            type = tokType;
            lexeme = tokLex;
            line = fileLine;
        }
        public int getLineNum()
        {
            return line_number;
        }
        public string getLexeme()
        {
            return lexeme;
        }
        public string getType()
        {
            return type;
        }
        public void setType(string tokType)
        {
            type = tokType;
        }
        public void setLexeme(string tokLex)
        {
            lexeme = tokLex;
        }
        public void setLineNumber(int num)
        {
            line_number = num;
        }
        public string getLine()
        {
            line = Regex.Replace(line, @"^\s+", "");
            line = line.TrimEnd(';');
            return line;
        }
    }
    public class Symbol
    {
        static private int currentAvailableSymNum = 0;
        static private int currentAvailableTempNum = 0;
        private string symID;
        private string scope;
        private string kind;
        private string value;
        private Dictionary<string, string> data;

        public Symbol()
        {
            symID = "";
            scope = "";
            kind = "";
            value = "";
            data = new Dictionary<string, string>();
        }
        public Symbol(string id, string scope, string kind, string value, Dictionary<string, string> data)
        {
            symID = id;
            this.scope = scope;
            this.kind = kind;
            this.value = value;
            this.data = data;
        }
        public void setID(string ID)
        {
            symID = ID;
        }
        public void setScope(string scope)
        {
            this.scope = scope;
        }
        public void setKind(string kind)
        {
            this.kind = kind;
        }
        public void setValue(string value)
        {
            this.value = value;
        }
        public void setData(Dictionary<string, string> input)
        {
            data = input;
        }
        public string getID()
        {
            return symID;
        }
        public string getScope()
        {
            return scope;
        }
        public string getKind()
        {
            return kind;
        }
        public string getValue()
        {
            return value;
        }
        public Dictionary<string, string> getData()
        {
            return data;
        }
        public int getNextNumber()
        {
            return ++currentAvailableSymNum;
        }
        public int getNextTempNumber()
        {
            return ++currentAvailableTempNum;
        }
        public void clearSymbol()
        {
            symID = "";
            scope = "";
            kind = "";
            value = "";
            data = new Dictionary<string, string>();
        }
        public Symbol Clone()
        {
            Symbol returnSym = new Symbol();
            returnSym.setScope(scope);
            returnSym.setID(symID);
            returnSym.setValue(value);
            returnSym.setKind(kind);
            returnSym.setData(data);
            return returnSym;
        }
    }
    public class Lexer
    {
        //regex strings
        Regex number_Regex = new Regex(@"^[0-9]+");
        Regex character_Regex = new Regex(@"^'[^']{0,2}'");
        Regex ident_keyword_Regex = new Regex(@"^[a-zA-Z][a-zA-Z0-9]*");
        Regex punctuation_Regex = new Regex(@"^[,;.]");
        Regex math_Regex = new Regex(@"^[\+\*\/-]");
        Regex relational_Regex = new Regex(@"^==|^!=|^<=|^>=|^<|^>");
        Regex boolean_Regex = new Regex(@"^and|^or");
        Regex assignment_Regex = new Regex(@"^=");
        Regex array_Regex = new Regex(@"^\[|^\]");
        Regex block_Regex = new Regex(@"^{|^}");
        Regex parenthesis_Regex = new Regex(@"^\(|^\)");
        Regex comment_Regex = new Regex(@"^\/\/");
        Regex whitespace_Regex = new Regex(@"^\s");
        Regex stream_Regex = new Regex(@"^>>|^<<");
        Regex set_Regex = new Regex(@"^set");

        List<Token> lineTokens = new List<Token>();
        List<string> keywords = new List<string> { "atoi", "bool", "class", "char", "cin", "cout", "else", "false", "if", "int", "itoa", "main", "new", "null", "object", "public", "private", "return", "string", "this", "true", "void", "while", "spawn", "lock", "release", "block", "sym", "kxi2017", "protected", "unprotected", "and", "or" };
        Token currentToken = new Token();
        Token nextToken = new Token();
        static string tokenLine = "";
        int lineNum;
        StreamReader myStream;
        string fileLine = "";
        int match_length = 0;
        public Lexer() { }
        public void startStream(string fileName)
        {
            myStream = new StreamReader(fileName);
            fileLine = myStream.ReadLine();
            tokenLine = fileLine;
            lineNum = 1;
            parseTokens(fileLine);
            getStartingTokens();
        }
        public void closeStream()
        {
            myStream.Close();
        }
        void getStartingTokens()
        {
            if (lineTokens.Count > 0)//if there are tokens to get
            {
                currentToken = lineTokens[0];
                lineTokens.RemoveAt(0);
            }
            if (lineTokens.Count > 0)//still have more tokens in the buffer
            {
                nextToken = lineTokens[0];
                lineTokens.RemoveAt(0);
            }
            else
            {
                getNextline();
                getStartingTokens();
            }
        }
        public bool readMore()
        {
            int next_line = myStream.Peek();
            if(next_line > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void getLastTokens()
        {
            currentToken = nextToken;
        }
        public void getNextline()
        {
            fileLine = myStream.ReadLine();
            if (fileLine != null)
            {
                tokenLine = fileLine;
                lineNum++;
                parseTokens(fileLine);
            }
        }
        public void parseTokens(string line)
        {
            Token tempTok = new Token();
            while (line != "")
            {
                //run each regex match for 1 possible match
                Match number_match = number_Regex.Match(line);
                Match character_match = character_Regex.Match(line);
                Match ident_keyword_match = ident_keyword_Regex.Match(line);
                Match punctuation_match = punctuation_Regex.Match(line);
                Match stream_match = stream_Regex.Match(line);
                Match math_match = math_Regex.Match(line);
                Match relational_match = relational_Regex.Match(line);
                Match assignment_match = assignment_Regex.Match(line);
                Match array_match = array_Regex.Match(line);
                Match block_match = block_Regex.Match(line);
                Match parenthesis_match = parenthesis_Regex.Match(line);
                Match whitespace_match = whitespace_Regex.Match(line);
                Match comment_match = comment_Regex.Match(line);
                Match set_match = set_Regex.Match(line);

                //check if a match came through, if not then unknown
                if (number_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "number", number_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = number_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (character_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "character", character_match.Value,tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = character_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (ident_keyword_match.Success)
                {

                    bool keyword = false;
                    //check if match is identifier or keyword
                    foreach (string value in keywords)
                    {
                        if (ident_keyword_match.Value == value)
                        {
                            keyword = true;
                        }
                    }
                    if (keyword == true)
                    {
                        //create token
                        tempTok = new Token(lineNum, "keyword", ident_keyword_match.Value,tokenLine);
                        lineTokens.Add(tempTok);
                        //consume match from string
                        match_length = ident_keyword_match.Value.Length;
                        line = line.Remove(0, match_length);
                    }
                    else
                    {
                        //create token
                        tempTok = new Token(lineNum, "identifier", ident_keyword_match.Value, tokenLine);
                        lineTokens.Add(tempTok);
                        //consume match from string
                        match_length = ident_keyword_match.Value.Length;
                        line = line.Remove(0, match_length);
                    }
                }
                else if (punctuation_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "punctuation", punctuation_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = punctuation_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (comment_match.Success)
                {
                    //throw away rest of the line
                    line = "";
                    getNextline();
                }
                else if (math_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", math_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = math_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (stream_match.Success)
                {
                    tempTok = new Token(lineNum, "stream op", stream_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = stream_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (relational_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", relational_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = relational_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (assignment_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", assignment_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = assignment_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (array_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", array_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = array_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (block_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", block_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = block_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (parenthesis_match.Success)
                {
                    //create token
                    tempTok = new Token(lineNum, "symbol", parenthesis_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = parenthesis_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else if (whitespace_match.Success)
                {
                    //ignore and remove from front of string
                    line = line.Remove(0, 1);
                }
                else if (set_match.Success)
                {
                    tempTok = new Token(lineNum, "set", set_match.Value, tokenLine);
                    lineTokens.Add(tempTok);
                    //consume match from string
                    match_length = set_match.Value.Length;
                    line = line.Remove(0, match_length);
                }
                else//unknown character
                {
                    //consume first character and keep going with parsing
                    char first_char = line[0];
                    tempTok = new Token(lineNum, "unknown", first_char.ToString(), tokenLine);
                    lineTokens.Add(tempTok);
                    line = line.Remove(0, 1);
                }
            }
        }
        public Token getToken()
        {
            return currentToken;
        }
        public Token peekToken()
        {
            return nextToken;
        }
        public void getNextToken()
        {
            currentToken = nextToken;
            if (lineTokens.Count > 0)//still have more tokens in the buffer
            {
                nextToken = lineTokens[0];
                lineTokens.RemoveAt(0);
            }
            else
            {
                getNextline();
                if (lineTokens.Count > 0)//still have more tokens in the buffer
                {
                    nextToken = lineTokens[0];
                    lineTokens.RemoveAt(0);
                }
                else
                {
                    if (readMore())
                    {
                        getNextline();
                        getNextToken();
                    }
                    else
                    {
                        nextToken = new Token(lineNum, "EOF", "EOF", "");
                    }
                }
            }
        }
    }
    public class Syntaxer
    {
        Lexer syntaxLexer = new Lexer();
        Token tokenToPrint = new Token();
        Token tokenNextToPrint = new Token();
        List<string> types = new List<string> { "int", "char", "bool", "void", "sym" };
        List<string> classTypes = new List<string>() { };
        List<string> dupList = new List<string>() { };
        List<string> modifiers = new List<string> { "public", "private" };
        List<string> currentScope = new List<string> { "g" };
        List<Quad> quadArray = new List<Quad>();
        List<Quad> squadArray = new List<Quad>();
        static bool toSquad = false;
        static string squadFrame = "";
        static string squadID = "";
        static string comment_line = "";
        static string main_id = "";
        string main_sym_id = "";
        static bool negate_num = false;
        static bool first_main_quad = false;
        static int class_size = 4;
        static int func_size = 0;
        static int main_size = 0;
        static string intSize = "";
        static string boolSize = "";
        static string main_name = "";
        static string class_mem_id = "";
        static string class_id = "";
        static string func_id = "";
        static string class_mem_type = "";
        static string parent_Param = "";
        static string parent_name = "";
        static string symType = "";
        static Symbol newSymbol = new Symbol();
        static string quadLabel = "";
        static Quad tempQuad;
        static bool secondPass;
        static bool hasReturn;
        static bool constructDec = false;
        static Dictionary<string, Symbol> symbolTable = new Dictionary<string, Symbol>();
        static Dictionary<string, string> symbolData = new Dictionary<string, string>();
        static Stack<SAR> SAS = new Stack<SAR>();
        static Stack<OPR> OpStack = new Stack<OPR>();
        static Stack<string> IfStack = new Stack<string>();
        static Stack<string> ElseStack = new Stack<string>();
        static Stack<string> BeginWhileStack = new Stack<string>();
        static Stack<string> EndWhileStack = new Stack<string>();

        public Syntaxer() { }
        public void setTempOffsets()
        {
            Dictionary<string, string> offsets = new Dictionary<string, string>();
            Dictionary<string, string> final_offsets = new Dictionary<string, string>();
            string current_offset = "";
            foreach(KeyValuePair<string, Symbol> entry in symbolTable)
            {
                if (entry.Value.getID().StartsWith("t") || entry.Value.getID().StartsWith("r")){}
                else
                {
                    if (entry.Value.getScope() == "g")
                    {
                        if (!entry.Value.getID().StartsWith("N"))
                        {
                            if (!entry.Value.getID().StartsWith("H"))
                            {
                                if (!entry.Value.getID().StartsWith("O"))
                                {
                                    if (!offsets.ContainsKey(entry.Value.getScope()))
                                    {
                                        if (entry.Value.getData().ContainsKey("size"))
                                        {
                                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["size"]);
                                        }
                                        else if (entry.Value.getData().ContainsKey("offset"))
                                        {
                                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["offset"]);
                                        }
                                    }
                                    else if (entry.Value.getScope().EndsWith(entry.Value.getValue()))
                                    {
                                        if (entry.Value.getData().ContainsKey("size"))
                                        {
                                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["size"]);
                                        }
                                        else if (entry.Value.getData().ContainsKey("offset"))
                                        {
                                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["offset"]);
                                        }
                                    }
                                    else if (entry.Value.getID().StartsWith("F") || entry.Value.getID().StartsWith("M"))
                                    {
                                        offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["size"]);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!offsets.ContainsKey(entry.Value.getScope()))
                        {
                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), "");
                        }
                        else if (entry.Value.getID().StartsWith("M")|| entry.Value.getID().StartsWith("X"))
                        {
                            offsets.Add(entry.Value.getScope() + "." + entry.Value.getValue(), entry.Value.getData()["size"]);
                        }
                    }
                }
            }     
            foreach (KeyValuePair<string, string> scope in offsets)
            {
                int scope_offset = int.Parse(scope.Value);
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    if(entry.Value.getScope() == scope.Key)
                    {
                        if (entry.Value.getID().StartsWith("t")||entry.Value.getID().StartsWith("r"))
                        {
                            Dictionary<string, string> tempData = symbolTable[entry.Value.getID()].getData();
                            tempData.Add("offset", scope_offset.ToString());
                            entry.Value.setData(tempData);
                            scope_offset += 4;
                        }
                    }
                }
                final_offsets.Add(scope.Key,scope_offset.ToString());
            }
            //add final offset back to function/class
            foreach(KeyValuePair<string, string> scope in final_offsets)
            {
                string[] newScope = scope.Key.Split('.');
                string scope_val = newScope[newScope.Count() - 1];
                newScope = newScope.Take(newScope.Length - 1).ToArray();
                string offsetScope = String.Join(".", newScope);
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    if(entry.Value.getScope() == offsetScope)
                    {
                        if(entry.Value.getValue() == scope_val)
                        {
                            string final_size = final_offsets[scope.Key];
                            Dictionary<string, string> tempData = symbolTable[entry.Value.getID()].getData();
                            tempData.Remove("size");
                            tempData.Add("size", final_size);
                            symbolTable[entry.Value.getID()].setData(tempData);
                        }
                    }
                }
            }
        }
        public void addLabel(string label)
        {
            if (!string.IsNullOrEmpty(tempQuad.getLabel()))
            {
                backpatcher(tempQuad.getLabel(), label);
            }
            tempQuad.setLabel(label);
        }
        public void backpatcher(string oldLabel, string newLabel)
        {
            var quadsToBP = quadArray.Where(q => q.getLOp() == oldLabel || q.getROp() == oldLabel || q.getLastOp() == oldLabel);
            foreach (Quad quad in quadsToBP)
            {
                if(quad.getLOp() == oldLabel)
                {
                    quad.setLOp(newLabel);
                }
                if (quad.getROp() == oldLabel)
                {
                    quad.setROp(newLabel);
                }
                if (quad.getLastOp() == oldLabel)
                {
                    quad.setLastOp(newLabel);
                }
                quad.setComment(quad.getComment().Replace(oldLabel, newLabel));
            }
        }
        public void addQuad(string opcode, string Lop, string Rop, string LastOp, string comment)
        {
            tempQuad = new Quad(tempQuad?.getLabel(),opcode, Lop, Rop, LastOp, comment);
            quadArray.Add(tempQuad);
            tempQuad = new Quad("","","","","");
        }
        public void printSymbolTable()
        {
            using (StreamWriter sw = new StreamWriter("symtable.txt"))
            {
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    sw.WriteLine("Scope: " + entry.Value.getScope());
                    sw.WriteLine("SymID: " + entry.Value.getID());
                    sw.WriteLine("Value: " + entry.Value.getValue());
                    sw.WriteLine("Kind: " + entry.Value.getKind());
                    sw.Write("Data: ");
                    Dictionary<string, string> tempData = entry.Value.getData();
                    foreach (KeyValuePair<string, string> data in tempData)
                    {
                        sw.WriteLine("\t" + data.Key + ": " + data.Value);
                    }
                    sw.WriteLine("\n");
                }
            }
        }
        public void printQuads()
        {
            using (StreamWriter sw = new StreamWriter("quads.txt"))
            {
                foreach (Quad entry in quadArray)
                {
                    sw.WriteLine(entry.getLabel() + "\t" + entry.getOPCode() + "\t" + entry.getLOp() + "\t" + entry.getROp() + "\t" + entry.getLastOp() + "\t" + entry.getComment());
                }
            }
        }
        public void printStacks()
        {
            Console.WriteLine("If Stack:");
            foreach(string item in IfStack)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("Else Stack:");
            foreach(string item in ElseStack)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("While Stack:");
            /*foreach(string item in WhileStack)
            {
                Console.WriteLine(item);
            }*/
        }
        public List<Quad> getQuadArray()
        {
            return quadArray;
        }
        public Dictionary<string,Symbol> getSymTable()
        {
            return symbolTable;
        }
        public Syntaxer(Lexer lexAnalyzer)
        {
            syntaxLexer = lexAnalyzer;
            tokenToPrint = syntaxLexer.getToken();
            tokenNextToPrint = syntaxLexer.peekToken();
        }
        public void startSyntax(bool second_pass)
        {
            if (second_pass)
            {
                secondPass = true;
                main_name = "";
            }
            while (tokenToPrint.getType() != "EOF")
            {
                compiliation_unit();
            }
        }
        public void restartSyntax()
        {
            tokenToPrint = syntaxLexer.getToken();
            tokenNextToPrint = syntaxLexer.peekToken();
            currentScope.Clear();
            currentScope.Add("g");
        }
        public void compiliation_unit()
        {
            if (secondPass)
            {
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    if (entry.Value.getScope() == "g")
                    {
                        if (entry.Value.getValue() == "main")
                        {
                            main_id = entry.Value.getID();
                        }
                    }
                }
                addQuad("FRAME", main_id, "null", "", ";void kxi2017 main()");
                addQuad("CALL", main_id, "", "", "");
                addQuad("END", "", "", "", "");

                newSymbol.setScope("g");
                newSymbol.setID("N" + newSymbol.getNextNumber().ToString());
                intSize = newSymbol.getID();
                newSymbol.setValue("4");
                newSymbol.setKind("ilit");
                symbolData.Add("type", "int");
                symbolData.Add("accessMod", "public");
                symbolData.Add("offset", "0");
                newSymbol.setData(symbolData);
                symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                symbolData = new Dictionary<string, string>();
                newSymbol.clearSymbol();

                newSymbol.setScope("g");
                newSymbol.setID("N" + newSymbol.getNextNumber().ToString());
                boolSize = newSymbol.getID();
                newSymbol.setValue("1");
                newSymbol.setKind("ilit");
                symbolData.Add("type", "int");
                symbolData.Add("accessMod", "public");
                symbolData.Add("offset", "0");
                newSymbol.setData(symbolData);
                symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                symbolData = new Dictionary<string, string>();
                newSymbol.clearSymbol();

                newSymbol.setScope("g");
                newSymbol.setID("N" + newSymbol.getNextNumber().ToString());
                boolSize = newSymbol.getID();
                newSymbol.setValue("0");
                newSymbol.setKind("ilit");
                symbolData.Add("type", "int");
                symbolData.Add("accessMod", "public");
                symbolData.Add("offset", "0");
                newSymbol.setData(symbolData);
                symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                symbolData = new Dictionary<string, string>();
                newSymbol.clearSymbol();
                add_over_under_flow();
            }
            if (isClass_declaration() == true)
            {
                while (isClass_declaration() == true)
                {
                    class_declaration();
                }
            }
            if (tokenToPrint.getLexeme() == "void")
            {
                if (!secondPass)
                {
                    newSymbol.setScope(String.Join(".", currentScope));
                    symbolData.Add("type", tokenToPrint.getLexeme());
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (tokenToPrint.getLexeme() == "kxi2017")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (tokenToPrint.getLexeme() == "main")
                    {
                        if (secondPass)
                        {
                            addLabel(main_id);
                            addQuad("FUNC", main_id, "", "", "");
                            main_id = "";
                        }
                        first_main_quad = true;
                        main_name = tokenToPrint.getLexeme();
                        currentScope.Add(tokenToPrint.getLexeme());
                        if (!secondPass)
                        {
                            newSymbol.setKind(tokenToPrint.getLexeme());
                            newSymbol.setValue(tokenToPrint.getLexeme());
                            newSymbol.setID("F" + newSymbol.getNextNumber().ToString());
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        if (tokenToPrint.getLexeme() == "(")
                        {
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            if (tokenToPrint.getLexeme() == ")")
                            {
                                if (!secondPass)
                                {
                                    symbolData.Add("Params", "");
                                    symbolData.Add("accessMod", "public");
                                    main_sym_id = newSymbol.getID();
                                    newSymbol.setData(symbolData);
                                    //Adding to symbol table
                                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                                    symbolData = new Dictionary<string, string>();
                                    newSymbol.clearSymbol();
                                }
                                syntaxLexer.getNextToken();
                                tokenToPrint = syntaxLexer.getToken();
                                tokenNextToPrint = syntaxLexer.peekToken();
                                if (isMethod_body() == true)
                                {
                                    method_body();
                                    if (!secondPass)
                                    {
                                        Dictionary<string, string> main_data = symbolTable[main_sym_id].getData();
                                        main_data.Add("size", main_size.ToString());
                                        symbolTable[main_sym_id].setData(main_data);
                                    }
                                }
                                else
                                {
                                    printError(tokenToPrint.getLexeme(), "method_body", tokenToPrint.getLineNum());
                                }
                            }
                            else
                            {
                                printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "main", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "kxi2017", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "void", tokenToPrint.getLineNum());
            }
        }
        public void class_declaration()
        {
            class_size = 0;
            if (tokenToPrint.getLexeme() == "class")
            {
                if (!secondPass)
                {
                    newSymbol.setScope(String.Join(".", currentScope));
                    newSymbol.setKind(tokenToPrint.getLexeme());
                    newSymbol.setID("C" + newSymbol.getNextNumber().ToString());
                    class_id = newSymbol.getID();
                    classTypes.Add(tokenNextToPrint.getLexeme());
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (tokenToPrint.getType() == "identifier")
                {
                    currentScope.Add(tokenToPrint.getLexeme());
                    if (secondPass)
                    {
                        dup(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope), "class");
                    }
                    else
                    {
                        newSymbol.setValue(tokenToPrint.getLexeme());
                        symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                        newSymbol.clearSymbol();
                        symbolData = new Dictionary<string, string>();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (tokenToPrint.getLexeme() == "{")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        if (isClass_member_declaration() == true)
                        {
                            if (secondPass)
                            {
                                string lastScope = currentScope[currentScope.Count() - 1];
                                squadFrame = lastScope + "_squad";
                                newSymbol.setID("X" + newSymbol.getNextNumber().ToString());
                                newSymbol.setKind("Static Initializer");
                                newSymbol.setValue(squadFrame);
                                newSymbol.setScope(String.Join(".", currentScope));
                                symbolData.Add("type", tokenToPrint.getLexeme());
                                symbolData.Add("accessMod", "private");
                                symbolData.Add("Params", "");
                                symbolData.Add("size", "0");
                                newSymbol.setData(symbolData);
                                symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                                Quad classFrame = new Quad(newSymbol.getID(),"FUNC", newSymbol.getID(), "", "", ";" + squadFrame);
                                squadArray.Add(classFrame);
                                currentScope.Add(squadFrame);
                                squadID = newSymbol.getID();
                                toSquad = true;
                                newSymbol.clearSymbol();
                                symbolData = new Dictionary<string, string>();
                            }
                            while (isClass_member_declaration() == true)
                            {
                                class_member_declaration();
                            }
                        }
                        if (tokenToPrint.getLexeme() == "}")
                        {
                            if (!secondPass)
                            {
                                symbolTable[class_id].getData().Add("size", class_size.ToString());
                            }
                            else
                            {
                                squadID = "";
                                squadFrame = "";
                                toSquad = false;
                                Quad tempQuad = new Quad("", "RTN", "", "", "", "");
                                squadArray.Add(tempQuad);
                                //add all SQUADs to the QUADarray
                                foreach (Quad squad in squadArray)
                                {
                                    quadArray.Add(squad);
                                }
                                squadArray.Clear();
                                currentScope.RemoveAt(currentScope.Count - 1);//remove squad name
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            currentScope.RemoveAt(currentScope.Count - 1);//remove class name
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "identifier", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "{", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getType(), "class_name", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "class", tokenToPrint.getLineNum());
            }
        }
        public void class_member_declaration()
        {
            if (isModifier() == true)
            {
                if (!secondPass)
                {
                    symbolData.Add("accessMod", tokenToPrint.getLexeme());
                    newSymbol.setScope(String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                bool isCorrectType = false;
                foreach (string value in types)
                {
                    if (tokenToPrint.getLexeme() == value)
                    {
                        isCorrectType = true;
                        break;
                    }
                    if (tokenToPrint.getType() == "identifier")
                    {
                        isCorrectType = true;
                        break;
                    }
                }
                if (isCorrectType == true)
                {
                    if (secondPass)
                    {
                        tPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString());
                        tExist();
                    }
                    else
                    {
                        symbolData.Add("type", tokenToPrint.getLexeme());
                        class_mem_type = tokenToPrint.getLexeme();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (tokenToPrint.getType() == "identifier")
                    {
                        string Add_to_scope = tokenToPrint.getLexeme();
                        if (secondPass)
                        {
                            bool isMethod = (tokenNextToPrint.getLexeme() == "(");
                            dup(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope), isMethod?"method":"ivar");
                            currentScope.RemoveAt(currentScope.Count() - 1);
                            vPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                            currentScope.Add(squadFrame);
                        }
                        else
                        {
                            newSymbol.setValue(tokenToPrint.getLexeme());
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        if (isField_declaration() == true)
                        {
                            if (secondPass)
                            {
                                currentScope.Add(Add_to_scope);
                            }
                            field_declaration();
                            if (secondPass)
                            {
                                currentScope.RemoveAt(currentScope.Count - 1);
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "field declaration", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "identifier", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "proper type", tokenToPrint.getLineNum());
                }
            }
            else if (isConstructor_declaration() == true)
            {
                constructor_declaration();
            }
        }
        public void field_declaration()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                if (!secondPass)
                {
                    string tempType = symbolData["type"];
                    if (main_name != "main")
                    {
                        func_size = 0;
                    }
                    symbolData.Remove("type");
                    symbolData.Add("type", tempType);
                    symbolData.Add("Params", "");
                    newSymbol.setKind("method");
                    newSymbol.setID("M" + newSymbol.getNextNumber().ToString());
                    func_id = newSymbol.getID();
                    newSymbol.setData(symbolData);
                    parent_Param = newSymbol.getID();
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (isParameter_list() == true)
                {
                    if (!secondPass)
                    {
                        currentScope.Add(symbolTable[parent_Param].getValue());
                    }
                    parameter_list();
                    if (!secondPass)
                    {
                        currentScope.RemoveAt(currentScope.Count - 1);
                    }
                }
                if (tokenToPrint.getLexeme() == ")")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (isMethod_body() == true)
                    {
                        if (!secondPass)
                        {
                            currentScope.Add(symbolTable[parent_Param].getValue());
                        }
                        else
                        {
                            currentScope.RemoveAt(currentScope.Count() - 2);
                            toSquad = false;
                        }
                        method_body();
                        if (!secondPass)
                        {
                            symbolTable[func_id].getData().Add("size", func_size.ToString());
                            currentScope.RemoveAt(currentScope.Count - 1);
                        }
                        else
                        {
                            currentScope.Add(squadFrame);
                            toSquad = true;
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "method_body", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                if (!secondPass)
                {
                    newSymbol.setKind("ivar");
                    newSymbol.setID("V" + newSymbol.getNextNumber().ToString());
                    string current_type = symbolData["type"];
                    symbolData.Remove("type");
                    symbolData.Add("type", "@: " + current_type);
                    newSymbol.setData(symbolData);
                    class_mem_id = newSymbol.getID();
                    symbolData = new Dictionary<string, string>();
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (tokenToPrint.getLexeme() == "]")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (tokenToPrint.getLexeme() == "=")
                    {
                        if (secondPass)
                        {
                            oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),0, String.Join(".",currentScope));
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        if (isAssignmentExpression() == true)
                        {
                            assignment_expression();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "assignment_expression", tokenToPrint.getLineNum());
                        }
                    }
                    if (tokenToPrint.getLexeme() == ";")
                    {
                        if (!secondPass)
                        {
                            symbolData = symbolTable[class_mem_id].getData();
                            class_mem_type = symbolData["type"];
                            if (class_mem_type.Length > 4)
                            {
                                class_mem_type = class_mem_type.Remove(0, 3);
                            }
                            switch (class_mem_type)
                            {
                                case "int":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                case "char":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 1;
                                    break;
                                case "bool":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                case "sym":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (secondPass)
                        {
                            EOE();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "]", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "=")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), 0, String.Join(".", currentScope));
                }
                else
                {
                    newSymbol.setKind("ivar");
                    newSymbol.setID("V" + newSymbol.getNextNumber().ToString());
                    class_mem_id = newSymbol.getID();
                    newSymbol.setData(symbolData);
                    symbolData = new Dictionary<string, string>();
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (isAssignmentExpression() == true)
                {
                    assignment_expression();
                    if (tokenToPrint.getLexeme() == ";")
                    {
                        if (!secondPass)
                        {
                            symbolData = symbolTable[class_mem_id].getData();
                            class_mem_type = symbolData["type"];
                            if (class_mem_type.Length > 4)
                            {
                                class_mem_type = class_mem_type.Remove(0, 3);
                            }
                            switch (class_mem_type)
                            {
                                case "int":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                case "char":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 1;
                                    break;
                                case "bool":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                case "sym":
                                    symbolData.Add("offset", class_size.ToString());
                                    symbolTable[class_mem_id].setData(symbolData);
                                    symbolData = new Dictionary<string, string>();
                                    class_size += 4;
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (secondPass)
                        {
                            EOE();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "assignment_expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == ";")
            {
                if (!secondPass)
                {
                    newSymbol.setKind("ivar");
                    newSymbol.setID("V" + newSymbol.getNextNumber().ToString());
                    class_mem_id = newSymbol.getID();
                    class_mem_type = symbolData["type"];
                    if (class_mem_type.Length > 4)
                    {
                        class_mem_type = class_mem_type.Remove(0, 3);
                    }
                    switch (class_mem_type)
                    {
                        case "int":
                            symbolData.Add("offset", class_size.ToString());
                            class_size += 4;
                            break;
                        case "char":
                            symbolData.Add("offset", class_size.ToString());
                            class_size += 1;
                            break;
                        case "bool":
                            symbolData.Add("offset", class_size.ToString());
                            class_size += 4;
                            break;
                        case "sym":
                            symbolData.Add("offset", class_size.ToString());
                            class_size += 4;
                            break;
                        default:
                            break;
                    }
                    newSymbol.setData(symbolData);
                    symbolData = new Dictionary<string, string>();
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    newSymbol.clearSymbol();
                }
                else
                {
                    EOE();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "field declaration", tokenToPrint.getLineNum());
            }
        }
        public void constructor_declaration()
        {
            constructDec = true;
            if (tokenToPrint.getType() == "identifier")
            {
                func_size = 0;
                if (secondPass)
                {
                    dup(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope), "constructor");
                    CD(tokenToPrint.getLexeme(), String.Join(".", currentScope), tokenToPrint.getLineNum().ToString());
                    parent_name = tokenToPrint.getLexeme();
                    currentScope.RemoveAt(currentScope.Count() - 1);
                    string scope = String.Join(".",currentScope);
                    foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                    {
                        if(entry.Value.getScope() == scope)
                        {
                            if(entry.Value.getValue() == tokenToPrint.getLexeme())
                            {
                                addLabel(entry.Value.getID());
                                addQuad("FUNC", entry.Value.getID(), "", "", ";" + tokenToPrint.getLexeme() + "(){");
                            }
                        }
                    }
                }
                else
                {
                    newSymbol.setID("X" + newSymbol.getNextNumber().ToString());
                    func_id = newSymbol.getID();
                    newSymbol.setKind("Constructor");
                    newSymbol.setValue(tokenToPrint.getLexeme());
                    newSymbol.setScope(String.Join(".", currentScope));
                    parent_Param = newSymbol.getID();
                    parent_name = tokenToPrint.getLexeme();
                    symbolData.Add("type", tokenToPrint.getLexeme());
                    symbolData.Add("accessMod", "public");
                    symbolData.Add("Params", "");
                    newSymbol.setData(symbolData);
                    symbolData = new Dictionary<string, string>();
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (tokenToPrint.getLexeme() == "(")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (isParameter_list() == true)
                    {
                        if (!secondPass)
                        {
                            currentScope.Add(parent_name);
                        }
                        parameter_list();
                        if (!secondPass)
                        {
                            currentScope.RemoveAt(currentScope.Count - 1);
                        }
                    }
                    if (tokenToPrint.getLexeme() == ")")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        if (isMethod_body() == true)
                        {
                            if (!secondPass)
                            {
                                currentScope.Add(parent_Param);
                            }
                            else
                            {
                                currentScope.Add(parent_name);
                                toSquad = false;
                            }
                            method_body();
                            currentScope.RemoveAt(currentScope.Count - 1);
                            if (!secondPass)
                            {
                                symbolTable[func_id].getData().Add("size", func_size.ToString());
                            }
                            else
                            {
                                currentScope.Add(squadFrame);
                                toSquad = true;
                                addQuad("RTN", "", "", "", "");
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "method_body", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "class_name", tokenToPrint.getLineNum());
            }
        }
        public void method_body()
        {
            if (tokenToPrint.getLexeme() == "{")
            {
                if (secondPass)
                {
                    if(main_name != "main")
                    {
                        string current_func = currentScope[currentScope.Count - 1];
                        string current_class = currentScope[currentScope.Count - 2];
                        if(current_func != current_class)
                        {
                            writeFunc(current_func, current_class);
                        }
                    }
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                comment_line = tokenToPrint.getLine();
                tokenNextToPrint = syntaxLexer.peekToken();
                if (secondPass)
                {
                    if (constructDec == true)
                    {
                        addQuad("FRAME", squadID, "this", "", "");
                        first_main_quad = false;
                        addQuad("CALL", squadID, "", "", "");
                    }
                }
                if (isVariable_declaration() == true)
                {
                    while (isVariable_declaration() == true)
                    {
                        variable_declaration();
                    }
                }
                if (isStatement() == true)
                {
                    while (isStatement() == true)
                    {
                        statement();
                    }
                }
                if (tokenToPrint.getLexeme() == "}")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    comment_line = tokenToPrint.getLine();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    if (secondPass)
                    {
                        if (main_name != "main")
                        {
                            if (constructDec == false)
                            {
                                if (hasReturn == false)
                                {
                                    addQuad("RTN", "", "", "", "");
                                }
                                else
                                {
                                    hasReturn = false;
                                }
                            }
                            toSquad = true;
                            constructDec = false;
                        }
                        else
                        {
                            if (hasReturn == false)
                            {
                                addQuad("RTN", "", "", "", "");
                            }
                            else
                            {
                                hasReturn = false;
                            }
                        }
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "}", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "{", tokenToPrint.getLineNum());
            }
        }
        public void variable_declaration()
        {
            bool isCorrectType = false;
            foreach (string value in types)
            {
                if (tokenToPrint.getLexeme() == value)
                {
                    isCorrectType = true;
                    break;
                }
                if (tokenToPrint.getType() == "identifier")
                {
                    isCorrectType = true;
                    break;
                }
            }
            if (isCorrectType == true)
            {
                if (secondPass)
                {
                    tPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString());
                    tExist();
                }
                else
                {
                    symType = tokenToPrint.getLexeme();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getType() == "identifier")
                {
                    Token tempTok = tokenToPrint;
                    if (!secondPass)
                    {
                        newSymbol.setScope(String.Join(".", currentScope));
                        newSymbol.setID("L" + newSymbol.getNextNumber().ToString());
                        newSymbol.setValue(tokenToPrint.getLexeme());
                        newSymbol.setKind("lvar");
                        symbolData.Add("accessMod", "private");
                        if (tokenNextToPrint.getLexeme() == "[")
                        {
                            symbolData.Add("type", "@: " + symType);
                        }
                        else
                        {
                            symbolData.Add("type", symType);
                        }
                        if (main_name != "main")
                        {
                            symbolData.Add("offset", func_size.ToString());
                        }
                        else
                        {
                            symbolData.Add("offset", main_size.ToString());
                            if (symType == "int" || symType == "bool")
                            {
                                main_size += 4;
                            }
                            else if (symType == "char")
                            {
                                main_size += 4;
                            }
                        }
                        newSymbol.setData(symbolData);
                        symbolData = new Dictionary<string, string>();
                        symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                        newSymbol.clearSymbol();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (tokenToPrint.getLexeme() == "[")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (tokenToPrint.getLexeme() == "]")
                        {
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "]", tokenToPrint.getLineNum());
                        }
                    }
                    if (secondPass)
                    {
                        dup(tempTok.getLexeme(), tempTok.getLineNum().ToString(), "", String.Join(".", currentScope), "lvar");
                        vPush(tempTok.getLexeme(), tempTok.getLineNum().ToString(), String.Join(".", currentScope), "");
                    }
                    if (tokenToPrint.getLexeme() == "=")
                    {
                        if (secondPass)
                        {
                            oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), 0, String.Join(".", currentScope));
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (isAssignmentExpression() == true)
                        {
                            assignment_expression();
                        }
                    }
                    if (tokenToPrint.getLexeme() == ";")
                    {
                        if (main_name != "main")
                        {
                            func_size += 4;
                        }
                        if (secondPass)
                        {
                            EOE();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                    }
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "proper type", tokenToPrint.getLineNum());
            }
        }
        public void parameter_list()
        {
            if (isParameter() == true)
            {
                int param_offset = 0;
                parameter(param_offset);
                param_offset += 4;
                if (tokenToPrint.getLexeme() == ",")
                {
                    while (tokenToPrint.getLexeme() == ",")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (isParameter() == true)
                        {
                            parameter(param_offset);
                            param_offset += 4;
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "parameter", tokenToPrint.getLineNum());
                        }
                    }
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "parameter", tokenToPrint.getLineNum());
            }
        }
        public void parameter(int offset)
        {
            bool isCorrectType = false;
            foreach (string value in types)
            {
                if (tokenToPrint.getLexeme() == value)
                {
                    isCorrectType = true;
                    break;
                }
                if (tokenToPrint.getType() == "identifier")
                {
                    isCorrectType = true;
                    break;
                }
            }
            if (isCorrectType == true)
            {
                if (secondPass)
                {
                    tPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString());
                    tExist();
                }
                else
                {
                    func_size += 4;
                    newSymbol.setID("P" + newSymbol.getNextNumber().ToString());
                    newSymbol.setKind("param");
                    Symbol replaceSym = symbolTable[parent_Param].Clone();
                    symbolTable.Remove(parent_Param);
                    symbolData = replaceSym.getData();
                    string replace_params = symbolData["Params"];
                    if (replace_params == "") { replace_params += newSymbol.getID(); }
                    else { replace_params += "," + newSymbol.getID(); }
                    symbolData["Params"] = replace_params;
                    replaceSym.setData(symbolData);
                    symbolData = new Dictionary<string, string>();
                    symbolTable.Add(replaceSym.getID(), replaceSym.Clone());
                    symbolData.Add("type", tokenToPrint.getLexeme());
                    symbolData.Add("accessMod", "private");
                    symbolData.Add("offset", offset.ToString());
                    newSymbol.setData(symbolData);
                    symbolData = new Dictionary<string, string>();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getType() == "identifier")
                {
                    if (!secondPass)
                    {
                        newSymbol.setValue(tokenToPrint.getLexeme());
                        newSymbol.setScope(String.Join(".", currentScope));
                        symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                        symbolData = new Dictionary<string, string>();
                        newSymbol.clearSymbol();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (tokenToPrint.getLexeme() == "[")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (tokenToPrint.getLexeme() == "]")
                        {
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "]", tokenToPrint.getLineNum());
                        }
                    }
                    if (secondPass)
                    {
                        dup(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope), "param");
                    }
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "proper type", tokenToPrint.getLineNum());
            }
        }
        public void statement()
        {
            if (isExpression() == true)
            {
                expression();
                if (tokenToPrint.getLexeme() == ";")
                {
                    if (secondPass)
                    {
                        EOE();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "{")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isStatement())
                {
                    while (isStatement() == true)
                    {
                        statement();
                    }
                }
                if (tokenToPrint.getLexeme() == "}")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "}", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "if")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "(")
                {
                    if (secondPass)
                    {
                        oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        expression();
                        if (tokenToPrint.getLexeme() == ")")
                        {
                            if (secondPass)
                            {
                                parenPop();
                                string temp_id = SAif();
                                Quad numQuad = new Quad("", "", "", "", "");
                                string nextQuadNum = "J" + numQuad.getNextLabelNum().ToString();
                                addQuad("BF", temp_id, nextQuadNum, "", ";" + "BranchFalse" + ", " + temp_id + " " + nextQuadNum);
                                IfStack.Push(nextQuadNum);
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                            if (isStatement())
                            {
                                statement();
                                if (secondPass)
                                {
                                    if (tokenToPrint.getLexeme() != "else")
                                    {
                                        //do things for ifstack
                                        string if_pop = IfStack.Pop();
                                        addLabel(if_pop);
                                    }
                                }
                                if (tokenToPrint.getLexeme() == "else")
                                {
                                    if (secondPass)
                                    { 
                                        //make JMP for jumping over else
                                        Quad numQuad = new Quad("", "", "", "", "", "");
                                        string nextQuadNum = "J" + numQuad.getNextLabelNum().ToString();
                                        addQuad("JMP", nextQuadNum, "", "", ";Jump to " + nextQuadNum);
                                        ElseStack.Push(nextQuadNum);
                                        string if_pop = IfStack.Pop();
                                        addLabel(if_pop);
                                    }
                                    syntaxLexer.getNextToken();
                                    tokenToPrint = syntaxLexer.getToken();
                                    tokenNextToPrint = syntaxLexer.peekToken();
                                    comment_line = tokenToPrint.getLine();
                                    if (isStatement())
                                    {
                                        statement();
                                        if (secondPass)
                                        {
                                            //possibly do things for elsestack
                                            string else_pop = ElseStack.Pop();
                                            addLabel(else_pop);
                                        }
                                    }
                                    else
                                    {
                                        printError(tokenToPrint.getLexeme(), "statement", tokenToPrint.getLineNum());
                                    }
                                }
                            }
                            else
                            {
                                printError(tokenToPrint.getLexeme(), "statement", tokenToPrint.getLineNum());
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "while")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "(")
                {
                    if (secondPass)
                    {
                        oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        if (secondPass)
                        {
                            Quad numQuad = new Quad("", "", "", "", "");
                            string nextQuadNum = "W" + numQuad.getNextLabelNum().ToString();
                            BeginWhileStack.Push(nextQuadNum);
                            addLabel(nextQuadNum);
                        }
                        expression();
                        if (tokenToPrint.getLexeme() == ")")
                        {
                            if (secondPass)
                            {
                                parenPop();
                                string while_temp_id = SAwhile();
                                Quad numQuad = new Quad("", "", "", "", "");
                                string nextQuadNum = "W" + numQuad.getNextLabelNum().ToString();
                                addQuad("BF", while_temp_id, nextQuadNum, "", ";" + "BranchFalse" + ", " + while_temp_id + " " + nextQuadNum);
                                EndWhileStack.Push(nextQuadNum);
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                            if (isStatement())
                            {
                                statement();
                                if (secondPass)
                                {
                                    //make JMP for jumping back to while
                                    Quad numQuad = new Quad("", "", "", "", "", "");
                                    string begin_while_jump = BeginWhileStack.Pop();
                                    string end_while_jump = EndWhileStack.Pop();
                                    addQuad("JMP", begin_while_jump, "", "", ";Jump to " + begin_while_jump);
                                    //possibly do things for while stack
                                    addLabel(end_while_jump);
                                }
                            }
                            else
                            {
                                printError(tokenToPrint.getLexeme(), "statement", tokenToPrint.getLineNum());
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "return")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression() == true)
                {
                    expression();
                }
                if (tokenToPrint.getLexeme() == ";")
                {
                    if (secondPass)
                    {
                        SAreturn();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "cout")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "<<")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        expression();
                        if (tokenToPrint.getLexeme() == ";")
                        {
                            if (secondPass)
                            {
                                SAcout();
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "<<", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "cin")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == ">>")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        expression();
                        if (tokenToPrint.getLexeme() == ";")
                        {
                            if (secondPass)
                            {
                                SAcin();
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ">>", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "spawn")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression() == true)
                {
                    expression();
                    if (tokenToPrint.getLexeme() == "set")
                    {
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (tokenToPrint.getType() == "identifier")
                        {
                            if (secondPass)
                            {
                                iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                            if (tokenToPrint.getLexeme() == ";")
                            {
                                if (secondPass)
                                {
                                    //spawn();
                                    SAS.Pop();
                                    SAS.Pop();
                                }
                                syntaxLexer.getNextToken();
                                tokenToPrint = syntaxLexer.getToken();
                                tokenNextToPrint = syntaxLexer.peekToken();
                                comment_line = tokenToPrint.getLine();
                            }
                            else
                            {
                                printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                            }
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "identifier", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "set", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "block")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == ";")
                {
                    if (secondPass)
                    {
                        //block(tokenToPrint.getLineNum().ToString(),String.Join(".", currentScope));
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "lock")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getType() == "identifier")
                {
                    if (secondPass)
                    {
                        //iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (tokenToPrint.getLexeme() == ";")
                    {
                        if (secondPass)
                        {
                            //SAlock();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "identifier", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "release")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getType() == "identifier")
                {
                    if (secondPass)
                    {
                        //iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (tokenToPrint.getLexeme() == ";")
                    {
                        if (secondPass)
                        {
                            //release();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ";", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "identifier", tokenToPrint.getLineNum());
                }
            }
        }
        public void expression()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression() == true)
                {
                    expression();
                    if (tokenToPrint.getLexeme() == ")")
                    {
                        if (secondPass)
                        {
                            parenPop();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (isExpressionZ() == true)
                        {
                            expressionz();
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "true")
            {
                if (secondPass)
                {
                    lPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                }
                else
                {
                    newSymbol.setScope("g");
                    newSymbol.setID("K" + newSymbol.getNextNumber().ToString());
                    newSymbol.setValue(tokenToPrint.getLexeme());
                    newSymbol.setKind("keyword");
                    symbolData.Add("type", "bool");
                    symbolData.Add("accessMod", "public");
                    newSymbol.setData(symbolData);
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getLexeme() == "false")
            {
                if (secondPass)
                {
                    lPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                }
                else
                {
                    newSymbol.setScope("g");
                    newSymbol.setID("K" + newSymbol.getNextNumber().ToString());
                    newSymbol.setValue(tokenToPrint.getLexeme());
                    newSymbol.setKind("Keyword");
                    symbolData.Add("type", "bool");
                    symbolData.Add("accessMod", "public");
                    newSymbol.setData(symbolData);
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getLexeme() == "null")
            {
                if (secondPass)
                {
                    lPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                }
                else
                {
                    newSymbol.setScope("g");
                    newSymbol.setID("K" + newSymbol.getNextNumber().ToString());
                    newSymbol.setValue(tokenToPrint.getLexeme());
                    newSymbol.setKind("Null");
                    symbolData.Add("type", "null");
                    symbolData.Add("accessMod", "public");
                    newSymbol.setData(symbolData);
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getLexeme() == "this")
            {
                if (secondPass)
                {
                    iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                    iExist();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isMember_refz() == true)
                {
                    member_refz();
                }
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getType() == "number")
            {
                if (secondPass)
                {
                    if (negate_num == true)
                    {
                        lPush("-"+tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                        negate_num = false;
                    }
                    else
                    {
                        lPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                    }
                }
                else
                {
                    newSymbol.setScope("g");
                    newSymbol.setID("N" + newSymbol.getNextNumber().ToString());
                    if (negate_num == true)
                    {
                        newSymbol.setValue("-"+tokenToPrint.getLexeme());
                        negate_num = false;
                    }
                    else
                    {
                        newSymbol.setValue(tokenToPrint.getLexeme());
                    }
                    newSymbol.setKind("ilit");
                    symbolData.Add("type", "int");
                    symbolData.Add("accessMod", "public");
                    symbolData.Add("offset", "0");
                    newSymbol.setData(symbolData);
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getType() == "character")
            {
                if (secondPass)
                {
                    lPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                }
                else
                {
                    newSymbol.setScope("g");
                    newSymbol.setID("H" + newSymbol.getNextNumber().ToString());
                    newSymbol.setValue(tokenToPrint.getLexeme());
                    newSymbol.setKind("clit");
                    symbolData.Add("type", "char");
                    symbolData.Add("accessMod", "public");
                    symbolData.Add("offset", "0");
                    newSymbol.setData(symbolData);
                    symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
                    symbolData = new Dictionary<string, string>();
                    newSymbol.clearSymbol();
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
            else if (tokenToPrint.getType() == "identifier")
            {
                if (secondPass)
                {
                    iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isFn_arr_member() == true)
                {
                    fn_arr_member();
                }
                if (secondPass)
                {
                    iExist();
                }
                if (isMember_refz() == true)
                {
                    member_refz();
                }
                if (isExpressionZ() == true)
                {
                    expressionz();
                }
            }
        }
        public void expressionz()
        {
            if (tokenToPrint.getLexeme() == "=")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), 0, String.Join(".",currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isAssignmentExpression())
                {
                    assignment_expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "assignment expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "and")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), 2, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "logic expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "or")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), 1, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "logic expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "==")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),3, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "!=")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),3, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "<=")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),4, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == ">=")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),4, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "<")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),4, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == ">")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),4, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "bool expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "+")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),5, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "-")
                {
                    negate_num = true;
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (tokenToPrint.getLexeme() == "+")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "math expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "-")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),5, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "-")
                {
                    negate_num = true;
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (tokenToPrint.getLexeme() == "+")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "math expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "*")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),6, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "-")
                {
                    negate_num = true;
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (tokenToPrint.getLexeme() == "+")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "math expression", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "/")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),6, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "-")
                {
                    negate_num = true;
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (tokenToPrint.getLexeme() == "+")
                {
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                if (isExpression())
                {
                    expression();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "math expression", tokenToPrint.getLineNum());
                }
            }
        }
        public void assignment_expression()
        {
            if (isExpression())
            {
                expression();
            }
            else if (tokenToPrint.getLexeme() == "new")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                bool isCorrectType = false;
                foreach (string value in types)
                {
                    if (tokenToPrint.getLexeme() == value)
                    {
                        isCorrectType = true;
                        break;
                    }
                    if (tokenToPrint.getType() == "identifier")
                    {
                        isCorrectType = true;
                        break;
                    }
                }
                if (isCorrectType == true)
                {
                    if (secondPass)
                    {
                        tPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString());
                        tExist();
                        //force type on to SAS
                        addType(tokenToPrint.getLexeme(), tokenToPrint.getLine());
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isNew_declaration() == true)
                    {
                        new_declaration();
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "proper type", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "atoi")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "(")
                {
                    if (secondPass)
                    {
                        oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        expression();
                        if (tokenToPrint.getLexeme() == ")")
                        {
                            if (secondPass)
                            {
                                parenPop();
                                SAatoi();
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "itoa")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getLexeme() == "(")
                {
                    if (secondPass)
                    {
                        oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isExpression() == true)
                    {
                        expression();
                        if (tokenToPrint.getLexeme() == ")")
                        {
                            if (secondPass)
                            {
                                parenPop();
                                SAitoa();
                            }
                            syntaxLexer.getNextToken();
                            tokenToPrint = syntaxLexer.getToken();
                            tokenNextToPrint = syntaxLexer.peekToken();
                            comment_line = tokenToPrint.getLine();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                        }
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "(", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "assignment expression", tokenToPrint.getLineNum());
            }
        }
        public void new_declaration()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    BAL(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),"",String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isArgument_list() == true)
                {
                    argument_list();
                }
                if (tokenToPrint.getLexeme() == ")")
                {
                    if (secondPass)
                    {
                        parenPop();
                        EAL(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope));
                        newOBJ();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),0, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression() == true)
                {
                    expression();
                    if (tokenToPrint.getLexeme() == "]")
                    {
                        if (secondPass)
                        {
                            bracketPop();
                            newarr();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "]", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                }
            }
        }
        public void fn_arr_member()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),null, String.Join(".", currentScope));
                    BAL(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),"",String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isArgument_list() == true)
                {
                    argument_list();
                }
                if (tokenToPrint.getLexeme() == ")")
                {
                    if (secondPass)
                    {
                        parenPop();
                        EAL(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), "", String.Join(".", currentScope));
                        func();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), ")", tokenToPrint.getLineNum());
                }
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                if (secondPass)
                {
                    oPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(),0, String.Join(".", currentScope));
                }
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (isExpression() == true)
                {
                    expression();
                    if (tokenToPrint.getLexeme() == "]")
                    {
                        if (secondPass)
                        {
                            bracketPop();
                            arr();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                    }
                    else
                    {
                        printError(tokenToPrint.getLexeme(), "]", tokenToPrint.getLineNum());
                    }
                }
                else
                {
                    printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                }
            }
        }
        public void argument_list()
        {
            if (isExpression() == true)
            {
                string test = String.Join(".",currentScope);
                expression();
                if (tokenToPrint.getLexeme() == ",")
                {
                    while (tokenToPrint.getLexeme() == ",")
                    {
                        if (secondPass)
                        {
                            commaPop();
                        }
                        syntaxLexer.getNextToken();
                        tokenToPrint = syntaxLexer.getToken();
                        tokenNextToPrint = syntaxLexer.peekToken();
                        comment_line = tokenToPrint.getLine();
                        if (isExpression() == true)
                        {
                            expression();
                        }
                        else
                        {
                            printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
                        }
                    }
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), "expression", tokenToPrint.getLineNum());
            }
        }
        public void member_refz()
        {
            if (tokenToPrint.getLexeme() == ".")
            {
                syntaxLexer.getNextToken();
                tokenToPrint = syntaxLexer.getToken();
                tokenNextToPrint = syntaxLexer.peekToken();
                comment_line = tokenToPrint.getLine();
                if (tokenToPrint.getType() == "identifier")
                {
                    if (secondPass)
                    {
                        iPush(tokenToPrint.getLexeme(), tokenToPrint.getLineNum().ToString(), String.Join(".", currentScope), "");
                        string add_to_scope = tokenToPrint.getLexeme();
                    }
                    syntaxLexer.getNextToken();
                    tokenToPrint = syntaxLexer.getToken();
                    tokenNextToPrint = syntaxLexer.peekToken();
                    comment_line = tokenToPrint.getLine();
                    if (isFn_arr_member() == true)
                    {
                        fn_arr_member();
                    }
                    if (secondPass)
                    {
                        rExist();
                    }
                    if (isMember_refz())
                    {
                        member_refz();
                    }
                }
                else
                {
                    printError(tokenToPrint.getType(), "identifier", tokenToPrint.getLineNum());
                }
            }
            else
            {
                printError(tokenToPrint.getLexeme(), ".", tokenToPrint.getLineNum());
            }
        }
        public bool isParameter()
        {
            bool isCorrectType = false;
            foreach (string value in types)
            {
                if (tokenToPrint.getLexeme() == value)
                {
                    isCorrectType = true;
                    break;
                }
                if (tokenToPrint.getType() == "identifier")
                {
                    isCorrectType = true;
                    break;
                }
            }
            if (isCorrectType == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isExpression()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "true")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "false")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "null")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "this")
            {
                return true;
            }
            else if (tokenToPrint.getType() == "number")
            {
                return true;
            }
            else if (tokenToPrint.getType() == "character")
            {
                return true;
            }
            else if (tokenToPrint.getType() == "identifier")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isExpressionZ()
        {
            if (tokenToPrint.getLexeme() == "=")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "and")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "or")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "==")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "!=")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "<=")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == ">=")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "<")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == ">")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "+")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "-")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "*")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "/")
            {
                return true;
            }
            else {
                return false;
            }
        }
        public bool isAssignmentExpression()
        {
            if (isExpression())
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "new")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "atoi")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "itoa")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isStatement()
        {
            if (isExpression() == true)
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "{")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "if")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "while")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "return")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "cout")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "cin")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "spawn")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "block")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "lock")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "release")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isFn_arr_member()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isArgument_list()
        {
            if (isExpression() == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isMember_refz()
        {
            if (tokenToPrint.getLexeme() == ".")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isNew_declaration()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isParameter_list()
        {
            if (isParameter() == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isVariable_declaration()
        {
            bool isCorrectType = false;
            foreach (string value in types)
            {
                if (tokenToPrint.getLexeme() == value)
                {
                    isCorrectType = true;
                    break;
                }
                if (tokenToPrint.getType() == "identifier")
                {
                    isCorrectType = true;
                    break;
                }
            }
            if (isCorrectType == true)
            {
                if (tokenNextToPrint.getType() == "identifier")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool isMethod_body()
        {
            if (tokenToPrint.getLexeme() == "{")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isConstructor_declaration()
        {
            if (tokenToPrint.getType() == "identifier")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isField_declaration()
        {
            if (tokenToPrint.getLexeme() == "(")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "[")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == "=")
            {
                return true;
            }
            else if (tokenToPrint.getLexeme() == ";")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isModifier()
        {
            bool isCorrectModifer = false;
            foreach (string value in modifiers)
            {
                if (tokenToPrint.getLexeme() == value)
                {
                    isCorrectModifer = true;
                }
            }
            return isCorrectModifer;
        }
        public bool isClass_member_declaration()
        {
            if (isModifier() == true)
            {
                return true;
            }
            else if (isConstructor_declaration() == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isClass_declaration()
        {
            if (tokenToPrint.getLexeme() == "class")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool symbolExist(Symbol incSym)
        {
            bool doesExist = false;
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if entry is in current scope
                if (entry.Value.getScope() == incSym.getScope())
                {
                    //if entry's value is the same as the lexeme
                    if (entry.Value.getValue() == incSym.getValue())
                    {
                        doesExist = true;
                    }
                }
            }
            if (doesExist == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void printError(string error, string expected, int line_number)
        {
            Console.WriteLine("Line Num {0}: found: {1}, expected: {2}", line_number, error, expected);
            throw new SyntaxException("");
        }
        public void printSemanticError(string error)
        {
            Console.WriteLine("Line Num: " + error);
            throw new SemanticException("");
        }
        public void add_over_under_flow()
        {
            newSymbol.setScope("g");
            newSymbol.setID("O1");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'O'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O2");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'v'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
                symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O3");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'e'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O4");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'r'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O5");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'f'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O6");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'l'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O7");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'o'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O8");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'w'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O9");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'U'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O10");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'n'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();

            newSymbol.setScope("g");
            newSymbol.setID("O11");
            boolSize = newSymbol.getID();
            newSymbol.setValue("'d'");
            newSymbol.setKind("clit");
            symbolData.Add("type", "char");
            symbolData.Add("accessMod", "public");
            symbolData.Add("offset", "0");
            newSymbol.setData(symbolData);
            symbolTable.Add(newSymbol.getID(), newSymbol.Clone());
            symbolData = new Dictionary<string, string>();
            newSymbol.clearSymbol();
        }
        public void writeFunc(string func_name, string class_name)
        {
            string class_scope = "g." + class_name;
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if the current symobl is in the classes scope
                if(entry.Value.getScope() == class_scope)
                {
                    //if the current symbol is the function
                    if(entry.Value.getValue() == func_name)
                    {
                        //add the quad to the array
                        addLabel(entry.Value.getID());
                        addQuad("FUNC", entry.Value.getID(), "", "", ";" + comment_line);
                    }
                }
            }
        }
        public void iPush(string lex, string line, string scope, string id)
        {
            id_sar new_id_sar = new id_sar(lex, line, scope, id);
            SAS.Push(new_id_sar);
        }
        public void lPush(string lex, string line, string scope, string id)
        {
            scope = "g";
            lit_sar new_lit_sar = new lit_sar(lex, line, scope, id);
            bool isDone = false;
            while (scope != "")
            {
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == scope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == new_lit_sar.getLex())
                        {
                            new_lit_sar.setId(entry.Value.getID());
                            SAS.Push(new_lit_sar);
                            isDone = true;
                            break;
                        }
                    }
                }
                if (isDone)
                {
                    break;
                }
                else
                {
                    if (scope.Contains("."))
                    {
                        //remove last piece of scope
                        string[] newScope = scope.Split('.');
                        newScope = newScope.Take(newScope.Length - 1).ToArray();
                        scope = String.Join(".", newScope);
                    }
                    else
                    {
                        //at the last scope variable in the program
                        scope = "";
                    }
                }
            }
            if (isDone == false)
            {
                printSemanticError(new_lit_sar.getLine() + ": " + new_lit_sar.getLex() + " not defined");
            }
        }
        public void oPush(string lex, string line, int? precedence, string scope)
        {
            OPR pushed_opr;
            if (OpStack.Count != 0)
            {
                OPR top_opr = OpStack.Peek();
                if (precedence == null)
                {
                    pushed_opr = new OPR(lex, line, precedence, scope);
                    OpStack.Push(pushed_opr);
                }
                else if (top_opr.getPrecedence() == null)
                {
                    pushed_opr = new OPR(lex, line, precedence, scope);
                    OpStack.Push(pushed_opr);
                }
                else if (top_opr.getPrecedence() > precedence)
                {
                    OPR popped_opr = OpStack.Pop();
                    PoppedCalc(popped_opr);
                    oPush(lex, line, precedence, scope);
                }
                else
               { 
                    pushed_opr = new OPR(lex, line, precedence, scope);
                    OpStack.Push(pushed_opr);
                }
            }
            else
            {
                pushed_opr = new OPR(lex, line, precedence, scope);
                OpStack.Push(pushed_opr);
            }
        }
        public void tPush(string lex, string line)
        {
            type_sar new_type_sar = new type_sar(lex, line,"","","");
            SAS.Push(new_type_sar);
        }
        public void addType (string lex, string line)
        {
            type_sar new_type_sar = new type_sar(lex, line, "", "", "");
            SAS.Push(new_type_sar);
        }
        public void iExist()
        {
            SAR popped_sar = SAS.Pop();
            if (popped_sar.getLex() == "this")
            {
                if (popped_sar.getScope() != "g" && !popped_sar.getScope().Contains("main"))
                {
                    SAS.Push(popped_sar);
                }
                else
                {
                    printSemanticError(popped_sar.getLine() + ": 'this' must be in a class");
                }
            }
            else if (popped_sar is func_sar)
            {
                func_sar converted_pop = popped_sar as func_sar;
                al_sar converted_al = converted_pop.get_al_sar();
                bool isDone = false;
                string scope = converted_pop.getScope();
                while (scope != "")
                {
                    foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                    {
                        //if entry is in current scope
                        if (entry.Value.getScope() == scope)
                        {
                            //if entry's value is the same as the SAR's lexeme
                            if (entry.Value.getValue() == converted_pop.getLex())
                            {
                                converted_pop.setId(entry.Value.getID());
                                break;
                            }
                        }
                    }
                    if (isDone)
                    {
                        break;
                    }
                    else
                    {
                        if (scope.Contains("."))
                        {
                            //remove last piece of scope
                            string[] newScope = scope.Split('.');
                            newScope = newScope.Take(newScope.Length - 1).ToArray();
                            scope = String.Join(".", newScope);
                        }
                        else
                        {
                            //at the last scope variable in the program
                            scope = "";
                        }
                    }
                }
                if(converted_pop.getId() == "")
                {
                    printSemanticError(popped_sar.getLine() + ": " + popped_sar.getLex() + " not defined");
                }
                Symbol tempSym = new Symbol();
                tempSym.setID("t" + tempSym.getNextTempNumber().ToString());
                tempSym.setValue(converted_pop.getLex() + "() -> " + tempSym.getID());
                tempSym.setKind("temp");
                tempSym.setScope(converted_pop.getScope());
                symbolData.Add("type", symbolTable[converted_pop.getId()].getData()["type"]);
                tempSym.setData(symbolData);
                symbolTable.Add(tempSym.getID(), tempSym.Clone());
                id_sar new_id = new id_sar(converted_pop.getLex(),converted_pop.getLine(),tempSym.getScope(), tempSym.getID());
                SAS.Push(new_id);
                symbolData = new Dictionary<string, string>();

                //add function to quad
                addQuad("FRAME", converted_pop.getId(), "this", "", ";" + comment_line);
                List<SAR> param_sars = converted_al.getList();
                string[] parameters = symbolTable[converted_pop.getId()].getData()["Params"].Split(',');
                List<string> param_list = parameters.OfType<string>().ToList();
                if (param_list[0] != "")
                {
                    param_sars.Reverse();
                    foreach (SAR param in param_sars)
                    {
                        addQuad("PUSH", param.getId(), "", "", ";Push " + param.getId() + " on to the run-time stack");
                    }
                }
                addQuad("CALL", popped_sar.getId(), "", "", "");
                if (tempSym.getData()["type"] != "void")
                {
                    addQuad("PEEK", tempSym.getID(), "", "", "");
                }
            }
            else if (popped_sar is arr_sar)
            {
                arr_sar converted_arr = popped_sar as arr_sar;
                bool isDone = false;
                string scope = converted_arr.getScope();
                while (scope != "")
                {
                    foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                    {
                        //if entry is in current scope
                        if (entry.Value.getScope() == converted_arr.getScope())
                        {
                            //if entry's value is the same as the SAR's lexeme
                            if (entry.Value.getValue() == popped_sar.getLex())
                            {
                                converted_arr.setId(entry.Value.getID());
                                break;
                            }
                        }
                    }
                    if (isDone)
                    {
                        break;
                    }
                    else
                    {
                        if (scope.Contains("."))
                        {
                            //remove last piece of scope
                            string[] newScope = scope.Split('.');
                            newScope = newScope.Take(newScope.Length - 1).ToArray();
                            scope = String.Join(".", newScope);
                        }
                        else
                        {
                            //at the last scope variable in the program
                            scope = "";
                        }
                    }
                }
                if(converted_arr.getId() == "")
                {
                    printSemanticError(converted_arr.getLine() + ": " + converted_arr.getLex() + " not defined");
                }
                Symbol tempSym = new Symbol();
                tempSym.setID("t" + tempSym.getNextTempNumber().ToString());
                tempSym.setValue(converted_arr.getLex() + "() -> " + tempSym.getID());
                tempSym.setKind("temp");
                tempSym.setScope(converted_arr.getScope());
                string trimmed_type = symbolTable[converted_arr.getId()].getData()["type"].Remove(0,3);
                symbolData.Add("type", trimmed_type);
                tempSym.setData(symbolData);
                symbolTable.Add(tempSym.getID(), tempSym.Clone());
                id_sar new_id = new id_sar(converted_arr.getLex(), converted_arr.getLine(), tempSym.getScope(), tempSym.getID());
                SAS.Push(new_id);
                symbolData = new Dictionary<string, string>();

                //Add array to quad
                Quad tempQuad = new Quad("AEF", converted_arr.getId(), popped_sar.getId(), tempSym.getID(), ";" + comment_line);        // TODO ASK ABOUT POPPED_SAR (inside array)
                //quadArray.Add(tempQuad);
            }
            else
            {                                                                                                                           //TODO ASK ABOUT TEMP SYMBOL
                id_sar converted_id = popped_sar as id_sar;
                bool isDone = false;
                string scope = converted_id.getScope();
                while (scope != "")
                {
                    foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                    {
                        //if entry is in current scope
                        if (entry.Value.getScope() == scope)
                        {
                            //if entry's value is the same as the SAR's lexeme
                            if (entry.Value.getValue() == converted_id.getLex())
                            {
                                converted_id.setId(entry.Value.getID());
                                break;
                            }
                        }
                    }
                    if (isDone)
                    {
                        break;
                    }
                    else
                    {
                        if (scope.Contains("."))
                        {
                            //remove last piece of scope
                            string[] newScope = scope.Split('.');
                            newScope = newScope.Take(newScope.Length - 1).ToArray();
                            scope = String.Join(".", newScope);
                        }
                        else
                        {
                            //at the last scope variable in the program
                            scope = "";
                        }
                    }
                }
                if(converted_id.getId() == "")
                {
                    printSemanticError(converted_id.getLine() + ": " + converted_id.getLex() + " not defined");
                }
                SAS.Push(converted_id);
            }
        }
        public void vPush(string lex, string line, string scope, string id)
        {
            var_sar new_var_sar = new var_sar(lex, line, scope, id);
            //check symbol table with data given to symbol
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if entry is in current scope
                if (entry.Value.getScope() == scope)
                {
                    //if entry's value is the same as the SAR's lexeme
                    if (entry.Value.getValue() == lex)
                    {
                        new_var_sar.setId(entry.Value.getID());
                        break;
                    }
                }
            }
            SAS.Push(new_var_sar);
        }
        public void rExist()
        { 
            SAR top_sar = SAS.Pop();
            SAR next_sar = SAS.Pop();

            //get id for function
            if (next_sar.getLex() == "this")
            {
                //grab class id
                string[] classScope = next_sar.getScope().Split('.');
                classScope = classScope.Take(classScope.Length - 1).ToArray();
                string compScope = String.Join(".", classScope);
                string newScope = compScope;
                Symbol newsym = new Symbol();

                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == newScope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == top_sar.getLex())
                        {
                            top_sar.setId(entry.Value.getID());
                            break;
                        }
                    }
                }

                string class_scope = "g";
                string class_val = currentScope[currentScope.Count() - 1];
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == class_scope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == class_val)
                        {
                            next_sar.setId(entry.Value.getID());
                            break;
                        }
                    }
                }
                string returnType = symbolTable[top_sar.getId()].getData()["type"];
                newsym.setID("r" + newsym.getNextTempNumber().ToString());
                newsym.setValue("this." + top_sar.getLex());
                newsym.setKind("ref");
                newsym.setScope(top_sar.getScope());
                symbolData.Add("type", returnType);
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                ref_sar new_ref = new ref_sar(newsym.getValue(), top_sar.getLine(), top_sar.getScope(), newsym.getID());
                SAS.Push(new_ref);
                Quad tempQuad = new Quad("REF", next_sar.getId(), top_sar.getId(), newsym.getID(), ";" + next_sar.getLex() + "." + top_sar.getLex());
                //quadArray.Add(tempQuad);

            }
            else if (next_sar is func_sar)
            {
                func_sar converted_func = next_sar as func_sar;
                al_sar converted_al = converted_func.get_al_sar();
                string func_type = symbolTable[next_sar.getId()].getData()["type"];
                string new_scope = "g." + func_type;
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == new_scope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == top_sar.getLex())
                        {
                            top_sar.setId(entry.Value.getID());
                            break;
                        }
                    }
                }
                if(top_sar.getId() == "")
                {
                    printSemanticError(top_sar.getLine() + ": Function" + top_sar.getLex() + " not defined in class " + func_type);
                }
                if (symbolTable[top_sar.getId()].getData()["accessMod"] != "public")
                {
                    printSemanticError(top_sar.getLine() + ": Function" + top_sar.getLex() + " not public in class " + func_type);
                }

                Symbol newsym = new Symbol();
                string returnType = symbolTable[top_sar.getId()].getData()["type"];
                newsym.setID("r" + newsym.getNextTempNumber().ToString());
                newsym.setValue(top_sar.getLex());
                newsym.setKind("ref");
                newsym.setScope(top_sar.getScope());
                symbolData.Add("type", returnType);
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                ref_sar new_ref = new ref_sar(newsym.getValue(), top_sar.getLine(), top_sar.getScope(), newsym.getID());
                SAS.Push(new_ref);

                List<SAR> param_sars = converted_al.getList();
                string[] parameters = symbolTable[func_id].getData()["Params"].Split(',');
                List<string> param_list = parameters.OfType<string>().ToList();
                //add quad for FRAME
                addQuad("FRAME", top_sar.getId(), "this", "", ";" + comment_line);
                if (param_list[0] != "")
                {
                    param_sars.Reverse();
                    foreach (SAR param in param_sars)
                    {
                        addQuad("PUSH", param.getId(), "", "", ";Push " + param.getId() + " on to the run-time stack");
                    }
                }
                addQuad("CALL", top_sar.getId(), "", "", "");
                if(func_type != "void")
                {
                    addQuad("PEEK", newsym.getID(), "", "", "");
                }
            }
            else if (next_sar is arr_sar)
            {
                arr_sar converted_arr = next_sar as arr_sar;
                string func_type = symbolTable[next_sar.getId()].getData()["type"];
                string new_scope = "g." + func_type;
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == new_scope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == top_sar.getLex())
                        {
                            top_sar.setId(entry.Value.getID());
                            break;
                        }
                    }
                }
                if (top_sar.getId() == "")
                {
                    printSemanticError(top_sar.getLine() + ": Array" + top_sar.getLex() + " not defined in class " + func_type);
                }
                if (symbolTable[top_sar.getId()].getData()["accessMod"] != "public")
                {
                    printSemanticError(top_sar.getLine() + ": Array" + top_sar.getLex() + " not public in class " + func_type);
                }
                Symbol newsym = new Symbol();
                string returnType = symbolTable[top_sar.getId()].getData()["type"];
                newsym.setID("r" + newsym.getNextTempNumber().ToString());
                newsym.setValue(top_sar.getLex());
                newsym.setKind("ref");
                newsym.setScope(top_sar.getScope());
                symbolData.Add("type", returnType);
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                ref_sar new_ref = new ref_sar(newsym.getValue(), top_sar.getLine(), top_sar.getScope(), newsym.getID());
                SAS.Push(new_ref);
                //Add array to quad
                Quad tempQuad = new Quad("AEF", converted_arr.getId(), top_sar.getId(), newsym.getID(), ";" + comment_line);        // TODO ASK ABOUT POPPED_SAR (inside array)
                //quadArray.Add(tempQuad);
            }
            else
            {
                string func_type = symbolTable[next_sar.getId()].getData()["type"];
                string new_scope = "g." + func_type;
                foreach (KeyValuePair<string, Symbol> entry in symbolTable)
                {
                    //if entry is in current scope
                    if (entry.Value.getScope() == new_scope)
                    {
                        //if entry's value is the same as the SAR's lexeme
                        if (entry.Value.getValue() == top_sar.getLex())
                        {
                            top_sar.setId(entry.Value.getID());
                            break;
                        }
                    }
                }
                if (top_sar.getId() == "")
                {
                    printSemanticError(top_sar.getLine() + ": Variable" + top_sar.getLex() + " not defined in class " + func_type);
                }
                if (symbolTable[top_sar.getId()].getData()["accessMod"] != "public")
                {
                    printSemanticError(top_sar.getLine() + ": Variable" + top_sar.getLex() + " not public in class " + func_type);
                }
                Symbol newsym = new Symbol();
                string returnType = symbolTable[top_sar.getId()].getData()["type"];
                newsym.setID("r" + newsym.getNextTempNumber().ToString());
                newsym.setValue(top_sar.getLex());
                newsym.setKind("ref");
                newsym.setScope(top_sar.getScope());
                symbolData.Add("type", returnType);
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                ref_sar new_ref = new ref_sar(newsym.getValue(), top_sar.getLine(), top_sar.getScope(), newsym.getID());
                SAS.Push(new_ref);
                Quad tempQuad = new Quad("REF", next_sar.getId(), top_sar.getId(), newsym.getID(), ";" + next_sar.getLex() + "." + top_sar.getLex());
                //quadArray.Add(tempQuad);
            }
        }
        public void tExist()
        {
            SAR popped_type = SAS.Pop();
            if (!classTypes.Contains(popped_type.getLex()) && !types.Contains(popped_type.getLex()))
            {
                printSemanticError(String.Format("{0}: Type {1} not defined",popped_type.getLine(),popped_type.getLex()));
            }
        }
        public void BAL(string lex, string line, string id, string scope)
        {
            bal_sar new_bal_sar = new bal_sar("BAL", line, scope, id);
            SAS.Push(new_bal_sar);
        }
        public void EAL(string lex, string line, string id, string scope)
        {
            al_sar new_al_sar = new al_sar(lex, line, scope, id);
            bool isBAL = false;
            while (isBAL == false)
            {
                SAR next_sar = SAS.Pop();
                if(next_sar.getLex() == "BAL")
                {
                    isBAL = true;
                }
                else
                {
                    new_al_sar.addSAR(next_sar);
                }
            }
            SAS.Push(new_al_sar);
        }
        public void func()
        {
            SAR popped_al = SAS.Pop();
            al_sar converted_al = popped_al as al_sar;
            SAR popped_id = SAS.Pop();
            id_sar converted_id = popped_id as id_sar;            
            func_sar pushed_func = new func_sar(converted_id.getLex(), converted_id.getLine(), converted_id.getScope(),converted_id, converted_al);
            SAS.Push(pushed_func);
        }
        public void arr()
        {
            SAR popped_sar = SAS.Pop();
            SAR popped_id = SAS.Pop();
            id_sar converted_id = popped_id as id_sar;            
            string expType = symbolTable[popped_sar.getId()].getData()["type"];
            if (expType != "int")
            {
                printSemanticError("Must have numeric value inside array size");
            }
            else
            {
                arr_sar arr = new arr_sar(converted_id.getLex(), converted_id.getLine(), converted_id.getScope());
                SAS.Push(arr);
            }
        }
        public string SAif()
        {
            SAR popped_sar = SAS.Pop();
            //compare it's type to test if the type of the expression is Boolean
            tvar_sar popped_tvar = popped_sar as tvar_sar;
            if(popped_tvar != null)
            {
                if (popped_tvar.getType() != "bool")
                {
                    printSemanticError(popped_tvar.getLine() + ": if requires bool got " + popped_tvar.getType());
                }
                return popped_tvar.getId();
            }
            else
            {
                printSemanticError("Could not convert type of SAR to a tvar_sar");
                return "";
            }
        }
        public string SAwhile()
        {
            SAR popped_sar = SAS.Pop();
            //compare it's type to test if the type of the expression is Boolean
            tvar_sar popped_tvar = popped_sar as tvar_sar;
            if (popped_tvar != null)
            {
                if (popped_tvar.getType() != "bool")
                {
                    printSemanticError(popped_tvar.getLine() + ": while requires bool got " + popped_tvar.getType());
                }
                return popped_tvar.getId();
            }
            else
            {
                printSemanticError("Could not convert type of SAR to a tvar_sar");
                return "";
            }
        }
        public void SAreturn()
        {
            while(OpStack.Count > 0)
            {
                OPR popped_op = OpStack.Pop();
                PoppedCalc(popped_op);
            }
            SAR popped_sar = SAS.Pop();
            string methodSymID = "";
            string methodValue = currentScope[currentScope.Count() - 1];
            List<string> tempScope = new List<string>();
            foreach(string item in currentScope)
            {
                if (item != methodValue)
                {
                    tempScope.Add(item);
                }
            }
            string scope = String.Join(".", tempScope);
            bool isDone = false;
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if entry is in current scope
                if (entry.Value.getScope() == scope)
                {
                    //if entry's value is the same as the SAR's lexeme
                    if (entry.Value.getValue() == methodValue)
                    {
                        methodSymID = entry.Value.getID();
                        isDone = true;
                        break;
                    }
                }
            }
            if(isDone == false)
            {
                printSemanticError(popped_sar.getLine() + ": could not find return type");
            }
            else
            {
                string popped_sar_type = symbolTable[popped_sar.getId()].getData()["type"];
                string method_type = symbolTable[methodSymID].getData()["type"];
                if (popped_sar_type != method_type)
                {
                    printSemanticError(popped_sar.getLine() + ": function requires " + method_type + " returned " + popped_sar_type);
                }
                //add Return Quad to array
                addQuad("RETURN", popped_sar.getId(), "", "", "");
                hasReturn = true;
            }
        }
        public void SAcout()
        {
            while (OpStack.Count > 0)
            {
                OPR popped_op = OpStack.Pop();
                PoppedCalc(popped_op);
            }
            SAR popped_sar = SAS.Pop();
            string sar_type = symbolTable[popped_sar.getId()].getData()["type"];
            switch (sar_type)
            {
                case "char":
                    addQuad("WRITE", "1", popped_sar.getId(), "", ";" + comment_line);
                    break;
                case "int":
                    addQuad("WRITE", "2", popped_sar.getId(), "", ";" + comment_line);
                    break;
                default:
                    printSemanticError(popped_sar.getLine() + ": cannot convert " + popped_sar.getLex() + " to character");
                    break;
            }
        }
        public void SAcin()
        {
            while (OpStack.Count > 0)
            {
                OPR popped_op = OpStack.Pop();
                PoppedCalc(popped_op);
            }
            SAR popped_sar = SAS.Pop();
            string sar_type = symbolTable[popped_sar.getId()].getData()["type"];
            switch (sar_type)
            {
                case "char":
                    addQuad("READ", "1", popped_sar.getId(), "", ";" + comment_line);
                    break;
                case "int":
                    addQuad("READ", "2", popped_sar.getId(), "", ";" + comment_line);
                    break;
                default:
                    printSemanticError(popped_sar.getLine() + ": cannot convert " + popped_sar.getLex() + " to character");
                    break;
            }
        }
        public void SAatoi()
        {
            SAR popped_sar = SAS.Pop();
            //test that the type of the expression can converted to an integer
            string sar_type = symbolTable[popped_sar.getId()].getData()["type"];
            if (sar_type != "char")
            {
                printSemanticError(popped_sar.getLine() + ": Cannot convert " + popped_sar.getLex() + " to an int");
            }
            else
            {
                Symbol newsym = new Symbol();
                newsym.setID("t" + newsym.getNextTempNumber().ToString());
                newsym.setValue(popped_sar.getLex());
                newsym.setKind("temp");
                newsym.setScope(popped_sar.getScope());
                symbolData.Add("type", "int");
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                //Push a SAR for the integer onto the SAS
                type_sar new_type_sar = new type_sar(popped_sar.getLex(),"char",popped_sar.getLine(),popped_sar.getScope(),newsym.getID());
                newsym.clearSymbol();
                SAS.Push(new_type_sar);
            }
        }
        public void SAitoa()
        {
            SAR popped_sar = SAS.Pop();
            //test that the type of the expression is an integer
            string sar_type = symbolTable[popped_sar.getId()].getData()["type"];
            if (sar_type != "int")
            {
                printSemanticError(popped_sar.getLine() + ": cannot convert " + popped_sar.getLex() + " to an integer");
            }
            else
            {
                //Push a SAR for the character onto the SAS
                Symbol newsym = new Symbol();
                newsym.setID("t" + newsym.getNextTempNumber().ToString());
                newsym.setValue(popped_sar.getLex());
                newsym.setKind("temp");
                newsym.setScope(popped_sar.getScope());
                symbolData.Add("type", "char");
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                //Push a SAR for the integer onto the SAS
                type_sar new_type_sar = new type_sar(popped_sar.getLex(), "char", popped_sar.getLine(), popped_sar.getScope(), newsym.getID());
                newsym.clearSymbol();
                SAS.Push(new_type_sar);
            }
        }
        public void newOBJ()
        {
            SAR popped_sar = SAS.Pop();
            al_sar popped_al_sar = popped_sar as al_sar;
            SAR popped_type = SAS.Pop();
            string tempScope = "g."+popped_type.getLex();
            string constructType = popped_type.getLex();
            string size_of_class = "";
            List<string> param_variable_list = new List<string>();
            Symbol constructSym = new Symbol();
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if entry is in current scope
                if (entry.Value.getScope() == tempScope)
                {
                    //if entry's value is the same as the SAR's lexeme
                    if (entry.Value.getValue() == constructType)
                    {
                        constructSym = entry.Value;
                        break;
                    }
                }
            }
            foreach (KeyValuePair<string, Symbol> entry in symbolTable)
            {
                //if entry is in current scope
                if (entry.Value.getScope() == "g")
                {
                    //if entry's value is the same as the SAR's lexeme
                    if (entry.Value.getValue() == constructType)
                    {
                        size_of_class = entry.Value.getData()["size"];
                        break;
                    }
                }
            }
            //comapre symbol's params with argument list
            string[] parameters = constructSym.getData()["Params"].Split(',');
            List<string> param_list = parameters.OfType<string>().ToList();
            param_list.Reverse();
            if (param_list.Count() == 1 && param_list[0] == "")
            {
                param_list.Clear();
            }
            List<SAR> al_sar_list = popped_al_sar.getList();
            if(al_sar_list.Count() != param_list.Count())
            {
                printSemanticError(popped_al_sar.getLine() + ": cannot find constructor that matches number of arguments");
            }
            else
            {
                for(int i = al_sar_list.Count() - 1; i >= 0; i--)
                {
                    SAR next_sar = al_sar_list[i];
                    param_variable_list.Add(next_sar.getId());
                    string next_param = param_list[i];
                    string next_sar_type = symbolTable[next_sar.getId()].getData()["type"];
                    string next_param_type = symbolTable[next_param].getData()["type"];
                    if(next_sar_type != next_param_type)
                    {
                        printSemanticError(popped_al_sar.getLine() + ": a parameter does not match constructor signature");
                    }
                }
                Symbol newsym = new Symbol();
                newsym.setID("t" + newsym.getNextTempNumber().ToString());
                newsym.setValue(popped_type.getLex());
                newsym.setKind("temp");
                newsym.setScope(constructSym.getScope());
                symbolData.Add("type", constructType);
                newsym.setData(symbolData);
                symbolTable.Add(newsym.getID(), newsym.Clone());
                symbolData = new Dictionary<string, string>();
                new_sar pushed_new = new new_sar(popped_type.getLex(),popped_sar.getLine(), popped_sar.getScope(), newsym.getID());
                SAS.Push(pushed_new);

                //Add quad to array
                Quad tempQuad = new Quad("NEWI", size_of_class, newsym.getID(), "", ";" + comment_line);
                //quadArray.Add(tempQuad);
                tempQuad = new Quad("FRAME", constructSym.getID(), newsym.getID(), "", ";" + comment_line);
                //quadArray.Add(tempQuad);
                //Push any parameters, if there are some
                string func_id = constructSym.getID();
                parameters = symbolTable[func_id].getData()["Params"].Split(',');
                param_list = parameters.OfType<string>().ToList();
                if (param_list[0] != "")
                {
                    param_variable_list.Reverse();
                    foreach (string param in param_variable_list)
                    {
                        Quad paramQuad = new Quad("PUSH", param, "", "", ";Push " + param + " on to the run-time stack");
                        //quadArray.Add(paramQuad);
                    }
                }
                tempQuad = new Quad("CALL", constructSym.getID(), "", "", "");
                //quadArray.Add(tempQuad);

                //get return type of function
                string func_type = symbolTable[func_id].getData()["type"];
                Symbol tempSym = new Symbol();
                tempSym.setID("t" + tempSym.getNextTempNumber().ToString());
                tempSym.setValue(newsym.getID() + "." + popped_type.getLex() + "() -> " + tempSym.getID());
                tempSym.setKind("temp");
                tempSym.setScope(constructSym.getScope());
                symbolData.Add("type", func_type);
                tempSym.setData(symbolData);
                symbolTable.Add(tempSym.getID(), tempSym.Clone());
                symbolData = new Dictionary<string, string>();

                SAR changed_new_sar = SAS.Pop();
                changed_new_sar.setId(tempSym.getID());
                SAS.Push(changed_new_sar);
                tempQuad = new Quad("PEEK", tempSym.getID(), "", "", "");
                //quadArray.Add(tempQuad);
            }
        }
        public void newarr()
        {
            SAR popped_sar = SAS.Pop();
            SAR popped_type = SAS.Pop();
            SAR converted_type = popped_type as type_sar;
            bool isClass = false;
            string type = symbolTable[popped_sar.getId()].getData()["type"];
            if(type != "int")
            {
                printSemanticError(popped_sar.getLine() + ": " + popped_sar.getLex() + " is not an integer");
            }
            else
            {
                bool isCorrectType = false;
                string sar_type = converted_type.getLex();
                foreach (string value in types)
                {
                    if (sar_type == value)
                    {
                        isCorrectType = true;
                        isClass = false;
                        break;
                    }
                }
                foreach(string value in classTypes)
                {
                    if (sar_type == value)
                    {
                        isCorrectType = true;
                        isClass = false;
                        break;
                    }
                }
                if (isCorrectType != true)
                {
                    printSemanticError(popped_sar.getLine() + ": cannot construct array of this type");
                }
                else
                {
                    Symbol sizeSym = new Symbol();
                    sizeSym.setID("t" + sizeSym.getNextTempNumber().ToString());
                    sizeSym.setValue(popped_sar.getId());
                    Symbol newsym = new Symbol();
                    newsym.setID("t" + newsym.getNextTempNumber().ToString());
                    newsym.setValue("[" + popped_sar.getLex() + "]");
                    newsym.setKind("temp");
                    newsym.setScope(popped_sar.getScope());
                    symbolData.Add("type", "@: " + sar_type);
                    newsym.setData(symbolData);
                    symbolTable.Add(newsym.getID(), newsym.Clone());
                    symbolData = new Dictionary<string, string>();
                    new_sar pushed_new_sar = new new_sar(newsym.getKind(),popped_sar.getLine(),newsym.getScope(),newsym.getID());
                    SAS.Push(pushed_new_sar);

                    //Add quads to array
                    //check if the type is a class or a correct type
                    if(isClass == true)
                    {
                        Quad classQuad = new Quad("MUL", class_id, popped_sar.getId(), sizeSym.getID(), ";" + comment_line);
                        //quadArray.Add(classQuad);
                        classQuad = new Quad("NEW", sizeSym.getID(), newsym.getID(), "", "");
                        //quadArray.Add(classQuad);
                    }
                    else
                    {
                        if (sar_type == "bool")
                        {
                            Quad typeQuad = new Quad("MUL", boolSize, popped_sar.getId(), sizeSym.getID(), ";" + comment_line);
                            //quadArray.Add(typeQuad);
                            typeQuad = new Quad("NEW", sizeSym.getID(), newsym.getID(), "", "");
                            //quadArray.Add(typeQuad);
                        }
                        else if(sar_type == "int")
                        {
                            Quad typeQuad = new Quad("MUL", intSize, popped_sar.getId(), sizeSym.getID(), ";" + comment_line);
                            //quadArray.Add(typeQuad);
                            typeQuad = new Quad("NEW", sizeSym.getID(), newsym.getID(), "", "");
                            //quadArray.Add(typeQuad);
                        }
                    }
                }
            }
        }
        public void CD(string lex, string scope, string lineNum)
        {
            if (!scope.Contains(lex))
            {
                printSemanticError(lineNum + ": Constructor name '" + lex + "' does not match the class name");
            }
        }
        public void dup(string lex, string line, string id, string scope, string kind)
        {
            if (!dupList.Contains(scope))
            {
                string dupy = lex + "_" + scope;
                dupList.Add(dupy);
                return;
            }
            else
            {
                string error = "";
                switch (kind)
                {
                    case "class":
                        error = "Duplicate class";
                        break;
                    case "method":
                        error = "Duplicte method";
                        break;
                    case "contructor":
                        error = "Duplicate constructor";
                        break;
                    default:
                        error = "Duplicate variable";
                        break;                   
                }
                printSemanticError(string.Format("Line num: {0}: {1}, {2}",line,error,lex));
            }
        }
        public void spawn()
        {
            iExist();
            SAR popped_sar = SAS.Pop();
            string sar_type = symbolTable[popped_sar.getId()].getData()["type"];
            if(sar_type != "int")
            {
                printSemanticError(popped_sar.getLine() + ": not of type int");
            }
            else
            {
                //Pop a Function SAR from the SAS
                SAR popped_func = SAS.Pop();
                func_sar popped_func_sar = popped_func as func_sar;
                if(popped_func_sar == null)
                {
                    printSemanticError("No function sar found");
                } 
            }
        }
        public void block(string lineNum, string scope)
        {
            //Check that the block statement is on the main thread
            if(scope != "g")
            {
                printSemanticError(lineNum + " Cannot run block");
            }
        }
        public void SAlock()
        {
            //Pop an identifier from the SAS and test that it is of type sym
            SAR popped_sar = SAS.Pop();
            if (symbolTable[popped_sar.getId()].getData()["type"] != "sym")
            {
                printSemanticError(popped_sar.getLine() + ": " + popped_sar.getLex() + " is not of type 'sym'");
            }
        }
        public void release()
        {
            //Pop an identifier from the SAS and test that it is of type sym
            SAR popped_sar = SAS.Pop();
            if (symbolTable[popped_sar.getId()].getData()["type"] != "sym")
            {
                printSemanticError(popped_sar.getLine() + ": " + popped_sar.getLex() + " is not of type 'sym'");
            }
        }
        public void EOE()
        {
            while (OpStack.Count > 0)
            {
                //pop top thing off op stack
                OPR popped_op = OpStack.Pop();
                PoppedCalc(popped_op);
            }
        }
        public void parenPop()
        {
            OPR popped_op = OpStack.Pop();
            while(popped_op.getLex() != "(")
            {
                PoppedCalc(popped_op);
                popped_op = OpStack.Pop();
            }
        }
        public void bracketPop()
        {
            OPR popped_op = OpStack.Pop();
            while (popped_op.getLex() != "[")
            {
                PoppedCalc(popped_op);
                popped_op = OpStack.Pop();
            }
        }
        public void commaPop()
        {
            OPR peek_op = OpStack.Peek();
            while (peek_op.getLex() != "(")
            {
                OPR popped_op = OpStack.Pop();
                PoppedCalc(popped_op);
                peek_op = OpStack.Peek();
            }
        }
        public void PoppedCalc(OPR inc_opr)
        {
            switch (inc_opr.getLex())
            {
                case "=":
                    SAR Erhs = SAS.Pop();
                    SAR Elhs = SAS.Pop();
                    string ElhsKind = symbolTable[Elhs.getId()].getKind();
                    if(ElhsKind == "keyword" || ElhsKind == "ilit" || ElhsKind == "clit")
                    {
                        printSemanticError(inc_opr.getLine() + ": " + "Cannot assign to the type on the left hand side");
                    }
                    string Erhs_type = symbolTable[Erhs.getId()].getData()["type"];
                    string Elhs_type = symbolTable[Elhs.getId()].getData()["type"];
                    string Erhs_value = symbolTable[Erhs.getId()].getValue();
                    string Elhs_value = symbolTable[Elhs.getId()].getValue();
                    if (!(Erhs_type == Elhs_type) && Erhs_value != "null")
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + Erhs_type + " " + Erhs_value + " = " + Elhs_type + " " + Elhs_value);
                    }
                    else
                    {
                        if (toSquad == false)
                        {
                            if (main_name != "main")
                            {
                                //add line to quad
                                addQuad("MOV", Elhs.getId(), Erhs.getId(), "", ";" + comment_line);
                            }
                            else
                            {
                                //add line to quad
                                addQuad("MOV", Elhs.getId(), Erhs.getId(), "", ";" + comment_line);
                                
                            }
                        }
                        else
                        {
                            //add line to squad
                            Quad tempQuad = new Quad("MOV", Elhs.getId(), Erhs.getId(), "", ";" + comment_line);
                            squadArray.Add(tempQuad);
                        }
                    }
                    break;
                case "+":
                    //pop 2 off the stack, check if they can be added together
                    SAR Arhs = SAS.Pop();
                    SAR Alhs = SAS.Pop();
                    string Arhs_type = symbolTable[Arhs.getId()].getData()["type"];
                    string Alhs_type = symbolTable[Alhs.getId()].getData()["type"];
                    string Arhs_value = symbolTable[Arhs.getId()].getValue();
                    string Alhs_value = symbolTable[Alhs.getId()].getValue();
                    if (!(Arhs_type == Alhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + Arhs_type + " " + Arhs_value + " + " + Alhs_type + " " + Alhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(Alhs.getId() + " + " + Arhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", Alhs_type);
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), Alhs_type);
                        SAS.Push(new_tvar);

                        string ArhsKind = symbolTable[Arhs.getId()].getKind();
                        if(ArhsKind == "ilit")
                        {
                            //ADI
                            addQuad("ADD", Alhs.getId(), Arhs.getId(), newsym.getID(), ";" + comment_line);
                        }
                        else
                        {
                            //add quad to array
                            addQuad("ADD", Alhs.getId(), Arhs.getId(), newsym.getID(), ";" + comment_line);
                        }       
                    }
                    break;
                case "-":
                    //pop 2 off the stack, check if they can be added together
                    SAR Srhs = SAS.Pop();
                    SAR Slhs = SAS.Pop();
                    string Srhs_type = symbolTable[Srhs.getId()].getData()["type"];
                    string Slhs_type = symbolTable[Slhs.getId()].getData()["type"];
                    string Srhs_value = symbolTable[Srhs.getId()].getValue();
                    string Slhs_value = symbolTable[Slhs.getId()].getValue();
                    if (!(Srhs_type == Slhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + Srhs_type + " " + Srhs_value + " - " + Slhs_type + " " + Slhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(Slhs.getId() + " - " + Srhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", Slhs_type);
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), Slhs_type);
                        SAS.Push(new_tvar);

                        //add quad to array
                        addQuad("SUB", Slhs.getId(), Srhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "/":
                    //pop 2 off the stack, check if they can be added together
                    SAR Drhs = SAS.Pop();
                    SAR Dlhs = SAS.Pop();
                    string Drhs_type = symbolTable[Drhs.getId()].getData()["type"];
                    string Dlhs_type = symbolTable[Dlhs.getId()].getData()["type"];
                    string Drhs_value = symbolTable[Drhs.getId()].getValue();
                    string Dlhs_value = symbolTable[Dlhs.getId()].getValue();
                    if (!(Drhs_type == Dlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + Drhs_type + " " + Drhs_value + " / " + Dlhs_type + " " + Dlhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(Dlhs.getId() + " / " + Drhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", Dlhs_type);
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), Dlhs_type);
                        SAS.Push(new_tvar);

                        //add quad to array
                        addQuad("DIV", Dlhs.getId(), Drhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "*":
                    //pop 2 off the stack, check if they can be added together
                    SAR Mrhs = SAS.Pop();
                    SAR Mlhs = SAS.Pop();
                    string Mrhs_type = symbolTable[Mrhs.getId()].getData()["type"];
                    string Mlhs_type = symbolTable[Mlhs.getId()].getData()["type"];
                    string Mrhs_value = symbolTable[Mrhs.getId()].getValue();
                    string Mlhs_value = symbolTable[Mlhs.getId()].getValue();
                    if (!(Mrhs_type == Mlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + Mrhs_type + " " + Mrhs_value + " - " + Mlhs_type + " " + Mlhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(Mlhs.getId() + " * " + Mrhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", Mlhs_type);
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), Mlhs_type);
                        SAS.Push(new_tvar);

                        //add to quad array
                        addQuad("MUL", Mlhs.getId(), Mrhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "<":
                    //pop 2 off the stack, check if they can be added together
                    SAR LTrhs = SAS.Pop();
                    SAR LTlhs = SAS.Pop();
                    string LTrhs_type = symbolTable[LTrhs.getId()].getData()["type"];
                    string LTlhs_type = symbolTable[LTlhs.getId()].getData()["type"];
                    string LTrhs_value = symbolTable[LTrhs.getId()].getValue();
                    string LTlhs_value = symbolTable[LTlhs.getId()].getValue();
                    if (!(LTrhs_type == LTlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + LTrhs_type + " " + LTrhs_value + " - " + LTlhs_type + " " + LTlhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(LTlhs.getId() + " < " + LTrhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("LT", LTlhs.getId(), LTrhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case ">":
                    //pop 2 off the stack, check if they can be added together
                    SAR GTrhs = SAS.Pop();
                    SAR GTlhs = SAS.Pop();
                    string GTrhs_type = symbolTable[GTrhs.getId()].getData()["type"];
                    string GTlhs_type = symbolTable[GTlhs.getId()].getData()["type"];
                    string GTrhs_value = symbolTable[GTrhs.getId()].getValue();
                    string GTlhs_value = symbolTable[GTlhs.getId()].getValue();
                    if (!(GTrhs_type == GTlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + GTrhs_type + " " + GTrhs_value + " - " + GTlhs_type + " " + GTlhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(GTlhs.getId() + " > " + GTrhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //add to quad array
                        addQuad("GT", GTlhs.getId(), GTrhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "==":
                    //pop 2 off the stack, check if they can be added together
                    SAR EErhs = SAS.Pop();
                    SAR EElhs = SAS.Pop();
                    string EErhs_type = symbolTable[EErhs.getId()].getData()["type"];
                    string EElhs_type = symbolTable[EElhs.getId()].getData()["type"];
                    string EErhs_value = symbolTable[EErhs.getId()].getValue();
                    string EElhs_value = symbolTable[EElhs.getId()].getValue();
                    if (!(EErhs_type == EElhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + EErhs_type + " " + EErhs_value + " - " + EElhs_type + " " + EElhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(EElhs.getId() + " == " + EErhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("EQ", EElhs.getId(), EErhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "<=":
                    //pop 2 off the stack, check if they can be added together
                    SAR LTErhs = SAS.Pop();
                    SAR LTElhs = SAS.Pop();
                    string LTErhs_type = symbolTable[LTErhs.getId()].getData()["type"];
                    string LTElhs_type = symbolTable[LTElhs.getId()].getData()["type"];
                    string LTErhs_value = symbolTable[LTErhs.getId()].getValue();
                    string LTElhs_value = symbolTable[LTElhs.getId()].getValue();
                    if (!(LTErhs_type == LTElhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + LTErhs_type + " " + LTErhs_value + " - " + LTElhs_type + " " + LTElhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(LTElhs.getId() + " <= " + LTErhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("LE", LTElhs.getId(), LTErhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case ">=":
                    //pop 2 off the stack, check if they can be added together
                    SAR GTErhs = SAS.Pop();
                    SAR GTElhs = SAS.Pop();
                    string GTErhs_type = symbolTable[GTErhs.getId()].getData()["type"];
                    string GTElhs_type = symbolTable[GTElhs.getId()].getData()["type"];
                    string GTErhs_value = symbolTable[GTErhs.getId()].getValue();
                    string GTElhs_value = symbolTable[GTElhs.getId()].getValue();
                    if (!(GTErhs_type == GTElhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Invalid Operation " + GTErhs_type + " " + GTErhs_value + " - " + GTElhs_type + " " + GTElhs_value);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(GTElhs.getId() + " >= " + GTErhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(),"bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("GE", GTElhs.getId(), GTErhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "!=":
                    //pop 2 off the stack, check if they can be added together
                    SAR NErhs = SAS.Pop();
                    SAR NElhs = SAS.Pop();
                    string NErhs_type = symbolTable[NErhs.getId()].getData()["type"];
                    string NElhs_type = symbolTable[NElhs.getId()].getData()["type"];
                    if (!(NErhs_type == NElhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Not Equal requires bool found " + NErhs_type);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(NElhs.getId() + " != " + NErhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("NE", NElhs.getId(), NErhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "and":
                    //pop 2 off the stack, check if they can be added together
                    SAR ANDrhs = SAS.Pop();
                    SAR ANDlhs = SAS.Pop();
                    string ANDrhs_type = symbolTable[ANDrhs.getId()].getData()["type"];
                    string ANDlhs_type = symbolTable[ANDlhs.getId()].getData()["type"];
                    if (!(ANDrhs_type == ANDlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": And requires bool found " + ANDrhs_type);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(ANDlhs.getId() + " and " + ANDrhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //add to quad array
                        addQuad("AND", ANDlhs.getId(), ANDrhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                case "or":
                    //pop 2 off the stack, check if they can be added together
                    SAR ORrhs = SAS.Pop();
                    SAR ORlhs = SAS.Pop();
                    string ORrhs_type = symbolTable[ORrhs.getId()].getData()["type"];
                    string ORlhs_type = symbolTable[ORlhs.getId()].getData()["type"];
                    if (!(ORrhs_type == ORlhs_type))
                    {
                        printSemanticError(inc_opr.getLine() + ": Or requires bool found " + ORrhs_type);
                    }
                    else
                    {
                        //create new symbol and add data to it
                        Symbol newsym = new Symbol();
                        newsym.setID("t" + newsym.getNextTempNumber().ToString());
                        newsym.setValue(ORlhs.getId() + " or " + ORrhs.getId());
                        newsym.setKind("temp");
                        newsym.setScope(inc_opr.getScope());
                        symbolData.Add("type", "bool");
                        newsym.setData(symbolData);
                        symbolTable.Add(newsym.getID(), newsym.Clone());
                        symbolData = new Dictionary<string, string>();
                        //create sar and place on stack
                        tvar_sar new_tvar = new tvar_sar(inc_opr.getLex(), inc_opr.getLine(), inc_opr.getScope(), newsym.getID(), "bool");
                        SAS.Push(new_tvar);

                        //Add to quad array
                        addQuad("OR", ORlhs.getId(), ORrhs.getId(), newsym.getID(), ";" + comment_line);
                    }
                    break;
                default:
                    break;
            }
        }
    }
    public class OPR
    {
        private string line_num = "";
        private string lexeme = "";
        private int? precedence = 0;
        private string current_scope = "";
        public OPR(string lex, string line, int? inc_prec, string scope)
        {
            lexeme = lex;
            line_num = line;
            precedence = inc_prec;
            current_scope = scope;
        }
        public string getLex()
        {
            return lexeme;
        }
        public int? getPrecedence()
        {
            return precedence;
        }
        public string getLine()
        {
            return line_num;
        }
        public string getScope()
        {
            return current_scope;
        }
    }
    public abstract class SAR
    {
        private string lexeme = "";
        private string line_num = "";
        private string id = "";
        private string current_scope = "";
        public SAR(string lex, string line, string symID, string scope)
        {
            lexeme = lex;
            line_num = line;
            id = symID;
            current_scope = scope;
        }
        public void setId(string inc_id)
        {
            id = inc_id;
        }
        public string getId()
        {
            return id;
        }
        public string getLine()
        {
            return line_num;
        }
        public string getLex()
        {
            return lexeme;
        }
        public string getScope()
        {
            return current_scope;
        }
    }
    public class id_sar : SAR
    {
        public id_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class lit_sar : SAR
    {
        public lit_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class type_sar : SAR
    {
        string type = "";
        public type_sar(string lex, string line, string inc_type, string id, string scope) : base(lex, line, id, scope)
        {
            type = inc_type;
        }
    }
    public class var_sar : SAR
    {
        public var_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class tvar_sar : SAR
    {
        private string type = "";
        public tvar_sar(string lex, string line, string scope, string id, string incType) : base(lex, line, id, scope)
        {
            type = incType;
        }
        public string getType()
        {
            return type;
        }
    }
    public class bal_sar : SAR
    {
        public bal_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class al_sar : SAR
    {
        private List<SAR> arguments = new List<SAR>();
        public al_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
        public void addSAR(SAR inc_sar)
        {
            arguments.Add(inc_sar);
        }
        public List<SAR> getList()
        {
            return arguments;
        }
    }
    public class func_sar : SAR
    {
        private al_sar func_al;
        private id_sar func_id;
        public func_sar(string lex, string line, string scope, id_sar inc_id, al_sar inc_al) : base(lex, line, "", scope)
        {
            func_id = inc_id;
            func_al = inc_al;
        }
        public al_sar get_al_sar()
        {
            return func_al;
        }
    }
    public class arr_sar : SAR
    {
        public arr_sar(string lex, string line, string scope) : base(lex, line, "", scope)
        {
        }
    }
    public class ref_sar : SAR
    {
        public ref_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class new_sar : SAR
    {
        public new_sar(string lex, string line, string scope, string id) : base(lex, line, id, scope)
        {
        }
    }
    public class Quad
    {
        static int currentLabelNum = 0;
        string label = "";
        string opcode = "";
        string left_op = "";
        string right_op = "";
        string last_op = "";
        string comment = "";
        public Quad(string ocode, string l_op, string r_op, string lst_op, string cmmnt)
        {
            opcode = ocode;
            left_op = l_op;
            right_op = r_op;
            last_op = lst_op;
            comment = cmmnt;
        }
        public Quad(string lbl, string ocode, string l_op, string r_op, string lst_op, string cmmnt)
        {
            label = lbl;
            opcode = ocode;
            left_op = l_op;
            right_op = r_op;
            last_op = lst_op;
            comment = cmmnt;
        }
        public string getLabel()
        {
            return label;
        }
        public string getOPCode()
        {
            return opcode;
        }
        public string getLOp()
        {
            return left_op;
        }
        public string getROp()
        {
            return right_op;
        }
        public string getLastOp()
        {
            return last_op;
        }
        public string getComment()
        {
            return comment;
        }
        public void setLabel(string inc_label)
        {
            label = inc_label;
        }
        public void setLOp(string inc_label)
        {
            left_op = inc_label;
        }
        public void setROp(string inc_label)
        {
            right_op = inc_label;
        }
        public void setLastOp(string inc_label)
        {
            last_op = inc_label;
        }
        public void setComment(string cmmt)
        {
            comment = cmmt;
        }
        public int getNextLabelNum()
        {
            return ++currentLabelNum;
        }
    }
    public class TcodeGen
    {
        static int nextJMPNum;
        static string R1 = "";
        static string R0 = "";
        Stack<String> jmpStack = new Stack<string>();
        Dictionary<string, Symbol> symTable = new Dictionary<string, Symbol>();
        List<Quad> quadArray = new List<Quad>();
        List<string> Registers = new List<string>() {"R2","R4","R5","R6","R7"};
        public TcodeGen(Dictionary<string, Symbol> incSymTable, List<Quad> incQuadArray)
        {
            symTable = incSymTable;
            quadArray = incQuadArray;
            nextJMPNum = 1;
            foreach(KeyValuePair<string, Symbol> entry in symTable)
            {
                if(entry.Value.getScope() == "g")
                {
                    if(entry.Value.getValue() == "1")
                    {
                        R1 = entry.Value.getID();
                    }
                    else if(entry.Value.getValue() == "0")
                    {
                        R0 = entry.Value.getID();
                    }
                }
            }
        }
        public void generateTCode()
        {
            using (StreamWriter output = new StreamWriter("compiler.asm"))
            {
                foreach (KeyValuePair<string, Symbol> entry in symTable)
                {
                    if (entry.Value.getScope() == "g")
                    {
                        if (entry.Value.getID().StartsWith("N"))
                        {
                            output.WriteLine(entry.Value.getID() + "\t.INT\t" + entry.Value.getValue());
                        }
                        else if (entry.Value.getID().StartsWith("H") || entry.Value.getID().StartsWith("O"))
                        {
                            output.WriteLine(entry.Value.getID() + "\t.BYT\t" + entry.Value.getValue());
                        }
                        else if (entry.Value.getID().StartsWith("K"))
                        {
                            if (entry.Value.getKind() == "false" || entry.Value.getKind() == "null")
                            {
                                output.WriteLine(entry.Value.getID() + "\t.INT\t" + "0");
                            }
                            else
                            {
                                output.WriteLine(entry.Value.getID() + "\t.INT\t" + "1");
                            }
                        }
                    }
                }
                //add underflow and overflow labels and values
                print_over_under(output);
                int value;
                string AReg = "";
                string BReg = "";
                string CReg = "";
                string DReg = "";
                string ALoc = "";
                string BLoc = "";
                string CLoc = "";
                string DLoc = "";
                string quad_comment = "";
                //load R0 and R1
                output.WriteLine("START\tLDR R0, " + R0);
                output.WriteLine("\tLDR R1, " + R1);
                while (quadArray.Count() > 0)
                {
                    Quad next_quad = quadArray[0];
                    quadArray.RemoveAt(0);
                    if (next_quad.getLabel() != "")
                    {
                        output.Write(next_quad.getLabel());
                    }
                    if (next_quad.getComment() != quad_comment)
                    {
                        quad_comment = next_quad.getComment();
                    }
                    else
                    {
                        quad_comment = "";
                    }
                    switch (next_quad.getOPCode())
                    {
                        case "ADD":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for for indirect storing/loading
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc + "\t" + quad_comment);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc + "\t" + quad_comment);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tADD " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "BF":
                            AReg = getNextFreeRegister();//for lhs and result
                            CReg = getNextFreeRegister();//for for indirect storing/loading
                            ALoc = getLocation(next_quad.getLOp());
                            output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                            if (!int.TryParse(ALoc, out value)) { value = int.Parse(symTable[ALoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            output.WriteLine("\tBRZ " + AReg + ", " + next_quad.getROp());
                            returnRegister(AReg);
                            returnRegister(CReg);
                            break;
                        case "DIV":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tDIV " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "EQ":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string brzjmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBRZ " + CReg + ", " + brzjmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string brzjmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + brzjmpfalse);
                            output.WriteLine(brzjmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(brzjmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "LT":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string bltjmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBLT " + CReg + ", " + bltjmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string bltjmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + bltjmpfalse);
                            output.WriteLine(bltjmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(bltjmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "GT":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string bgtjmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBGT " + CReg + ", " + bgtjmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string bgtjmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + bgtjmpfalse);
                            output.WriteLine(bgtjmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(bgtjmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "LE":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string blejmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBLT " + CReg + ", " + blejmptrue);
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            output.WriteLine("\tBRZ " + CReg + ", " + blejmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string blejmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + blejmpfalse);
                            output.WriteLine(blejmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(blejmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "NE":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string bnzjmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBNZ " + CReg + ", " + bnzjmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string bnzjmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + bnzjmpfalse);
                            output.WriteLine(bnzjmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(bnzjmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "GE":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            string bgejmptrue = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tBGT " + CReg + ", " + bgejmptrue);
                            output.WriteLine("\tMOV " + CReg + ", " + AReg);
                            output.WriteLine("\tCMP " + CReg + ", " + BReg);
                            output.WriteLine("\tBRZ " + CReg + ", " + bgejmptrue);
                            output.WriteLine("\tMOV " + CReg + ", R0");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine("\tSTR " + CReg + ", (" + AReg + ")");
                            string bgejmpfalse = "L" + nextJMPNum++.ToString();
                            output.WriteLine("\tJMP " + bgejmpfalse);
                            output.WriteLine(bgejmptrue + "\tMOV " + CReg + ", R1");
                            output.WriteLine("\tMOV " + AReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            output.WriteLine(bgejmpfalse + "\tSTR " + CReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "AND":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tAND " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "OR":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tOR " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "BT":
                            break;
                        case "PUSH":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            CReg = getNextFreeRegister();
                            DReg = getNextFreeRegister();
                            ALoc = getLocation(next_quad.getLOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc + "\t" + quad_comment);
                                output.WriteLine("\tSTR " + AReg + ", (SP)");
                            }
                            else if (next_quad.getLOp().StartsWith("L") || next_quad.getLOp().StartsWith("t"))
                            {
                                //go back to previous frame to get value
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-4");
                                output.WriteLine("\tLDR " + DReg + ", (" + CReg + ")");
                                output.WriteLine("\tADI " + DReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + DReg + ")");
                                output.WriteLine("\tSTR " + AReg + ", (SP)");
                            }
                            else
                            {
                                //go back to previous frame to get value
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-4");
                                output.WriteLine("\tLDR " + DReg + ", (" + CReg + ")");
                                value = int.Parse(symTable[ALoc].getValue());
                                output.WriteLine("\tADI " + DReg + ", #-" + value);
                                output.WriteLine("\tLDR " + AReg + ", (" + DReg + ")");
                                output.WriteLine("\tSTR " + AReg + ", (SP)");
                            }
                            output.WriteLine("\tADI SP, #-4");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            returnRegister(DReg);
                            break;
                        case "POP":
                            AReg = getNextFreeRegister();
                            output.WriteLine("\tADI SP, #4");
                            output.WriteLine("\tLDR " + AReg + ", (SP)\t" + quad_comment);
                            returnRegister(AReg);
                            break;
                        case "PEEK":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            CReg = getNextFreeRegister();
                            CLoc = getLocation(next_quad.getLOp());
                            output.WriteLine("\tMOV " + BReg + ", (SP)\t" + quad_comment);
                            output.WriteLine("\tLDR " + AReg + ", (" + BReg + ")");
                            //store value in CLoc
                            output.WriteLine("\tMOV " + CReg + ", (FP)\t");
                            output.WriteLine("\tADI " + CReg + ", #-" + CLoc);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "FRAME":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            output.WriteLine("\tMOV " + AReg + ", (SP)\t" + quad_comment);
                            output.WriteLine("\tADI " + AReg + ", #-4");
                            output.WriteLine("\tMOV " + BReg + ", (SL)");
                            output.WriteLine("\tCMP " + AReg + ", " + BReg);
                            output.WriteLine("\tBLT " + AReg + ", overflow");
                            output.WriteLine("\tMOV " + BReg + ", (FP)");
                            output.WriteLine("\tMOV FP, (SP)");
                            //Adjust Stack Pointer for Return Address
                            output.WriteLine("\tADI SP, #-4");
                            //Store PFP to Top of Stack
                            output.WriteLine("\tSTR " + BReg + ", (SP)");
                            //Adjust Stack Pointer for PFP
                            output.WriteLine("\tADI SP, #-4");
                            //Store this pointer to Top of Stack
                            output.WriteLine("\tSTR R0, (SP)");
                            //Adjust Stack Pointer for this
                            output.WriteLine("\tADI SP, #-4");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            break;
                        case "CALL":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            string funcSize = symTable[next_quad.getLOp()].getData()["size"];
                            output.WriteLine("\tMOV " + AReg + ", (PC)\t" + quad_comment);
                            output.WriteLine("\tADI " + AReg + ", #36");
                            output.WriteLine("\tSTR " + AReg + ", (FP)");
                            output.WriteLine("\tJMP " + next_quad.getLOp());
                            returnRegister(AReg);
                            returnRegister(BReg);
                            break;
                        case "RTN":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            CReg = getNextFreeRegister();
                            //De-allocate Current Activation Record
                            //Test for Underflow (SP > SB)
                            output.WriteLine("\tMOV " + AReg + ", (SP)\t" + quad_comment);
                            output.WriteLine("\tMOV " + BReg + ", (SB)");
                            output.WriteLine("\tCMP " + AReg + ", " + BReg);
                            output.WriteLine("\tBGT " + AReg + ", underflow");
                            //Set Previous Frame to Current Frame and Return
                            //Load Return Address from the Frame
                            output.WriteLine("\tLDR " + AReg + ", (FP)");
                            //Load PFP from the Frame
                            output.WriteLine("\tMOV " + BReg + ", (FP)");
                            output.WriteLine("\tADI " + BReg + ", #-4");
                            //Set FP = PFP
                            output.WriteLine("\tLDR FP, (" + BReg + ")");
                            //Jump using JMR to Return Address*/
                            output.WriteLine("\tJMR " + AReg);
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "RETURN":
                            AReg = getNextFreeRegister();
                            BReg = getNextFreeRegister();
                            CReg = getNextFreeRegister();
                            CLoc = getLocation(next_quad.getLOp());
                            //De-allocate Current Activation Record
                            //Test for Underflow (SP > SB)
                            output.WriteLine("\tMOV " + AReg + ", (SP)\t" + quad_comment);
                            output.WriteLine("\tMOV " + BReg + ", (SB)");
                            output.WriteLine("\tCMP " + AReg + ", " + BReg);
                            output.WriteLine("\tBGT " + AReg + ", underflow");
                            //Set Previous Frame to Current Frame and Return A
                            output.WriteLine("\tMOV SP, (FP)");
                            //Load Return Address from Frame
                            output.WriteLine("\tLDR " + AReg + ", (FP)");
                            output.WriteLine("\tMOV " + BReg + ", (FP)");
                            if (CLoc.StartsWith("N") || CLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + CReg + ", " + CLoc);
                            }
                            else
                            {
                                output.WriteLine("\tADI " + BReg + ", #-" + CLoc);
                                output.WriteLine("\tLDR " + CReg + ", (" + BReg + ")");
                            }
                            //Load PFP from the Frame
                            output.WriteLine("\tMOV " + BReg + ", (FP)");
                            output.WriteLine("\tADI " + BReg + ", #-4");
                            output.WriteLine("\tLDR FP, (" + BReg + ")");
                            //Store Return Value on Top of Stack
                            output.WriteLine("\tSTR " + CReg + ", (SP)");
                            //Jump using JMR to Address in Register*/
                            output.WriteLine("\tJMR " + AReg);
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "FUNC":
                            string func_size = symTable[next_quad.getLOp()].getData()["size"];
                            output.WriteLine("\tADI SP, #-" + func_size + "\t" + quad_comment);
                            break;
                        case "NEWI":
                            break;
                        case "NEW":
                            break;
                        case "MOV":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            output.WriteLine("\tMOV " + AReg + ", (FP)\t" + quad_comment);
                            if (!int.TryParse(ALoc, out value)) { value = int.Parse(symTable[ALoc].getValue()); }
                            output.WriteLine("\tADI " + AReg + ", #-" + value);
                            if (BLoc.StartsWith("N")||BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tSTR " + BReg + ", (" + AReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "MOVI":
                            break;
                        case "REF":
                            break;
                        case "JMP":
                            output.WriteLine("\tJMP " + next_quad.getLOp() + "\t\t" + quad_comment);
                            break;
                        case "MUL":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (BLoc.StartsWith("N") || BLoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " + BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tMUL " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "SUB":
                            AReg = getNextFreeRegister();//for lhs and result
                            BReg = getNextFreeRegister();//for rhs
                            CReg = getNextFreeRegister();//for storing/loading into mem
                            ALoc = getLocation(next_quad.getLOp());
                            BLoc = getLocation(next_quad.getROp());
                            CLoc = getLocation(next_quad.getLastOp());
                            if (ALoc.StartsWith("N") || ALoc.StartsWith("H"))
                            {
                                output.WriteLine("\tLDR " + AReg + ", " + ALoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)\t" + quad_comment);
                                output.WriteLine("\tADI " + CReg + ", #-" + ALoc);
                                output.WriteLine("\tLDR " + AReg + ", (" + CReg + ")");
                            }
                            if (!int.TryParse(BLoc, out value))
                            {
                                output.WriteLine("\tLDR " + BReg + ", " +BLoc);
                            }
                            else
                            {
                                output.WriteLine("\tMOV " + CReg + ", (FP)");
                                output.WriteLine("\tADI " + CReg + ", #-" + BLoc);
                                output.WriteLine("\tLDR " + BReg + ", (" + CReg + ")");
                            }
                            output.WriteLine("\tSUB " + AReg + ", " + BReg);
                            output.WriteLine("\tMOV " + CReg + ", (FP)");
                            if (!int.TryParse(CLoc, out value)) { value = int.Parse(symTable[CLoc].getValue()); }
                            output.WriteLine("\tADI " + CReg + ", #-" + value);
                            output.WriteLine("\tSTR " + AReg + ", (" + CReg + ")");
                            returnRegister(AReg);
                            returnRegister(BReg);
                            returnRegister(CReg);
                            break;
                        case "WRITE":
                            string writeLoc = "";
                            if (next_quad.getLOp() == "1")
                            {
                                AReg = getNextFreeRegister();
                                writeLoc = getLocation(next_quad.getROp());
                                if (writeLoc.StartsWith("N") || writeLoc.StartsWith("H"))
                                {
                                    output.WriteLine("\tLDB R3, " + writeLoc);
                                }
                                else
                                {
                                    output.WriteLine("\tMOV " + AReg + ", (FP)\t" + quad_comment);
                                    output.WriteLine("\tADI " + AReg + ", #-" + writeLoc);
                                    output.WriteLine("\tLDB R3, (" + AReg + ")");
                                }
                                output.WriteLine("\tTRP 3");
                                returnRegister(AReg);
                            }
                            else if (next_quad.getLOp() == "2")
                            {
                                AReg = getNextFreeRegister();
                                writeLoc = getLocation(next_quad.getROp());
                                if (writeLoc.StartsWith("N")||writeLoc.StartsWith("H"))
                                {
                                    output.WriteLine("\tLDR R3, " + writeLoc);
                                }
                                else
                                {
                                    output.WriteLine("\tMOV " + AReg + ", (FP)\t" + quad_comment);
                                    output.WriteLine("\tADI " + AReg + ", #-" + writeLoc);
                                    output.WriteLine("\tLDR R3, (" + AReg + ")");
                                }
                                output.WriteLine("\tTRP 1");
                                returnRegister(AReg);
                            }
                            break;
                        case "READ":
                            string readLoc = getLocation(next_quad.getROp());
                            if (next_quad.getLOp() == "1")
                            {
                                AReg = getNextFreeRegister();
                                output.WriteLine("\tTRP 4\t\t" + quad_comment);
                                output.WriteLine("\tMOV " + AReg + ", (FP)");
                                if (!int.TryParse(readLoc, out value)) { value = int.Parse(symTable[readLoc].getValue()); }
                                output.WriteLine("\tADI " + AReg + ", #-" + value);
                                output.WriteLine("\tSTB R3, (" + AReg + ")");
                                returnRegister(AReg);
                            }
                            else
                            {
                                AReg = getNextFreeRegister();
                                output.WriteLine("\tTRP 2\t\t" + quad_comment);
                                output.WriteLine("\tMOV " + AReg + ", (FP)");
                                if (!int.TryParse(readLoc, out value)) { value = int.Parse(symTable[readLoc].getValue()); }
                                output.WriteLine("\tADI " + AReg + ", #-" + value);
                                output.WriteLine("\tSTR R3, (" + AReg + ")");
                                returnRegister(AReg);
                            }
                            break;
                        case "END":
                            output.WriteLine("\tTRP 0");
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public string getNextFreeRegister()
        {
            string reg = Registers[0];
            Registers.RemoveAt(0);
            return reg;
        }
        public void returnRegister(string reg)
        {
            if (reg.StartsWith("R"))
            {
                Registers.Add(reg);
            }
            else
            {
                throw new SemanticException("Invalid Register to add to list of registers");
            }
        }
        public string getLocation(string symId)
        {
            if (symId.StartsWith("N") || symId.StartsWith("H"))
            {
                return symId;
            }
            else
            {
                string loc = "";
                int offset = int.Parse(symTable[symId].getData()["offset"]);
                offset += 12;
                loc = offset.ToString();
                return loc;
            }
        }
        public void print_over_under(StreamWriter sw)
        {
            sw.WriteLine("overflow\tLDB R3, O1");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O2");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O3");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O4");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O5");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O6");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O7");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O8");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tTRP 0");

            sw.WriteLine("underflow\tLDB R3, O9");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O10");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O11");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O3");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O4");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O5");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O6");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O7");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tLDB R3, O8");
            sw.WriteLine("\tTRP 3");
            sw.WriteLine("\tTRP 0");
        }
    }
}
 