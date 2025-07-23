using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public enum Flags:byte
	{
		None = 0b00,
		Urgent = 0b10000000,
		UrgentEncrypted = 0b11000000,
		UrgentCompressed = 0b1010000,
        UrgentEncryptedCompressed = 0b11100000,
        Encrypted = 0b0100000,
        EncryptedCompressed = 0b0110000,
        Compressed = 0b00100000

	}
}

