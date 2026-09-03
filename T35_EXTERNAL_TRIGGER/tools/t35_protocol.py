"""Binary protocol helpers for the T35 P3 external-trigger controller."""

from __future__ import annotations

import struct
import time
from dataclasses import dataclass
from typing import BinaryIO

SOF = b"\x55\xaa"
VERSION = 1
MAX_PAYLOAD = 64

CMD = {
    "ping": 0x01,
    "set_period": 0x10,
    "set_width": 0x11,
    "set_polarity": 0x12,
    "start": 0x13,
    "stop": 0x14,
    "pulse_once": 0x15,
    "read_status": 0x20,
    "read_stats": 0x21,
    "clear_stats": 0x22,
    "loopback": 0x30,
}

STATUS_TEXT = {
    0: "ok",
    1: "bad_version",
    2: "unknown_command",
    3: "bad_length",
    4: "bad_crc",
    5: "invalid_parameter",
    6: "not_configured",
    7: "busy",
    8: "frame_timeout",
    9: "uart_frame_error",
}


class ProtocolError(RuntimeError):
    pass


class DeviceError(ProtocolError):
    def __init__(self, status: int):
        self.status = status
        super().__init__(STATUS_TEXT.get(status, f"device_error_{status}"))


def crc16_ccitt_false(data: bytes) -> int:
    crc = 0xFFFF
    for value in data:
        crc ^= value << 8
        for _ in range(8):
            crc = ((crc << 1) ^ 0x1021) & 0xFFFF if crc & 0x8000 else (crc << 1) & 0xFFFF
    return crc


def encode_frame(command: int, payload: bytes = b"") -> bytes:
    if len(payload) > MAX_PAYLOAD:
        raise ValueError("payload exceeds 64 bytes")
    body = bytes((VERSION, command & 0xFF, len(payload))) + payload
    return SOF + body + struct.pack("<H", crc16_ccitt_false(body))


@dataclass(frozen=True)
class Frame:
    version: int
    command: int
    payload: bytes


def decode_frame(frame: bytes) -> Frame:
    if len(frame) < 7 or frame[:2] != SOF:
        raise ProtocolError("invalid SOF or truncated frame")
    version, command, length = frame[2:5]
    if length > MAX_PAYLOAD or len(frame) != 7 + length:
        raise ProtocolError("invalid frame length")
    body = frame[2 : 5 + length]
    received_crc = struct.unpack_from("<H", frame, 5 + length)[0]
    if crc16_ccitt_false(body) != received_crc:
        raise ProtocolError("CRC mismatch")
    return Frame(version, command, frame[5 : 5 + length])


def _read_exact(port: BinaryIO, size: int, deadline: float) -> bytes:
    data = bytearray()
    while len(data) < size:
        if time.monotonic() >= deadline:
            raise ProtocolError(f"serial timeout while reading {size} bytes")
        chunk = port.read(size - len(data))
        if chunk:
            data.extend(chunk)
    return bytes(data)


def read_frame(port: BinaryIO, timeout: float = 1.0) -> Frame:
    deadline = time.monotonic() + timeout
    matched = 0
    while matched < 2:
        value = _read_exact(port, 1, deadline)[0]
        if matched == 0:
            matched = 1 if value == 0x55 else 0
        elif value == 0xAA:
            matched = 2
        else:
            matched = 1 if value == 0x55 else 0
    header = _read_exact(port, 3, deadline)
    length = header[2]
    if length > MAX_PAYLOAD:
        raise ProtocolError(f"response payload length {length} exceeds 64")
    tail = _read_exact(port, length + 2, deadline)
    return decode_frame(SOF + header + tail)


