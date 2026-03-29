"""Proxy rotation manager for residential proxy pool."""

import itertools
import logging
from dataclasses import dataclass, field
from pathlib import Path

logger = logging.getLogger(__name__)


@dataclass
class Proxy:
    host: str
    port: int
    username: str = ""
    password: str = ""
    accounts_created: int = 0

    @property
    def url(self) -> str:
        if self.username:
            return f"http://{self.username}:{self.password}@{self.host}:{self.port}"
        return f"http://{self.host}:{self.port}"

    @property
    def playwright_config(self) -> dict:
        config = {"server": f"http://{self.host}:{self.port}"}
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
