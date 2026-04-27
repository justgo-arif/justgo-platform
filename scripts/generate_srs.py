"""Generate JustGo SRS and endpoint traceability matrix.

The generator discovers ASP.NET Core controllers under src/Modules, builds a
deterministic endpoint inventory, maps legacy endpoint-shaped FRs to grouped
business capability FRs, and emits:

* docs/justgo-platform-software-requirements-specification.md
* docs/srs-endpoint-traceability-matrix.md
"""

from __future__ import annotations

import argparse
import datetime as dt
import re
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from pathlib import Path


HTTP_VERBS = ("Get", "Post", "Put", "Delete", "Patch")

MODULE_DESCRIPTIONS = {
    "AssetManagementModule": "asset registers, asset categories, licenses, transfers, inspections, workflow status, and operational asset administration",
    "AuthModule": "identity, login, tenant resolution, authorization, MFA, lookup data, shared notes, attachments, and user/security administration",
    "BookingModule": "class, course, session, attendee, occurrence, eligibility, attendance, and profile booking operations",
    "CredentialModule": "member credentials, credential templates, issuance, approvals, and credential-related member data",
    "FieldManagementModule": "custom extension fields, field sets, schemas, entity-specific metadata, and configurable data capture",
    "FinanceModule": "payments, balances, subscriptions, installments, payment accounts, products, refunds, and finance grid views",
    "MemberProfileModule": "member profile, family, emergency contacts, preferences, notes, and member self-service profile workflows",
    "MembershipModule": "membership plans, licenses, family membership information, member entitlements, and downloadable membership artifacts",
    "MobileAppsModule": "mobile-focused experiences for clubs, events, classes, content, settings, MFA, bookings, and attendance",
    "OrganisationModule": "clubs, organisation hierarchy, member organisation relationships, join/leave/transfer workflows, and primary club management",
    "ResultModule": "sports results, event results, competition data, rankings, result uploads, validation, and result-file lifecycle management",
}

MODULE_TITLES = {
    "AssetManagementModule": "Asset Management",
    "AuthModule": "User Management and Authentication",
    "BookingModule": "Booking Management",
    "CredentialModule": "Credential Management",
    "FieldManagementModule": "Field and Preference Management",
    "FinanceModule": "Finance and Payment Management",
    "MemberProfileModule": "Member Profile Management",
    "MembershipModule": "Membership Management",
    "MobileAppsModule": "Mobile Application Services",
    "OrganisationModule": "Organisation and Club Management",
    "ResultModule": "Results and Competition Management",
}

MODULE_ORDER = [
    "AuthModule",
    "MemberProfileModule",
    "OrganisationModule",
    "MembershipModule",
    "BookingModule",
    "AssetManagementModule",
    "CredentialModule",
    "FieldManagementModule",
    "FinanceModule",
    "MobileAppsModule",
    "ResultModule",
]

MODULE_OUTCOMES = {
    "AssetManagementModule": "asset owners and administrators can maintain controlled, auditable asset operations",
    "AuthModule": "users, administrators, and tenants can access the platform securely and consistently",
    "BookingModule": "members, coaches, and administrators can manage participation in classes, courses, events, and sessions",
    "CredentialModule": "members and administrators can manage eligibility evidence and credential lifecycle activities",
    "FieldManagementModule": "administrators can configure tenant-specific data capture without code changes",
    "FinanceModule": "organisations can manage payments, balances, recurring plans, products, and payment accounts",
    "MemberProfileModule": "members and support users can maintain accurate personal, family, contact, and preference information",
    "MembershipModule": "members and organisations can understand entitlements, memberships, licences, and related downloadable records",
    "MobileAppsModule": "mobile users can complete operational club, event, class, attendance, and security workflows",
    "OrganisationModule": "members and administrators can manage club relationships, hierarchy, and transfer activities",
    "ResultModule": "sports administrators and members can manage result data, rankings, competitions, and validation workflows",
}

CONTROLLER_TITLES = {
    "AbacAuthorizes": "Authorization",
    "MultiFactorAuth": "Multi-Factor Authentication",
    "TwoFactorAuths": "Two-Factor Authentication",
    "UiPermissions": "User Interface Permissions",
}

TECHNICAL_CONTROLLERS = {"CacheInvalidation"}
TECHNICAL_ROUTE_WORDS = {"hash", "encrypt", "decrypt", "cache", "token-claims", "refresh-token"}


@dataclass
class Endpoint:
    module: str
    controller: str
    source: Path
    line: int
    api_versions: list[str]
    route_prefix: str
    verb: str
    route: str
    action: str
    summary: str
    auth: str
    request_type: str = ""
    response_type: str = ""
    mediator_requests: list[str] = field(default_factory=list)
    legacy_fr_id: str = ""
    legacy_fr_text: str = ""
    trace_id: str = ""
    capability_id: str = ""
    review_status: str = ""

    @property
    def full_route(self) -> str:
        prefix = self.route_prefix.strip("/")
        suffix = self.route.strip("/")
        path = "/".join(part for part in (prefix, suffix) if part)
        return "/" + path.replace("//", "/")

    @property
    def signal(self) -> str:
        return split_words(
            " ".join(
                [
                    self.controller,
                    self.action,
                    self.route.replace("/", " "),
                    " ".join(self.mediator_requests),
                    self.request_type,
                ]
            )
        )


@dataclass(frozen=True)
class CapabilityKey:
    module: str
    feature: str
    category: str


@dataclass
class Capability:
    key: CapabilityKey
    id: str
    title: str
    requirement: str
    endpoints: list[Endpoint] = field(default_factory=list)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def clean_attr_value(value: str) -> str:
    value = value.strip()
    if value.startswith("@"):
        value = value[1:]
    match = re.search(r'"([^"]*)"', value)
    return match.group(1) if match else ""


def discover_endpoints(root: Path) -> list[Endpoint]:
    endpoints: list[Endpoint] = []
    for controller in sorted((root / "src" / "Modules").glob("**/*Controller.cs")):
        endpoints.extend(parse_controller_file(controller, root))
    endpoints = sorted(endpoints, key=stable_endpoint_sort_key)
    for index, endpoint in enumerate(endpoints, 1):
        endpoint.trace_id = f"EP-{index:03d}"
        endpoint.legacy_fr_id = f"FR-{index:02d}"
        endpoint.legacy_fr_text = requirement_detail(endpoint)
    return endpoints


def stable_endpoint_sort_key(endpoint: Endpoint) -> tuple[int, str, str, str, str, str]:
    module_rank = {name: index for index, name in enumerate(MODULE_ORDER)}
    return (
        module_rank.get(endpoint.module, len(module_rank)),
        endpoint.controller,
        endpoint.full_route,
        endpoint.verb,
        endpoint.action,
        endpoint.source.as_posix(),
    )


