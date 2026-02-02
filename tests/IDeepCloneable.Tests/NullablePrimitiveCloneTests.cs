using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for nullable primitive cloning optimizations.
/// </summary>
public class NullablePrimitiveCloneTests
{
    [Fact]
    public void DeepClone_NullablePrimitives_CopiesValues()
    {
        var original = new NullablePrimitiveHolder
        {
            IntValue = 10,
            LongValue = 20,
            BoolValue = true,
            DoubleValue = 1.5,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.IntValue.ShouldBe(10);
        clone.LongValue.ShouldBe(20);
        clone.BoolValue.ShouldBe(true);
        clone.DoubleValue.ShouldBe(1.5);
    }

    [Fact]
    public void CloneInternal_DoesNotCallCloneByRuntimeType_ForNullablePrimitives()
    {
        var extensionsType = typeof(NullablePrimitiveHolder).Assembly.GetType(
            "IDeepCloneable.Generator.DeepCloneableExtensions",
            throwOnError: true
        );
        extensionsType.ShouldNotBeNull();

        var method = extensionsType!.GetMethod(
            "IDeepCloneable_Tests_NullablePrimitiveHolder_CloneInternal",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        method.ShouldNotBeNull();

        var calledMethods = GetCalledMethods(method!).ToList();
        calledMethods.ShouldNotContain(m => m.Name == "CloneByRuntimeType");
    }

    private static IEnumerable<MethodBase> GetCalledMethods(MethodInfo method)
    {
        var body = method.GetMethodBody();
        if (body == null)
            yield break;

        var il = body.GetILAsByteArray();
        if (il == null || il.Length == 0)
            yield break;

        var position = 0;
        while (position < il.Length)
        {
            var opCode = ReadOpCode(il, ref position);

            if (opCode.OperandType == OperandType.InlineMethod)
            {
                var token = ReadInt32(il, ref position);
                var called = method.Module.ResolveMethod(token);
                if (called != null)
                {
                    yield return called;
                }
                continue;
            }

            position += GetOperandSize(il, ref position, opCode.OperandType);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int position)
    {
        var code = il[position++];
        if (code != 0xFE)
        {
            return SingleByteOpCodes[code];
        }

        var second = il[position++];
        return MultiByteOpCodes[second];
    }

    private static int ReadInt32(byte[] il, ref int position)
    {
        var value =
            il[position]
            | (il[position + 1] << 8)
            | (il[position + 2] << 16)
            | (il[position + 3] << 24);
        position += 4;
        return value;
    }

    private static int GetOperandSize(byte[] il, ref int position, OperandType operandType)
    {
        switch (operandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineI:
            case OperandType.InlineField:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.InlineBrTarget:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineSwitch:
                var count = ReadInt32(il, ref position);
                return count * 4;
            default:
                return 0;
        }
    }

    private static readonly OpCode[] SingleByteOpCodes = CreateOpCodeLookup(singleByte: true);
    private static readonly OpCode[] MultiByteOpCodes = CreateOpCodeLookup(singleByte: false);

    private static OpCode[] CreateOpCodeLookup(bool singleByte)
    {
        var opcodes = new OpCode[0x100];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opcode)
                continue;

            var value = (ushort)opcode.Value;
            if (singleByte)
            {
                if (value < 0x100)
                {
                    opcodes[value] = opcode;
                }
            }
            else
            {
                if ((value & 0xFF00) == 0xFE00)
                {
                    opcodes[value & 0xFF] = opcode;
                }
            }
        }

        return opcodes;
    }
}

[DeepCloneable]
public partial class NullablePrimitiveHolder
{
    public int? IntValue { get; set; }
    public long? LongValue { get; set; }
    public bool? BoolValue { get; set; }
    public double? DoubleValue { get; set; }
}
