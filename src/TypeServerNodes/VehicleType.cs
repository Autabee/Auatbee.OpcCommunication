using Opc.Ua;


namespace TypeServerNodes
{
	public class VehicleType : EncodeableObject
	{
		public string Make { get; set; }
		public string Model { get; set; }
		public EngineType Engine { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(332,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(329,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(317,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		encoder.WriteString("Make", this.Make);
		encoder.WriteString("Model", this.Model);
		encoder.WriteEnumerated("EngineType", this.Engine);
	}

	public override void Decode(IDecoder decoder)
	{
		this.Make = decoder.ReadString("Make");
		this.Model = decoder.ReadString("Model");
		this.Engine = (EngineType)decoder.ReadEnumerated("Engine" ,typeof(EngineType));
	}
	}
}