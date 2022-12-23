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
                            MethodReference operandMethodReference = (MethodReference)instruction.Operand;
                            markForRemoval = operandMethodReference.FullName == PATCH_REMOVAL_TYPE;

                            if (markForRemoval)
                            {
                                instructionsToRemove.Add(instruction);
                                instructionsToRemove.Add(methodDefinition.Body.Instructions[i - 1]);
                            }
                        }
                        else if (instruction.OpCode.Code == Code.Throw)
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
}
