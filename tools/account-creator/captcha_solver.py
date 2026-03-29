"""CAPTCHA solver with multiple backends: free auto-solver, harvester, nopecha, or paid services."""

import base64
import http.server
import json
import logging
import re
import threading
import time
import webbrowser

import requests

logger = logging.getLogger(__name__)

CAPSOLVER_API = "https://api.capsolver.com"
TWOCAPTCHA_API = "https://api.2captcha.com"
NOPECHA_API = "https://api.nopecha.com"
WAF_HELPER_API = "https://aws-waf-helper.vercel.app"


class CaptchaSolver:
    """Multi-backend CAPTCHA solver.

    Backends:
      - waf-solver: Free automated AWS WAF solver (uses aws-waf-helper.vercel.app AI endpoint)
      - harvester: Free manual solving via local browser page
      - nopecha: Free tier (100 solves/month) cloud API
      - capsolver: Paid cloud API
      - 2captcha: Paid cloud API
    """

    def __init__(self, backend: str = "waf-solver", api_key: str = ""):
        self.backend = backend
        self.api_key = api_key

        if backend not in ("harvester", "waf-solver") and not api_key:
            raise ValueError(f"API key required for backend '{backend}'")

    def solve_aws_waf(self, website_url: str, page_html: str = "", challenge_js_url: str | None = None) -> str:
        """Solve AWS WAF CAPTCHA. Returns the aws-waf-token cookie value.

        Args:
            website_url: The URL being protected by WAF.
            page_html: Raw HTML of the WAF challenge page (needed for waf-solver backend).
            challenge_js_url: URL to challenge.js (optional, for capsolver).
        """
        dispatch = {
            "waf-solver": lambda: self._waf_solver_solve(website_url, page_html),
            "harvester": lambda: self._harvest_waf_token(website_url),
            "capsolver": lambda: self._capsolver_aws_waf(website_url, challenge_js_url),
            "2captcha": lambda: self._twocaptcha_aws_waf(website_url),
            "nopecha": lambda: self._harvest_waf_token(website_url),  # nopecha doesn't support WAF
        }
        handler = dispatch.get(self.backend)
        if not handler:
            raise ValueError(f"Unknown backend: {self.backend}")
        return handler()

    def solve_funcaptcha(self, website_url: str, public_key: str, surl: str | None = None) -> str:
        """Solve Arkose Labs FunCAPTCHA. Returns the token."""
        dispatch = {
            "waf-solver": lambda: self._harvest_funcaptcha(website_url, public_key),  # WAF solver is AWS-only
            "harvester": lambda: self._harvest_funcaptcha(website_url, public_key),
            "capsolver": lambda: self._capsolver_funcaptcha(website_url, public_key, surl),
            "2captcha": lambda: self._twocaptcha_funcaptcha(website_url, public_key, surl),
            "nopecha": lambda: self._nopecha_funcaptcha(website_url, public_key),
        }
        handler = dispatch.get(self.backend)
        if not handler:
            raise ValueError(f"Unknown backend: {self.backend}")
        return handler()

    # --- Free AWS WAF Auto-Solver Backend (uses aws-waf-helper.vercel.app) ---

    def _waf_solver_solve(self, website_url: str, page_html: str, max_retries: int = 3) -> str:
        """Solve AWS WAF CAPTCHA using the free aws-waf-helper AI endpoint.

        Protocol:
        1. Parse gokuProps from the challenge page HTML
        2. Fetch the CAPTCHA problem (visual or audio) from the WAF challenge endpoint
        3. Send the problem assets to the free AI solver API
        4. Submit the solution back to the WAF verify endpoint
        5. Exchange the voucher for an aws-waf-token
        """
        from urllib.parse import urlparse

        domain = urlparse(website_url).hostname or ""
        if domain.startswith("www."):
            domain = domain[4:]

        # Step 1: Extract gokuProps and challenge base URL from HTML
        goku_props, base_url = self._waf_parse_goku_props(page_html)
        if not goku_props or not base_url:
            logger.warning("Could not extract gokuProps from page HTML, falling back to harvester")
            return self._harvest_waf_token(website_url)

        for attempt in range(1, max_retries + 1):
            try:
                token = self._waf_solver_attempt(goku_props, base_url, domain)
                if token:
                    logger.info("AWS WAF solved on attempt %d", attempt)
                    return token
            except Exception as e:
                logger.warning("WAF solve attempt %d failed: %s", attempt, e)

        logger.warning("All WAF solver attempts failed, falling back to harvester")
        return self._harvest_waf_token(website_url)

    def _waf_parse_goku_props(self, html: str) -> tuple[dict | None, str | None]:
        """Extract window.gokuProps and challenge base URL from WAF page HTML."""
        # Extract gokuProps JSON
        match = re.search(r'window\.gokuProps\s*=\s*({.*?});', html, re.DOTALL)
        if not match:
            return None, None
        try:
            goku_props = json.loads(match.group(1))
        except json.JSONDecodeError:
            return None, None

        # Extract challenge base URL from script src
        script_match = re.search(r'<script\s+src="(https://[^"]*?/challenge\.js)"', html)
        if script_match:
            challenge_url = script_match.group(1)
            # Base URL is everything before /challenge.js, with /captcha appended
            base_url = challenge_url.rsplit("/", 1)[0] + "/captcha"
        else:
            base_url = None

        return goku_props, base_url

    def _waf_solver_attempt(self, goku_props: dict, base_url: str, domain: str, solution_type: str = "visual") -> str | None:
        """Single attempt to solve the WAF CAPTCHA."""
        key = goku_props.get("key", "")

        # Fetch the problem
        problem_url = f"{base_url}/problem?kind={solution_type}&key={key}&domain={domain}&locale=en-us"
        resp = requests.get(problem_url, timeout=30)
        resp.raise_for_status()
        problem = resp.json()

        if solution_type == "visual":
            solution = self._waf_solve_visual(problem)
        else:
            solution = self._waf_solve_audio(problem)

        if not solution:
            return None

        # Verify the solution
        verify_url = f"{base_url}/verify"
        verify_resp = requests.post(
            verify_url,
            json={"key": key, "answer": solution, "domain": domain},
            timeout=30,
        )
        verify_resp.raise_for_status()
        verify_data = verify_resp.json()

        voucher = verify_data.get("captcha_voucher")
        if not voucher:
            logger.debug("Verification failed: %s", verify_data)
            return None

        # Exchange voucher for token
        token_base = base_url.replace("/captcha", "/token")
        token_resp = requests.post(
            f"{token_base}/voucher",
            json={"key": key, "voucher": voucher, "domain": domain},
            timeout=30,
        )
        token_resp.raise_for_status()
        token_data = token_resp.json()
        return token_data.get("token", "")

    def _waf_solve_visual(self, problem: dict) -> str | None:
        """Send visual CAPTCHA images to the free AI solver."""
        images = problem.get("images", [])
        target = problem.get("target", "")
        if not images or not target:
            return None

        resp = requests.post(
            f"{WAF_HELPER_API}/getVisualSolution",
            json={"images": images, "target": target},
            timeout=60,
        )
        resp.raise_for_status()
        data = resp.json()
        return data.get("result")

    def _waf_solve_audio(self, problem: dict) -> str | None:
        """Send audio CAPTCHA to the free AI solver."""
        audio_data = problem.get("audioData", "")
        if not audio_data:
            return None

        resp = requests.post(
            f"{WAF_HELPER_API}/getAudioSolution",
            json={"audioData": audio_data},
            timeout=60,
        )
        resp.raise_for_status()
        data = resp.json()
        return data.get("result")

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
