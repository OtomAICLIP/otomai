"""CAPTCHA solver with multiple backends: free harvester, nopecha, or paid services."""

import http.server
import json
import logging
import threading
import time
import webbrowser

import requests

logger = logging.getLogger(__name__)

CAPSOLVER_API = "https://api.capsolver.com"
TWOCAPTCHA_API = "https://api.2captcha.com"
NOPECHA_API = "https://api.nopecha.com"


class CaptchaSolver:
    """Multi-backend CAPTCHA solver. Supports: harvester (free), nopecha, capsolver, 2captcha."""

    def __init__(self, backend: str = "harvester", api_key: str = ""):
        self.backend = backend
        self.api_key = api_key

        if backend != "harvester" and not api_key:
            raise ValueError(f"API key required for backend '{backend}'")

    def solve_aws_waf(self, website_url: str, challenge_js_url: str | None = None) -> str:
        """Solve AWS WAF CAPTCHA. Returns the aws-waf-token cookie value."""
        if self.backend == "harvester":
            return self._harvest_waf_token(website_url)
        elif self.backend == "capsolver":
            return self._capsolver_aws_waf(website_url, challenge_js_url)
        elif self.backend == "2captcha":
            return self._twocaptcha_aws_waf(website_url)
        elif self.backend == "nopecha":
            logger.warning("Nopecha does not support AWS WAF directly, falling back to harvester")
            return self._harvest_waf_token(website_url)
        else:
            raise ValueError(f"Unknown backend: {self.backend}")

    def solve_funcaptcha(self, website_url: str, public_key: str, surl: str | None = None) -> str:
        """Solve Arkose Labs FunCAPTCHA. Returns the token."""
        if self.backend == "harvester":
            return self._harvest_funcaptcha(website_url, public_key)
        elif self.backend == "capsolver":
            return self._capsolver_funcaptcha(website_url, public_key, surl)
        elif self.backend == "2captcha":
            return self._twocaptcha_funcaptcha(website_url, public_key, surl)
        elif self.backend == "nopecha":
            return self._nopecha_funcaptcha(website_url, public_key)
        else:
            raise ValueError(f"Unknown backend: {self.backend}")

    # --- Free Harvester Backend ---

    def _harvest_waf_token(self, website_url: str) -> str:
        """Open a local server where user solves CAPTCHA manually and pastes the token."""
        logger.info("CAPTCHA harvester: waiting for manual WAF token...")
        return self._run_harvester_server(
            "AWS WAF Token",
            f"Visit {website_url} in a real browser, solve the CAPTCHA, "
            "then copy the 'aws-waf-token' cookie value and paste it below.",
        )

    def _harvest_funcaptcha(self, website_url: str, public_key: str) -> str:
        logger.info("CAPTCHA harvester: waiting for manual FunCAPTCHA token...")
        return self._run_harvester_server(
            "FunCAPTCHA Token",
            f"Visit {website_url} in a real browser, solve the FunCAPTCHA, "
            "then extract the fc-token from the page and paste it below.",
        )

    def _run_harvester_server(self, title: str, instructions: str, timeout_s: int = 300) -> str:
        """Spin up a tiny local HTTP server for the user to submit a solved token."""
        result = {"token": ""}
        event = threading.Event()

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                self.send_response(200)
                self.send_header("Content-Type", "text/html")
                self.end_headers()
                self.wfile.write(f"""<!DOCTYPE html>
<html><head><title>{title}</title>
<style>body{{font-family:sans-serif;max-width:600px;margin:40px auto;padding:20px}}
textarea{{width:100%;height:80px}}button{{padding:10px 20px;margin-top:10px;cursor:pointer}}</style>
</head><body>
<h2>{title}</h2><p>{instructions}</p>
<textarea id="token" placeholder="Paste token here..."></textarea><br>
<button onclick="fetch('/submit',{{method:'POST',body:document.getElementById('token').value}}).then(()=>document.body.innerHTML='<h2>Done! You can close this tab.</h2>')">Submit Token</button>
</body></html>""".encode())

            def do_POST(self):
                length = int(self.headers.get("Content-Length", 0))
                result["token"] = self.rfile.read(length).decode().strip()
                self.send_response(200)
                self.end_headers()
                self.wfile.write(b"ok")
                event.set()

            def log_message(self, *args):
                pass

        server = http.server.HTTPServer(("127.0.0.1", 0), Handler)
        port = server.server_address[1]
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()

        url = f"http://127.0.0.1:{port}"
        logger.info("Harvester ready at %s — open in browser to submit token", url)
        try:
            webbrowser.open(url)
        except Exception:
            pass

        print(f"\n>>> CAPTCHA HARVESTER: Open {url} in your browser to submit the token <<<\n")
        event.wait(timeout=timeout_s)
        server.shutdown()

        if not result["token"]:
            raise TimeoutError(f"No token received within {timeout_s}s")

        logger.info("Harvester received token (%d chars)", len(result["token"]))
        return result["token"]

    # --- CapSolver Backend ---

    def _capsolver_create_task(self, task: dict) -> str:
        resp = requests.post(
            f"{CAPSOLVER_API}/createTask",
            json={"clientKey": self.api_key, "task": task},
            timeout=30,
        )
        resp.raise_for_status()
        data = resp.json()
        if data.get("errorId", 0) != 0:
            raise RuntimeError(f"CapSolver error: {data.get('errorDescription')}")
        return data["taskId"]

    def _capsolver_poll(self, task_id: str, timeout_s: int = 120) -> dict:
        deadline = time.monotonic() + timeout_s
        while time.monotonic() < deadline:
            time.sleep(3)
            resp = requests.post(
                f"{CAPSOLVER_API}/getTaskResult",
                json={"clientKey": self.api_key, "taskId": task_id},
                timeout=30,
            )
            resp.raise_for_status()
            data = resp.json()
            if data.get("errorId", 0) != 0:
                raise RuntimeError(f"CapSolver error: {data.get('errorDescription')}")
            if data.get("status") == "ready":
                return data["solution"]
        raise TimeoutError(f"CAPTCHA solve timed out after {timeout_s}s")

    def _capsolver_aws_waf(self, website_url: str, challenge_js_url: str | None = None) -> str:
        task = {"type": "AntiAwsWafTaskProxyLess", "websiteURL": website_url}
        if challenge_js_url:
            task["awsChallengeJS"] = challenge_js_url
        logger.info("CapSolver: submitting AWS WAF solve...")
        task_id = self._capsolver_create_task(task)
        solution = self._capsolver_poll(task_id)
        return solution.get("cookie", "")

    def _capsolver_funcaptcha(self, website_url: str, public_key: str, surl: str | None = None) -> str:
        task = {"type": "FunCaptchaTaskProxyLess", "websiteURL": website_url, "websitePublicKey": public_key}
        if surl:
            task["funcaptchaApiJSSubdomain"] = surl
        logger.info("CapSolver: submitting FunCAPTCHA solve...")
        task_id = self._capsolver_create_task(task)
        solution = self._capsolver_poll(task_id)
        return solution.get("token", "")

    # --- 2Captcha Backend ---

    def _twocaptcha_aws_waf(self, website_url: str) -> str:
        resp = requests.post(
            f"{TWOCAPTCHA_API}/createTask",
            json={"clientKey": self.api_key, "task": {"type": "AmazonTaskProxyless", "websiteURL": website_url}},
            timeout=30,
        )
        resp.raise_for_status()
        task_id = resp.json().get("taskId")
        return self._twocaptcha_poll(task_id).get("cookies", {}).get("aws-waf-token", "")

    def _twocaptcha_funcaptcha(self, website_url: str, public_key: str, surl: str | None = None) -> str:
        task = {"type": "FunCaptchaTaskProxyless", "websiteURL": website_url, "websitePublicKey": public_key}
        if surl:
            task["funcaptchaApiJSSubdomain"] = surl
        resp = requests.post(
            f"{TWOCAPTCHA_API}/createTask",
            json={"clientKey": self.api_key, "task": task},
            timeout=30,
        )
        resp.raise_for_status()
        task_id = resp.json().get("taskId")
        return self._twocaptcha_poll(task_id).get("token", "")

    def _twocaptcha_poll(self, task_id: str, timeout_s: int = 120) -> dict:
        deadline = time.monotonic() + timeout_s
        while time.monotonic() < deadline:
            time.sleep(5)
            resp = requests.post(
                f"{TWOCAPTCHA_API}/getTaskResult",
                json={"clientKey": self.api_key, "taskId": task_id},
                timeout=30,
            )
            resp.raise_for_status()
            data = resp.json()
            if data.get("status") == "ready":
                return data.get("solution", {})
        raise TimeoutError(f"2captcha timed out after {timeout_s}s")

    # --- Nopecha Backend (free tier: 100 solves/month) ---

    def _nopecha_funcaptcha(self, website_url: str, public_key: str) -> str:
        resp = requests.post(
            f"{NOPECHA_API}/token",
            json={"type": "funcaptcha", "sitekey": public_key, "url": website_url, "key": self.api_key},
            timeout=60,
        )
        resp.raise_for_status()
        data = resp.json()
        if "data" in data:
            return data["data"]
        raise RuntimeError(f"Nopecha error: {data}")
