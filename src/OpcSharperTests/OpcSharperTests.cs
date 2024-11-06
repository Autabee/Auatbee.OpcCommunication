using Xunit;
using System;
using System.Linq;
using Autabee.OpcToClass;
using Opc.Ua;
using Xunit.Abstractions;
using Autabee.Communication.ManagedOpcClient;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.ObjectModel;
using Autabee.OpcSharper;
using Serilog;
using Serilog.Core;

namespace Tests
{
  public class OpcSharperTests
  {
        private Logger logger;

        public OpcSharperTests(ITestOutputHelper outputHelper)
    {
            logger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.TestOutput(outputHelper)
                    .CreateLogger();
            
    }
    [SkippableFact]
    public async void GenerateProjectTest()
    {
      var file = "config/secret.json";

      SharperTestSettings settings = GetSettings(file);
      AutabeeManagedOpcClient service = await GetService(settings);
      OpcSharper.GenerateProject(service, settings.GeneratorSettings, logger);
    }

    private void AcceptCert(CertificateValidator sender, CertificateValidationEventArgs e)
    {
      e.Accept = true;
    }

    [SkippableFact]
    public async void GenerateProjectTestBoiler()
    {
      string file = "config/boiler.json";

      SharperTestSettings settings = GetSettings(file);
      AutabeeManagedOpcClient service = await GetService(settings);
      OpcSharper.GenerateProject(service, settings.GeneratorSettings, logger);
    }

    [SkippableFact]
    public async void GenerateProjectTypeServer()
    {
        string file = "config/TypeServer.json";

        SharperTestSettings settings = GetSettings(file);
        AutabeeManagedOpcClient service = await GetService(settings);
        OpcSharper.GenerateProject(service, settings.GeneratorSettings, logger);
    }

        [SkippableFact]
    public async void GenerateProjectTestDataAccess()
    {
      var file = "config/DataAccess.json";

      SharperTestSettings settings = GetSettings(file);
      AutabeeManagedOpcClient service = await GetService(settings);

      OpcSharper.GenerateProject(service, settings.GeneratorSettings, logger);
    }

    private async Task<AutabeeManagedOpcClient> GetService(SharperTestSettings settings)
    {
      var service = new AutabeeManagedOpcClient("Autabee", "UnitX.ApiTester", "Autabee.Tester");
      await TryConnect(settings, service);
      Skip.If(!service.Connected, "Error connecting to server");
      return service;
    }

    private static SharperTestSettings GetSettings(string file)
    {
      Skip.If(!File.Exists(file)
                      , $"{file} not found");
      SharperTestSettings settings;
      using (StreamReader r = new StreamReader(file))
      {
        string json = r.ReadToEnd();
        settings = JsonConvert.DeserializeObject<SharperTestSettings>(json);
      }
      settings.GeneratorSettings.nodes.AddRange(settings.nodes.Select(o => new NodeId(o)));
      return settings;
    }

    private async Task TryConnect(SharperTestSettings settings, AutabeeManagedOpcClient service)
    {
      try
      {
        EndpointDescriptionCollection endpointDescriptionCollection = AutabeeManagedOpcClient.GetEndpoints(settings.server);
        endpointDescriptionCollection.ForEach(ep => Console.WriteLine(ep.EndpointUrl));

        //always accept cert
        service.CertificateValidationNotification += Service_CertificateValidationNotification;

        var ano = endpointDescriptionCollection.Where(o => o.SecurityMode == MessageSecurityMode.None);
        var endpoint = ano.Count() == 0 ? endpointDescriptionCollection.First() : ano.First();

        await service.Connect(endpoint, settings.GetUserIdentity());
      }
      catch (Exception)
      {
        Skip.If(true, "Error connecting to server");
      }
    }



    private void Service_CertificateValidationNotification(CertificateValidator sender, CertificateValidationEventArgs e)
    {
      e.Accept = true;
    }
  }
}