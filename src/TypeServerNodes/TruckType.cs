using Opc.Ua;


namespace TypeServerNodes
{
	public class TruckType : VehicleType
	{
		public uint CargoCapacity { get; set; }
		private static ExpandedNodeId typeId = new ExpandedNodeId(338,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId TypeId => typeId;
		private static ExpandedNodeId binaryEncodingId = new ExpandedNodeId(331,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId BinaryEncodingId => binaryEncodingId;
		private static ExpandedNodeId xmlNodeId = new ExpandedNodeId(319,"http://opcfoundation.org/UA/Quickstarts/DataTypes/Types");
		public override ExpandedNodeId  XmlEncodingId => xmlNodeId;

	public override void Encode(IEncoder encoder)
	{
		base.Encode(encoder);
		encoder.WriteUInt32("CargoCapacity", this.CargoCapacity);
	}

	public override void Decode(IDecoder decoder)
	{
		base.Decode(decoder);
		this.CargoCapacity = decoder.ReadUInt32("CargoCapacity");
	}
	}
}