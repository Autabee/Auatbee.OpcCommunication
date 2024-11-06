using Opc.Ua;


namespace TypeServerNodes
{
	public class TwoWheelerType : VehicleType
	{
		public string ManufacturerName { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(15018,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(15016,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(15024,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		base.Encode(encoder);
		encoder.WriteString("ManufacturerName", this.ManufacturerName);
	}

	public override void Decode(IDecoder decoder)
	{
		base.Decode(decoder);
		this.ManufacturerName = decoder.ReadString("ManufacturerName");
	}
	}
}