def parse_controller_file(path: Path, root: Path) -> list[Endpoint]:
    text = read_text(path)
    module = next((part for part in path.parts if part.endswith("Module")), "UnknownModule")
    controller_match = re.search(r"public\s+class\s+(\w+Controller)\s*:", text)
    if not controller_match:
        return []

    controller = controller_match.group(1).replace("Controller", "")
    before_class = text[: controller_match.start()]
    class_attrs = re.findall(r"^\s*\[([^\]]+)\]", before_class, re.MULTILINE)
    api_versions = [clean_attr_value(a) for a in class_attrs if a.startswith("ApiVersion")]
    route_prefix = next((clean_attr_value(a) for a in class_attrs if a.startswith("Route")), "")
    if not route_prefix:
        route_prefix = f"api/v{{version:apiVersion}}/{controller.lower()}"

    endpoints: list[Endpoint] = []
    method_pattern = re.compile(
        r"(?P<attrs>(?:\s*\[[^\]]+\]\s*)+)"
        r"\s*public\s+(?:async\s+)?(?P<return>[\w<>,\s\.\?]+)\s+"
        r"(?P<name>\w+)\s*\((?P<params>[^)]*)\)",
        re.MULTILINE,
    )

    for match in method_pattern.finditer(text):
        attrs = re.findall(r"\[([^\]]+)\]", match.group("attrs"))
        http_attr = next((a for a in attrs if a.startswith(tuple(f"Http{v}" for v in HTTP_VERBS))), None)
        if not http_attr:
            continue
        verb_match = re.match(r"Http(Get|Post|Put|Delete|Patch)", http_attr)
        verb = verb_match.group(1).upper() if verb_match else "GET"
        route = clean_attr_value(http_attr)
        action = match.group("name")
        method_start = match.end()
        next_method = method_pattern.search(text, method_start)
        body = text[method_start : next_method.start() if next_method else len(text)]
        response_type = parse_response_type(attrs)
        request_type = parse_request_type(match.group("params"), body)
        mediator_requests = sorted(set(re.findall(r"new\s+(\w+(?:Query|Command))\b", body)))
        auth = parse_auth(attrs, class_attrs)
        line = text[: match.start()].count("\n") + 1
        endpoints.append(
            Endpoint(
                module=module,
                controller=controller,
                source=path.relative_to(root),
                line=line,
                api_versions=api_versions or ["unspecified"],
                route_prefix=route_prefix,
                verb=verb,
                route=route,
                action=action,
                summary=business_summary(controller, action, verb, route, mediator_requests),
                auth=auth,
                request_type=request_type,
                response_type=response_type,
                mediator_requests=mediator_requests,
            )
        )
    return endpoints


def parse_response_type(attrs: list[str]) -> str:
    for attr in attrs:
        if not attr.startswith("ProducesResponseType"):
            continue
        match = re.search(r"typeof\(([^)]+)\)", attr)
        if match:
            return re.sub(r"\s+", " ", match.group(1)).strip()
    return ""


def parse_request_type(params: str, body: str) -> str:
    candidates = []
    for part in params.split(","):
        part = part.strip()
        if not part or "CancellationToken" in part:
            continue
        if any(marker in part for marker in ("[FromBody]", "[FromQuery]", "[FromRoute]", "[FromForm]")):
            cleaned = re.sub(r"\[[^\]]+\]\s*", "", part).strip()
            type_name = cleaned.split()[0] if cleaned.split() else ""
            if type_name and type_name not in {"string", "int", "Guid", "long", "bool"}:
                candidates.append(type_name)
    body_match = re.search(r"_mediator\.Send\((\w+)", body)
    if body_match:
        candidates.append(body_match.group(1))
    return next((c for c in candidates if c), "")


def parse_auth(method_attrs: list[str], class_attrs: list[str]) -> str:
    all_attrs = method_attrs + class_attrs
    if any(a.startswith("AllowAnonymous") for a in all_attrs):
        return "Anonymous"
    custom = next((a for a in all_attrs if a.startswith("CustomAuthorize")), "")
    if custom:
        quoted = re.findall(r'"([^"]+)"', custom)
        return "ABAC/JWT" + (f" ({', '.join(quoted)})" if quoted else "")
    if any(a.startswith("Authorize") for a in all_attrs):
        return "JWT"
    return "Not declared"


def split_words(name: str) -> str:
    name = re.sub(r"Controller$", "", name)
    name = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", name)
    name = name.replace("_", " ").replace("-", " ")
    return re.sub(r"\s+", " ", name).strip().lower()


def table_escape(value: object) -> str:
    return str(value or "").replace("|", "\\|").replace("\n", " ").strip()


def title_case(value: str) -> str:
    if value in CONTROLLER_TITLES:
        return CONTROLLER_TITLES[value]
    return " ".join(word.capitalize() for word in split_words(value).split())


def business_feature_name(value: str) -> str:
    return CONTROLLER_TITLES.get(value, split_words(value)).lower()


def module_title(module: str) -> str:
    return MODULE_TITLES.get(module, module.replace("Module", ""))


def ordered_modules(modules: list[str]) -> list[str]:
    order = {name: index for index, name in enumerate(MODULE_ORDER)}
    return sorted(modules, key=lambda name: (order.get(name, len(order)), name))


def business_summary(controller: str, action: str, verb: str, route: str, mediator_requests: list[str]) -> str:
    subject = requirement_subject(controller, action, route, mediator_requests)
    operation = requirement_operation(verb, action, route, mediator_requests)
    return f"The system SHALL allow authorized users to {operation} {subject}."


def requirement_subject(controller: str, action: str, route: str, mediator_requests: list[str]) -> str:
    signal = split_words(" ".join([controller, action, route.replace("/", " "), " ".join(mediator_requests)]))
    subject = split_words(controller)
    if controller == "Accounts":
        subject = "user accounts"
    elif controller == "CacheInvalidation":
        subject = "cached platform data"

    replacements = {
        "mfa": "multi-factor authentication",
        "multi factor auth": "multi-factor authentication",
        "two factor auths": "two-factor authentication",
        "ui permissions": "user interface permissions",
        "qr": "QR code",
        "otp": "one-time password",
    }
    for needle, replacement in replacements.items():
        if needle in signal:
            subject = replacement

    if "abac authorizes" in subject:
        subject = "authorization"
    if subject == "ui permissions":
        subject = "user interface permissions"

    if any(word in signal for word in ("change status", "status completed", "status archive", "status unarchive", "approve", "cancel")):
        subject = f"{subject} status"
    elif "attendance" in signal:
        subject = f"{subject} attendance"
    elif "booking" in signal and "booking" not in subject:
        subject = f"{subject} booking"
    elif "permission" in signal:
        subject = f"{subject} permissions"
    elif "metadata" in signal:
        subject = f"{subject} reference data"
    elif "history" in signal:
        subject = f"{subject} history"
    elif "note" in signal and "note" not in subject:
        subject = f"{subject} notes"
    elif "image" in signal or "photo" in signal:
        subject = f"{subject} media"
    elif "family" in signal and "family" not in subject:
        subject = f"{subject} family information"

    subject = re.sub(r"\bpermissions permissions\b", "permissions", subject)
    subject = re.sub(r"\breference data reference data\b", "reference data", subject)
    return re.sub(r"\s+", " ", subject).strip()


