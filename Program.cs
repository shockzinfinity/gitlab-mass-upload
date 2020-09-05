using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text.Json.Serialization;
using System.Web;

namespace gitlabUpload
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			string privateToken = "<your gitlab private token>";

			var directoryInfos = new DirectoryInfo(@"D:\00.referenceALM").GetDirectories().Select(x => x.Name);
			//var directoryInfos = new DirectoryInfo(@"D:\testTemp").GetDirectories().Select(x => x.Name);

			foreach (var dir in directoryInfos)
			{
				string addProject = HttpUtility.UrlEncode(dir);
				// namespace_id in query = could be user id or group id
				string createText = $"https://git.shockz.io/api/v4/projects?name={addProject}&namespace_id=7";

				try
				{
					Process process = new Process();
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = "curl";
					//processStartInfo.Arguments = $"--header \"Authorization: Bearer {privateToken}\" \"https://git.shockz.io/api/v4/projects\"";
					processStartInfo.Arguments = $"--header \"Authorization: Bearer {privateToken}\" -X POST {createText}";
					processStartInfo.RedirectStandardOutput = true;
					process.StartInfo = processStartInfo;
					process.Start();

					string output = process.StandardOutput.ReadToEnd();
					dynamic temp = JObject.Parse(output);
					//Console.WriteLine(temp.http_url_to_repo);

					using (Runspace runspace = RunspaceFactory.CreateRunspace())
					{
						runspace.Open();
						runspace.SessionStateProxy.Path.SetLocation($"d:\\00.referenceALM\\{dir}");
						//runspace.SessionStateProxy.Path.SetLocation($"d:\\testTemp\\{dir}");
						using (Pipeline pl = runspace.CreatePipeline())
						{
							pl.Commands.AddScript(@"git.exe init");
							pl.Commands.AddScript(@"git.exe remote add origin " + temp.http_url_to_repo);
							pl.Commands.AddScript(@"git.exe add .");
							pl.Commands.AddScript(@"git.exe commit -m 'Initial commit'");
							pl.Commands.AddScript(@"git.exe push -u origin master");

							pl.Invoke();
						}
						runspace.Close();
					}
				}
				catch (Exception ex)
				{
					using (StreamWriter sw = File.AppendText("D:\\00.referenceALM\\failLog.txt"))
					//using (StreamWriter sw = File.AppendText("D:\\testTemp\\failLog.txt"))
					{
						Log(dir, sw);
						Log("Detail -------------------------", sw);
						Log(ex.ToString(), sw);
						Log("--------------------------------", sw);
					}
				}
			}

			Console.ReadLine();
		}

		public static void Log(string logMessage, TextWriter w)
		{
			w.WriteLine($"{logMessage}");
		}
	}

	public class repoResult
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("http_url_to_repo")]
		public string CloneUrl { get; set; }
	}
}