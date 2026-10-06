"""Optional Windows adapter for the user's existing CodexUsageAccess-v1 launcher.

Imports the configured launcher locally; never copies its credentials or opens a browser.
The one JSON result goes only to the parent app's pipe. Do not run with output redirected
to a file: a successful result contains a temporary localhost launch key.
"""
import argparse
import importlib.util
import json
import sys
import time
from pathlib import Path


def emit(**value):
    print(json.dumps(value), flush=True)


def existing(access):
    try:
        runtime = access.load_protected(access.RUNTIME_FILE)
        if access.runtime_request(runtime, "/_access_health"):
            return f"http://127.0.0.1:{int(runtime['port'])}/_access_launch/{runtime['launch_key']}"
    except Exception:
        pass
    return None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--launcher", required=True)
    args = parser.parse_args()
    path = Path(args.launcher).resolve(strict=True)
    spec = importlib.util.spec_from_file_location("codex_usage_access_bridge_target", path)
    access = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = access
    spec.loader.exec_module(access)
    url = existing(access)
    if url:
        emit(ready=True, url=url)
        return 0
    lock = access.InstanceLock()
    acquired = lock.acquire()
    if not acquired:
        # The desktop shortcut may be starting or displaying its login prompt.
        deadline = time.monotonic() + 8
        while time.monotonic() < deadline:
            time.sleep(0.25)
            url = existing(access)
            if url:
                emit(ready=True, url=url)
                return 0
        emit(ready=False, error="existing_launcher_not_ready")
        return 1
    tunnel = access.Tunnel()
    server = None
    try:
        credentials = access.load_protected(access.CREDENTIAL_FILE)
        if tunnel.status(credentials) != 200:
            emit(ready=False, error="login_or_connection_required")
            return 1
        server = access.AccessServer(tunnel, credentials)
        credentials = None
        access.save_protected(access.RUNTIME_FILE, {"port": server.server_port, "launch_key": server.launch_key})
        emit(ready=True, url=server.launch_url)
        while not server.stop_requested and time.monotonic() - server.last_activity < access.IDLE_SECONDS:
            server.handle_request()
        return 0
    finally:
        if server is not None:
            server.server_close()
            access.RUNTIME_FILE.unlink(missing_ok=True)
        tunnel.close()
        lock.close()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception:
        # Never emit raw exceptions, passwords, SSH diagnostics, or protected runtime contents.
        emit(ready=False, error="launcher_unavailable")
        raise SystemExit(1)
