using ATL;
using FFmpeg.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace eu.nerdfactor.ConvertAudiobook {
	public class Converter {

		/// <summary>
		/// Wrapper for communication with ffmpeg program.
		/// </summary>
		protected Engine ffmpeg;

		/// <summary>
		/// Callback object to report progress to caller.
		/// </summary>
		protected IProgress<double> progress;

		protected double totalDuration;

		protected double completedDuration;

		protected double chapterDuration;

		/// <summary>
		/// Converter that uses ffmpeg to convert a m4b audiobook into
		/// mp3 files.
		/// </summary>
		/// <param name="ffmpegPath"></param>
		public Converter(string ffmpegPath) {
			this.ffmpeg = new Engine(ffmpegPath);
			this.ffmpeg.Progress += Ffmpeg_Progress;
		}

		~Converter() {
			this.ffmpeg.Progress -= Ffmpeg_Progress;
		}

		protected void Ffmpeg_Progress(object sender, FFmpeg.NET.Events.ConversionProgressEventArgs e) {
			double percent = (this.completedDuration + e.ProcessedDuration.TotalSeconds) / this.totalDuration;
			this.ReportProgress(this.progress, percent);
		}

		/// <summary>
		/// Converts the audiobook source into mp3 format. Can split the chapters
		/// of the audiobook into seperate files.
		/// </summary>
		/// <param name="sourcePath">Path to the source file. Should be of m4b format and with a .m4b extension. But every format, that is readable by ffmpeg with chapter informations will probably work.</param>
		/// <param name="outputPath">Path for the conversion output. Should be a directory if chapters are split into seperate files.</param>
		/// <param name="splitChapters">Decide if the chapters of the audiobook should be split into seperate files.</param>
		/// <param name="progress">Callback object to report progress of conversion.</param>
		/// <returns>A list of paths to the converted files.</returns>
		public async Task<List<string>> Convert(string sourcePath, string outputPath = null, bool splitChapters = true, IProgress<double> progress = null) {
			List<string> convertedFiles = new List<string>();
			if (null == this.ffmpeg || !File.Exists(sourcePath)) { return convertedFiles; }
			this.progress = progress;

			// Create a directory path for the output files, even if a file path or nothing was provided.
			if (String.IsNullOrEmpty(outputPath)) {
				outputPath = Path.GetDirectoryName(sourcePath);
			}
			outputPath = outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			string outputDir = (Path.HasExtension(outputPath)) ? Path.GetDirectoryName(outputPath) : outputPath;		
			outputDir = outputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!Directory.Exists(outputDir)) {
				// Make sure the output directory exists.
				Directory.CreateDirectory(outputDir);
			}

			// Create a filename for output files. This will be the source filename with extension .mp3 or the provided outputPath if it is a file path.
			string outputFileName = (outputDir == outputPath) ? Path.GetFileNameWithoutExtension(sourcePath) + ".mp3" : Path.GetFileName(outputPath);


			// Read the source file and metadata.
			MediaFile sourceFile = new MediaFile(sourcePath);
			Track sourceMetaData = new Track(sourcePath);
			this.totalDuration = sourceMetaData.Duration;
			this.completedDuration = 0;

			IList<ChapterInfo> chapters = sourceMetaData.Chapters;
			if (!splitChapters) {
				// If the chapters should not be split, create a fake chapter for the complete audiobook.
				chapters = new List<ChapterInfo>() { new ChapterInfo() { StartTime = 0, EndTime = (uint)sourceMetaData.DurationMs } };
			}

			// Find the padding size for the chapter numbers depending on the total amounts of chapters
			string paddingSize = "D" + (chapters.Count.ToString().Length);


			// Iterate over the chapters
			for (int i = 0; i < chapters.Count; i++) {
				// create a output file path for the chapter
				string outputFilePath = outputDir + outputFileName;

				ConversionOptions options = new ConversionOptions();
				// find the beginning and end of the chapter
				int beginning = (int)chapters[i].StartTime / 1000;
				int end = (i + 1 >= chapters.Count) ? sourceMetaData.Duration : (int)chapters[i + 1].StartTime / 1000;
				int duration = end - beginning;
				// decide if a chapter has to be cut from the complete file
				if (chapters.Count > 1) {				
					// and provide the information to ffmpeg
					options.CutMedia(TimeSpan.FromSeconds(beginning), TimeSpan.FromSeconds(duration));

					// change the chapter file path to include the chapter number
					string chapterNumber = (i + 1).ToString(paddingSize);
					outputFilePath = outputFilePath.Replace(".mp3", $" - {chapterNumber}.mp3");
				}
				this.chapterDuration = duration;

				// Convert the chapter with ffmpeg
				await this.ffmpeg.ConvertAsync(sourceFile, new MediaFile(outputFilePath), options);

				// and add a tracknumber to the file.
				Track chapter = new Track(outputFilePath);
				chapter.TrackNumber = (i + 1);
				chapter.Save();

				convertedFiles.Add(outputFilePath);
				this.completedDuration += duration;
			}

			this.ReportProgress(progress, 1d);
			this.progress = null;
			return convertedFiles;
		}

		/// <summary>
		/// Helper method to report progress.
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="percent"></param>
		private void ReportProgress(IProgress<double> progress, double percent) {
			if (null != progress) {
				progress.Report(percent);
			}
		}

	}
}
