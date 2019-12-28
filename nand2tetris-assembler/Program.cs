using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace nand2tetris_assembler
{
    class Assembler
    {
        // FileString is path + file name
        public string FileString { get; }
        private Dictionary<string, int> SymTable;
        private Dictionary<string, string> JmpTable;
        private Dictionary<string, string> compTable;

        // index of register in memory to be assigned to next
        // new variable/label
        private int freeIndex;

        public Assembler(String file, String path = "/Users/aashray/Projects/nand2tetris-assembler/nand2tetris-assembler/assembly/")
        {
            string location = path + file;
            if (!File.Exists(@location)) {
                throw new FileNotFoundException(location + " not found");
            } else
            {
                this.FileString = location;
                // initialize table with 15 named registers
                // as well as named memory locations marking
                // the memory mappings for the screen and keyboard
                // as well as a set of pre-defined variables
                this.SymTable = new Dictionary<string, int>
                {
                    {"R0", 0},
                    {"R1", 1},
                    {"R2", 2},
                    {"R3", 3},
                    {"R4", 4},
                    {"R5", 5},
                    {"R6", 6},
                    {"R7", 7},
                    {"R8", 8},
                    {"R9", 9},
                    {"R10", 10},
                    {"R11", 11},
                    {"R12", 12},
                    {"R13", 13},
                    {"R14", 14},
                    {"R15", 15},
                    {"SCREEN", 16384},
                    {"KBD", 24576},
                    {"SP", 0},
                    {"LCL", 1},
                    {"ARG", 2},
                    {"THIS", 3},
                    {"THAT", 4}
                };
                // binary codes for each differnt jump
                this.JmpTable = new Dictionary<string, string>
                {
                    {"JMP", "111"},
                    {"JLE", "110"},
                    {"JNE", "101"},
                    {"JLT", "100"},
                    {"JGE", "011"},
                    {"JEQ", "010"},
                    {"JGT", "001"},

                };
                // binary codes for each different computation
                this.compTable = new Dictionary<string, string>
                {
                    {"0", "0101010"},
                    {"1", "0111111"},
                    {"-1", "0111010"},
                    {"D", "0001100"},
                    {"A", "0110000"},
                    {"M", "1110000"},
                    {"!D", "0001101"},
                    {"!A", "0110001"},
                    {"!M", "1110001"},
                    {"-D", "0001111"},
                    {"-A", "0110011"},
                    {"-M", "1110011" },
                    {"D+1", "0011111"},
                    {"1+D", "0011111"},
                    {"A+1", "0110111"},
                    {"1+A", "0110111"},
                    {"M+1", "1110111"},
                    {"1+M", "1110111"},
                    {"D-1", "0001110"},
                    {"A-1", "0110010"},
                    {"M-1", "1110010"},
                    {"D+A", "0000010"},
                    {"A+D", "0000010"},
                    {"D+M", "1000010"},
                    {"M+D", "1000010"},
                    {"D-A", "0010011"},
                    {"A-D", "0000111"},
                    {"D-M", "1010011"},
                    {"M-D", "1000111"},
                    {"D&A", "0000000"},
                    {"A&D", "0000000"},
                    {"D&M", "1000000"},
                    {"M&D", "1000000"},
                    {"D|A", "0010101"},
                    {"A|D", "0010101"},
                    {"D|M", "1010101"},
                    {"M|D", "1010101"},
                };
                this.freeIndex = 16;
            }
        }

        public void Assemble()
        {

            // open input assembly file, and create output
            using (StreamReader sr = System.IO.File.OpenText(@FileString))
            {
                Console.WriteLine("============= HANDLING LABEL DECLARATION ===========");
                // iterate  over all instructions (lines) of the assembly file
                this.HandleLabels(sr);
                Console.WriteLine("========== END OF LABEL DECLARATION HANDLING ========");

            }
            // generate output machine language file
            using (StreamReader sr = System.IO.File.OpenText(@FileString))
            using (StreamWriter sw = System.IO.File.CreateText(FileString.Split('.')[0] + ".hack"))
            {
                // iterate  over all instructions (lines) of the assembly file

                while (!sr.EndOfStream)
                {
                    this.HandleLine(sr.ReadLine(), sw);
                }
            }
            Console.WriteLine("finished assembling " + FileString);
        }

        // handler for first pass over of assembly code,
        // checks for all label declarations and adds
        // values for labels to symbol table
        private void HandleLabels(StreamReader sr)
        {
            // tracker for current line number, used to assign values
            // for label declarations in symbol table
            int lineNum = 0;
            // iterate over all lines of file
            while (!sr.EndOfStream)
            {
                // remove any leading/trailing WS, and any WS between
                // characters
                string line = sr.ReadLine().Trim().Replace(" ", "");
                // check if line will be skipped when we assemble to machine language
                // only increment lineNum when line will not be skipped
                if (!this.checkSkipLine(line))
                {
                    // if label declaration found, add to sym table
                    if (this.checkLabel(line))
                    {
                        this.SymTable[line.Substring(1, line.Length - 2)] = lineNum;
                    } else
                    {
                        lineNum++;
                    }
                }
            }
        }

        // regex conditional, check if we should skip line of code
        private bool checkSkipLine(string line)
        {
            Regex blank = new Regex(@"^\s?\r?\n?$");
            Regex comment = new Regex(@"^//.*$");
            // if whitespace or comment, return without writing to file
            if (comment.IsMatch(line) || blank.IsMatch(line))
            {
                return true;
            }
            return false;
        }

        // regex conditional, check if line represents A-instruction
        // of the form @value
        private string checkA(string line)
        {
            // regex for a-instructions of form @value
            Regex a_instruction = new Regex(@"^(@)([^/\s]+)(.*)$");
            var match = a_instruction.Match(line);
            if (match.Success)
            {
                // get the value symbol or number
                return match.Groups[2].Value;
            }
            return null;
        }

        // regex conditional, check if line represents a label instruction
        // of form (XXX)
        private bool checkLabel(string line)
        {
            // regex for assembly labels e.g. (LOOP)
            Regex label = new Regex(@"^\(.+\)$");
            if (label.IsMatch(line))
            {
                return true;
            }
            return false;
        }

        private void HandleLine(string line, StreamWriter sw)
        {
            // remove leading/trailing whitespace and
            // whitespace between characters
            line = line.Trim().Replace(" ", "");
            // if whitespace or comment, return without writing to file
            // also, if label, ignore, because labels are irrelevant to
            // output machine language, only the symbol table values
            // for labels are found on first pass, and applied in A-instructions
            if (checkSkipLine(line) || checkLabel(line)) {
                return;
            }

            // check if instruction is an A or C instruction, handle accordingly
            // A_value will be the variable name, or number for the A-instruction
            // and will be null if not an A-instruction
            string A_value = this.checkA(line);
            if (A_value != null)
            {
                this.HandleAInstruction(A_value, sw);
            } else
            {
                this.HandleCInstruction(line, sw);
            }
        }

        // handler for C-instructions, of form dest = comp; jmp
        // where both dest and jmp are optional terms
        private void HandleCInstruction(string line, StreamWriter sw) {
            // remove any comments/whit
            line = this.StripCommentsWS(line);
            // op code for c-instruction
            // along with 2 unused bits
            string opcode = "111";
            // 000 jump code if no jump
            string jmp_code = "000";
            // dest bits for each register 0 by default
            string[] destBits = { "0", "0", "0" };
            // comp bits
            string compBits = "";
            (string destination, string comp_jmp) = this.SplitCInstruction(line, "=");
            // if we couldn't split on equals, then this C-instruction is simply a
            // jump instruction of form XXX;JMP, where XXX is some variable or number
            // and JMP is some jump code
            if(destination != null && comp_jmp != null)
            {
                // set the appropriate destination bits
                if (destination.Contains("A"))
                {
                    destBits[0] = "1";
                }

                if (destination.Contains("D"))
                {
                    destBits[1] = "1";

                }

                if (destination.Contains("M"))
                {
                    destBits[2] = "1";
                }

                // check if comp_jmp is only a computation, or a computation
                // and a jump code
                (string comp, string jmp) = this.SplitCInstruction(comp_jmp, ";");
                if (comp != null && jmp != null)
                {
                    // case of C-instruction of form dest=comp;jmp
                    // use comp and jmp

                    // get binary code for the computation
                    compBits = this.compTable[comp];

                    // update jump binary code
                    jmp_code = this.JmpTable[jmp];
                    string res = opcode + compBits + String.Join("", destBits) + jmp_code;
                    sw.WriteLine(res);
                }
                else
                {
                   
                    // get  binary code for the computation
                    compBits = this.compTable[comp_jmp];

                    // case of C-instruction of form dest=comp, use comp_jmp
                    string res = opcode + compBits + String.Join("", destBits) + jmp_code;
                    sw.WriteLine(res);
                }
            } else
            {
                // value is used for getting the compBits
                (string value, string jumpCode) = this.SplitCInstruction(line, ";");

                // get binary code for the specified computation
                compBits = this.compTable[value];

                if (value != null && jumpCode != null)
                {
                    jmp_code = this.JmpTable[jumpCode];
                    string res = opcode + compBits + String.Join("", destBits) + jmp_code;
                    sw.WriteLine(res);
                }
                else
                {
                    throw new Exception("invalid c instruction " + line);

                }
            }
        }

        private (string dest1, string dest2) SplitCInstruction(string line, string splitter)
        {
            Regex splitEquals = new Regex(@"^([^=]+)(" + splitter + ")(.*)$");
            var match = splitEquals.Match(line);
            if (!match.Success)
            {
                return (dest1: null, dest2: null);
            }
            return (dest1: match.Groups[1].Value, dest2: match.Groups[3].Value);
        }

        private string StripCommentsWS(string line)
        {
            Regex trailingComment = new Regex(@"^(//)(.*)$");
            var matchTrailComment = trailingComment.Match(line);
            // get line after trailing comment, if exist
            if (matchTrailComment.Success)
            {
                line = matchTrailComment.Groups[2].Value;
            }

            Regex LeadingWSComment = new Regex(@"^([^/\s]+)(.*)$");
            var matchLeadWSComment = LeadingWSComment.Match(line);
            // remove leading WS/comment, if exist
            if (matchLeadWSComment.Success)
            {
                line = matchLeadWSComment.Groups[1].Value;
            }
            return line;

        }

        // handler for A-instructions, of form @value, in the case of
        // an integer value (e.g. @42), should simply return the
        // value as a 16-bit binary number, with the MSB being set
        // to 0 (op code of A-instructions)
        private void HandleAInstruction(string value, StreamWriter sw)
        {
            //string binaryCode = "0";
            // get characters of A-instruction after @ symbol
            bool isNumber = int.TryParse(value, out int numericValue);
            // if isNumber, then the A-instruction is for a numerical value
            // and we do not need to consider a symbol, otherwise, consider the symbol
            if (!isNumber)
            {
                bool isSymbol = this.SymTable.TryGetValue(value, out numericValue);
                // if symbol already exists, no need to consider anything further
                // otherwise, need to add new symbol to table
                if (!isSymbol)
                {
                    this.SymTable[value] = freeIndex;
                    numericValue = freeIndex;
                    freeIndex++;
                }
            }
            string binaryCode = "0";
            for(int i = 1; i <= 15; i++)
            {
                int bit = (int)Math.Pow(2, 15 - i);
                if(bit <= numericValue)

                {
                    binaryCode += "1";
                    numericValue -= bit;
                } else
                {
                    binaryCode += "0";
                }
            }
            sw.WriteLine(binaryCode);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Aashray's nand2tetris HACK Assembler");
            Console.Write("enter directory ");
            string path = Console.ReadLine();
            Console.WriteLine();

            Console.Write("enter file name: ");
            string file = Console.ReadLine();
            while(file.Length > 0)
            {
                Console.WriteLine();
                Assembler assembler;
                if (path.Length > 0)
                {
                    assembler = new Assembler(file, path);
                }
                else
                {
                    assembler = new Assembler(file);

                }
                assembler.Assemble();
                Console.Write("enter file name: ");
                file = Console.ReadLine();
            }
            
        }
    }
}
