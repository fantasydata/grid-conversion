using SportsDataIO.GRidConversion;
using System;
using System.Buffers.Text;
using Xunit; // Requires 'xunit' and 'xunit.runner.visualstudio' nuget packages

namespace SportsDataIO.GRidConversion.Tests
{
    public class Base62Tests
    {
        [Fact]
        public void RoundTrip_EncodeDecode_ReturnsOriginalGuid()
        {
            // Arrange
            var originalGuid = Guid.NewGuid();
            string typeId = "event";

            // Act
            string encoded = SportsDataIO.GRidConversion.Base62.Encode(originalGuid, typeId);
            Guid decoded = SportsDataIO.GRidConversion.Base62.Decode(encoded);

            // Assert
            Assert.Equal(originalGuid, decoded);
            Assert.StartsWith("event_", encoded);
            Assert.Equal(22, encoded.Split('_')[1].Length); // Ensure 22 chars
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")] // Empty
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")] // Max
        public void EdgeCases_HandleLimits(string guidString)
        {
            var guid = Guid.Parse(guidString);
            string encoded = SportsDataIO.GRidConversion.Base62.Encode(guid, "tst");
            Guid decoded = SportsDataIO.GRidConversion.Base62.Decode(encoded);
            Assert.Equal(guid, decoded);
        }

        [Fact]
        public void KnownValue_ConsistencyCheck()
        {
            // This specific GUID ensures we catch Endianness bugs. Via testing on different 
            // If the first 4 bytes (int) or next 2 bytes (short) are flipped incorrectly,
            // the resulting number will be wildly different.
            var guid = Guid.Parse("9b3ea5f2-e43b-44d0-83f3-e2d97dfff065");
            string typeId = "item";

            // Expected value calculated from a verified BigInteger implementation
            // If this test passes on Windows and Linux, your Endian logic is portable.
            string expected = "item_4iwRLEQjyM887lIHKr7h3d";

            string encoded = Base62.Encode(guid, typeId);

            Assert.Equal(expected, encoded);
            Assert.Equal(guid, Base62.Decode(encoded));
        }
    }
}