using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using Opc.Ua;
using TypeServerNodes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcTypeSampleTests2 : IClassFixture<OpcUaTypeSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;
        private readonly IAutabeeLogger logger;

        public OpcTypeSampleTests2(OpcUaTypeSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new AutabeeXunitLogger(outputHelper);
        }

        [SkippableFact]
        public void ConnectWithTestServer()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var vehicleInLot = NodeId.Parse("ns=4;i=283");

            //communicator.Session.Factory.AddEncodeableTypes(typeof(ParkingLotType).Assembly);

            var data = communicator.ReadValue(vehicleInLot);



            if (data is object[] nData)
            {
                //check if it correctly maps sub types
                if (nData[0] is Dictionary<string, object> obj)
                {
                    Assert.Contains("CargoCapacity", obj);
                }
            }
        }



        [SkippableFact]
        public void ConnectWithTestServerDefindedTypes()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var vehicleInLot = NodeId.Parse("ns=4;i=283");

            //communicator.AddTypeAssemby(typeof(ParkingLotType).Assembly);

            var data = communicator.ReadValue(vehicleInLot) as object[];

            Assert.IsType<TruckType>(data[0]);
            Assert.IsType<TruckType>(data[1]);
            Assert.IsType<CarType>(data[2]);
            //Assert.IsType<BicycleType>(data[3]);
            //Assert.IsType<ScooterType>(data[4]);
            
        }

        [SkippableFact]
        public void WriteValue()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var LotType = NodeId.Parse("ns=4;i=380");

            communicator.AddTypeAssemby(typeof(ParkingLotType).Assembly);

            communicator.WriteValue(LotType, (int)2 );


            var vehicleInLot = NodeId.Parse("ns=4;i=283");
            communicator.WriteValue(vehicleInLot, new VehicleType[]
            {
                new TruckType
                {
                    Engine = EngineType.Diesel,
                    Make = "DAFF",
                    Model = "DAFF Transit",
                    CargoCapacity = 4000

                },
                 new TruckType
                {
                    Engine = EngineType.Diesel,
                    Make = "DAFF",
                    Model = "DAFF Transit",
                    CargoCapacity = 5000

                },
                  new CarType
                {
                    Engine = EngineType.Petrol,
                    Make = "Chevrolet",
                    Model = "Kalos",
                    NoOfPassengers = 2

                }
            });
        }
    }
}