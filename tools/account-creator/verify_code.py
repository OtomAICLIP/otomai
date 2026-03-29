"""Register a new Ankama account and wait for the verification code.

Combines registration + verification in one session so the code doesn't expire.
Reads the code from stdin (piped or interactive) once the verification page appears.
"""

import argparse
import json
import logging
import sys
import time
import random

from camoufox.sync_api import Camoufox
from identity import generate_identity

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(name)s: %(message)s")
logger = logging.getLogger("ankama-verify")

WEBAUTH_URL = "https://account.ankama.com/webauth/authorize?from=https://www.ankama.com/en"


def _rand_delay(range_ms):
    lo, hi = range_ms
    time.sleep(random.randint(lo, hi) / 1000)


def _human_type(page, selector, text, delay_range=(50, 150)):
    el = page.query_selector(selector)
    if not el:
        raise RuntimeError(f"Element not found: {selector}")
    el.click()
    for ch in text:
        el.type(ch, delay=random.randint(*delay_range))


def create_and_verify(email_prefix: str = "otomai.project",
                      email_domain: str = "protonmail.com",
                      code: str | None = None,
                      wait_for_code: bool = True):
    """Register account, then enter verification code in the same session."""

    identity = generate_identity(email_prefix=email_prefix, email_domain=email_domain)
    logger.info("Identity: %s (%s %s)", identity.email, identity.first_name, identity.last_name)

    with Camoufox(headless=False) as browser:
        page = browser.new_page()

        logger.info("Navigating to Ankama webauth...")
        page.goto(WEBAUTH_URL, wait_until="networkidle", timeout=60000)
        _rand_delay((1000, 3000))

        page.wait_for_selector("a, button, input", timeout=15000)
        logger.info("At: %s", page.url)

        # Click "Ankama Connect"
        ankama_connect = page.query_selector("a[href*='login/ankama']")
        if ankama_connect:
            logger.info("Clicking Ankama Connect...")
            ankama_connect.click()
            page.wait_for_selector("input[name='login']", timeout=15000)
            _rand_delay((1000, 2000))

        # Click "Create an Account"
        create_link = page.query_selector("a[href*='register']")
        if create_link:
            logger.info("Clicking 'Create an Account'...")
            create_link.click()
        else:
            raise RuntimeError("No 'Create an Account' link found")

        page.wait_for_selector("input[name='email']", timeout=30000)
        _rand_delay((1000, 2000))

        # Fill registration form
        logger.info("Filling form for %s...", identity.email)
        _human_type(page, "input[name='email']", identity.email)
        _rand_delay((200, 500))
        _human_type(page, "input[name='password']", identity.password)
        _rand_delay((200, 500))
        _human_type(page, "input[name='firstname']", identity.first_name)
        _rand_delay((200, 500))
        _human_type(page, "input[name='lastname']", identity.last_name)
        _rand_delay((200, 500))

        # Birthday (zero-padded)
        day_sel = page.query_selector("select[name='birthday-day']")
        if day_sel:
            day_sel.select_option(value=f"{identity.birthday_day:02d}")
        month_sel = page.query_selector("select[name='birthday-month']")
        if month_sel:
            month_sel.select_option(value=f"{identity.birthday_month:02d}")
        year_sel = page.query_selector("select[name='birthday-year']")
        if year_sel:
            year_sel.select_option(value=str(identity.birthday_year))
        _rand_delay((500, 1000))

        # Submit
        logger.info("Submitting registration form...")
        submit_btn = page.query_selector("button[type='submit']")
        if submit_btn:
            submit_btn.click()
        _rand_delay((3000, 6000))
        try:
            page.wait_for_load_state("networkidle", timeout=30000)
        except Exception:
            pass

        logger.info("After submit: %s", page.url)

        # Check for verification code page
        if "/code" not in page.url:
            page_text = page.inner_text("body")[:500] if page.query_selector("body") else ""
            logger.info("Page: %s", page_text.strip())
            if "success" in page.url.lower() or "authorized" in page.url.lower():
                logger.info("Account created without verification!")
                return {"success": True, "verified": True, "identity": identity}
            logger.error("Did not reach verification page")
            return {"success": False, "identity": identity}

        logger.info("Verification code page reached. A code was sent to %s", identity.email)

        # Get the code
        if not code and wait_for_code:
            # Poll for code from a file (works over non-interactive SSH)
            code_file = "pending_code.txt"
            logger.info("WAITING FOR CODE — write the 6-digit code to %s", code_file)
            # Clear any stale code file
            import os
            if os.path.exists(code_file):
                os.remove(code_file)
            sys.stdout.flush()
            for _ in range(120):  # wait up to 10 minutes
                if os.path.exists(code_file):
                    with open(code_file, "r") as f:
                        code = f.read().strip()
                    if code and len(code) >= 4:
                        os.remove(code_file)
                        break
                time.sleep(5)

        if not code:
            logger.warning("No code provided — account pending verification")
            return {"success": True, "verified": False, "identity": identity,
                    "note": "Pending email verification"}

        # Enter the code
        logger.info("Entering verification code: %s", code)
        code_input = page.query_selector("input[name='code'], input[type='text'], input[placeholder*='code' i]")
        if code_input:
            _human_type(page, "input[name='code'], input[type='text'], input[placeholder*='code' i]", code)
            _rand_delay((500, 1000))
            submit_btn = page.query_selector("button[type='submit']")
            if submit_btn:
                submit_btn.click()
            _rand_delay((3000, 6000))
            try:
                page.wait_for_load_state("networkidle", timeout=30000)
            except Exception:
                pass

            logger.info("After verification: %s", page.url)
            page_text = page.inner_text("body")[:500] if page.query_selector("body") else ""
            logger.info("Page: %s", page_text.strip())

            # Check if still on code page (code was wrong)
            if "/code" in page.url:
                logger.warning("Still on verification page — code may be invalid")
                return {"success": True, "verified": False, "identity": identity,
                        "note": "Verification code rejected"}

            logger.info("Account verified!")
            return {"success": True, "verified": True, "identity": identity}
        else:
            logger.error("No code input found")
            return {"success": True, "verified": False, "identity": identity}


def main():
    parser = argparse.ArgumentParser(description="Create Ankama account and verify in one session")
    parser.add_argument("--code", help="6-digit verification code (if known in advance)")
    parser.add_argument("--no-wait", action="store_true", help="Don't wait for code on stdin")
    parser.add_argument("--email-prefix", default="otomai.project")
    parser.add_argument("--email-domain", default="protonmail.com")
    parser.add_argument("--accounts-file", default="accounts.json")
    args = parser.parse_args()

    result = create_and_verify(
        email_prefix=args.email_prefix,
        email_domain=args.email_domain,
        code=args.code,
        wait_for_code=not args.no_wait,
    )

    identity = result["identity"]

    # Save to accounts.json
    try:
        with open(args.accounts_file, "r") as f:
            accounts = json.load(f)
    except (FileNotFoundError, json.JSONDecodeError):
        accounts = []

    accounts.append({
        "success": result["success"],
        "email": identity.email,
        "password": identity.password,
        "first_name": identity.first_name,
        "last_name": identity.last_name,
        "verified": result.get("verified", False),
        "note": result.get("note", ""),
    })

    with open(args.accounts_file, "w") as f:
        json.dump(accounts, f, indent=2)

    logger.info("Saved to %s", args.accounts_file)

    if result.get("verified"):
        logger.info("SUCCESS: %s is fully verified", identity.email)
    else:
        logger.info("PENDING: %s needs verification", identity.email)


if __name__ == "__main__":
    main()
