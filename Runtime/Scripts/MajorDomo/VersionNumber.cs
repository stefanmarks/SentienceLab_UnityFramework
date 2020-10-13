#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using FlatBuffers;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing a version number.
	/// </summary>
	/// 
	public class VersionNumber
	{
		public readonly byte Major;
		public readonly byte Minor;
		public readonly ushort Revision;


		public VersionNumber(byte _major = 0, byte _minor = 0, ushort _revision = 0)
		{
			Major    = _major;
			Minor    = _minor;
			Revision = _revision;
		}

		public VersionNumber(AUT_WH.MajorDomoProtocol.Version _version)
		{
			Major    = _version.NumMajor;
			Minor    = _version.NumMinor;
			Revision = _version.NumRevision;
		}

		public Offset<AUT_WH.MajorDomoProtocol.Version> ToFlatbuffer(FlatBufferBuilder _builder)
		{
			return AUT_WH.MajorDomoProtocol.Version.CreateVersion(_builder, Major, Minor, Revision);
		}

		override public string ToString() 
		{ 
			return Major + "." + Minor + "." + Revision;
		}
	}
}
