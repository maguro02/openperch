using System;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenPerch.Runtime.Tests.Editor
{
    /// <summary>One instruction of a method body, decoded by <see cref="IlReader"/>.</summary>
    public sealed class IlInstruction
    {
        /// <summary>Creates a decoded instruction.</summary>
        public IlInstruction(int offset, OpCode opCode, MethodBase method, FieldInfo field, int[] branchTargets)
        {
            Offset = offset;
            OpCode = opCode;
            Method = method;
            Field = field;
            BranchTargets = branchTargets ?? Array.Empty<int>();
        }

        /// <summary>Offset of the instruction from the start of the method body.</summary>
        public int Offset { get; }

        /// <summary>The operation.</summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// The method the instruction calls, constructs an object with or takes a pointer to; null for every other
        /// instruction, including <c>calli</c>, whose target is only known at run time.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>The field the instruction reads, writes or takes the address of; null for every other instruction.</summary>
        public FieldInfo Field { get; }

        /// <summary>Offsets the instruction can jump to; empty when it does not branch.</summary>
        public int[] BranchTargets { get; }
    }
}
