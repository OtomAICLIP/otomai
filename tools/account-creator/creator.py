"""Ankama account creation via Camoufox anti-detect browser."""

import logging
import random
import time

from camoufox.sync_api import Camoufox

from captcha_solver import CaptchaSolver
from identity import Identity
from proxy_manager import Proxy

logger = logging.getLogger(__name__)

REGISTER_URL = "https://www.ankama.com/en/create-account"
AUTH_REGISTER_URL = "https://auth.ankama.com/register/ankama/form"
CHALLENGE_JS_URL = "https://3f38f7f4f368.edge.sdk.awswaf.com/3f38f7f4f368/ab89a1b580a1/challenge.js"


def _rand_delay(range_ms: tuple[int, int]) -> None:
    time.sleep(random.randint(range_ms[0], range_ms[1]) / 1000.0)


def _human_type(page, selector: str, text: str, delay_ms: tuple[int, int] = (50, 150)) -> None:
    """Type text character by character with random delays."""
    page.click(selector)
    _rand_delay((100, 300))
    for char in text:
        page.keyboard.press(char)
        _rand_delay(delay_ms)


def _human_select(page, selector: str, value: str) -> None:
    """Select a dropdown value with human-like timing."""
    page.click(selector)
    _rand_delay((200, 500))
    page.select_option(selector, value)
    _rand_delay((100, 300))


def create_account(
    identity: Identity,
    captcha_solver: CaptchaSolver | None = None,
    proxy: Proxy | None = None,
    behavior: dict | None = None,
    headless: bool = True,
) -> dict:
    """Create an Ankama account using Camoufox browser automation.

    Returns dict with account details on success, raises on failure.
    """
    beh = behavior or {}
    typing_delay = tuple(beh.get("typing_delay_ms", [50, 150]))
    click_delay = tuple(beh.get("click_delay_ms", [200, 800]))
    page_wait = tuple(beh.get("page_wait_ms", [1000, 3000]))

    camoufox_kwargs = {"headless": headless}
    if proxy:
        camoufox_kwargs["proxy"] = proxy.playwright_config

    with Camoufox(**camoufox_kwargs) as browser:
        page = browser.new_page()

        # Navigate to registration page
        logger.info("Navigating to Ankama registration page...")
        page.goto(REGISTER_URL, wait_until="networkidle", timeout=60000)
        _rand_delay(page_wait)

        # Check if we got blocked by WAF (403)
        if "403 ERROR" in page.content() or "Request could not be satisfied" in page.content():
            # Try injecting WAF token via CAPTCHA solver
            if captcha_solver:
                logger.info("Got WAF block, attempting CAPTCHA solve...")
                waf_cookie = captcha_solver.solve_aws_waf(REGISTER_URL, CHALLENGE_JS_URL)
                if waf_cookie:
                    # Set the WAF cookie and retry
                    page.context.add_cookies([{
                        "name": "aws-waf-token",
                        "value": waf_cookie,
                        "domain": ".ankama.com",
                        "path": "/",
                    }])
                    page.goto(REGISTER_URL, wait_until="networkidle", timeout=60000)
                    _rand_delay(page_wait)

            if "403 ERROR" in page.content():
                raise RuntimeError("WAF blocked access even after CAPTCHA solve. Need residential proxy.")

        # Wait for registration form to load
        logger.info("Waiting for registration form...")
        page.wait_for_selector("input[name='email'], #email, [data-testid='email']", timeout=30000)
        _rand_delay(page_wait)

        # Fill the registration form with human-like typing
        logger.info("Filling registration form for %s...", identity.email)

        # Email
        _human_type(page, "input[name='email']", identity.email, typing_delay)
        _rand_delay(click_delay)

        # Password
        _human_type(page, "input[name='password']", identity.password, typing_delay)
        _rand_delay(click_delay)

        # First name
        _human_type(page, "input[name='firstname']", identity.first_name, typing_delay)
        _rand_delay(click_delay)

        # Last name
        _human_type(page, "input[name='lastname']", identity.last_name, typing_delay)
        _rand_delay(click_delay)

        # Birthday - select dropdowns
        _human_select(page, "select[name='birthday-day']", str(identity.birthday_day))
        _rand_delay(click_delay)
        _human_select(page, "select[name='birthday-month']", str(identity.birthday_month))
        _rand_delay(click_delay)
        _human_select(page, "select[name='birthday-year']", str(identity.birthday_year))
        _rand_delay(click_delay)

        # Handle CAPTCHA if present
        _handle_captcha(page, captcha_solver)

        # Submit the form
        logger.info("Submitting registration form...")
        submit_btn = page.query_selector("button[type='submit'], input[type='submit'], .submit-btn")
        if submit_btn:
            submit_btn.click()
        else:
            # Try triggering the secure-form handleSubmit
            page.evaluate("document.querySelector('secure-form')?.handleSubmit()")

        # Wait for response
        try:
            page.wait_for_url("**/register/ankama/success**", timeout=30000)
            logger.info("Account created successfully: %s", identity.email)
            return {
                "success": True,
                "email": identity.email,
                "password": identity.password,
                "first_name": identity.first_name,
                "last_name": identity.last_name,
            }
        except Exception:
            pass

        # Check for error messages
        error_el = page.query_selector(".error-message, .alert-danger, [role='alert']")
        error_text = error_el.inner_text() if error_el else "Unknown error"

        # Check if WAF blocked the POST
        if "403" in page.content() or "waf" in page.content().lower():
            raise RuntimeError(f"WAF blocked form submission: {error_text}")

        raise RuntimeError(f"Registration failed: {error_text}")


