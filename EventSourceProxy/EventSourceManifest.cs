using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Exposes ETW manifest information for provider types.
	/// </summary>
	public static class EventSourceManifest
	{
		#region Metadata Members
		/// <summary>
		/// Return the GUID of a provider name.
		/// </summary>
		/// <param name="providerName">The name of the provider.</param>
		/// <returns>The GUID representing the name.</returns>
		public static Guid GetGuidFromProviderName(string providerName)
		{
			string name = providerName.ToUpperInvariant();
			byte[] buffer = new byte[(name.Length * 2) + 0x10];
			uint num = 0x482c2db2;
			uint num2 = 0xc39047c8;
			uint num3 = 0x87f81a15;
			uint num4 = 0xbfc130fb;
			for (int i = 3; 0 <= i; i--)
			{
				buffer[i] = (byte)num;
				num = num >> 8;
				buffer[i + 4] = (byte)num2;
				num2 = num2 >> 8;
				buffer[i + 8] = (byte)num3;
				num3 = num3 >> 8;
				buffer[i + 12] = (byte)num4;
				num4 = num4 >> 8;
			}

			for (int j = 0; j < name.Length; j++)
			{
				buffer[((2 * j) + 0x10) + 1] = (byte)name[j];
				buffer[(2 * j) + 0x10] = (byte)(name[j] >> 8);
			}

			byte[] buffer2 = SHA1.Create().ComputeHash(buffer);
			int a = (((((buffer2[3] << 8) + buffer2[2]) << 8) + buffer2[1]) << 8) + buffer2[0];
			short b = (short)((buffer2[5] << 8) + buffer2[4]);
			short num9 = (short)((buffer2[7] << 8) + buffer2[6]);
			return new System.Guid(a, b, (short)((num9 & 0xfff) | 0x5000), buffer2[8], buffer2[9], buffer2[10], buffer2[11], buffer2[12], buffer2[13], buffer2[14], buffer2[15]);
		}

		/// <summary>
		/// Return the GUID of a provider from the assembly and type.
		/// </summary>
		/// <param name="assemblyPath">The path to the assembly containing the type.</param>
		/// <param name="typeName">The full name of the type.</param>
		/// <returns>The GUID representing the name.</returns>
		public static Guid GetGuid(string assemblyPath, string typeName)
		{
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			Type type = assembly.GetType(typeName);

			return GetGuid(type);
		}

		/// <summary>
		/// Return the GUID of a provider.
		/// </summary>
		/// <param name="type">The provider type.</param>
		/// <returns>The GUID representing the name.</returns>
		public static Guid GetGuid(Type type)
		{
			EventSource eventSource = EventSourceImplementer.GetEventSource(type);
			return eventSource.Guid;
		}

		/// <summary>
		/// Return the manifest of a provider from the assembly and type.
		/// </summary>
		/// <param name="assemblyPath">The path to the assembly containing the type.</param>
		/// <param name="typeName">The full name of the type.</param>
		/// <returns>The XML manifest content.</returns>
		public static string GenerateManifest(string assemblyPath, string typeName)
		{
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			Type type = assembly.GetType(typeName);

			return GenerateManifest(type);
		}

		/// <summary>
		/// Return the manifest of a provider for the given type.
		/// </summary>
		/// <param name="type">The provider type.</param>
		/// <returns>The XML manifest content.</returns>
		public static string GenerateManifest(Type type)
		{
			EventSource eventSource = EventSourceImplementer.GetEventSource(type);
			return EventSource.GenerateManifest(eventSource.GetType(), Assembly.GetAssembly(type).Location);
		}
		#endregion
	}
}
