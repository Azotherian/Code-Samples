using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace vm
{
    class Program
    {
        static void Main(string[] args)
        {
            VirtualMachine VM = new VirtualMachine();
            VM.FirstPass(args[0]);
            Console.ReadLine();
        }
    }
    public class VirtualMachine
    {
        const int MEM_SIZE = 1000000;
        const int FIVE = 5;
        int OCValue = 0;
        int data = 0;
        int destination = 0;
        int source = 0;
        int labelValue = 0;
        int pc = 0;
        int tempAddress = 0;
        int tempLabelLocation = 0;
        int nextContextVal = 0;
        int codeSegSize = 0;
        int stackSize = 0;
        int threadSize = 0;
        int threadID = 0;
        int threadLock = -1;
        int threadNum = -1;
        int nextThreadNum = -1;
        int valueInReg = 0;
        bool isDone = false;
        bool testBranch = false;
        bool isInt = false;
        bool isByte = false;
        int position = 0;
        int tempSymbolValue;
        int memCounter = 0;
        int immediateValue = 0;
        string tempString = "";
        char tempChar = ' ';
        byte[] mem = new byte[MEM_SIZE];
        int[] reg = new int[13]; //SL = 9, SP = 10, FP = 11, SB = 12
        Dictionary<string, int> SymbolTable = new Dictionary<string, int>();
        Queue<int> runningThreads = new Queue<int>(); //Used for round robin scheduling
        public void FirstPass(string inFile)
        {
            //-----------
            //First Pass
            //-----------
            using (StreamReader myStream = new StreamReader(inFile))
            {
                while (!myStream.EndOfStream)
                {
                    List<string> lineOfWords = myStream.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    //check first string for Label
                    if (IsOp(lineOfWords[0]) == true)
                    {
                        //find out what type of OpCode it is to move counter ahead by so many bytes to be able to keep track of labels in memeory
                        switch (lineOfWords[0])
                        {
                            case "TRP": // OpCode is 8 bytes
                                memCounter += 8; //moves counter 8 bytes
                                break;
                            case "JMP":// OpCode is 8 bytes
                                memCounter += 8; //moves counter 8 bytes
                                break;
                            case "JMR":// OpCode is 8 bytes
                                memCounter += 8; //moves counter 8 bytes
                                break;
                            case "LDB":
                                if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))
                                {
                                    memCounter += 12;
                                }
                                else
                                {
                                    memCounter += 9;
                                }
                                break;
                            case "END": //OpCode is 4 bytes
                                memCounter += 4; //moves counter by 4 bytes
                                break;
                            case "BLK"://OpCode is 4 bytes
                                memCounter += 4; //moves counter by 4 bytes
                                break;
                            case "LCK"://OpCode is 8 bytes
                                memCounter += 8; //moves counter 8 bytes
                                break;
                            case "ULK"://OpCode is 8 bytes
                                memCounter += 8; //moves counter 8 bytes
                                break;
                            default: // OpCode is any that are not another case and are all 12 bytes long
                                memCounter += 12; //moves counter 12 bytes
                                break;
                        }
                    }
                    else if (!IsReg(lineOfWords[0]) && !IsImmediate(lineOfWords[0], lineOfWords[1]))
                    {
                        AddSymbol(lineOfWords[0], memCounter);
                        if (lineOfWords[1] == ".INT")
                        {
                            memCounter += 4;
                        }
                        else if (lineOfWords[1] == ".BYT")
                        {
                            memCounter++;
                        }
                        else //increment counter by type of OpCode that follows after label is added
                        {
                            //find out what type of OpCode it is to move counter ahead by so many bytes to be able to keep track of labels in memeory
                            switch (lineOfWords[1])
                            {
                                case "TRP": // OpCode is 8 bytes
                                    memCounter += 8; //moves counter 8 bytes
                                    break;
                                case "JMP":// OpCode is 8 bytes
                                    memCounter += 8; //moves counter 8 bytes
                                    break;
                                case "JMR":// OpCode is 8 bytes
                                    memCounter += 8; //moves counter 8 bytes
                                    break;
                                case "LDB":
                                    if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))
                                    {
                                        memCounter += 12;
                                    }
                                    else
                                    {
                                        memCounter += 9;
                                    }
                                    break;
                                case "END": //OpCode is 4 bytes
                                    memCounter += 4; //moves counter by 4 bytes
                                    break;
                                case "BLK"://OpCode is 4 bytes
                                    memCounter += 4; //moves counter by 4 bytes
                                    break;
                                case "LCK"://OpCode is 8 bytes
                                    memCounter += 8; //moves counter 8 bytes
                                    break;
                                case "ULK"://OpCode is 8 bytes
                                    memCounter += 8; //moves counter 8 bytes
                                    break;
                                default: // OpCode is any that are not another case and are all 12 bytes long
                                    memCounter += 12; //moves counter 12 bytes
                                    break;
                            }
                        }
                    }
                    else if (IsInArray(lineOfWords[0]) == true)
                    {
                        //increment memCounter if int
                        if (lineOfWords[0] == ".INT")
                        {
                            memCounter += 4; //increment counter by 4 to make room for number in array in memory
                        }
                        //increment memCounter if char
                        else if (lineOfWords[0] == ".BYT")
                        {
                            memCounter++; //increment counter by 1 to make room for character in array in memory
                        }
                    }
                }
            }
            //-------------
            // Second Pass
            //-------------
            codeSegSize = memCounter;
            pc = SymbolTable["START"];//start PC at end of static data (start of instructions)
            using (StreamReader myStream = new StreamReader(inFile))
            {
                while (!myStream.EndOfStream)
                {
                    List<string> lineOfWords = myStream.ReadLine().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (SymbolTable.ContainsKey(lineOfWords[0]))
                    {
                        //insert data from lineofwords of 2 into mem at symbol table value location
                        if (lineOfWords[1] == ".INT")
                        {
                            InsertInttoMem(int.Parse(lineOfWords[2]));
                        }
                        else if (lineOfWords[1] == ".BYT")
                        {
                            if (lineOfWords[2] == "'")
                            {
                                tempString = " ";
                            }
                            else if (lineOfWords[2].Contains("\\n"))
                            {
                                tempString = lineOfWords[2].Replace("'", string.Empty).Replace("\\n", "\n");
                            }
                            else if (lineOfWords[2].Contains("'"))
                            {
                                tempString = lineOfWords[2].Replace("'", string.Empty);
                            }
                            char tempChar = Convert.ToChar(tempString);
                            InsertChartoMem(tempChar);
                        }
                    }
                    else if (IsInArray(lineOfWords[0]) == true)//if the first string is in an array
                    {
                        if (lineOfWords[0] == ".INT")
                        {
                            InsertInttoMem(int.Parse(lineOfWords[1]));
                        }
                        else if (lineOfWords[0] == ".BYT")
                        {
                            if (lineOfWords[1] == "'")
                            {
                                tempString = " ";
                            }
                            else if (lineOfWords[1].Contains("'"))
                            {
                                tempString = lineOfWords[1].Replace("'", string.Empty);
                            }
                            else
                            {
                                tempString = lineOfWords[2].Replace("'", string.Empty).Replace("\\n", "\n");
                            }
                            tempChar = Convert.ToChar(tempString);
                            InsertChartoMem(tempChar);
                        }
                    }
                    if (IsOp(lineOfWords[0]) == false) //if the first string is the not the OpCode
                    {
                        lineOfWords.RemoveAt(0);
                    }
                    if (IsOp(lineOfWords[0]) == true)
                    {
                        switch (lineOfWords[0])
                        {
                            case "TRP": //OpCode is 0
                                InsertInttoMem(0);
                                //check what kind of TRP
                                if (lineOfWords[1] == "0")
                                {
                                    InsertInttoMem(0);
                                }
                                else if (lineOfWords[1] == "1")
                                {
                                    InsertInttoMem(1);
                                }
                                else if (lineOfWords[1] == "2")
                                {
                                    InsertInttoMem(2);
                                }
                                else if (lineOfWords[1] == "3")
                                {
                                    InsertInttoMem(3);
                                }
                                else if (lineOfWords[1] == "4")
                                {
                                    InsertInttoMem(4);
                                }
                                break;
                            case "JMP": //OpCode is 1
                                InsertInttoMem(1);
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[1]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "JMR": //OpCode is 2
                                InsertInttoMem(2);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                break;
                            case "BNZ": //OpCode is 3
                                InsertInttoMem(3);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "BGT": //OpCode is 4
                                InsertInttoMem(4);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "BLT": //OpCode is 5
                                InsertInttoMem(5);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "BRZ": //OpCode is 6
                                InsertInttoMem(6);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "MOV": //OpCode is 7
                                InsertInttoMem(7);
                                if (lineOfWords[1].Equals("FP"))
                                {
                                    InsertInttoMem(11);
                                }
                                else if (lineOfWords[1].Equals("SP"))
                                {
                                    InsertInttoMem(10);
                                }
                                else
                                {
                                    //store RD into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                }
                                if (lineOfWords[2].Equals("(PC)"))
                                {
                                    //store PC into mem
                                    InsertInttoMem(8);
                                }
                                else if (lineOfWords[2].Equals("(FP)"))
                                {
                                    //store FP into mem
                                    InsertInttoMem(11);
                                }
                                else if (lineOfWords[2].Equals("(SP)"))
                                {
                                    //store SP into mem
                                    InsertInttoMem(10);
                                }
                                else if (lineOfWords[2].Equals("(SL)"))
                                {
                                    //store SL into mem
                                    InsertInttoMem(9);
                                }
                                else if (lineOfWords[2].Equals("(SB)"))
                                {
                                    //store SB into mem
                                    InsertInttoMem(12);
                                }
                                else
                                {
                                    //store RS into mem
                                    InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                }
                                break;
                            case "LDA":
                                //OpCode is 8
                                InsertInttoMem(8);
                                //Store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store label value into mem
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "STR":
                                //check what type of STR (indirect or memory)
                                if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))//if second operand is a register indirect
                                {
                                    tempString = lineOfWords[2].Replace("(", string.Empty).Replace(")", string.Empty);
                                    //indrect OpCode is 21
                                    InsertInttoMem(21);
                                    if (lineOfWords[1].Equals("FP"))
                                    {
                                        InsertInttoMem(11);
                                    }
                                    else
                                    {
                                        //store RS into mem
                                        InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    }
                                    if (lineOfWords[2].Equals("(FP)"))
                                    {
                                        //store FP into mem
                                        InsertInttoMem(11);
                                    }
                                    else if (lineOfWords[2].Equals("(SP)"))
                                    {
                                        //store SP into mem
                                        InsertInttoMem(10);
                                    }
                                    else
                                    {
                                        //store RG into mem
                                        InsertInttoMem(int.Parse(tempString[1].ToString()));
                                    }
                                }
                                else
                                {
                                    //OpCode for STR is 9
                                    InsertInttoMem(9);
                                    //store RS into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store label value into mem
                                    tempSymbolValue = SymbolTable[lineOfWords[2]];
                                    InsertInttoMem(tempSymbolValue);
                                }
                                break;
                            case "LDR":
                                //check what type of LDR (indirect or memory)
                                if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))//if second operand is a register indirect
                                {
                                    tempString = lineOfWords[2].Replace("(", string.Empty).Replace(")", string.Empty);
                                    //Indirect OpCode is 22
                                    InsertInttoMem(22);
                                    if (lineOfWords[1].Equals("FP"))
                                    {
                                        InsertInttoMem(11);
                                    }
                                    else
                                    {
                                        //store RD into mem
                                        InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    }
                                    if (lineOfWords[2].Equals("(FP)"))
                                    {
                                        //store FP into mem
                                        InsertInttoMem(11);
                                    }
                                    else if (lineOfWords[2].Equals("(SP)"))
                                    {
                                        //store SP into mem
                                        InsertInttoMem(10);
                                    }
                                    else
                                    {
                                        //store RG into mem
                                        InsertInttoMem(int.Parse(tempString[1].ToString()));
                                    }
                                }
                                else
                                {
                                    //Memory OpCode is 10
                                    InsertInttoMem(10);
                                    //store RD into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store value from label
                                    tempSymbolValue = SymbolTable[lineOfWords[2]];
                                    InsertInttoMem(tempSymbolValue);
                                }
                                break;
                            case "STB":
                                //check what type of STB (indirect or memeory)
                                if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))//if second operand is a register indirect
                                {
                                    tempString = lineOfWords[2].Replace("(", string.Empty).Replace(")", string.Empty).Replace("R", string.Empty);
                                    //indirect OpCode is 23
                                    InsertInttoMem(23);
                                    //store RS into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store RG into mem
                                    InsertInttoMem(int.Parse(tempString));
                                }
                                else
                                {
                                    //OpCode for STB is 11
                                    InsertInttoMem(11);
                                    //store RS into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store label value into mem
                                    tempSymbolValue = SymbolTable[lineOfWords[2]];
                                    InsertInttoMem(tempSymbolValue);
                                }
                                break;
                            case "LDB":
                                //check what type of LBD (indirect or memory)
                                if (lineOfWords[2].Contains("(") && lineOfWords[2].Contains(")"))//if second operand is a register indirect
                                {
                                    tempString = lineOfWords[2].Replace("(", string.Empty).Replace(")", string.Empty).Replace("R", string.Empty);
                                    //indirect OpCode is 24
                                    InsertInttoMem(24);
                                    //store RD into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store RG into mem
                                    InsertInttoMem(int.Parse(tempString));
                                }
                                else
                                {
                                    // Memory OpCode is 12
                                    InsertInttoMem(12);
                                    //Store RD into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                    //store label value into mem
                                    tempChar = (char)SymbolTable[lineOfWords[2]];
                                    InsertChartoMem(tempChar);
                                }
                                break;
                            case "ADD": //OpCode is 13
                                InsertInttoMem(13);
                                //store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                break;
                            case "ADI": //OpCode is 14
                                InsertInttoMem(14);
                                if (lineOfWords[1].Equals("FP"))
                                {
                                    InsertInttoMem(11);
                                }
                                else if (lineOfWords[1].Equals("SP"))
                                {
                                    InsertInttoMem(10);
                                }
                                else
                                {
                                    //store RD into mem
                                    InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                }
                                //store immediate value into mem
                                tempString = lineOfWords[2].Replace("#", string.Empty);
                                InsertInttoMem(int.Parse(tempString));
                                break;
                            case "SUB": //OpCode is 15
                                InsertInttoMem(15);
                                //store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                break;
                            case "MUL": //OpCode is 16
                                InsertInttoMem(16);
                                //store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                break;
                            case "DIV": //OpCode is 17
                                InsertInttoMem(17);
                                //store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                break;
                            case "CMP": //OpCode is 20
                                InsertInttoMem(20);
                                //store RD into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[2][1].ToString()));
                                break;
                            /*case "RUN": //OpCode is 25
                                InsertInttoMem(25);
                                //store RS into mem
                                InsertInttoMem(int.Parse(lineOfWords[1][1].ToString()));
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[2]];
                                InsertInttoMem(tempSymbolValue);
                                break;*/
                            case "END": //OpCode is 26
                                InsertInttoMem(26);
                                break;
                            /*case "BLK": //OpCode is 27
                                InsertInttoMem(27);
                                break;
                            case "LCK": //OpCode is 28
                                InsertInttoMem(28);
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[1]];
                                InsertInttoMem(tempSymbolValue);
                                break;
                            case "ULK": //OpCode is 29
                                InsertInttoMem(29);
                                //store value from label
                                tempSymbolValue = SymbolTable[lineOfWords[1]];
                                InsertInttoMem(tempSymbolValue);
                                break;*/
                            default:
                                break;
                        }
                    }
                }
            }
            reg[8] = pc;
            reg[9] = codeSegSize; //SL = end of code segment (hard coded)
            reg[10] = 999999; //SP = last mem location (for start of stack)
            reg[11] = 999999; //FP = last mem location (for start of stack)
            reg[12] = 999999; //SB = last mem location (for start of stack)

            //create thread 0 context
            /*nextContextVal = 999999; //Next available spot in mem for context value
            InsertInttoStack(nextContextVal, reg[0]);//store reg 0 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[1]);//store reg 1 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[2]);//store reg 2 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[3]);//store reg 3 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[4]);//store reg 4 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[5]);//store reg 5 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[6]);//store reg 6 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[7]);//store reg 7 into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[8]);//store PC into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[9]);//store SL into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[10]);//store SP into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[11]);//store FP into thread stack
            nextContextVal -= 4;
            InsertInttoStack(nextContextVal, reg[12]);//store SB into thread stack
            nextContextVal -= 4;*/
            //---------------
            //Virtual Machine
            //---------------
            do
            {
                int OpCode = RetrieveIntFromMem(reg[8]);
                reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                switch (OpCode)
                {
                    case 0: //OpCode is a TRP
                        OCValue = RetrieveIntFromMem(reg[8]);
                        if (OCValue == 0) { isDone = true; }
                        else if (OCValue == 1)
                        {
                            string registerInt = reg[3].ToString();
                            Console.Write(registerInt);
                        }
                        else if (OCValue == 2)
                        {
                            string inputString = Console.ReadLine();
                            reg[3] = int.Parse(inputString);
                        }
                        else if (OCValue == 3)
                        {
                            char registerThree = Convert.ToChar(reg[3]);
                            Console.Write(registerThree);
                        }
                        else if (OCValue == 4)
                        {
                            //do getchar() stuff
                            int inputCharValue = Console.Read();
                            if (inputCharValue == 13) //If a '\r'
                            {
                                inputCharValue = Console.Read();
                            }
                            if (inputCharValue == 10) //If a '\n'
                            {
                                inputCharValue = Console.Read();
                            }
                            tempChar = Convert.ToChar(inputCharValue);
                            inputCharValue = int.Parse(tempChar.ToString());
                            reg[3] = inputCharValue;
                        }
                        isInt = true;
                        break;
                    case 1: //OpCode is a JMP
                        reg[8] = RetrieveIntFromMem(reg[8]) - 4; //set PC to label location in memory
                        isInt = true;
                        break;
                    case 2: //OpCode is a JMR
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[source];
                        break;
                    case 3: //OpCode is a BNZ
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        testBranch = BranchonZero(reg[source]);
                        if (testBranch == false) //take BNZ
                        {
                            //set PC to label location in memory
                            tempLabelLocation = RetrieveIntFromMem(reg[8]) - 4;
                            reg[8] = tempLabelLocation;
                        }
                        isInt = true;
                        break;
                    case 4: //OpCode is BGT
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        testBranch = BranchonPositive(reg[source]);
                        if (testBranch == true)
                        {
                            //set PC to label location in memory
                            tempLabelLocation = RetrieveIntFromMem(reg[8]) - 4;
                            reg[8] = tempLabelLocation;
                        }
                        isInt = true;
                        break;
                    case 5: //OpCode is a BLT
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        testBranch = BranchonNegative(reg[source]);
                        if (testBranch == true)
                        {
                            //set PC to label location in memory
                            tempLabelLocation = RetrieveIntFromMem(reg[8]) - 4;
                            reg[8] = tempLabelLocation;
                        }
                        isInt = true;
                        break;
                    case 6: //OpCode is a BRZ
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        testBranch = BranchonZero(reg[source]);
                        if (testBranch == true) //take BRZ
                        {
                            //set PC to label location in memory
                            tempLabelLocation = RetrieveIntFromMem(reg[8]) - 4;
                            reg[8] = tempLabelLocation;
                        }
                        isInt = true;
                        break;
                    case 7: //OpCode is a MOV
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] = reg[source];
                        isInt = true;
                        break;
                    case 8: //OpCode is an LDA
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        tempAddress = RetrieveIntFromMem(reg[8]); //get base address
                        reg[destination] = tempAddress; //set RD to have base address
                        isInt = true;
                        break;
                    case 9: //OpCode is a Memory STR
                        source = RetrieveCharFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        tempLabelLocation = RetrieveIntFromMem(reg[8]);
                        InsertInttoMem(tempLabelLocation, reg[source]);//replace value at memory location with source value
                        isInt = true;
                        break;
                    case 10: //OpCode is a Memory LDR
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        labelValue = RetrieveIntFromMem(mem[reg[8]]);
                        reg[destination] = labelValue;
                        isInt = true;
                        break;
                    case 11: //OpCode is a Memory STB
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        tempLabelLocation = RetrieveIntFromMem(reg[8]);
                        InsertInttoMem(tempLabelLocation, reg[source]);
                        isInt = true;
                        break;
                    case 12: //OpCode is a Memory LDB
                        destination = RetrieveIntFromMem(reg[8]);
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        labelValue = RetrieveCharFromMem(mem[reg[8]]);
                        reg[destination] = labelValue;
                        isByte = true;
                        break;
                    case 13: //OpCode is an ADD
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] += reg[source];
                        isInt = true;
                        break;
                    case 14: //OpCode is an ADI
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        immediateValue = RetrieveIntFromMem(reg[8]); //get immediate value
                        reg[destination] += immediateValue;
                        isInt = true;
                        break;
                    case 15: //OpCode is a SUB
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] -= reg[source];
                        isInt = true;
                        break;
                    case 16: //OpCode is a MUL
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] *= reg[source];
                        isInt = true;
                        break;
                    case 17: //OpCode is a DIV
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] /= reg[source];
                        isInt = true;
                        break;
                    case 20: //OpCode is a CMP
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[destination] = CompareRegisters(reg[destination], reg[source]);
                        isInt = true;
                        break;
                    case 21: //OpCode is a Register STR
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        data = RetrieveIntFromMem(reg[8]); //get RG
                        if (data == 11 || source == 11)
                        {
                            InsertInttoStack(reg[data], reg[source]);
                        }
                        else if (data == 10 || source == 10)
                        {
                            InsertInttoStack(reg[data], reg[source]);
                        }
                        else
                        {
                            if (reg[data] > codeSegSize) //if the location is on the stack
                            {
                                InsertInttoStack(reg[data], reg[source]);
                            }
                            else
                            {
                                InsertInttoMem(reg[data], reg[source]); //store data in RG into memory location of RS
                            }
                        }
                        isInt = true;
                        break;
                    case 22: //OpCode is a Register LDR
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        data = RetrieveIntFromMem(reg[8]); //get RG
                        if (data == 11 || source == 11)
                        {
                            reg[destination] = RetrieveIntFromStack(reg[data]);
                        }
                        else if (data == 10 || source == 10)
                        {
                            reg[destination] = RetrieveIntFromStack(reg[data]);
                        }
                        else
                        {
                            if (reg[data] > codeSegSize) //the location is on the stack, not in the code segment
                            {
                                reg[destination] = RetrieveIntFromStack(reg[data]);
                            }
                            else
                            {
                                reg[destination] = RetrieveIntFromMem(reg[data]); //store RG data into RD
                            }
                        }
                        isInt = true;
                        break;
                    case 23: //OpCode is a Register STB
                        source = RetrieveIntFromMem(reg[8]); //get source register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        data = RetrieveIntFromMem(reg[8]); //get RG
                        tempChar = Convert.ToChar(reg[source]);
                        InsertChartoMem(reg[data], tempChar); //store data in RG into memory location of RS
                        isInt = true;
                        break;
                    case 24: //OpCode is a Register LDB
                        destination = RetrieveIntFromMem(reg[8]); //get destination register
                        reg[8] = reg[8] + 4; //increment PC by 4 bytes to next piece of current instruction
                        data = RetrieveIntFromMem(reg[8]); //get RG
                        reg[destination] = RetrieveCharFromMem(reg[data]);
                        isInt = true;
                        break;
                    /*case 25: //OpCode is a RUN
                        threadID++;
                        destination = RetrieveIntFromMem(reg[8]);
                        reg[8] = reg[8] + 4;
                        int[] copyReg = new int[13]; //create new register array to copy registers
                        tempLabelLocation = RetrieveIntFromMem(reg[8]);
                        Array.Copy(reg, copyReg, 13); //Copy Registers
                        copyReg[7] = threadID; //R7 = thread id
                        int threadOffset = (threadSize * copyReg[7]);
                        copyReg[8] = tempLabelLocation; //setting PC
                        copyReg[9] -= threadOffset; //thread SL = current SL - (thread size * thread id)
                        copyReg[10] -= threadOffset; //thread SP = current SP - (thread size * thread id)
                        copyReg[11] -= threadOffset; //thread FP = current FP - (thread size * thread id)
                        copyReg[12] -= threadOffset; //thread SB = current SB - (thread size * thread id)
                        //new thread context creation
                        nextContextVal = (MEM_SIZE - threadOffset) - 1; //set the first location to insert new stack context
                        InsertInttoStack(nextContextVal, copyReg[0]);//store reg 0 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[1]);//store reg 1 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[2]);//store reg 2 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[3]);//store reg 3 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[4]);//store reg 4 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[5]);//store reg 5 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[6]);//store reg 6 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[7]);//store reg 7 into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[8]);//store PC into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[9]);//store SL into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[10]);//store SP into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[11]);//store FP into thread stack
                        nextContextVal -= 4;
                        InsertInttoStack(nextContextVal, copyReg[12]);//store SB into thread stack
                        nextContextVal -= 4;
                        AddThread(copyReg[7]);//add new thread to thread queue
                        isInt = true;
                        break;*/
                    case 26: //OpCode is a END
                        if (reg[7] != 0) //if not the main thread
                        {
                            reg[7] = -1;
                        }
                        isInt = true;
                        break;
                    /*case 27: //OpCode is a BLK
                        if (runningThreads.Count != 0)//other threads need to run
                        {
                            reg[8] -= 4; //reset back to BLK
                        }
                        break;
                    case 28: //OpCode is a LCK
                        tempLabelLocation = RetrieveIntFromMem(reg[8]);
                        labelValue = RetrieveIntFromMem(mem[reg[8]]); //get label value
                        if (labelValue == -1)
                        {
                            InsertInttoMem(tempLabelLocation, reg[7]); //insert the current thread ID into the label location to lock
                            isInt = true;
                        }
                        else
                        {
                            reg[8] -= 4;//reset back to LCK
                        }
                        break;
                    case 29: //OpCode is a ULK
                        tempLabelLocation = RetrieveIntFromMem(reg[8]);
                        labelValue = RetrieveIntFromMem(mem[reg[8]]); //get label value
                        if (labelValue == reg[7]) //if current thread had locked the mutex
                        {
                            InsertInttoMem(tempLabelLocation, -1); //set mutex to -1
                        }
                        isInt = true;
                        break;*/
                    default:
                        break;
                }
                if (isInt == true)
                {
                    reg[8] = reg[8] + 4; //increment PC by 4 bytes to next instruction
                    isInt = false;
                }
                else if (isByte == true)
                {
                    reg[8] = reg[8] + 1; //increment PC by 4 bytes to next instruction
                    isByte = false;
                }
                if (isDone == false)
                {
                    //ContextSwitch();
                }
            } while (isDone == false);
        }
        /*void ContextSwitch()
        {
            if (reg[7] != -1)
            {
                AddThread(reg[7]);
                threadNum = reg[7];
                nextContextVal = 999999 - (threadSize * threadNum);
                InsertInttoStack(nextContextVal, reg[0]);//store reg 0 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[1]);//store reg 1 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[2]);//store reg 2 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[3]);//store reg 3 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[4]);//store reg 4 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[5]);//store reg 5 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[6]);//store reg 6 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[7]);//store reg 7 into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[8]);//store PC into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[9]);//store SL into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[10]);//store SP into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[11]);//store FP into thread stack
                nextContextVal -= 4;
                InsertInttoStack(nextContextVal, reg[12]);//store SB into thread stack
                nextContextVal -= 4;
            }
            //switch registers to new thread
            threadNum = runningThreads.Dequeue(); ;//set current thread num to next thread num
            nextContextVal = 999999 - (threadSize * threadNum);
            reg[0] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[1] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[2] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[3] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[4] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[5] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[6] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[7] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[8] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[9] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[10] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[11] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
            reg[12] = RetrieveIntFromStack(nextContextVal);
            nextContextVal -= 4;
        }*/
        void AddThread(int num)
        {
            runningThreads.Enqueue(num);
        }
        bool BranchonZero(int a)
        {
            if (a == 0) { return true; }
            else { return false; }
        }
        bool BranchonNegative(int a)
        {
            if (a < 0) { return true; }
            else { return false; }
        }
        bool BranchonPositive(int a)
        {
            if (a > 0) { return true; }
            else { return false; }
        }
        bool IsOp(string s)
        {
            //check if an OpCode in an ENUM
            foreach (OpCodes label in Enum.GetValues(typeof(OpCodes)))
            {
                if (s == label.ToString())
                {
                    return true;
                }
            }
            return false;
        }
        void AddSymbol(string s, int a)
        {
            if (!SymbolTable.ContainsKey(s))
            {
                SymbolTable.Add(s, a);
            }
        }
        bool IsReg(string s)
        {
            if (s.StartsWith("R"))
            {
                if (s.Length == 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        bool IsImmediate(string s, string opcode)
        {
            if (opcode == ".INT" || opcode == ".BYT")
            {
                return false;
            }
            else
            {
                foreach (OpCodes label in Enum.GetValues(typeof(OpCodes)))
                {
                    if (opcode == label.ToString())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        bool IsLabel(string s)
        {
            if (SymbolTable.ContainsKey(s))
            {
                return true;
            }
            return false;
        }
        bool IsInArray(string s)
        {
            if (s == ".INT")
            {
                return true;
            }
            else if (s == ".BYT")
            {
                return true;
            }
            return false;
        }
        int CompareRegisters(int destination, int source)
        {
            if (destination == source)
            {
                return 0;
            }
            else if (destination > source)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
        int RetrieveIntFromMem(int index)
        {
            return mem[index++] | (mem[index++] << 8) | (mem[index++] << 16) | (mem[index++] << 24);
        }
        int RetrieveIntFromStack(int index)
        {
            return mem[index--] | (mem[index--] << 8) | (mem[index--] << 16) | (mem[index--] << 24);
        }
        char RetrieveCharFromMem(int index)
        {
            return Convert.ToChar(mem[index]);
        }
        void InsertInttoMem(int number)
        {
            mem[position++] = (byte)(number >> 0);
            mem[position++] = (byte)(number >> 8);
            mem[position++] = (byte)(number >> 16);
            mem[position++] = (byte)(number >> 24);
        }
        void InsertInttoMem(int index, int value)
        {
            mem[index++] = (byte)(value >> 0);
            mem[index++] = (byte)(value >> 8);
            mem[index++] = (byte)(value >> 16);
            mem[index++] = (byte)(value >> 24);
        }
        void InsertInttoStack(int index, int value)
        {
            mem[index--] = (byte)(value >> 0);
            mem[index--] = (byte)(value >> 8);
            mem[index--] = (byte)(value >> 16);
            mem[index--] = (byte)(value >> 24);
        }
        void InsertChartoMem(char character)
        {
            mem[position++] = Convert.ToByte(character);
        }
        void InsertChartoMem(int index, char character)
        {
            mem[index] = Convert.ToByte(character);
        }
        enum OpCodes { JMP, JMR, BNZ, BLT, BGT, BRZ, MOV, LDA, STR, LDR, STB, LDB, ADD, ADI, SUB, MUL, DIV, CMP, TRP, RUN, END, BLK, LCK, ULK };
    }
}