def _handle_captcha(page, captcha_solver: CaptchaSolver | None) -> None:
    """Detect and solve CAPTCHAs on the page."""
    # Check for Arkose FunCAPTCHA iframe
    funcaptcha_frame = page.query_selector("iframe[src*='arkoselabs'], iframe[data-e2e='enforcement-frame']")
    if funcaptcha_frame and captcha_solver:
        logger.info("Arkose FunCAPTCHA detected, solving...")
        # Extract the public key from the iframe src or page scripts
        public_key = page.evaluate("""() => {
            const iframe = document.querySelector('iframe[src*="arkoselabs"]');
            if (iframe) {
                const url = new URL(iframe.src);
                return url.searchParams.get('pkey') || '';
            }
            // Try to find it in page scripts
            const scripts = document.querySelectorAll('script');
            for (const s of scripts) {
                const match = s.textContent.match(/publicKey['":\\s]+['"]([a-f0-9-]+)['"]/);
                if (match) return match[1];
            }
            return '';
        }""")

        if public_key:
            token = captcha_solver.solve_funcaptcha(page.url, public_key)
            # Inject the solved token
            page.evaluate(f"""() => {{
                const textarea = document.querySelector('textarea[name="fc-token"]');
                if (textarea) {{
                    textarea.value = '{token}';
                    textarea.dispatchEvent(new Event('change', {{bubbles: true}}));
                }}
                // Also try the callback approach
                if (window.fcCallback) window.fcCallback('{token}');
            }}""")
            _rand_delay((500, 1000))
        else:
            logger.warning("FunCAPTCHA detected but could not extract public key")

    # Check for AWS WAF CAPTCHA
    waf_captcha = page.query_selector("#captcha-container, .awswaf-captcha")
    if waf_captcha and captcha_solver:
        logger.info("AWS WAF CAPTCHA detected, solving...")
        waf_cookie = captcha_solver.solve_aws_waf(page.url, CHALLENGE_JS_URL)
        if waf_cookie:
            page.context.add_cookies([{
                "name": "aws-waf-token",
                "value": waf_cookie,
                "domain": ".ankama.com",
                "path": "/",
            }])
            page.reload(wait_until="networkidle")
            _rand_delay((1000, 2000))
