"""Ankama account creation via Camoufox anti-detect browser."""

import json
import logging
import random
import re
import time

from camoufox.sync_api import Camoufox

from captcha_solver import CaptchaSolver
from identity import Identity
from proxy_manager import Proxy

logger = logging.getLogger(__name__)

WEBAUTH_URL = "https://account.ankama.com/webauth/authorize?from=https://www.ankama.com/en"
CHALLENGE_JS_URL = "https://3f38f7f4f368.edge.sdk.awswaf.com/3f38f7f4f368/e1fcfc58118e/challenge.js"
PROTONMAIL_URL = "https://mail.proton.me"


def _rand_delay(range_ms: tuple[int, int]) -> None:
    time.sleep(random.randint(range_ms[0], range_ms[1]) / 1000.0)


def _human_type(page, selector: str, text: str, delay_ms: tuple[int, int] = (50, 150)) -> None:
    """Type text character by character with random delays."""
    el = page.query_selector(selector)
    if el:
        el.scroll_into_view_if_needed()
    _rand_delay((80, 200))
    page.click(selector)
    _rand_delay((100, 300))
    for char in text:
        page.keyboard.press(char)
        _rand_delay(delay_ms)


def _human_select(page, selector: str, value: str) -> None:
    """Select a dropdown value with human-like timing."""
    el = page.query_selector(selector)
    if el:
        el.scroll_into_view_if_needed()
    _rand_delay((80, 200))
    page.click(selector)
    _rand_delay((200, 500))
    page.select_option(selector, value)
    _rand_delay((100, 300))


def _add_mouse_noise(page, duration_ms: int = 2000) -> None:
    """Generate random mouse movements to build behavioral telemetry for WAF."""
    viewport = page.viewport_size or {"width": 1280, "height": 720}
    steps = random.randint(3, 7)
    for _ in range(steps):
        x = random.randint(100, viewport["width"] - 100)
        y = random.randint(100, viewport["height"] - 100)
        page.mouse.move(x, y, steps=random.randint(5, 15))
        _rand_delay((100, int(duration_ms / steps)))


def _wait_for_waf_integration(page, timeout_s: int = 30) -> bool:
    """Wait until AwsWafIntegration is loaded and has a token.

    The <secure-form> component on auth.ankama.com calls
    AwsWafIntegration.getToken() before every POST. If the integration
    isn't ready, the POST will be rejected with 403.
    """
    logger.info("Waiting for AwsWafIntegration to initialise...")
    deadline = time.monotonic() + timeout_s
    while time.monotonic() < deadline:
        ready = page.evaluate("""() => {
            if (typeof AwsWafIntegration === 'undefined') return 'missing';
            if (typeof AwsWafIntegration.hasToken === 'function' && AwsWafIntegration.hasToken()) return 'ready';
            if (typeof AwsWafIntegration.getToken === 'function') return 'no_token';
            return 'partial';
        }""")
        if ready == "ready":
            logger.info("AwsWafIntegration ready with valid token")
            return True
        if ready == "no_token":
            # Integration exists but no token yet — try requesting one
            page.evaluate("AwsWafIntegration.getToken().catch(() => {})")
        time.sleep(1)
    logger.warning("AwsWafIntegration not ready after %ds (last state: %s)", timeout_s, ready)
    return False


def _refresh_waf_token(page) -> str | None:
    """Request a fresh token from AwsWafIntegration.getToken() right before POST."""
    try:
        token = page.evaluate("""async () => {
            if (typeof AwsWafIntegration === 'undefined') return null;
            try {
                const t = await AwsWafIntegration.getToken();
                return t || null;
            } catch (e) {
                return null;
            }
        }""")
        if token:
            logger.info("Refreshed WAF token (%d chars)", len(token))
        else:
            logger.warning("AwsWafIntegration.getToken() returned empty")
        return token
    except Exception as e:
        logger.warning("Failed to refresh WAF token: %s", e)
        return None


