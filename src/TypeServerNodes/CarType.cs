using Opc.Ua;


namespace TypeServerNodes
{
	public class CarType : VehicleType
	{
		public uint NoOfPassengers { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(335,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(330,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(318,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		base.Encode(encoder);
		encoder.WriteUInt32("NoOfPassengers", this.NoOfPassengers);
	}

	public override void Decode(IDecoder decoder)
	{
		base.Decode(decoder);
		this.NoOfPassengers = decoder.ReadUInt32("NoOfPassengers");
	}
	}
}