"""CAPTCHA solver integration for AWS WAF and Arkose Labs FunCAPTCHA."""

import logging
import time

import requests

logger = logging.getLogger(__name__)

CAPSOLVER_API = "https://api.capsolver.com"


class CaptchaSolver:
    def __init__(self, api_key: str):
        if not api_key:
            raise ValueError("CAPTCHA solver API key is required")
        self.api_key = api_key

    def _create_task(self, task: dict) -> str:
        resp = requests.post(
            f"{CAPSOLVER_API}/createTask",
            json={"clientKey": self.api_key, "task": task},
            timeout=30,
        )
        resp.raise_for_status()
        data = resp.json()
        if data.get("errorId", 0) != 0:
            raise RuntimeError(f"CapSolver createTask error: {data.get('errorDescription')}")
        return data["taskId"]

    def _poll_result(self, task_id: str, timeout_s: int = 120) -> dict:
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
                raise RuntimeError(f"CapSolver poll error: {data.get('errorDescription')}")
            if data.get("status") == "ready":
                return data["solution"]
        raise TimeoutError(f"CAPTCHA solve timed out after {timeout_s}s")

    def solve_aws_waf(self, website_url: str, challenge_js_url: str | None = None) -> str:
        """Solve AWS WAF CAPTCHA. Returns the aws-waf-token cookie value."""
        task = {
            "type": "AntiAwsWafTaskProxyLess",
            "websiteURL": website_url,
        }
        if challenge_js_url:
            task["awsChallengeJS"] = challenge_js_url

        logger.info("Submitting AWS WAF CAPTCHA solve request...")
        task_id = self._create_task(task)
        solution = self._poll_result(task_id)
        cookie = solution.get("cookie", "")
        logger.info("AWS WAF CAPTCHA solved successfully")
        return cookie

    def solve_funcaptcha(self, website_url: str, public_key: str, surl: str | None = None) -> str:
        """Solve Arkose Labs FunCAPTCHA. Returns the token."""
        task = {
            "type": "FunCaptchaTaskProxyLess",
            "websiteURL": website_url,
            "websitePublicKey": public_key,
        }
        if surl:
            task["funcaptchaApiJSSubdomain"] = surl

        logger.info("Submitting FunCAPTCHA solve request...")
        task_id = self._create_task(task)
        solution = self._poll_result(task_id)
        token = solution.get("token", "")
        logger.info("FunCAPTCHA solved successfully")
        return token