def _retrieve_verification_code(browser, email_address: str, email_password: str,
                                 max_wait_s: int = 120) -> str | None:
    """Log into ProtonMail and retrieve the Ankama verification code.

    Opens a new tab, logs in, finds the latest Ankama email, extracts the code.
    """
    logger.info("Opening ProtonMail to retrieve verification code for %s...", email_address)
    # The base email (without +tag) is what we log into
    base_email = re.sub(r'\+[^@]+', '', email_address)

    mail_page = browser.new_page()
    try:
        mail_page.goto(PROTONMAIL_URL, wait_until="domcontentloaded", timeout=60000)
        time.sleep(2)

        # Always try to log in — check for the sign-in form
        username_field = mail_page.query_selector("#username")
        if username_field:
            logger.info("Logging into ProtonMail as %s...", base_email)
            mail_page.fill("#username", base_email)
            mail_page.click("button[type='submit']")
            time.sleep(2)
            mail_page.wait_for_selector("#password", timeout=15000)
            mail_page.fill("#password", email_password)
            mail_page.click("button[type='submit']")
            time.sleep(3)

            # Wait for inbox to load — try multiple selectors
            try:
                mail_page.wait_for_selector("[data-testid='message-item'], [data-shortcut-target='message-container'], .item-container, [data-element-id]", timeout=60000)
                logger.info("ProtonMail inbox loaded at %s", mail_page.url)
            except Exception as e:
                logger.warning("Inbox load wait failed: %s — continuing anyway", e)
                time.sleep(5)
        else:
            logger.info("No login form found — may already be logged in at %s", mail_page.url)

        # Poll for the Ankama verification email
        code = None
        start = time.time()
        while time.time() - start < max_wait_s:
            # Debug: log current URL and page state
            logger.info("ProtonMail URL: %s", mail_page.url)

            # Try multiple selectors for email items
            email_items = mail_page.query_selector_all("[data-testid='message-item']")
            if not email_items:
                email_items = mail_page.query_selector_all("[data-shortcut-target='message-container']")
            if not email_items:
                email_items = mail_page.query_selector_all(".item-container")
            if not email_items:
                # Fallback: try to find any list items in the inbox
                email_items = mail_page.query_selector_all("[data-element-id]")

            logger.info("Found %d email items in inbox", len(email_items))
            if not email_items:
                # Log page text for debugging
                page_text = mail_page.inner_text("body")[:300] if mail_page.query_selector("body") else "empty"
                logger.info("Page text: %s", page_text.strip())

            for item in email_items[:5]:  # check top 5 emails
                text = item.inner_text().lower()
                logger.info("Email item text: %s", text[:100])
                if "ankama" in text or "security code" in text or "verification" in text or "code" in text:
                    item.click()
                    time.sleep(3)

                    # Scroll to bottom of conversation
                    mail_page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
                    time.sleep(2)

                    # ProtonMail collapses most messages in long threads.
                    # Find all collapsed message headers and expand the LAST one
                    # Collapsed messages are clickable elements without an expanded body
                    collapsed = mail_page.query_selector_all(
                        "[data-testid='message-header-collapsed']"
                    )
                    if not collapsed:
                        # Try alternative selectors for collapsed messages
                        collapsed = mail_page.query_selector_all(
                            ".message-header-collapsed, "
                            ".message-container:not(.is-opened) .message-header"
                        )
                    logger.info("Found %d collapsed message headers", len(collapsed))

                    if collapsed:
                        # Click the LAST collapsed header (newest message)
                        last = collapsed[-1]
                        try:
                            last.scroll_into_view_if_needed()
                            time.sleep(1)
                            last.click()
                            time.sleep(3)
                            logger.info("Expanded last collapsed message")
                        except Exception as e:
                            logger.warning("Failed to expand last message: %s", e)

                    # Now scroll to bottom again and get iframes
                    mail_page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
                    time.sleep(2)

                    # Get all iframes — filter out system frames
                    iframes = mail_page.query_selector_all("iframe")
                    real_iframes = []
                    for iframe in iframes:
                        frame = iframe.content_frame()
                        if not frame:
                            continue
                        try:
                            body = frame.inner_text("body")
                        except Exception:
                            continue
                        if body and len(body) > 20 and "requires Javascript" not in body:
                            real_iframes.append((iframe, body))

                    logger.info("Found %d real message iframes after expand", len(real_iframes))

                    # Check from last (newest) to first
                    for idx in range(len(real_iframes) - 1, -1, -1):
                        _, body = real_iframes[idx]
                        code_match = re.search(r'\b(\d{6})\b', body)
                        if code_match:
                            candidate = code_match.group(1)
                            logger.info("Iframe %d code: %s, body: %s",
                                        idx, candidate, body[:100])
                            # Newest iframe with a code — use it
                            return candidate

                    # Go back to inbox for retry
                    mail_page.go_back()
                    time.sleep(2)
                    break  # Retry in next poll iteration

            logger.info("Ankama email not found yet, waiting 10s... (%.0fs elapsed)",
                        time.time() - start)
            time.sleep(10)
            try:
                mail_page.reload(wait_until="domcontentloaded", timeout=30000)
            except Exception:
                pass  # SPA may not fully settle — that's fine
            time.sleep(5)

        logger.warning("Could not find verification code after %ds", max_wait_s)
        return None
    finally:
        mail_page.close()


