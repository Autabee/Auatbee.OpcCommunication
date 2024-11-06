using Opc.Ua;


namespace TypeServerNodes
{
	public class ScooterType : TwoWheelerType
	{
		public uint NoOfSeats { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(15021,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(15017,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(15025,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		base.Encode(encoder);
		encoder.WriteUInt32("NoOfSeats", this.NoOfSeats);
	}

	public override void Decode(IDecoder decoder)
	{
		base.Decode(decoder);
		this.NoOfSeats = decoder.ReadUInt32("NoOfSeats");
	}
	}
}