def requirement_operation(verb: str, action: str, route: str, mediator_requests: list[str]) -> str:
    signal = split_words(" ".join([action, route.replace("/", " "), " ".join(mediator_requests)]))
    if any(word in signal for word in ("authenticate", "refresh token", "token claims")):
        return "authenticate and manage secure access for"
    if "password" in signal:
        return "manage password recovery and password changes for"
    if any(word in signal for word in ("hash", "encrypt", "decrypt")):
        return "protect sensitive text values for"
    if "invalidate" in signal:
        return "invalidate"
    if any(word in signal for word in ("permission", "authorize", "authorization", "abac")):
        return "evaluate and manage"
    if any(word in signal for word in ("approve", "cancel", "archive", "unarchive", "enable", "disable", "set primary", "change status", "status completed", "status archive", "status unarchive")):
        return "change the status of"
    if any(word in signal for word in ("search", "find", "filter")):
        return "search, filter, and retrieve"
    if any(word in signal for word in ("list", "all", "grid", "dashboard")):
        return "view and review"
    if any(word in signal for word in ("details", "detail", "summary", "profile", "metadata", "lookup", "status", "count")):
        return "view"
    if any(word in signal for word in ("validate", "eligibility", "eligible", "verify", "verification")):
        return "validate"
    if any(word in signal for word in ("export", "download")):
        return "download or export"
    if any(word in signal for word in ("upload", "import")):
        return "upload and submit"
    if verb == "DELETE":
        return "remove"
    if verb == "PATCH":
        return "change selected information for"
    if verb == "PUT":
        return "update"
    if verb == "POST":
        return "create, submit, or process"
    return "view"


def requirement_detail(endpoint: Endpoint) -> str:
    base = endpoint.summary
    outcome = MODULE_OUTCOMES.get(endpoint.module, "the related business process remains consistent and auditable")
    signal = endpoint.signal

    if "invalidate" in signal:
        detail = f" The system SHALL remove stale cached information according to the requested scope so that {outcome}."
    elif any(word in signal for word in ("list", "search", "filter", "grid")):
        detail = f" The system SHALL support criteria-based retrieval where applicable so that {outcome}."
    elif any(word in signal for word in ("create", "add", "save", "submit", "upload", "import")):
        detail = f" The system SHALL capture the required business data, validate it, and make it available to downstream workflows so that {outcome}."
    elif any(word in signal for word in ("edit", "update", "change", "status", "approve", "cancel", "archive")):
        detail = f" The system SHALL preserve the integrity and traceability of the change so that {outcome}."
    elif any(word in signal for word in ("delete", "remove")):
        detail = f" The system SHALL apply the removal according to authorization and business rules so that {outcome}."
    elif any(word in signal for word in ("validate", "eligibility", "verify")):
        detail = f" The system SHALL return a clear eligibility or validation outcome so that {outcome}."
    else:
        detail = f" The system SHALL present the information in a usable form so that {outcome}."
    return base + detail


def capability_key(endpoint: Endpoint) -> CapabilityKey:
    signal = endpoint.signal
    controller = endpoint.controller
    category = "core"

    if endpoint.module == "ResultModule" and controller == "UploadResult":
        category = "result-upload-lifecycle"
    elif controller in TECHNICAL_CONTROLLERS or any(word in signal for word in TECHNICAL_ROUTE_WORDS):
        category = "technical-utility"
    elif endpoint.module == "AuthModule" and controller == "Accounts":
        if "password" in signal:
            category = "password-recovery"
        elif any(word in signal for word in ("hash", "encrypt", "decrypt")):
            category = "sensitive-text-utility"
        else:
            category = "authentication-session"
    elif "permission" in signal or "authorize" in signal or "abac" in signal:
        category = "permissions"
    elif "mfa" in signal or "multi factor" in signal or "two factor" in signal or "otp" in signal or "one time password" in signal:
        category = "mfa"
    elif "upload" in signal or "import" in signal or "file" in signal:
        category = "file-import"
    elif "download" in signal or "export" in signal:
        category = "download-export"
    elif endpoint.module == "FinanceModule" and any(word in signal for word in ("refund", "refundable")):
        category = "refunds"
    elif endpoint.module == "FinanceModule" and any(word in signal for word in ("receipt", "overview", "details", "summary", "log", "method", "terminal", "payment")):
        category = "payment-review"
    elif endpoint.module == "FinanceModule" and any(word in signal for word in ("product", "plan", "subscription", "installment", "balance")):
        category = "finance-products-plans"
    elif "attendance" in signal:
        category = "attendance"
    elif "note" in signal:
        category = "notes"
    elif "booking" in signal:
        category = "booking"
    elif any(word in signal for word in ("eligibility", "eligible", "validate", "verify", "duplicate", "check")):
        category = "validation"
    elif any(word in signal for word in ("metadata", "lookup", "status", "type", "category", "configuration", "dropdown", "field")):
        category = "reference-data"
    elif any(word in signal for word in ("list", "search", "find", "filter", "grid")):
        category = "search-list"
    elif any(word in signal for word in ("create", "add", "save", "submit")):
        category = "create-submit"
    elif any(word in signal for word in ("edit", "update", "change", "set primary", "approve", "cancel", "archive", "unarchive", "delete", "remove")):
        category = "lifecycle"
    elif any(word in signal for word in ("history", "audit", "activity")):
        category = "history-audit"
    elif any(word in signal for word in ("details", "profile", "summary")):
        category = "detail-view"

    if endpoint.module == "MobileAppsModule":
        if controller in {"MultiFactorAuth", "TwoFactorAuths"}:
            category = "mobile-mfa"
        elif controller == "Classes" and category == "core":
            category = "class-session-review"
    if endpoint.module == "FinanceModule" and controller == "Payments" and category == "core":
        category = "payment-review"
    if endpoint.module == "AssetManagementModule" and controller in {"AssetLeases", "AssetLicenses", "AssetOwnershipTransfers", "AssetRegisters"}:
        if category == "core":
            category = "asset-lifecycle"

    return CapabilityKey(endpoint.module, title_case(controller), category)


def build_capabilities(endpoints: list[Endpoint]) -> list[Capability]:
    grouped: dict[CapabilityKey, list[Endpoint]] = defaultdict(list)
    for endpoint in endpoints:
        grouped[capability_key(endpoint)].append(endpoint)

    capabilities: list[Capability] = []
    module_rank = {name: index for index, name in enumerate(MODULE_ORDER)}
    keys = sorted(grouped, key=lambda k: (module_rank.get(k.module, len(module_rank)), k.feature, k.category))
    for index, key in enumerate(keys, 1):
        cap_id = f"FR-{index:03d}"
        capability = Capability(
            key=key,
            id=cap_id,
            title=capability_title(key),
            requirement=capability_requirement(key),
            endpoints=sorted(grouped[key], key=stable_endpoint_sort_key),
        )
        for endpoint in capability.endpoints:
            endpoint.capability_id = cap_id
            endpoint.review_status = review_status(endpoint, capability)
        capabilities.append(capability)
    return capabilities


