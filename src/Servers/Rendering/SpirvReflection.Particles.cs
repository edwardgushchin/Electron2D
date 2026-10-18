using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class SpirvReflection
{
    private static void ValidateParticleFrame(nint compiler, nint type, string? name, nuint size)
    {
        if (name is not ("ParticleFrame" or "type.ParticleFrame") || size != ParticleFrameLayout.ByteSize || GetMemberCount(type) != 12)
            throw new NotSupportedException("Particle binding zero requires the complete ParticleFrame block.");
        for (uint member = 0; member < 12; member++)
        {
            var field = GetType(compiler, GetMemberType(type, member));
            if (GetMemberOffset(compiler, type, member, out var offset) != 0 || offset != member * 16 || GetBitWidth(field) != 32 || GetVectorSize(field) != 4 || GetColumns(field) != 1 || GetArrayDimensions(field) != 0 || GetBaseType(field) != (member is 0 or 11 ? 8 : 13))
                throw new NotSupportedException("ParticleFrame requires twelve aligned uint4/float4 fields matching the process ABI.");
        }
    }
    private static void ValidateParticleStorage(nint compiler, nint resources)
    {
        if (GetResources(resources, 2, out var pointer, out var count) != 0 || count is < 1 or > 2)
            throw new NotSupportedException("Particle programs require state storage and optional subemission storage.");
        var seen = 0u;
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)pointer, (int)count))
        {
            if (HasDecoration(compiler, resource.Id, DescriptorSet) == 0 || HasDecoration(compiler, resource.Id, Binding) == 0 || GetDecoration(compiler, resource.Id, DescriptorSet) != 1)
                throw new NotSupportedException("Particle writable buffers require descriptor set one.");
            var binding = GetDecoration(compiler, resource.Id, Binding);
            if (binding >= count || (seen & 1u << (int)binding) != 0) throw new NotSupportedException("Particle writable buffers require contiguous unique bindings.");
            seen |= 1u << (int)binding;
            if (GetArrayDimensions(GetType(compiler, resource.TypeId)) != 0) throw new NotSupportedException("Particle storage descriptors cannot be arrays.");
            var type = GetType(compiler, resource.BaseTypeId);
            if (GetMemberCount(type) != 1 || GetMemberOffset(compiler, type, 0, out var start) != 0 || start != 0 || GetArrayStride(compiler, type, 0, out var stride) != 0 || stride != ParticleStateLayout.ByteSize)
                throw new NotSupportedException("Particle state requires a tightly packed runtime array.");
            var element = GetType(compiler, GetMemberType(type, 0));
            if (GetArrayDimensions(element) != 1 || GetArrayLength(element, 0) != 0 || GetMemberCount(element) != 6)
                throw new NotSupportedException("Particle state requires six vector lanes per runtime record.");
            for (uint member = 0; member < 6; member++)
            {
                var field = GetType(compiler, GetMemberType(element, member));
                if (GetMemberOffset(compiler, element, member, out var offset) != 0 || offset != member * 16 || GetBitWidth(field) != 32 || GetVectorSize(field) != 4 || GetColumns(field) != 1 || GetArrayDimensions(field) != 0 || GetBaseType(field) != (member == 5 ? 8 : 13))
                    throw new NotSupportedException("Particle records require five float4 lanes and one uint4 lane.");
            }
        }
    }
}
