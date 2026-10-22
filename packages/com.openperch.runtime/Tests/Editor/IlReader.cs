using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>
    /// Decodes the IL of a method body, so that tests can check what a method calls, which fields it uses and where it
    /// jumps without running it.
    /// </summary>
    public static class IlReader
    {
        private const byte TwoByteOpCodePrefix = 0xFE;

        private static readonly Dictionary<short, OpCode> OpCodesByValue = LoadOpCodes();

        /// <summary>The instructions of <paramref name="method"/> in order; empty when it has no body.</summary>
        /// <exception cref="NotSupportedException">The body holds an opcode or operand type the reader does not know.</exception>
        public static List<IlInstruction> Read(MethodBase method)
        {
            var instructions = new List<IlInstruction>();
            var body = method.GetMethodBody();
            if (body == null)
            {
                return instructions;
            }

            var il = body.GetILAsByteArray();
            var module = method.Module;
            var typeArguments = method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments()
                : null;
            var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

            var position = 0;
            while (position < il.Length)
            {
                var offset = position;
                var value = (short)il[position++];
                if (value == TwoByteOpCodePrefix)
                {
                    value = unchecked((short)((TwoByteOpCodePrefix << 8) | il[position++]));
                }

                if (!OpCodesByValue.TryGetValue(value, out var opCode))
                {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        "Unknown opcode 0x{0:X4} at IL_{1:X4} of {2}.{3}.", value, offset, method.DeclaringType, method.Name));
                }

                MethodBase callee = null;
                FieldInfo field = null;
                int[] branchTargets = null;
                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                        var shortJump = unchecked((sbyte)il[position]);
                        position += 1;
                        branchTargets = new[] { position + shortJump };
                        break;
                    case OperandType.InlineBrTarget:
                        var jump = BitConverter.ToInt32(il, position);
                        position += 4;
                        branchTargets = new[] { position + jump };
                        break;
                    case OperandType.InlineSwitch:
                        var caseCount = BitConverter.ToInt32(il, position);
                        position += 4;
                        var afterSwitch = position + (4 * caseCount);
                        branchTargets = new int[caseCount];
                        for (var i = 0; i < caseCount; i++)
                        {
                            branchTargets[i] = afterSwitch + BitConverter.ToInt32(il, position + (4 * i));
                        }

                        position = afterSwitch;
                        break;
                    case OperandType.InlineMethod:
                        callee = module.ResolveMethod(BitConverter.ToInt32(il, position), typeArguments, methodArguments);
                        position += 4;
                        break;
                    case OperandType.InlineField:
                        field = module.ResolveField(BitConverter.ToInt32(il, position), typeArguments, methodArguments);
                        position += 4;
                        break;
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        position += 1;
                        break;
                    case OperandType.InlineVar:
                        position += 2;
                        break;
                    case OperandType.InlineI:
                    case OperandType.InlineSig:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.ShortInlineR:
                        position += 4;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        position += 8;
                        break;
                    default:
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture,
                            "Operand type {0} of {1} at IL_{2:X4} of {3}.{4} is not supported.",
                            opCode.OperandType, opCode.Name, offset, method.DeclaringType, method.Name));
                }

                instructions.Add(new IlInstruction(offset, opCode, callee, field, branchTargets));
            }

            return instructions;
        }

        private static Dictionary<short, OpCode> LoadOpCodes()
        {
            var opCodes = new Dictionary<short, OpCode>();
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(OpCode))
                {
                    var opCode = (OpCode)field.GetValue(null);
                    opCodes[opCode.Value] = opCode;
                }
            }

            return opCodes;
        }
    }
}