def create_account(
    identity: Identity,
    captcha_solver: CaptchaSolver | None = None,
    proxy: Proxy | None = None,
    behavior: dict | None = None,
    headless: bool = True,
    email_password: str | None = None,
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

        # Navigate through OAuth flow: webauth -> login page -> "Create an Account" -> registration form
        logger.info("Navigating to Ankama webauth (generates PKCE challenge)...")
        page.goto(WEBAUTH_URL, wait_until="networkidle", timeout=60000)
        _rand_delay(page_wait)

        # Check if we got blocked by WAF (403)
        page_html = page.content()
        if "403 ERROR" in page_html or "Request could not be satisfied" in page_html:
            if captcha_solver:
                logger.info("Got WAF block, attempting CAPTCHA solve...")
                waf_cookie = captcha_solver.solve_aws_waf(
                    page.url, page_html=page_html, challenge_js_url=CHALLENGE_JS_URL,
                )
                if waf_cookie:
                    page.context.add_cookies([{
                        "name": "aws-waf-token",
                        "value": waf_cookie,
                        "domain": ".ankama.com",
                        "path": "/",
                    }])
                    page.goto(WEBAUTH_URL, wait_until="networkidle", timeout=60000)
                    _rand_delay(page_wait)

            if "403 ERROR" in page.content():
                raise RuntimeError("WAF blocked access even after CAPTCHA solve. Need residential proxy.")

        # Wait for SPA to render (page is a Vue/React app)
        page.wait_for_selector("a, button, input", timeout=15000)
        _rand_delay(page_wait)

        # Build behavioral telemetry early — WAF scores mouse/scroll events
        _add_mouse_noise(page, duration_ms=3000)

        logger.info("Reached login page at %s", page.url)

        # Click "Ankama Connect" to get to the login form with "Create an Account" link
        ankama_connect = page.query_selector("a[href*='login/ankama']")
        if ankama_connect:
            logger.info("Clicking Ankama Connect...")
            ankama_connect.click()
            # Wait for the login form to appear (has the email/password fields)
            page.wait_for_selector("input[name='login']", timeout=15000)
            _rand_delay(page_wait)
            logger.info("Login form loaded at %s", page.url)

        # Click "Create an Account" to reach registration form
        create_link = page.query_selector("a[href*='register']")
        if create_link:
            logger.info("Clicking 'Create an Account'...")
            create_link.click()
        else:
            raise RuntimeError(f"Could not find 'Create an Account' link on page: {page.url}")

        # Wait for registration form to load
        logger.info("Waiting for registration form...")
        page.wait_for_selector("input[name='email']", timeout=30000)
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

        # Birthday - select dropdowns (values are zero-padded: "01", "02", etc.)
        _human_select(page, "select[name='birthday-day']", f"{identity.birthday_day:02d}")
        _rand_delay(click_delay)
        _human_select(page, "select[name='birthday-month']", f"{identity.birthday_month:02d}")
        _rand_delay(click_delay)
        _human_select(page, "select[name='birthday-year']", str(identity.birthday_year))
        _rand_delay(click_delay)

        # Handle CAPTCHA if present
        _handle_captcha(page, captcha_solver)

        # Add more mouse noise after filling the form — builds WAF behavioral score
        _add_mouse_noise(page, duration_ms=2000)

        # --- Diagnostics: capture page state before POST ---
        _log_page_diagnostics(page)

        # Check if the WAF token cookie is present. challenge.js sets it automatically
        # during navigation. If it's missing, try the external solver as fallback.
        waf_cookies = [c for c in page.context.cookies() if c["name"] == "aws-waf-token"]
        if waf_cookies:
            logger.info("WAF token cookie present (domain=%s, %d chars)",
                        waf_cookies[0]["domain"], len(waf_cookies[0]["value"]))
        else:
            logger.warning("No aws-waf-token cookie found — trying external solver")
            if captcha_solver:
                waf_cookie = captcha_solver.solve_aws_waf(
                    page.url, page_html=page.content(), challenge_js_url=CHALLENGE_JS_URL,
                )
                if waf_cookie:
                    page.context.add_cookies([{
                        "name": "aws-waf-token",
                        "value": waf_cookie,
                        "domain": ".auth.ankama.com",
                        "path": "/",
                    }])

        # Check for AwsWafIntegration JS API (may not be present — challenge.js
        # on auth.ankama.com operates silently via cookies without exposing globals)
        has_waf_api = page.evaluate("typeof AwsWafIntegration !== 'undefined'")
        if has_waf_api:
            _wait_for_waf_integration(page, timeout_s=15)
        else:
            logger.info("No AwsWafIntegration JS API — WAF operates via cookie only")

        # Submit the form (with WAF retry logic)
        max_waf_retries = 3
        for attempt in range(max_waf_retries):
            logger.info("Submitting registration form (attempt %d/%d)...", attempt + 1, max_waf_retries)

            # If AwsWafIntegration JS API exists, refresh token before POST
            if has_waf_api:
                _refresh_waf_token(page)

            # Capture the response status from the form submission
            form_response_status = [None]
            form_response_body = [None]
            form_response_headers = [None]
            form_request_headers = [None]
            def capture_response(response):
                if "form-submit" in response.url:
                    form_response_status[0] = response.status
                    form_response_headers[0] = response.headers
                    try:
                        form_request_headers[0] = response.request.headers
                    except Exception:
                        pass
                    try:
                        form_response_body[0] = response.text()
                    except Exception:
                        pass
            page.on("response", capture_response)

            # Submit via button click (the <secure-form> on auth.ankama.com
            # does not expose a handleSubmit JS API — it intercepts form
            # submission at the DOM level)
            submit_btn = page.query_selector("button[type='submit']")
            if submit_btn:
                submit_btn.scroll_into_view_if_needed()
                _rand_delay((200, 500))
                submit_btn.click()
            else:
                page.evaluate("document.querySelector('form')?.submit()")

            # Wait for navigation / response
            _rand_delay((3000, 5000))
            page.remove_listener("response", capture_response)

            # Check for success redirect
            if "success" in page.url.lower():
                logger.info("Account created successfully: %s", identity.email)
                return {
                    "success": True,
                    "email": identity.email,
                    "password": identity.password,
                    "first_name": identity.first_name,
                    "last_name": identity.last_name,
                }

            # Check for email verification code page
            if "/register/ankama/code" in page.url:
                logger.info("Registration submitted — email verification required")
                page_text = page.inner_text("body")[:500] if page.query_selector("body") else ""
                logger.info("Verification page: %s", page_text.strip())

                if email_password:
                    code = _retrieve_verification_code(browser, identity.email, email_password)
                    if code:
                        logger.info("Entering verification code: %s", code)
                        code_input = page.query_selector("input[name='code'], input[type='text'], input[placeholder*='code' i]")
                        if code_input:
                            _human_type(page, "input[name='code'], input[type='text'], input[placeholder*='code' i]", code, typing_delay)
                            _rand_delay(click_delay)
                            submit_btn = page.query_selector("button[type='submit']")
                            if submit_btn:
                                submit_btn.click()
                            _rand_delay((3000, 5000))

                            if "success" in page.url.lower() or "authorized" in page.url.lower() or "account" in page.url.lower():
                                logger.info("Account created and verified: %s", identity.email)
                                return {
                                    "success": True,
                                    "email": identity.email,
                                    "password": identity.password,
                                    "first_name": identity.first_name,
                                    "last_name": identity.last_name,
                                    "verified": True,
                                }
                            else:
                                logger.warning("Verification code submitted but unexpected redirect: %s", page.url)
                    else:
                        logger.warning("Could not retrieve verification code from email")

                # Return partial success — registration worked, needs manual verification
                return {
                    "success": True,
                    "email": identity.email,
                    "password": identity.password,
                    "first_name": identity.first_name,
                    "last_name": identity.last_name,
                    "verified": False,
                    "note": "Email verification code required. Check inbox for code.",
                }

            # Check if WAF blocked the POST (403)
            if form_response_status[0] == 403 or "form-submit" in page.url:
                logger.info("WAF challenge on POST (status=%s, body=%s)",
                            form_response_status[0],
                            (form_response_body[0] or "")[:200])

                if attempt < max_waf_retries - 1:
                    # Log full 403 details for analysis
                    logger.info("403 response headers: %s", form_response_headers[0])
                    logger.info("403 request headers: %s", form_request_headers[0])
                    logger.info("403 response body:\n%s", form_response_body[0] or "")

                    # Wait for challenge.js to regenerate a fresh cookie
                    logger.info("Waiting for challenge.js to regenerate token...")
                    time.sleep(5)

                    # Check if challenge.js updated the cookie
                    new_cookies = [c for c in page.context.cookies() if c["name"] == "aws-waf-token"]
                    if new_cookies:
                        logger.info("Cookie after wait: domain=%s, %d chars",
                                    new_cookies[0]["domain"], len(new_cookies[0]["value"]))

                    # Actively request a fresh token if JS API is available
                    if has_waf_api:
                        _refresh_waf_token(page)

                    # Also try external solver as backup
                    if captcha_solver:
                        waf_cookie = captcha_solver.solve_aws_waf(
                            page.url, page_html=page.content(),
                            challenge_js_url=CHALLENGE_JS_URL,
                        )
                        if waf_cookie:
                            page.context.add_cookies([{
                                "name": "aws-waf-token",
                                "value": waf_cookie,
                                "domain": ".auth.ankama.com",
                                "path": "/",
                            }])

                    # Add more behavioral noise before retry
                    _add_mouse_noise(page, duration_ms=2000)

                    # Go back to the form and refill
                    logger.info("Going back to form for retry...")
                    page.go_back()
                    page.wait_for_selector("input[name='email']", timeout=15000)
                    _rand_delay(page_wait)

                    # Wait for WAF to re-init (if JS API available)
                    if has_waf_api:
                        _wait_for_waf_integration(page, timeout_s=15)

                    # Refill the form
                    _human_type(page, "input[name='email']", identity.email, typing_delay)
                    _rand_delay(click_delay)
                    _human_type(page, "input[name='password']", identity.password, typing_delay)
                    _rand_delay(click_delay)
                    _human_type(page, "input[name='firstname']", identity.first_name, typing_delay)
                    _rand_delay(click_delay)
                    _human_type(page, "input[name='lastname']", identity.last_name, typing_delay)
                    _rand_delay(click_delay)
                    _human_select(page, "select[name='birthday-day']", f"{identity.birthday_day:02d}")
                    _rand_delay(click_delay)
                    _human_select(page, "select[name='birthday-month']", f"{identity.birthday_month:02d}")
                    _rand_delay(click_delay)
                    _human_select(page, "select[name='birthday-year']", str(identity.birthday_year))
                    _rand_delay(click_delay)
                    _add_mouse_noise(page, duration_ms=1500)
                    continue

            # Check for validation errors
            error_els = page.query_selector_all(".text-error, .has-error, .alert-danger, [role='alert']")
            errors = [el.inner_text().strip() for el in error_els if el.inner_text().strip()]
            if errors:
                raise RuntimeError(f"Registration failed with errors: {'; '.join(errors)}")

            # No success, no clear error — check page content
            logger.info("No success redirect detected. Final URL: %s", page.url)
            logger.info("Form POST response status: %s", form_response_status[0])
            logger.info("Form POST response body: %s", (form_response_body[0] or "")[:500])

            # Check for success indicators beyond URL
            page_text = page.inner_text("body")[:1000] if page.query_selector("body") else ""
            logger.info("Page body text (first 1000 chars): %s", page_text)

            # Check if we actually landed on a success/account page
            if any(kw in page.url.lower() for kw in ["authorized", "account", "dashboard", "profile", "welcome"]):
                logger.info("Possible success — landed on account-related URL: %s", page.url)
                return {
                    "success": True,
                    "email": identity.email,
                    "password": identity.password,
                    "first_name": identity.first_name,
                    "last_name": identity.last_name,
                    "note": f"Redirected to {page.url} (may need email verification)",
                }

            break

        # Final error check
        error_el = page.query_selector(".text-error, .error-message, .alert-danger, [role='alert']")
        error_text = error_el.inner_text().strip() if error_el else ""

        # Log full page state for debugging
        logger.info("Final page URL: %s", page.url)
        logger.info("Final page title: %s", page.title())
        page_text = page.inner_text("body")[:1000] if page.query_selector("body") else ""
        logger.info("Final page body (first 1000): %s", page_text)

        if "form-submit" in page.url:
            raise RuntimeError(f"WAF blocked form submission after {max_waf_retries} retries. "
                             f"The AWS WAF challenge could not be solved automatically. "
                             f"Last POST status: {form_response_status[0]}. "
                             f"Errors: {error_text or 'none visible'}")

        raise RuntimeError(f"Registration failed: {error_text or 'Unknown error'} | URL: {page.url} | POST status: {form_response_status[0]}")


def _log_page_diagnostics(page) -> None:
    """Log page state to understand what WAF scripts/tokens are available."""
    try:
        diag = page.evaluate("""() => {
            const result = {};
            // Check for AwsWafIntegration variants
            result.AwsWafIntegration = typeof AwsWafIntegration !== 'undefined';
            result.AwsWafCaptcha = typeof AwsWafCaptcha !== 'undefined';
            result.awsWafCookieManager = typeof awsWafCookieManager !== 'undefined';
            // Check for secure-form
            const sf = document.querySelector('secure-form');
            result.secureForm = !!sf;
            result.secureFormMethods = sf ? Object.getOwnPropertyNames(
                Object.getPrototypeOf(sf)).filter(m => m !== 'constructor').slice(0, 10) : [];
            // List all scripts loaded
            result.scripts = Array.from(document.querySelectorAll('script[src]'))
                .map(s => s.src).filter(s => s.includes('waf') || s.includes('challenge') || s.includes('captcha'));
            // Check for WAF-related globals
            const wafGlobals = [];
            for (const key of Object.keys(window)) {
                const lk = key.toLowerCase();
                if (lk.includes('waf') || lk.includes('captcha') || lk.includes('goku') || lk.includes('challenge')) {
                    wafGlobals.push(key);
                }
            }
            result.wafGlobals = wafGlobals.slice(0, 20);
            return result;
        }""")
        logger.info("Page diagnostics: %s", json.dumps(diag, indent=2))

        # Also log cookies
        cookies = page.context.cookies()
        waf_cookies = [c for c in cookies if 'waf' in c['name'].lower() or 'aws' in c['name'].lower()]
        logger.info("WAF-related cookies: %s", json.dumps([{"name": c["name"], "domain": c["domain"],
                     "value": c["value"][:50] + "..." if len(c["value"]) > 50 else c["value"]}
                     for c in waf_cookies], indent=2))
        all_cookie_names = [c['name'] for c in cookies]
        logger.info("All cookie names: %s", all_cookie_names)
    except Exception as e:
        logger.warning("Diagnostics failed: %s", e)


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
        waf_cookie = captcha_solver.solve_aws_waf(page.url, page_html=page.content(), challenge_js_url=CHALLENGE_JS_URL)
        if waf_cookie:
            page.context.add_cookies([{
                "name": "aws-waf-token",
                "value": waf_cookie,
                "domain": ".ankama.com",
                "path": "/",
            }])
            page.reload(wait_until="networkidle")
            _rand_delay((1000, 2000))