def capability_title(key: CapabilityKey) -> str:
    subject = key.feature
    labels = {
        "authentication-session": "Authenticate Users and Manage Sessions",
        "password-recovery": "Manage Account Password Recovery",
        "sensitive-text-utility": "Protect Sensitive Text Utilities",
        "technical-utility": f"Operate {subject} Technical Utilities",
        "permissions": f"Evaluate {subject} Permissions",
        "mfa": f"Manage {subject}",
        "mobile-mfa": f"Manage Mobile {subject}",
        "file-import": f"Upload and Process {subject}",
        "download-export": f"Download and Export {subject}",
        "refunds": "Manage Refunds",
        "payment-review": "Review Payment Activity",
        "finance-products-plans": f"Manage {subject} Products and Plans",
        "attendance": f"Manage {subject} Attendance",
        "notes": f"Manage {subject} Notes",
        "booking": f"Manage {subject} Booking",
        "validation": f"Validate {subject}",
        "reference-data": f"Provide {subject} Reference Data",
        "search-list": f"Search and List {subject}",
        "create-submit": f"Create or Submit {subject}",
        "lifecycle": f"Manage {subject} Lifecycle",
        "history-audit": f"Review {subject} History and Audit",
        "detail-view": f"Review {subject} Details",
        "class-session-review": "Review Mobile Class Sessions",
        "result-upload-lifecycle": "Manage Result Upload Lifecycle",
        "asset-lifecycle": f"Manage {subject} Lifecycle",
        "core": f"Manage {subject}",
    }
    return labels.get(key.category, f"Manage {subject}")


def capability_requirement(key: CapabilityKey) -> str:
    outcome = MODULE_OUTCOMES.get(key.module, "the related business process remains consistent and auditable")
    subject = subject_phrase(key)
    category = key.category

    templates = {
        "authentication-session": "The system SHALL authenticate users, issue or refresh access tokens, and provide token-related session information so that {outcome}.",
        "password-recovery": "The system SHALL support password recovery and password change workflows so that {outcome}.",
        "sensitive-text-utility": "The system SHALL provide controlled hashing, encryption, verification, and decryption utilities for sensitive text values so that {outcome}.",
        "technical-utility": "The system SHALL provide controlled technical utility operations for {subject} so that {outcome}.",
        "permissions": "The system SHALL evaluate authorization and user-interface permissions for requested policies, actions, resources, and fields so that {outcome}.",
        "mfa": "The system SHALL support multi-factor authentication enrollment, verification, one-time-password handling, and status management so that {outcome}.",
        "mobile-mfa": "The system SHALL support mobile multi-factor and two-factor authentication setup, verification, one-time-password handling, and status management so that {outcome}.",
        "file-import": "The system SHALL allow authorized users to upload, preview, process, and manage files used by downstream workflows so that {outcome}.",
        "download-export": "The system SHALL allow authorized users to download or export {subject} information in a usable form so that {outcome}.",
        "refunds": "The system SHALL allow authorized finance users to review refundable items, capture refund reasons, create refunds, and review refund history so that {outcome}.",
        "payment-review": "The system SHALL allow authorized finance users to review payment receipts, payment details, payment methods, refund eligibility, terminals, logs, and payment history so that {outcome}.",
        "finance-products-plans": "The system SHALL allow authorized finance users to review and manage products, balances, plans, subscriptions, installments, and billing history so that {outcome}.",
        "attendance": "The system SHALL allow authorized users to review attendees and update attendance outcomes for {subject} so that {outcome}.",
        "notes": "The system SHALL allow authorized users to create, review, update, and remove notes for {subject} so that {outcome}.",
        "booking": "The system SHALL allow authorized users to review, validate, and manage booking-related information for {subject} so that {outcome}.",
        "validation": "The system SHALL validate {subject} eligibility, rules, duplicates, or requested state changes and return clear outcomes so that {outcome}.",
        "reference-data": "The system SHALL provide {subject} lookup, metadata, status, field, and configuration data required by client workflows so that {outcome}.",
        "search-list": "The system SHALL allow authorized users to search, filter, page, and review {subject} records so that {outcome}.",
        "create-submit": "The system SHALL allow authorized users to create, submit, or process {subject} records while preserving required business data so that {outcome}.",
        "lifecycle": "The system SHALL allow authorized users to update, remove, approve, cancel, archive, or otherwise change {subject} lifecycle state so that {outcome}.",
        "history-audit": "The system SHALL allow authorized users to review {subject} history, activity, and audit information so that {outcome}.",
        "detail-view": "The system SHALL allow authorized users to review detailed {subject} information so that {outcome}.",
        "class-session-review": "The system SHALL allow mobile users to review class lists, sessions, occurrences, tickets, member details, booking details, eligibility, and payment-related class context so that {outcome}.",
        "result-upload-lifecycle": "The system SHALL allow authorized users to upload result files, preview data, confirm mappings, validate member data, monitor import status, and manage uploaded result records so that {outcome}.",
        "asset-lifecycle": "The system SHALL allow authorized users to create, review, update, submit, approve, cancel, transfer, reinstate, or remove {subject} records so that {outcome}.",
        "core": "The system SHALL support {subject} workflows so that {outcome}.",
    }
    return templates.get(category, templates["core"]).format(subject=subject, outcome=outcome)


def subject_phrase(key: CapabilityKey) -> str:
    subject = key.feature.lower()
    if key.category == "reference-data" and subject in {"lookup", "asset metadata"}:
        return "reference"
    if key.category == "notes" and subject == "notes":
        return "shared notes"
    if key.category == "notes" and subject.endswith("notes"):
        return subject
    return subject


def review_status(endpoint: Endpoint, capability: Capability) -> str:
    legacy = endpoint.legacy_fr_text.lower()
    signal = endpoint.signal
    operation = requirement_operation(endpoint.verb, endpoint.action, endpoint.route, endpoint.mediator_requests)

    if capability.key.category == "technical-utility":
        return "Technical Utility"
    if capability.key.category in {"search-list", "reference-data", "payment-review", "class-session-review", "result-upload-lifecycle"}:
        return "Should Be Grouped"
    if endpoint.verb in {"POST", "PUT", "DELETE", "PATCH"} and operation in {"view", "view and review"}:
        return "Wrong Operation"
    if any(phrase in legacy for phrase in ("view payments", "view classes", "view events", "view upload result", "view booking class")):
        return "Vague"
    if len(capability.endpoints) > 1:
        return "Duplicate"
    return "Accurate"


