using System;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FhirPatientMatchClient
{
	class Program
	{
		public static async System.Threading.Tasks.Task Main(string[] args)
		{
			string fhirServerUrl = "https://test.ahdis.ch/mag-cara/fhir";
			var client = new FhirClient(fhirServerUrl);

			if (args.Length > 0 && args[0] == "-publish")
			{
				// Usage: -publish key1=value1 key2=value2 ...
				var variables = new Dictionary<string, string>();
				// for (int i = 1; i < args.Length; i++)
				// {
				// 	var kv = args[i].Split('=', 2);
				// 	if (kv.Length == 2)
				// 		variables[kv[0]] = kv[1];
				// }
				const long min = 200000;
				const long max = 900000;
				variables["conf-oid-ihe-sourceId"] = "2.16.756.5.30.1.109.6.5.3.2";
				variables["conf-oid-local-assigning-patient-authority"] = "2.16.756.5.32.1";
				variables["eprSpid"] = "761337611735842172";
				variables["dateNow"] = new DateTimeOffset(DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:sszzz");
				variables["name-family"] = "Erne Cehic";
				variables["name-given"] = "Andrea Juliana";
				variables["localPid"] = "2.16.756.5.32";
				variables["docUniqueId"] = "2.16.756.5.32.1.4^"+Guid.NewGuid().ToString();
				variables["ssUniqueId"] = "2.16.756.5.30.1.145.1.4.1." + (Math.Round(new Random().NextDouble() * (max - min)) + min);
				variables["docEntryUuid"] = Guid.NewGuid().ToString();
				variables["dateDoc"] = new DateTimeOffset(DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:sszzz");
				variables["title"] = "Test Document";

				// Read the bundle template
				string bundleJson = File.ReadAllText("iti-65-bundle.json");
				// Replace {{var}} with provided values
				bundleJson = Regex.Replace(bundleJson, "{{(.*?)}}", m => variables.ContainsKey(m.Groups[1].Value) ? variables[m.Groups[1].Value] : m.Value);

				// Parse the bundle
				var fhirJsonParser = new FhirJsonParser();
				Bundle bundle = null;
				try
				{
					bundle = fhirJsonParser.Parse<Bundle>(bundleJson);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error parsing bundle: {ex.Message}");
					return;
				}

				// POST the bundle to the FHIR endpoint
				try
				{
					var result = await client.TransactionAsync(bundle);
					Console.WriteLine("Bundle published successfully.");
					Console.WriteLine(result.ToJson());
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error publishing bundle: {ex.Message}");
				}
			}
			else
			{
				// Default: Patient $match
				var parameters = new Parameters();
				parameters.Add("resource", new Patient
				{
					Identifier = new List<Identifier> {
						new Identifier {
							System = "urn:oid:2.16.756.5.32",
							Value = "7560639795737"
						}
					}
				});
				try
				{
					// Call $match operation on Patient (type-level operation)
					var result = await client.TypeOperationAsync(
						"match",
						"Patient",
						parameters
					);
					var bundle = result as Bundle;
					if (bundle != null)
					{
						Console.WriteLine($"Found {bundle.Entry.Count} matching patients:");
						foreach (var entry in bundle.Entry)
						{
							var patient = entry.Resource as Patient;
							if (patient != null)
							{
								Console.WriteLine($"Patient: {patient.Name[0].ToString()} | BirthDate: {patient.BirthDate}");
							}
						}
					}
					else
					{
						Console.WriteLine("No matching patients found or unexpected result.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
				}
			}
		}
	}
}
