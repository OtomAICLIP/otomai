#!/usr/bin/env python3
"""Ankama account creator - automated account generation with anti-detection.

Usage:
    python main.py --count 1 --config config.json
    python main.py --count 5 --headful  # visible browser for debugging
"""

import argparse
import json
import logging
import random
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from captcha_solver import CaptchaSolver
from creator import create_account
from identity import generate_identity
from proxy_manager import ProxyManager

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("ankama-creator")


def load_config(path: str) -> dict:
    config_file = Path(path)
    if not config_file.exists():
        logger.warning("Config file %s not found, using defaults", path)
        return {}
    return json.loads(config_file.read_text())


def save_account(account: dict, output_file: str) -> None:
    path = Path(output_file)
    accounts = []
    if path.exists():
        accounts = json.loads(path.read_text())

    account["created_at"] = datetime.now(timezone.utc).isoformat()
    accounts.append(account)
    path.write_text(json.dumps(accounts, indent=2))
    logger.info("Account saved to %s (total: %d)", output_file, len(accounts))


def main() -> int:
    parser = argparse.ArgumentParser(description="Ankama account creator")
    parser.add_argument("--count", type=int, default=1, help="Number of accounts to create")
    parser.add_argument("--config", default="config.json", help="Config file path")
    parser.add_argument("--headful", action="store_true", help="Run browser in visible mode")
    args = parser.parse_args()

    config = load_config(args.config)

    # Init CAPTCHA solver
    capsolver_key = config.get("capsolver_api_key", "")
    captcha_solver = CaptchaSolver(capsolver_key) if capsolver_key else None
    if not captcha_solver:
        logger.warning("No CAPTCHA solver API key configured. CAPTCHA challenges will block account creation.")

    # Init proxy manager
    proxy_cfg = config.get("proxy", {})
    proxy_mgr = None
    if proxy_cfg.get("enabled"):
        proxy_mgr = ProxyManager.from_file(
            proxy_cfg.get("list_file", "proxies.txt"),
            proxy_cfg.get("format", "host:port:user:pass"),
            proxy_cfg.get("rotate_after", 4),
        )
        logger.info("Proxy manager: %d proxies available", proxy_mgr.available_count)

    account_cfg = config.get("account", {})
    behavior_cfg = config.get("behavior", {})
    rate_cfg = config.get("rate_limit", {})
    output_file = config.get("output", {}).get("file", "accounts.json")

    created = 0
    failed = 0

    for i in range(args.count):
        logger.info("--- Account %d/%d ---", i + 1, args.count)

        identity = generate_identity(
            email_prefix=account_cfg.get("email_prefix", "otomai.gen"),
            email_domain=account_cfg.get("email_domain", "protonmail.com"),
            password_length=account_cfg.get("password_length", 16),
        )
        logger.info("Generated identity: %s (%s %s)", identity.email, identity.first_name, identity.last_name)

        proxy = proxy_mgr.next() if proxy_mgr else None
        if proxy_mgr and not proxy:
            logger.error("No proxies available, stopping")
            break

        try:
            account = create_account(
                identity=identity,
                captcha_solver=captcha_solver,
                proxy=proxy,
                behavior=behavior_cfg,
                headless=not args.headful,
            )
            save_account(account, output_file)
            created += 1
            if proxy_mgr and proxy:
                proxy_mgr.mark_used(proxy)
        except Exception:
            logger.exception("Failed to create account %s", identity.email)
            failed += 1

        # Rate limiting delay between accounts
        if i < args.count - 1:
            delay_range = rate_cfg.get("delay_between_accounts_s", [30, 90])
            delay = random.randint(delay_range[0], delay_range[1])
            logger.info("Waiting %ds before next account...", delay)
            time.sleep(delay)

    logger.info("Done: %d created, %d failed out of %d attempted", created, failed, args.count)
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
