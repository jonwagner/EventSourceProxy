using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventSourceProxy;

namespace GenerateProxyManifest
{
	class Program
	{
		static bool _showHelp = false;
		static bool _outputGuid = false;
		static string _assemblyPath = null;
		static string _typeName = null;
		static string _name = null;

		static void Main(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i].ToLowerInvariant())
				{
					case "-h":
					case "--help":
					case "-?":
					case "/?":
						_showHelp = true;
						break;

					case "-n":
					case "-name":
						_name = args[++i];
						_outputGuid = true;
						break;

					case "-g":
					case "-guid":
						_outputGuid = true;
						break;

					case "-a":
					case "-assembly":
						_assemblyPath = args[++i];
						break;

					case "-t":
					case "-type":
						_typeName = args[++i];
						break;

					default:
						if (args[i][0] == '-')
							throw new ApplicationException(String.Format("Unknown option {0}", args[i]));
						else if (_assemblyPath == null)
							_assemblyPath = args[i];
						else if (_typeName == null)
							_typeName = args[i];
						else
							throw new ApplicationException("Too many parameters");
						break;
				}
			}

			if (_showHelp)
				Console.WriteLine(@"
GenerateProxyManifest - outputs the ETW information for a class.

Parameters:
	-h 
	-help 
	-? 
	/?
		Shows Help

	-a [assemblypath]
	-assembly [assemblypath]
		The path to the assembly .exe or .dll

	-t [type full name]
	-type [type full name]
		The full name of the type.

	-g
	-guid
		To output the ETW provider GUID rather than the manifest.

	-n [provider name]
	-name [provider name]
		Outputs the ETW provider guid given the provider name.
");
			else if (_name != null)
				Console.WriteLine(EventSourceManifest.GetGuidFromProviderName(_name));
			else if (_outputGuid)
				Console.WriteLine(EventSourceManifest.GetGuid(_assemblyPath, _typeName));
			else
				Console.WriteLine(EventSourceManifest.GenerateManifest(_assemblyPath, _typeName));
		}
	}
}
