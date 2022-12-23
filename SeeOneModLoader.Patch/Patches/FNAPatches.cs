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
    public class FNA_SDL_Patch : IPatch
    {
        private static string NAMESPACE = "BasicXNAProject";
        private static string GAME_CLASS_NAME = "Game1";
        private static string PATCH_REMOVAL_TYPE = "Microsoft.Xna.Framework.GameWindow Microsoft.Xna.Framework.Game::get_Window()";

        public void Patch(Patcher patcher, AssemblyDefinition assembly)
        {
            TypeDefinition game1Definition = assembly.MainModule.GetType(NAMESPACE, GAME_CLASS_NAME);

            foreach (AssemblyNameReference asmNameRef in assembly.MainModule.AssemblyReferences)
            {
                if (asmNameRef.Name.Equals("System.Windows.Forms"))
                {
                    assembly.MainModule.AssemblyReferences.Remove(asmNameRef);
                    break;
                }
            }
            
            foreach (MethodDefinition methodDefinition in game1Definition.Methods)
            {
                if (methodDefinition.Name == "SetRes")
                {
                    bool markForRemoval = false;
                    List<Instruction> instructionsToRemove = new List<Instruction>();

                    for (int i = 0; i < methodDefinition.Body.Instructions.Count; i++)
                    {
                        Instruction instruction = methodDefinition.Body.Instructions[i];

                        if (markForRemoval)
                        {
                            instructionsToRemove.Add(instruction);
                        }
                        
                        if (instruction.OpCode.Code == Code.Call && !markForRemoval)
                        {
                            MethodReference operandMethodReference = (MethodReference) instruction.Operand;
                            markForRemoval = operandMethodReference.FullName == PATCH_REMOVAL_TYPE;

                            if (markForRemoval)
                            {
                                instructionsToRemove.Add(instruction);
                                instructionsToRemove.Add(methodDefinition.Body.Instructions[i - 1]);
                            }
                        }
                        else if(instruction.OpCode.Code == Code.Throw)
                        {
                            break;
                        }
                    }

                    foreach (Instruction instruction in instructionsToRemove)
                    {
                        methodDefinition.Body.Instructions.Remove(instruction);
                    }

                    methodDefinition.Body.Variables.RemoveAt(0);
                    methodDefinition.Body.MaxStackSize = 2;

                    foreach (Instruction instruction in methodDefinition.Body.Instructions)
                    {
                        if (instruction.OpCode.Code == Code.Ldloc_1)
                        {
                            instruction.OpCode = Mono.Cecil.Cil.OpCodes.Ldloc_0;
                        }
                        else if (instruction.OpCode.Code == Code.Ldloc_2)
                        {
                            instruction.OpCode = Mono.Cecil.Cil.OpCodes.Ldloc_1;
                        }
                        else if (instruction.OpCode.Code == Code.Stloc_1)
                        {
                            instruction.OpCode = Mono.Cecil.Cil.OpCodes.Stloc_0;
                        }
                    }
                }
            }
        }
    }

    public class FNA_Remove_SlimInput_Patch : IPatch
    {
        private static string NAMESPACE = "BasicXNAProject";
        private static string GAME_CLASS_NAME = "Game1";
        private static string SLIMINPUTWRAPPER_CLASS_NAME = "SlimInputWrapper";
        private static string PATCH_REMOVAL_TYPE_SLIMINPUT_INIT = "Microsoft.Xna.Framework.GameWindow Microsoft.Xna.Framework.Game::get_Window()";
        private static string PATCH_REMOVAL_TYPE_SLIMINPUT_UPDATE = "System.Void SlimInput.SlimGamePad::Update()";

        public void Patch(Patcher patcher, AssemblyDefinition assembly)
        {
            TypeDefinition game1Definition = assembly.MainModule.GetType(NAMESPACE, GAME_CLASS_NAME);

            foreach (AssemblyNameReference asmNameRef in assembly.MainModule.AssemblyReferences)
            {
                if (asmNameRef.Name.Equals("SlimInput"))
                {
                    assembly.MainModule.AssemblyReferences.Remove(asmNameRef);
                    break;
                }
            }

            foreach (MethodDefinition methodDefinition in game1Definition.Methods)
            {
                if (methodDefinition.Name == "LoadContent")
                {
                    bool markForRemoval = false;
                    List<Instruction> instructionsToRemove = new List<Instruction>();

                    for (int i = 0; i < methodDefinition.Body.Instructions.Count; i++)
                    {
                        Instruction instruction = methodDefinition.Body.Instructions[i];

                        if (instruction.OpCode.Code == Code.Nop && markForRemoval)
                        {
                            break;
                        }

                        if (markForRemoval)
                        {
                            instructionsToRemove.Add(instruction);
                        }

                        if (instruction.OpCode.Code == Code.Call && !markForRemoval)
                        {
                            MethodReference operandMethodReference = (MethodReference)instruction.Operand;
                            markForRemoval = operandMethodReference.FullName == PATCH_REMOVAL_TYPE_SLIMINPUT_INIT;

                            if (markForRemoval)
                            {
                                instructionsToRemove.Add(instruction);
                                instructionsToRemove.Add(methodDefinition.Body.Instructions[i - 1]);
                            }
                        }
                    }

                    foreach (Instruction instruction in instructionsToRemove)
                    {
                        methodDefinition.Body.Instructions.Remove(instruction);
                    }
                }
                else if (methodDefinition.Name == "Update")
                {
                    bool markForRemoval = false;
                    List<Instruction> instructionsToRemove = new List<Instruction>();
                    int leave = 0;

                    for (int i = 0; i < methodDefinition.Body.Instructions.Count; i++)
                    {
                        Instruction instruction = methodDefinition.Body.Instructions[i];

                        if (markForRemoval)
                        {
                            instructionsToRemove.Add(instruction);
                        }

                        if (instruction.OpCode.Code == Code.Call && !markForRemoval)
                        {
                            MethodReference operandMethodReference = (MethodReference)instruction.Operand;
                            markForRemoval = operandMethodReference.FullName == PATCH_REMOVAL_TYPE_SLIMINPUT_UPDATE;

                            if (markForRemoval)
                            {
                                instructionsToRemove.Add(instruction);
                                instructionsToRemove.Add(methodDefinition.Body.Instructions[i - 1]);
                            }
                        }
                        else if (instruction.OpCode.Code == Code.Leave_S && markForRemoval)
                        {
                            leave++;
                            if (leave == 2)
                            {
                                break;
                            }
                        }
                    }

                    methodDefinition.Body.ExceptionHandlers.RemoveAt(0);

                    foreach (Instruction instruction in instructionsToRemove)
                    {
                        methodDefinition.Body.Instructions.Remove(instruction);
                    }
                }
            }

            TypeDefinition slimInputWrapperDefinition = assembly.MainModule.GetType(NAMESPACE, SLIMINPUTWRAPPER_CLASS_NAME);

            foreach (MethodDefinition methodDefinition in slimInputWrapperDefinition.Methods)
            {
                if (methodDefinition.IsConstructor)
                {
                    List<Instruction> instructionsToRemove = new List<Instruction>();
                    bool markForRemoval = false;
                    int seenLdarg0 = 0;

                    for (int i = 0; i < methodDefinition.Body.Instructions.Count; i++)
                    {
                        Instruction instruction = methodDefinition.Body.Instructions[i];

                        if (markForRemoval)
                        {
                            instructionsToRemove.Add(instruction);
                        }

                        if (instruction.OpCode.Code == Code.Nop && !markForRemoval)
                        {
                            markForRemoval = true;
                        }

                        if (instruction.OpCode.Code == Code.Ldarg_0)
                        {
                            seenLdarg0++;

                            if (seenLdarg0 == 2 && markForRemoval)
                            {
                                markForRemoval = false;
                                instructionsToRemove.Remove(instruction);
                                break;
                            }
                        }
                    }

                    foreach (Instruction instruction in instructionsToRemove)
                    {
                        methodDefinition.Body.Instructions.Remove(instruction);
                    }

                    methodDefinition.Body.Instructions.Insert(2, Instruction.Create(Mono.Cecil.Cil.OpCodes.Stloc_1));
                    methodDefinition.Body.Instructions.Insert(2, Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_1));
                    methodDefinition.Body.Instructions.Insert(2, Instruction.Create(Mono.Cecil.Cil.OpCodes.Stloc_0));
                    methodDefinition.Body.Instructions.Insert(2, Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_1));
                    Console.WriteLine();
                }
            }
        }
    }

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
