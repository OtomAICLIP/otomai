"""Proxy rotation manager with support for file-based lists, free proxy scraping, and direct mode."""

import itertools
import logging
import re
from dataclasses import dataclass, field
from pathlib import Path

import requests

logger = logging.getLogger(__name__)

FREE_PROXY_SOURCES = [
    "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
    "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/socks5.txt",
    "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
]


@dataclass
class Proxy:
    host: str
    port: int
    username: str = ""
    password: str = ""
    protocol: str = "http"
    accounts_created: int = 0

    @property
    def url(self) -> str:
        if self.username:
            return f"{self.protocol}://{self.username}:{self.password}@{self.host}:{self.port}"
        return f"{self.protocol}://{self.host}:{self.port}"

    @property
    def playwright_config(self) -> dict:
        config = {"server": f"{self.protocol}://{self.host}:{self.port}"}
        if self.username:
            config["username"] = self.username
            config["password"] = self.password
        return config


@dataclass
class ProxyManager:
    proxies: list[Proxy] = field(default_factory=list)
    max_per_ip: int = 4
    _cycle: itertools.cycle | None = field(default=None, repr=False)

    @classmethod
    def from_file(cls, path: str, fmt: str = "host:port:user:pass", max_per_ip: int = 4) -> "ProxyManager":
        proxies = []
        proxy_file = Path(path)
        if not proxy_file.exists():
            logger.warning("Proxy file %s not found, running without proxies", path)
            return cls(max_per_ip=max_per_ip)

        for line in proxy_file.read_text().strip().splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split(":")
            if fmt == "host:port:user:pass" and len(parts) >= 4:
                proxies.append(Proxy(host=parts[0], port=int(parts[1]), username=parts[2], password=parts[3]))
            elif len(parts) >= 2:
                proxies.append(Proxy(host=parts[0], port=int(parts[1])))

        logger.info("Loaded %d proxies from %s", len(proxies), path)
        return cls(proxies=proxies, max_per_ip=max_per_ip)

    @classmethod
    def from_free_lists(cls, max_per_ip: int = 4, test_timeout: int = 5, max_proxies: int = 20) -> "ProxyManager":
        """Scrape free proxy lists and test connectivity. Best-effort — free proxies are unreliable."""
        proxies = []
        seen = set()

        for source_url in FREE_PROXY_SOURCES:
            try:
                resp = requests.get(source_url, timeout=10)
                resp.raise_for_status()
                for line in resp.text.strip().splitlines():
                    line = line.strip()
                    match = re.match(r"^(\d+\.\d+\.\d+\.\d+):(\d+)$", line)
                    if match and line not in seen:
                        seen.add(line)
                        proxies.append(Proxy(host=match.group(1), port=int(match.group(2)), protocol="socks5"))
            except Exception as e:
                logger.debug("Failed to fetch %s: %s", source_url, e)

        logger.info("Scraped %d raw proxies from free lists, testing connectivity...", len(proxies))

        # Quick connectivity test
        working = []
        for proxy in proxies[:100]:  # Test at most 100
            try:
                requests.get(
                    "https://httpbin.org/ip",
                    proxies={"https": proxy.url},
                    timeout=test_timeout,
                )
                working.append(proxy)
                if len(working) >= max_proxies:
                    break
            except Exception:
                continue

        logger.info("Found %d working free proxies", len(working))
        return cls(proxies=working, max_per_ip=max_per_ip)

    def next(self) -> Proxy | None:
        if not self.proxies:
            return None
        if self._cycle is None:
            self._cycle = itertools.cycle(self.proxies)

        for _ in range(len(self.proxies)):
            proxy = next(self._cycle)
            if proxy.accounts_created < self.max_per_ip:
                return proxy

        logger.warning("All proxies exhausted (max %d accounts each)", self.max_per_ip)
        return None

    def mark_used(self, proxy: Proxy) -> None:
        proxy.accounts_created += 1
        logger.info("Proxy %s:%d used %d/%d times", proxy.host, proxy.port, proxy.accounts_created, self.max_per_ip)

    @property
    def available_count(self) -> int:
        return sum(1 for p in self.proxies if p.accounts_created < self.max_per_ip)
