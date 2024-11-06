using Opc.Ua;


namespace TypeServerNodes
{
	public class BicycleType : TwoWheelerType
	{
		public uint NoOfGears { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(15006,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(15005,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(15009,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		base.Encode(encoder);
		encoder.WriteUInt32("NoOfGears", this.NoOfGears);
	}

	public override void Decode(IDecoder decoder)
	{
		base.Decode(decoder);
		this.NoOfGears = decoder.ReadUInt32("NoOfGears");
	}
	}
}