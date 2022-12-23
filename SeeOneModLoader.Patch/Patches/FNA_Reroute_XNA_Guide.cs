using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ILRepacking;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SeeOneModLoader.Patch.Patches
{
    [EngineAttribute(Engine.FNA)]
    public class FNA_Reroute_XNA_Guide : IPatch
    {
        private static string NAMESPACE = "BasicXNAProject";
        private static string GAME_CLASS_NAME = "Game1";
        private static string SLIMINPUTWRAPPER_CLASS_NAME = "SlimInputWrapper";
        private static string XGUIDE_CLASS_NAME = "XGuide";
        private static string PATCH_REMOVAL_TYPE_SLIMINPUT_INIT = "Microsoft.Xna.Framework.GameWindow Microsoft.Xna.Framework.Game::get_Window()";
        private static string PATCH_REMOVAL_TYPE_SLIMINPUT_UPDATE = "System.Void SlimInput.SlimGamePad::Update()";

        public void Patch(Patcher patcher, AssemblyDefinition assembly)
        {
            TypeDefinition game1Definition = assembly.MainModule.GetType(NAMESPACE, GAME_CLASS_NAME);

            foreach (MethodDefinition methodDefinition in game1Definition.Methods)
            {
                if (methodDefinition.Name == "UpdateGameState")
                {
                    bool searching = false;
                    List<Instruction> relevantInstructions = new List<Instruction>();
                    object callOperand = null;
                    List<Instruction> firstValueInstructions = new List<Instruction>();
                    List<Instruction> instructions = new List<Instruction>(methodDefinition.Body.Instructions);

                    for (int i = 0; i < instructions.Count; i++)
                    {
                        Instruction instruction = instructions[i];

                        if (searching)
                        {
                            relevantInstructions.Add(instruction);

                            if (firstValueInstructions.Count == 0 && instruction.OpCode.Code == Code.Ldstr)
                            {
                                firstValueInstructions.Add(methodDefinition.Body.Instructions[i - 2]);
                                firstValueInstructions.Add(methodDefinition.Body.Instructions[i - 1]);
                            }
                        }

                        if (instruction.OpCode.Code == Code.Newobj && !searching)
                        {
                            MethodReference operandMethodReference = (MethodReference)instruction.Operand;
                            if (operandMethodReference.FullName == "System.Void System.Object::.ctor()")
                            {
                                searching = true;
                                relevantInstructions.Add(methodDefinition.Body.Instructions[i - 1]);
                                relevantInstructions.Add(instruction);
                            }
                        }
                        else if(searching && instruction.OpCode.Code == Code.Pop)
                        {
                            searching = false;
                            foreach (Instruction relevantInstruction in relevantInstructions)
                            {
                                if (relevantInstruction.OpCode.Code == Code.Call && relevantInstruction.Operand != null)
                                {
                                    MethodReference operandMethodDefinition = (MethodReference)relevantInstruction.Operand;
                                    //System.IAsyncResult BasicXNAProject.XGuide::BeginShowMessageBox(Microsoft.Xna.Framework.PlayerIndex,System.String,System.String,System.Collections.Generic.IEnumerable`1<System.String>,System.Int32,Microsoft.Xna.Framework.GamerServices.MessageBoxIcon,System.AsyncCallback,System.Object)
                                    if (operandMethodDefinition.DeclaringType.FullName != "BasicXNAProject.XGuide")
                                    {
                                        relevantInstruction.Operand = callOperand;

                                        foreach (Instruction instr in relevantInstructions)
                                        {
                                            if (instr.OpCode.Code == Code.Ldstr)
                                            {
                                                methodDefinition.Body.Instructions.Insert(methodDefinition.Body.Instructions.IndexOf(instr), firstValueInstructions[0]);
                                                methodDefinition.Body.Instructions.Insert(methodDefinition.Body.Instructions.IndexOf(instr), firstValueInstructions[1]);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (callOperand == null)
                                        {
                                            callOperand = relevantInstruction.Operand;
                                        }
                                        relevantInstructions.Clear();
                                        break;
                                    }
                                }
                                Console.WriteLine("T");
                            }
                        }
                    }
                }
            }
        }
    }

}