class T35Device:
    def __init__(self, port: BinaryIO):
        self.port = port

    def request(self, command: int, payload: bytes = b"", timeout: float = 1.0) -> bytes:
        self.port.write(encode_frame(command, payload))
        if hasattr(self.port, "flush"):
            self.port.flush()
        frame = read_frame(self.port, timeout)
        expected = command | 0x80
        if frame.version != VERSION or frame.command != expected:
            raise ProtocolError(
                f"unexpected response version/cmd: {frame.version}/{frame.command:#04x}, expected 1/{expected:#04x}"
            )
        if not frame.payload:
            raise ProtocolError("response has no status byte")
        if frame.payload[0] != 0:
            raise DeviceError(frame.payload[0])
        return frame.payload[1:]

    def ping(self) -> dict:
        data = self.request(CMD["ping"])
        if len(data) != 12:
            raise ProtocolError("invalid PING response length")
        return {
            "firmware_version": f"{data[0]}.{data[1]}.{data[2]}",
            "protocol_version": data[3],
            "clock_hz": struct.unpack_from("<I", data, 4)[0],
            "capabilities": struct.unpack_from("<I", data, 8)[0],
        }

    def set_period(self, ticks: int) -> None:
        self.request(CMD["set_period"], struct.pack("<I", ticks))

    def set_width(self, ticks: int) -> None:
        self.request(CMD["set_width"], struct.pack("<I", ticks))

    def set_polarity(self, output_active_low: bool, input_active_low: bool) -> None:
        value = int(output_active_low) | (int(input_active_low) << 1)
        self.request(CMD["set_polarity"], bytes((value,)))

    def start(self, count: int = 0) -> None:
        self.request(CMD["start"], struct.pack("<I", count))

    def stop(self) -> None:
        self.request(CMD["stop"])

    def pulse_once(self) -> None:
        self.request(CMD["pulse_once"])

    def clear_stats(self) -> None:
        self.request(CMD["clear_stats"])

    def status(self) -> dict:
        data = self.request(CMD["read_status"])
        if len(data) != 16:
            raise ProtocolError("invalid READ_STATUS response length")
        flags = struct.unpack_from("<H", data, 0)[0]
        return {
            "running": bool(flags & (1 << 0)),
            "configured": bool(flags & (1 << 1)),
            "pending_update": bool(flags & (1 << 2)),
            "precharge": bool(flags & (1 << 3)),
            "loopback_busy": bool(flags & (1 << 4)),
            "loopback_pass": bool(flags & (1 << 5)),
            "loopback_fail": bool(flags & (1 << 6)),
            "input_timeout": bool(flags & (1 << 7)),
            "counter_overflow": bool(flags & (1 << 8)),
            "period_ticks": struct.unpack_from("<I", data, 2)[0],
            "width_ticks": struct.unpack_from("<I", data, 6)[0],
            "output_active_low": bool(data[10] & 1),
            "input_active_low": bool(data[10] & 2),
            "remaining": struct.unpack_from("<I", data, 11)[0],
            "last_error": STATUS_TEXT.get(data[15], f"error_{data[15]}"),
        }

    def stats(self) -> dict:
        data = self.request(CMD["read_stats"])
        if len(data) != 18:
            raise ProtocolError("invalid READ_INPUT_STATS response length")
        flags = data[16] | (data[17] << 8)
        return {
            "event_count": struct.unpack_from("<I", data, 0)[0],
            "last_width_ticks": struct.unpack_from("<I", data, 4)[0],
            "last_period_ticks": struct.unpack_from("<I", data, 8)[0],
            "too_narrow_count": struct.unpack_from("<I", data, 12)[0],
            "timeout": bool(flags & 1),
            "overflow": bool(flags & 2),
        }

    def loopback(self, period_ticks: int, width_ticks: int, count: int) -> None:
        # The stopped-low requirement makes low-active physical loopback
        # ambiguous at STOP, so the deterministic loopback command is high-active.
        payload = struct.pack("<III", period_ticks, width_ticks, count) + b"\x00"
        self.request(CMD["loopback"], payload)

    def wait_loopback(self, timeout: float) -> dict:
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            status = self.status()
            if not status["loopback_busy"]:
                if status["loopback_pass"]:
                    return status
                raise ProtocolError(f"loopback failed: {status}")
            time.sleep(0.02)
        raise ProtocolError("loopback completion timeout")
