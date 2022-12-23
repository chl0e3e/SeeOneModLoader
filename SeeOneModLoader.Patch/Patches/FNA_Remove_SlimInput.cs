using Mono.Cecil.Cil;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeOneModLoader.Patch.Patches
{
    [EngineAttribute(Engine.FNA)]
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
}
