#!/usr/bin/env python3
"""Command-line and deterministic report tool for T35_EXTERNAL_TRIGGER."""

from __future__ import annotations

import argparse
import csv
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

try:
    import serial
    from serial.tools import list_ports
except ImportError as exc:  # pragma: no cover - exercised on deployment hosts
    raise SystemExit("pyserial is required: python -m pip install -r requirements.txt") from exc

from t35_protocol import ProtocolError, T35Device


def ns_to_ticks(value: int) -> int:
    if value <= 0 or value % 10:
        raise ValueError("nanosecond values must be positive multiples of 10")
    return value // 10


def choose_port(explicit: str | None) -> str:
    if explicit:
        return explicit
    ports = [item.device for item in list_ports.comports()]
    if len(ports) != 1:
        raise ProtocolError(f"serial auto-discovery requires exactly one port; found {ports}")
    return ports[0]


def write_reports(record: dict, json_path: str | None, csv_path: str | None) -> None:
    if json_path:
        Path(json_path).write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding="utf-8")
    if csv_path:
        flat = {}
        for key, value in record.items():
            if isinstance(value, dict):
                for child_key, child_value in value.items():
                    flat[f"{key}.{child_key}"] = child_value
            else:
                flat[key] = value
        path = Path(csv_path)
        with path.open("w", newline="", encoding="utf-8-sig") as handle:
            writer = csv.DictWriter(handle, fieldnames=list(flat))
            writer.writeheader()
            writer.writerow(flat)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", help="serial port; omitted only when exactly one port exists")
    parser.add_argument("--json", dest="json_path", help="write a JSON result")
    parser.add_argument("--csv", dest="csv_path", help="write a CSV result")
    sub = parser.add_subparsers(dest="action", required=True)
    sub.add_parser("ping")
    config = sub.add_parser("configure")
    config.add_argument("--period-ns", type=int, required=True)
    config.add_argument("--width-ns", type=int, required=True)
    config.add_argument("--output-active-low", action="store_true")
    config.add_argument("--input-active-high", action="store_true")
    start = sub.add_parser("start")
    start.add_argument("--count", type=int, default=0)
    sub.add_parser("stop")
    sub.add_parser("pulse")
    sub.add_parser("status")
    sub.add_parser("stats")
    sub.add_parser("clear")
    loop = sub.add_parser("loopback")
    loop.add_argument("--period-ns", type=int, required=True)
    loop.add_argument("--width-ns", type=int, required=True)
    loop.add_argument("--count", type=int, default=1000)
    loop.add_argument("--timeout", type=float, default=10.0)
    sweep = sub.add_parser("sweep")
    sweep.add_argument("--periods-ns", required=True, help="comma-separated periods")
    sweep.add_argument("--widths-ns", required=True, help="comma-separated widths")
    sweep.add_argument("--count", type=int, default=1000)
    return parser


def run_action(device: T35Device, args: argparse.Namespace) -> dict:
    if args.action == "ping":
        return device.ping()
    if args.action == "configure":
        period = ns_to_ticks(args.period_ns)
        width = ns_to_ticks(args.width_ns)
        device.set_period(period)
        device.set_width(width)
        device.set_polarity(args.output_active_low, not args.input_active_high)
        return device.status()
    if args.action == "start":
        device.start(args.count)
        return device.status()
    if args.action == "stop":
        device.stop()
        return device.status()
    if args.action == "pulse":
        device.pulse_once()
        return device.status()
    if args.action == "status":
        return device.status()
    if args.action == "stats":
        return device.stats()
    if args.action == "clear":
        device.clear_stats()
        return device.stats()
    if args.action == "loopback":
        period = ns_to_ticks(args.period_ns)
        width = ns_to_ticks(args.width_ns)
        device.loopback(period, width, args.count)
        status = device.wait_loopback(args.timeout)
        return {"status": status, "stats": device.stats()}
    if args.action == "sweep":
        periods = [ns_to_ticks(int(value)) for value in args.periods_ns.split(",")]
        widths = [ns_to_ticks(int(value)) for value in args.widths_ns.split(",")]
        results = []
        for period in periods:
            for width in widths:
                if width >= period:
                    continue
                started = datetime.now(timezone.utc)
                try:
                    device.loopback(period, width, args.count)
                    status = device.wait_loopback(max(5.0, period * args.count / 100_000_000 * 2 + 1))
                    result = {"passed": True, "status": status, "stats": device.stats()}
                except ProtocolError as exc:
                    result = {"passed": False, "failure": str(exc)}
                result.update({"period_ticks": period, "width_ticks": width, "count": args.count,
                               "started_utc": started.isoformat()})
                results.append(result)
        return {"cases": results, "passed": all(case["passed"] for case in results)}
    raise AssertionError(args.action)


def main() -> int:
    args = build_parser().parse_args()
    port_name = choose_port(args.port)
    record = {
        "timestamp_utc": datetime.now(timezone.utc).isoformat(),
        "serial_port": port_name,
        "action": args.action,
    }
    try:
        with serial.Serial(port_name, 115200, timeout=0.05, write_timeout=1.0) as port:
            device = T35Device(port)
            record["device"] = device.ping()
            record["result"] = run_action(device, args)
        record["passed"] = True
    except (ProtocolError, ValueError, serial.SerialException) as exc:
        record["passed"] = False
        record["failure"] = str(exc)

    write_reports(record, args.json_path, args.csv_path)
    print(json.dumps(record, ensure_ascii=False, indent=2))
    return 0 if record["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
