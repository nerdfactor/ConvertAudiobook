using eu.nerdfactor.SimpleArguments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace eu.nerdfactor.ConvertAudiobook {
	class App {

		const string ARG_SOURCE = "source";
		const string ARG_OUTPUT = "output";
		const string ARG_FFMPEG = "ffmpeg";
		const string ARG_CHAPTERS = "chapters";
		const string ARG_HELP = "help";

		public static async Task Main(string[] args) {

			Argument.ParseArgsArray(args, new List<Argument>() {
				new Argument(ARG_SOURCE, "s", "Path to the audiobook file."),
				new Argument(ARG_OUTPUT, "o", "Path for the file output."),
				new Argument(ARG_CHAPTERS, "c", "Split audiobook into chapters."),
				new Argument(ARG_FFMPEG, "f", "Path to ffmpeg.exe. Default is same as application directory."),			
				new Argument(ARG_HELP, "h", "Prints this help text.", action:PrintHelpText)
			});

			if (Argument.HasArgument(ARG_HELP)) {
				Argument.GetArgument(ARG_HELP).Action();
				return;
			}

			String ffmpeg = Argument.GetArgument(ARG_FFMPEG).Value;
			if (String.IsNullOrEmpty(ffmpeg)) {
				ffmpeg = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + Path.DirectorySeparatorChar + "ffmpeg.exe";
			}
			if (!File.Exists(ffmpeg)) {
				Console.WriteLine($"Can't find ffmpeg.exe at {ffmpeg}.");
				return;
			}
			if (!File.Exists(Argument.GetArgument(ARG_SOURCE).Value)) {
				Console.WriteLine($"Can't find source at {Argument.GetArgument(ARG_SOURCE).Value}.");
				return;
			}

			try {
				Console.WriteLine($"Converting audiobook {Argument.GetArgument(ARG_SOURCE).Value}.");
				Converter converter = new Converter(ffmpeg);
				await converter.Convert(
					Argument.GetArgument(ARG_SOURCE).Value,
					Argument.GetArgument(ARG_OUTPUT).Value,
					Argument.HasArgument(ARG_CHAPTERS),
					new ProgressBar()
				);
				Console.WriteLine();
				Console.WriteLine("Finished conversion.");
			} catch (Exception e) {
				Console.WriteLine($"Error during conversion: {e.Message}.");
			}
		}

		public static int PrintHelpText() {
			Console.WriteLine("ConvertAudiobook v 1.0");
			Console.WriteLine("Converts an audiobook from .m4b format to .mp3 format.");
			Console.WriteLine(@"Example: ConvertAudiobook.exe -source c:\path\to\audiobook.m4b -output c:\put\converted\files\here\ -chapters");
			Console.WriteLine("Arguments:");
			foreach (Argument arg in Argument.Arguments) {
				Console.WriteLine(String.Format(" -{0,-10}-- {1}", arg.Name, arg.Description));
			}
			return 0;
		}
	}
}
