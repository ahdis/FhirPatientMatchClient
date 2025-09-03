using System;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FhirPatientMatchClient
{
	class Program
	{
		public static async System.Threading.Tasks.Task Main(string[] args)
		{
			string fhirServerUrl = "https://test.ahdis.ch/mag-cara/fhir";
			var client = new FhirClient(fhirServerUrl);

			// Create a Parameters resource for $match with only identifier
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
				// Call $match operation on Patient

				// Call $match operation on Patient (type-level operation)
				var result = await client.TypeOperationAsync(
					"match",
					"Patient",
					parameters
				);

				// Print the result
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
