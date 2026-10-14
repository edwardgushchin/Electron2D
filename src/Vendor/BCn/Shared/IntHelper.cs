#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;
using System.Collections.Generic;
using System.Text;

namespace Electron2DBlockCodecs.Shared
{
	internal static class IntHelper
	{

		public static int SignExtend(int orig, int precision)
		{
			var signMask = (1 << (precision - 1));
			var numberMask = signMask - 1;

			if ((orig & signMask) != 0)
			{
				return (~numberMask) | (orig & numberMask);
			}

			return (orig & numberMask);
		}
	}
}
