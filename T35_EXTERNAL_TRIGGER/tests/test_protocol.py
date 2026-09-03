import struct
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))

from t35_protocol import ProtocolError, crc16_ccitt_false, decode_frame, encode_frame


class ProtocolTests(unittest.TestCase):
    def test_standard_crc_vector(self):
        self.assertEqual(crc16_ccitt_false(b"123456789"), 0x29B1)

    def test_frame_round_trip(self):
        raw = encode_frame(0x10, struct.pack("<I", 12345))
        frame = decode_frame(raw)
        self.assertEqual(frame.version, 1)
        self.assertEqual(frame.command, 0x10)
        self.assertEqual(struct.unpack("<I", frame.payload)[0], 12345)

    def test_crc_rejection(self):
        raw = bytearray(encode_frame(0x01))
        raw[-1] ^= 1
        with self.assertRaises(ProtocolError):
            decode_frame(bytes(raw))

    def test_payload_limit(self):
        with self.assertRaises(ValueError):
            encode_frame(0x01, bytes(65))


if __name__ == "__main__":
    unittest.main()
