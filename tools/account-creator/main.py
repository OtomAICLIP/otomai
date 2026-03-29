#!/usr/bin/env python3
"""Ankama account creator - automated account generation with anti-detection.

Usage:
    python main.py --count 1 --config config.json
    python main.py --count 5 --headful  # visible browser for debugging
    python main.py --count 1 --captcha-backend harvester  # free: manual CAPTCHA solving
    python main.py --count 1 --captcha-backend nopecha --captcha-key YOUR_KEY  # nopecha free tier
    python main.py --count 1 --proxy-mode free  # scrape free proxy lists
    python main.py --count 1 --proxy-mode direct  # use local IP (must be residential)
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

CAPTCHA_BACKENDS = ["waf-solver", "harvester", "capsolver", "2captcha", "nopecha"]
PROXY_MODES = ["direct", "file", "free"]


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


def build_captcha_solver(config: dict, args: argparse.Namespace) -> CaptchaSolver | None:
    backend = args.captcha_backend or config.get("captcha_backend", "waf-solver")
    api_key = args.captcha_key or config.get("captcha_api_key", "")

    if backend not in CAPTCHA_BACKENDS:
        logger.error("Unknown CAPTCHA backend: %s (available: %s)", backend, ", ".join(CAPTCHA_BACKENDS))
        return None

    if backend == "waf-solver":
        logger.info("Using free AWS WAF auto-solver (aws-waf-helper.vercel.app)")
        return CaptchaSolver(backend="waf-solver")

    if backend == "harvester":
        logger.info("Using free CAPTCHA harvester (manual solving in browser)")
        return CaptchaSolver(backend="harvester")

    if not api_key:
        logger.warning("No API key for %s backend, falling back to waf-solver", backend)
        return CaptchaSolver(backend="waf-solver")

    logger.info("Using %s CAPTCHA backend", backend)
    return CaptchaSolver(backend=backend, api_key=api_key)


def build_proxy_manager(config: dict, args: argparse.Namespace) -> ProxyManager | None:
    mode = args.proxy_mode or config.get("proxy_mode", "direct")
    proxy_cfg = config.get("proxy", {})
    max_per_ip = proxy_cfg.get("rotate_after", 4)

    if mode == "direct":
        logger.info("Using direct connection (no proxy). Your IP must be residential.")
        return None

    if mode == "free":
        logger.info("Scraping free proxy lists...")
        mgr = ProxyManager.from_free_lists(max_per_ip=max_per_ip)
        if mgr.available_count == 0:
            logger.warning("No working free proxies found, falling back to direct connection")
            return None
        logger.info("Free proxy pool: %d proxies available", mgr.available_count)
        return mgr

    if mode == "file":
        if not proxy_cfg.get("list_file"):
            logger.warning("No proxy list file configured, running direct")
            return None
        mgr = ProxyManager.from_file(
            proxy_cfg["list_file"],
            proxy_cfg.get("format", "host:port:user:pass"),
            max_per_ip,
        )
        if mgr.available_count == 0:
            logger.warning("No proxies in file, running direct")
            return None
        logger.info("File proxy pool: %d proxies available", mgr.available_count)
        return mgr

    logger.error("Unknown proxy mode: %s (available: %s)", mode, ", ".join(PROXY_MODES))
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Ankama account creator")
    parser.add_argument("--count", type=int, default=1, help="Number of accounts to create")
    parser.add_argument("--config", default="config.json", help="Config file path")
    parser.add_argument("--headful", action="store_true", help="Run browser in visible mode")
    parser.add_argument("--captcha-backend", choices=CAPTCHA_BACKENDS, help="CAPTCHA solver backend")
    parser.add_argument("--captcha-key", help="API key for CAPTCHA solver")
    parser.add_argument("--proxy-mode", choices=PROXY_MODES, help="Proxy mode: direct, file, or free")
    args = parser.parse_args()

    config = load_config(args.config)

    captcha_solver = build_captcha_solver(config, args)
    proxy_mgr = build_proxy_manager(config, args)

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