def generate_trace_matrix(root: Path, output: Path, endpoints: list[Endpoint], capabilities: list[Capability]) -> None:
    by_id = {cap.id: cap for cap in capabilities}
    lines = [
        "# SRS Endpoint Traceability Matrix",
        "",
        f"Generated: {dt.date.today().isoformat()}",
        "",
        "This matrix preserves endpoint-level evidence for the grouped business requirements in the SRS. The `Current FR` columns show the legacy endpoint-shaped requirement generated from the same deterministic endpoint order.",
        "",
        f"- Endpoint count: {len(endpoints)}",
        f"- Grouped capability FR count: {len(capabilities)}",
        "",
        "| Trace ID | Module | Controller/Feature | HTTP Method | Route | Action | Request/Command/Query | Response Type | Auth | Current FR ID | Current FR Text | Review Status | Suggested Capability FR |",
        "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for endpoint in endpoints:
        capability = by_id[endpoint.capability_id]
        lines.append(
            "| "
            + " | ".join(
                table_escape(value)
                for value in [
                    endpoint.trace_id,
                    module_title(endpoint.module),
                    capability.key.feature,
                    endpoint.verb,
                    endpoint.full_route,
                    endpoint.action,
                    endpoint.request_type or ", ".join(endpoint.mediator_requests),
                    endpoint.response_type,
                    endpoint.auth,
                    endpoint.legacy_fr_id,
                    endpoint.legacy_fr_text,
                    endpoint.review_status,
                    f"{capability.id}: {capability.requirement}",
                ]
            )
            + " |"
        )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")


def generate_srs(root: Path, output: Path, trace_output: Path, pdf_path: Path) -> None:
    endpoints = discover_endpoints(root)
    capabilities = build_capabilities(endpoints)
    generate_trace_matrix(root, trace_output, endpoints, capabilities)

    by_module: dict[str, list[Capability]] = defaultdict(list)
    for capability in capabilities:
        by_module[capability.key.module].append(capability)

    lines = generate_document_intro(root, pdf_path, endpoints, capabilities)
    lines.extend(generate_functional_requirements(by_module))
    lines.extend(generate_interface_and_nonfunctional_sections())
    lines.extend(generate_traceability_appendix(capabilities))

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")


def generate_document_intro(root: Path, pdf_path: Path, endpoints: list[Endpoint], capabilities: list[Capability]) -> list[str]:
    now = dt.date.today().isoformat()
    controller_count = len({(e.module, e.controller, e.source) for e in endpoints})
    version_count = Counter(v for e in endpoints for v in e.api_versions)
    version_text = ", ".join(f"v{version}: {count}" for version, count in sorted(version_count.items()))
    pdf_ref = pdf_path.relative_to(root) if pdf_path.is_relative_to(root) else pdf_path

    return [
        "# Software Requirements Specification (SRS)",
        "",
        "# JustGo Platform",
        "",
        "## Document Control",
        "",
        "### Document Information",
        "",
        "| Field | Value |",
        "| --- | --- |",
        "| Title | Software Requirements Specification for JustGo Platform |",
        f"| Date | {now} |",
        "| Status | Draft |",
        "| Version | 1.1 |",
        "| Prepared for | JustGo Technologies Limited |",
        "| Prepared by | Software Architecture Review |",
        f"| Reference | `{pdf_ref}` |",
        "",
        "Disclaimer:",
        "",
        "This document is prepared from source-code observation and architectural review for internal analysis and stakeholder validation. It is not a signed product baseline until reviewed and approved by JustGo business owners, implementation leads, operations, security, and support representatives.",
        "",
        "## Table of Contents",
        "",
        "1. Introduction",
        "2. Overall Description",
        "3. System Features and Functional Requirements",
        "4. External Interface Requirements",
        "5. Non-Functional Requirements",
        "6. Other Requirements",
        "7. Endpoint Traceability Appendix",
        "",
        "## Glossary",
        "",
        "| Term | Definition |",
        "| --- | --- |",
        "| ABAC | Attribute-Based Access Control used to evaluate permissions from user, action, resource, and contextual attributes. |",
        "| API | Application Programming Interface. |",
        "| Club | A sports club or operating unit managed through JustGo. |",
        "| CQRS | Command Query Responsibility Segregation, used by the platform to separate read and write use cases. |",
        "| Endpoint Trace | A technical mapping row that links an observed API endpoint to a grouped business requirement. |",
        "| JWT | JSON Web Token used for authenticated access. |",
        "| Member | A person who participates in memberships, bookings, credentials, profiles, or results workflows. |",
        "| MFA | Multi-Factor Authentication. |",
        "| SRS | Software Requirements Specification. |",
        "| Tenant | A customer organisation whose data and configuration are isolated from other organisations. |",
        "",
        "## 1. Introduction",
        "",
        "### 1.1. Purpose",
        "",
        "This Software Requirements Specification (SRS) document provides a structured, business-readable description of the JustGo Platform, a multi-tenant SaaS application used by sports organisations to manage members, clubs, bookings, memberships, credentials, assets, finance, mobile workflows, and results.",
        "",
        "The functional requirements in this version are grouped by business capability. Endpoint-level evidence is preserved in a separate traceability matrix and in Section 7 so that product owners can review intent while engineers can verify API coverage.",
        "",
        "### 1.2. Document Conventions",
        "",
        "| Term | Description |",
        "| --- | --- |",
        "| SHALL | Refers to a mandatory requirement that must be fulfilled by the system. |",
        "| SHOULD | Indicates a recommended requirement that should be considered for current or near-future implementation. |",
        "| MAY | Refers to an optional or future requirement that may be considered for subsequent phases. |",
        "| TBD | To Be Determined; indicates information that requires stakeholder confirmation. |",
        "| Note | Provides additional information or clarification. |",
        "",
        "| Requirement Number | Description |",
        "| --- | --- |",
        "| FR-XXX | Grouped functional requirements mapped to one or more observed API endpoints. |",
        "| EP-XXX | Endpoint trace identifiers used in the traceability matrix. |",
        "| NFR-XX | Non-Functional Requirements |",
        "| IR-XX | Interface Requirements |",
        "| DR-XX | Data Requirements |",
        "| SR-XX | Security Requirements |",
        "",
        "### 1.3. Intended Audience",
        "",
        "| Stakeholder | Role |",
        "| --- | --- |",
        "| JustGo Product Owners | Review the business fit and completeness of the stated capabilities. |",
        "| JustGo Engineering Team | Use the requirements as a baseline for design, development, testing, and refactoring decisions. |",
        "| Support and Operations Teams | Understand supported workflows, operational dependencies, and service expectations. |",
        "| Security and Compliance Stakeholders | Review authentication, authorization, auditability, tenant isolation, and data protection expectations. |",
        "| Customer Success and Implementation Teams | Use the document to explain product capabilities and implementation boundaries to customers. |",
        "",
        "### 1.4. Project Scope",
        "",
        "The JustGo Platform provides a modular SaaS backend for sports administration. Its scope includes tenant-aware identity and access control, member profile management, organisation and club management, booking and attendance operations, asset lifecycle activities, credential management, membership and licence visibility, finance and payment workflows, custom field configuration, mobile app support, and sports result management.",
        "",
        "The scope of this SRS is limited to capabilities observable from the backend codebase. Detailed UI behavior, exact database stored procedure behavior, and final business policy wording require validation with product and domain stakeholders.",
        "",
        "### 1.5. References",
        "",
        "| Reference | Description |",
        "| --- | --- |",
        f"| `{pdf_ref}` | Reference SRS format used for this document. |",
        "| Application source code | Backend modules used to infer business capabilities and functional requirements. |",
        "| `docs/srs-endpoint-traceability-matrix.md` | Endpoint-level evidence, legacy endpoint FR text, review status, and grouped capability mapping. |",
        "| `AGENTS.md` | Repository architecture and development guidance for module boundaries and implementation conventions. |",
        "| `README.md` | Platform overview, technology stack, and setup guidance. |",
        "",
        "## 2. Overall Description",
        "",
        "### 2.1. Product Perspective",
        "",
        "JustGo is a cloud-oriented, multi-tenant SaaS backend implemented as a .NET modular monolith. The system hosts multiple business modules behind versioned REST APIs, with shared authentication, authorization, repository, caching, logging, tenant, and file services.",
        "",
        "Each tenant represents a customer organisation. The platform separates central tenant metadata from tenant operational data and uses the active tenant context to route database access appropriately.",
        "",
        "### 2.2. Product Functions",
        "",
        "At a high level, the JustGo Platform SHALL provide the following product functions:",
        "",
        "- Manage user authentication, authorization, tenant access, MFA, permissions, and shared operational lookups.",
        "- Manage member profile, family, emergency contact, note, preference, and media-related information.",
        "- Manage clubs, organisation hierarchy, member-club relationships, join/leave workflows, transfer workflows, and primary club information.",
        "- Manage memberships, licences, member entitlements, downloadable membership records, and membership-related catalogue information.",
        "- Manage class, course, event, occurrence, attendance, eligibility, and booking workflows.",
        "- Manage asset registers, asset credentials, asset licences, leases, ownership transfers, audits, tags, metadata, and status changes.",
        "- Manage payment accounts, products, balances, subscriptions, instalments, finance grids, payment consoles, and refunds.",
        "- Manage custom fields and tenant-specific metadata for extensible business data capture.",
        "- Support mobile app experiences for clubs, events, classes, content, settings, MFA, attendance, notes, and booking operations.",
        "- Manage result uploads, result-file lifecycle, sports results, competitions, events, rankings, player profiles, and validation workflows.",
        "",
        "### 2.3. User Classes, Characteristics, and Needs",
        "",
        "| User Class | Characteristics and Needs |",
        "| --- | --- |",
        "| Members and Guardians | Need self-service access to profile, family, emergency contact, membership, booking, credential, preference, and result information. |",
        "| Club and Organisation Administrators | Need secure tools to manage clubs, members, bookings, attendance, assets, results, finance views, and operational configuration. |",
        "| Coaches and Event Staff | Need mobile-friendly workflows for class and event attendance, attendee lists, notes, occurrence data, and booking validation. |",
        "| Finance Users | Need visibility and control over payment accounts, products, balances, subscriptions, instalments, refunds, and finance grid views. |",
        "| Platform Administrators | Need tenant, identity, MFA, lookup, permission, reference data, and support capabilities across the platform. |",
        "| Implementation and Support Users | Need consistent diagnostics, documentation, audit information, and configuration visibility to support customers. |",
        "",
        "### 2.4. Operating Environment",
        "",
        "The observed implementation targets .NET 9, ASP.NET Core Web API, SQL Server or Azure SQL, Azure hosting patterns, JWT/JWE authentication, ABAC authorization, Serilog logging, Redis or hybrid cache components, Swagger/OpenAPI, Azure Blob based file storage, and external payment-provider integrations.",
        "",
        f"Source-code discovery found {len(endpoints)} endpoints across {controller_count} controller files ({version_text}). These endpoints are mapped to {len(capabilities)} grouped functional requirements.",
        "",
        "### 2.5. Design and Implementation Constraints",
        "",
        "- The platform SHALL preserve module boundaries in the modular monolith architecture.",
        "- Domain projects SHALL remain free of infrastructure and sibling module dependencies.",
        "- Cross-module business coordination SHALL be performed through mediator-style requests rather than direct module coupling.",
        "- Tenant context SHALL be resolved before tenant-specific data access.",
        "- Inline SQL and stored procedure usage SHALL remain parameterized to protect data integrity and security.",
        "- Public API behavior SHALL remain version-aware to support client evolution.",
        "",
        "### 2.6. User Documentation",
        "",
        "The system SHOULD be supported by user documentation for member self-service, club administration, booking operations, mobile attendance workflows, finance operations, asset administration, result management, MFA setup, and tenant onboarding. Technical documentation SHOULD include API usage, deployment, configuration, module boundaries, and troubleshooting guidance.",
        "",
        "### 2.7. Assumptions and Dependencies",
        "",
        "- Requirements reflect implemented backend API capabilities and may not capture all intended roadmap items.",
        "- Business rules inside handlers, database objects, payment-provider configuration, and tenant-specific settings require additional stakeholder validation.",
        "- The platform depends on SQL Server or Azure SQL, configured tenant databases, authentication secrets, external storage, payment services, and hosting infrastructure.",
        "- Endpoint routes, HTTP verbs, controller file paths, and source line numbers are intentionally kept out of Section 3 and preserved in the traceability matrix instead.",
        "",
    ]


def generate_functional_requirements(by_module: dict[str, list[Capability]]) -> list[str]:
    lines = [
        "## 3. System Features and Functional Requirements",
        "",
        "This section describes grouped business capabilities. Each requirement maps to one or more observed backend endpoints; endpoint-level evidence is listed in `docs/srs-endpoint-traceability-matrix.md` and Section 7.",
        "",
    ]
    for feature_index, module in enumerate(ordered_modules(list(by_module)), 1):
        capabilities = sorted(by_module[module], key=lambda c: (c.key.feature, c.key.category, c.id))
        lines.append(f"### 3.{feature_index}. {module_title(module)}")
        lines.append("")
        lines.append(f"Business scope: {MODULE_DESCRIPTIONS.get(module, 'module-specific business capabilities')}.")
        lines.append("")
        grouped: dict[str, list[Capability]] = defaultdict(list)
        for capability in capabilities:
            grouped[capability.key.feature].append(capability)
        for sub_index, feature in enumerate(sorted(grouped), 1):
            lines.append(f"#### 3.{feature_index}.{sub_index}. {feature}")
            lines.append("")
            lines.append(f"The system SHALL support {feature.lower()} workflows within the {module_title(module)} feature area.")
            lines.append("")
            lines.append("| ID | Requirement | Endpoint Trace IDs |")
            lines.append("| --- | --- | --- |")
            for capability in sorted(grouped[feature], key=lambda c: c.id):
                trace_ids = ", ".join(endpoint.trace_id for endpoint in capability.endpoints)
                lines.append(f"| {capability.id}. | {table_escape(capability.requirement)} | {trace_ids} |")
            lines.append("")
    return lines


def generate_interface_and_nonfunctional_sections() -> list[str]:
    return [
        "## 4. External Interface Requirements",
        "",
        "### 4.1. User Interfaces",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| IR-01. | The system SHALL expose capabilities that can be consumed by web and mobile user interfaces for members, guardians, coaches, club administrators, finance users, and platform administrators. |",
        "| IR-02. | User interfaces SHOULD present terminology that is consistent with JustGo business concepts such as member, club, organisation, booking, credential, membership, asset, finance, and result. |",
        "| IR-03. | User interfaces SHOULD support role-appropriate workflows and avoid exposing actions that the current user is not authorized to perform. |",
        "",
        "### 4.2. Hardware Interfaces",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| IR-04. | The system SHALL operate without requiring specialized client hardware beyond standard web or mobile devices. |",
        "| IR-05. | The system SHOULD support mobile device camera workflows where QR, attendance, identity, or media-upload use cases are enabled by client applications. |",
        "",
        "### 4.3. Software Interfaces",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| IR-06. | The system SHALL interface with SQL Server or Azure SQL databases for tenant and central data storage. |",
        "| IR-07. | The system SHALL interface with authentication, token, and authorization services to validate users and enforce access control. |",
        "| IR-08. | The system SHALL interface with file storage services for profile images, club images, event images, class images, attachments, and imported files. |",
        "| IR-09. | The system SHALL interface with payment-provider services for payment accounts, products, recurring plans, balances, refunds, and payment console workflows. |",
        "| IR-10. | The system SHOULD expose machine-readable API documentation for client and integration teams. |",
        "",
        "### 4.4. Communications Interfaces",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| IR-11. | The system SHALL communicate with clients over secure HTTPS. |",
        "| IR-12. | The system SHALL exchange application data using structured request and response payloads. |",
        "| IR-13. | The system SHOULD support version-aware communication contracts so that clients can evolve without immediate breaking changes. |",
        "| IR-14. | The system SHOULD use consistent response structures and error messages so that client applications can handle success, validation, authorization, and failure outcomes predictably. |",
        "",
        "## 5. Non-Functional Requirements",
        "",
        "### 5.1. Performance Requirements",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-01. | The system SHALL provide acceptable response times for standard member, club, booking, finance, asset, and result operations under normal load conditions. |",
        "| NFR-02. | The system SHALL return list, search, and grid-view results within business-acceptable timeframes when pagination and filtering are used. |",
        "| NFR-03. | The system SHALL avoid unnecessary blocking operations in request processing so that long-running imports, validations, and batch activities do not degrade standard user workflows. |",
        "| NFR-04. | The system SHALL support concurrent usage by members, coaches, administrators, and mobile users across multiple tenants. |",
        "| NFR-05. | The system SHALL support peak operational periods such as membership renewal, event booking, attendance capture, and result publication. |",
        "| NFR-06. | The system SHALL use database, cache, storage, and compute resources efficiently for high-volume list and reporting workflows. |",
        "| NFR-07. | The system SHALL use caching where appropriate for frequently accessed reference data, permissions, tenant settings, and lookup information. |",
        "| NFR-08. | The system SHALL be deployable in a cloud environment that can scale application capacity as usage grows. |",
        "| NFR-09. | The system SHALL support growth in tenants, members, organisations, bookings, assets, financial records, and results without requiring major architectural redesign. |",
        "",
        "### 5.2. Security Requirements",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-10. | The system SHALL authenticate users before protected resources are accessed. |",
        "| NFR-11. | The system SHALL enforce authorization using role, permission, resource, tenant, and contextual attributes where required. |",
        "| NFR-12. | The system SHALL support multi-factor authentication workflows for users and administrators where enabled by policy. |",
        "| NFR-13. | The system SHALL prevent users from accessing data that belongs to another tenant unless explicitly authorized by platform-level rules. |",
        "| NFR-14. | The system SHALL protect sensitive member, finance, tenant, and authentication data at rest and in transit. |",
        "| NFR-15. | The system SHALL store tenant database credentials and sensitive configuration securely. |",
        "| NFR-16. | The system SHALL avoid exposing sensitive implementation details in user-facing errors. |",
        "| NFR-17. | The system SHALL support privacy-aware handling of member profiles, family information, emergency contacts, preferences, credentials, and documents. |",
        "| NFR-18. | The system SHALL maintain auditability for sensitive data access and administrative changes where required by business policy. |",
        "| NFR-19. | The system SHALL log security-relevant events such as authentication, authorization failure, MFA activity, and administrative changes. |",
        "| NFR-20. | The system SHOULD support operational monitoring and alerting for suspicious activity, repeated failures, and service errors. |",
        "",
        "### 5.3. Reliability and Availability",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-21. | The system SHALL be available during agreed service hours for tenant business operations. |",
        "| NFR-22. | The system SHALL support planned maintenance practices with appropriate communication and operational controls. |",
        "| NFR-23. | The system SHALL handle transient infrastructure and external-service failures gracefully where retry or fallback behavior is appropriate. |",
        "| NFR-24. | The system SHALL isolate failures so that one module, tenant, or external dependency does not unnecessarily disrupt unrelated business workflows. |",
        "| NFR-25. | The system SHALL support backup and restoration procedures for central and tenant databases. |",
        "| NFR-26. | The system SHALL define recovery objectives for critical platform capabilities before production operation. |",
        "| NFR-27. | The system SHALL validate inputs and return meaningful error responses for invalid, unauthorized, conflicting, or unavailable operations. |",
        "| NFR-28. | The system SHALL log diagnostic error details for support and troubleshooting without exposing sensitive information to end users. |",
        "",
        "### 5.4. Usability and Accessibility",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-29. | The system SHOULD support client experiences that are consistent across member, admin, finance, booking, result, and mobile workflows. |",
        "| NFR-30. | The system SHOULD provide enough metadata and permission information for clients to show role-appropriate navigation and actions. |",
        "| NFR-31. | Client applications SHOULD be able to meet accessibility expectations using the platform's structured data, validation outcomes, and permission responses. |",
        "| NFR-32. | The system SHOULD support tenant and user-facing content patterns that can be localized where required by customer configuration. |",
        "| NFR-33. | The system SHOULD support progressive and role-specific workflows so that users can complete common tasks without unnecessary steps. |",
        "",
        "### 5.5. Maintainability and Portability",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-34. | The system SHALL maintain clear module boundaries to support independent development and testing of business domains. |",
        "| NFR-35. | The system SHALL use consistent CQRS, validation, mapping, repository, logging, and response conventions across modules. |",
        "| NFR-36. | The system SHALL include automated tests for critical business logic and architecture boundary rules. |",
        "| NFR-37. | The system SHOULD remain deployable across supported cloud or hosted environments with externalized configuration. |",
        "| NFR-38. | The system SHOULD minimize hard dependencies on local developer machine settings in production deployment paths. |",
        "| NFR-39. | The system SHALL preserve API compatibility through versioned endpoints and managed contract changes. |",
        "| NFR-40. | The system SHOULD remain compatible with standard JSON clients, Swagger/OpenAPI tooling, and supported SQL Server versions. |",
        "",
        "### 5.6. Legal, Compliance, and Operational Requirements",
        "",
        "| ID | Requirement |",
        "| --- | --- |",
        "| NFR-41. | The system SHALL support customer compliance obligations for sports organisation records, member data, payment data, and audit logs as configured by tenant policy. |",
        "| NFR-42. | The system SHALL respect licensing obligations for third-party components, libraries, and integrations used by the platform. |",
        "| NFR-43. | The system SHOULD define measurable service targets for availability, incident response, support, and recovery before production rollout. |",
        "| NFR-44. | The system SHALL provide structured application, exception, event, and audit logging for operational support. |",
        "| NFR-45. | The system SHOULD provide health and diagnostic signals for infrastructure, database, cache, storage, and external-service dependencies. |",
        "| NFR-46. | The system SHALL support backup and restore operations for tenant and central data stores. |",
        "| NFR-47. | The system SHALL support administrative management of users, permissions, tenants, settings, reference data, and operational content. |",
        "| NFR-48. | The system SHALL maintain technical documentation for setup, architecture, module ownership, API usage, deployment, and troubleshooting. |",
        "| NFR-49. | The system SHOULD support tenant-specific terminology, branding, and operational configuration where customer needs differ. |",
        "| NFR-50. | The system SHALL support continuity of critical member, booking, payment, attendance, and result operations during normal operational disruptions according to agreed business priorities. |",
        "",
        "## 6. Other Requirements",
        "",
        "### 6.1. Data Migration",
        "",
        "The system SHALL support migration of existing customer and tenant data into the JustGo Platform where required. This includes mapping, transforming, validating, testing, correcting, and preserving tenant ownership for member, club, membership, booking, asset, finance, credential, and result records.",
        "",
        "### 6.2. Internationalization Requirements",
        "",
        "The system SHOULD support tenant localization needs, including adding languages, separating configurable user-facing content, local date/time/currency formatting, tenant-specific terminology, branding, and local sports organisation practices.",
        "",
        "### 6.3. Training Requirements",
        "",
        "The system SHALL include provisions for administrator, end-user, coach, event-staff, finance, asset, result-management, and customer onboarding training.",
        "",
        "### 6.4. Appendix A: Analysis Models",
        "",
        "#### A.1. System Context Diagram",
        "",
        "```mermaid",
        "C4Context",
        "title JustGo Platform - System Context",
        "Person(member, \"Member / Guardian\", \"Manages profile, family, memberships, bookings, credentials, and results\")",
        "Person(admin, \"Club / Organisation Administrator\", \"Manages clubs, members, bookings, assets, finance, and results\")",
        "Person(coach, \"Coach / Event Staff\", \"Uses mobile workflows for attendance, notes, and bookings\")",
        "System(justgo, \"JustGo Platform\", \"Multi-tenant SaaS platform for sports organisation operations\")",
        "System_Ext(sql, \"SQL Server / Azure SQL\", \"Central and tenant databases\")",
        "System_Ext(storage, \"Azure Blob Storage\", \"Images, attachments, and imported files\")",
        "System_Ext(payment, \"Payment Provider\", \"Payment accounts, products, balances, and refunds\")",
        "Rel(member, justgo, \"Uses\")",
        "Rel(admin, justgo, \"Administers\")",
        "Rel(coach, justgo, \"Operates\")",
        "Rel(justgo, sql, \"Stores and retrieves data\")",
        "Rel(justgo, storage, \"Stores and retrieves files\")",
        "Rel(justgo, payment, \"Processes finance operations\")",
        "```",
        "",
        "#### A.2. Container Diagram",
        "",
        "```mermaid",
        "C4Container",
        "title JustGo Platform - Container View",
        "Person(user, \"Platform User\")",
        "System_Boundary(justgo, \"JustGo Platform\") {",
        "  Container(api, \"ASP.NET Core API\", \".NET 9\", \"Hosts versioned REST APIs and composes all modules\")",
        "  Container(auth, \"Authentication Infrastructure\", \".NET\", \"JWT/JWE, ABAC, tenant context, caching, logging, repositories\")",
        "  ContainerDb(db, \"Tenant Databases\", \"SQL Server\", \"Tenant operational data\")",
        "  ContainerDb(central, \"Central Database\", \"SQL Server\", \"Tenant registry and shared platform data\")",
        "}",
        "System_Ext(blob, \"Azure Blob Storage\", \"File and media storage\")",
        "System_Ext(payments, \"Payment Provider\", \"External finance services\")",
        "Rel(user, api, \"Uses\", \"HTTPS/JSON\")",
        "Rel(api, auth, \"Uses\")",
        "Rel(api, db, \"Reads/writes tenant data\")",
        "Rel(api, central, \"Reads tenant metadata\")",
        "Rel(api, blob, \"Stores/retrieves files\")",
        "Rel(api, payments, \"Processes payments\")",
        "```",
        "",
        "### 6.5. Appendix B: Issues List",
        "",
        "| ID | Issue | Resolution Approach |",
        "| --- | --- | --- |",
        "| ISS-01. | Exact business rule wording is inferred from source code and requires domain-owner validation. | Conduct module-by-module review with product owners and support leads. |",
        "| ISS-02. | Stored procedure behavior and database constraints are not fully represented in controller-level analysis. | Review database objects and handler implementation in a follow-up requirements pass. |",
        "| ISS-03. | User interface behavior is outside the backend repository. | Validate UI requirements with frontend applications, user journeys, and customer documentation. |",
        "| ISS-04. | Non-functional targets such as exact response time, uptime, RTO, RPO, and concurrency are not confirmed. | Establish measurable service targets with operations and customer stakeholders. |",
        "| ISS-05. | Endpoint-derived wording may still need domain-owner refinement. | Use the traceability matrix to review each grouped FR and update capability rules or product wording. |",
        "",
    ]


def generate_traceability_appendix(capabilities: list[Capability]) -> list[str]:
    lines = [
        "## 7. Endpoint Traceability Appendix",
        "",
        "This appendix lists endpoint trace IDs and routes for each grouped functional requirement. The full review matrix, including legacy endpoint-shaped FR text and review status, is generated at `docs/srs-endpoint-traceability-matrix.md`.",
        "",
        "| FR ID | Capability | Endpoint Traces | Supporting Routes |",
        "| --- | --- | --- | --- |",
    ]
    for capability in capabilities:
        trace_ids = ", ".join(endpoint.trace_id for endpoint in capability.endpoints)
        routes = "<br>".join(f"{endpoint.verb} {endpoint.full_route}" for endpoint in capability.endpoints)
        lines.append(f"| {capability.id} | {table_escape(capability.title)} | {trace_ids} | {table_escape(routes)} |")
    lines.append("")
    return lines


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate JustGo SRS markdown and endpoint trace matrix from source code.")
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="Repository root.")
    parser.add_argument("--pdf", type=Path, default=None, help="Reference SRS PDF path.")
    parser.add_argument("--output", type=Path, default=None, help="Output SRS markdown path.")
    parser.add_argument("--trace-output", type=Path, default=None, help="Output endpoint traceability markdown path.")
    args = parser.parse_args()

    root = args.root.resolve()
    pdf_path = args.pdf.resolve() if args.pdf else root / "docs" / "Annex-A-Detailed-Software-Requirements-Specification-SRS.pdf"
    output = args.output.resolve() if args.output else root / "docs" / "justgo-platform-software-requirements-specification.md"
    trace_output = args.trace_output.resolve() if args.trace_output else root / "docs" / "srs-endpoint-traceability-matrix.md"
    generate_srs(root, output, trace_output, pdf_path)
    print(f"Generated {output}")
    print(f"Generated {trace_output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
