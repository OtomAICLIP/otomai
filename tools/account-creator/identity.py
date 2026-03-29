"""Generate realistic fake identities for account registration."""

import random
import secrets
import string
from dataclasses import dataclass

from faker import Faker

fake = Faker("en_US")


@dataclass
class Identity:
    email: str
    password: str
    first_name: str
    last_name: str
    birthday_day: int
    birthday_month: int
    birthday_year: int

    @property
    def birthday_str(self) -> str:
        return f"{self.birthday_year}-{self.birthday_month:02d}-{self.birthday_day:02d}"


def generate_password(length: int = 16) -> str:
    alphabet = string.ascii_letters + string.digits + "!@#$%"
    while True:
        pw = "".join(secrets.choice(alphabet) for _ in range(length))
        if (any(c.islower() for c in pw) and any(c.isupper() for c in pw)
                and any(c.isdigit() for c in pw) and any(c in "!@#$%" for c in pw)):
            return pw


def generate_identity(email_prefix: str = "otomai.gen", email_domain: str = "protonmail.com", password_length: int = 16) -> Identity:
    tag = "".join(random.choices(string.ascii_lowercase + string.digits, k=8))
    email = f"{email_prefix}+{tag}@{email_domain}"

    year = random.randint(1985, 2003)
    month = random.randint(1, 12)
    day = random.randint(1, 28)

    return Identity(
        email=email,
        password=generate_password(password_length),
        first_name=fake.first_name(),
        last_name=fake.last_name(),
        birthday_day=day,
        birthday_month=month,
        birthday_year=year,
    )
