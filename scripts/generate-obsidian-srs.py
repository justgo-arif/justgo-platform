import os
import re

base = r"d:/Workspace/personal/srs-generator/justgo-platform/docs/SRS"
os.makedirs(base, exist_ok=True)

data_sections = {
    "3.1": {"name": "User Management and Authentication", "scope": "identity, login, tenant resolution, authorization, MFA, lookup data, shared notes, attachments, and user/security administration", "fr_range": "FR-01 to FR-80"},
    "3.2": {"name": "Member Profile Management", "scope": "member profile, family, emergency contacts, preferences, notes, and member self-service profile workflows", "fr_range": "FR-81 to FR-120"},
    "3.3": {"name": "Organisation and Club Management", "scope": "clubs, organisation hierarchy, member organisation relationships, join/leave/transfer workflows, and primary club management", "fr_range": "FR-121 to FR-129"},
    "3.4": {"name": "Membership Management", "scope": "membership plans, licenses, family membership information, member entitlements, and downloadable membership artifacts", "fr_range": "FR-130 to FR-142"},
    "3.5": {"name": "Booking Management", "scope": "class, course, session, attendee, occurrence, eligibility, attendance, and profile booking operations", "fr_range": "FR-143 to FR-175"},
    "3.6": {"name": "Asset Management", "scope": "asset registers, asset categories, licenses, transfers, inspections, workflow status, and operational asset administration", "fr_range": "FR-176 to FR-250"},
    "3.7": {"name": "Credential Management", "scope": "member credentials, credential templates, issuance, approvals, and credential-related member data", "fr_range": "FR-251 to FR-252"},
    "3.8": {"name": "Field and Preference Management", "scope": "custom extension fields, field sets, schemas, entity-specific metadata, and configurable data capture", "fr_range": "FR-253 to FR-270"},
    "3.9": {"name": "Finance and Payment Management", "scope": "payments, balances, subscriptions, installments, payment accounts, products, refunds, and finance grid views", "fr_range": "FR-271 to FR-335"},
    "3.10": {"name": "Mobile Application Services", "scope": "mobile-focused experiences for clubs, events, classes, content, settings, MFA, bookings, and attendance", "fr_range": "FR-336 to FR-435"},
    "3.11": {"name": "Results and Competition Management", "scope": "sports results, event results, competition data, rankings, result uploads, validation, and result-file lifecycle management", "fr_range": "FR-436 to FR-502"},
}

subsections = [
    ("3.1","3.1.1","Authorization","FR-01","FR-04"),
    ("3.1","3.1.2","Accounts","FR-05","FR-16"),
    ("3.1","3.1.3","Cache Invalidation","FR-17","FR-22"),
    ("3.1","3.1.4","Files","FR-23","FR-36"),
    ("3.1","3.1.5","Lookup","FR-37","FR-41"),
    ("3.1","3.1.6","Multi-Factor Authentication","FR-42","FR-56"),
    ("3.1","3.1.7","Notes","FR-57","FR-62"),
    ("3.1","3.1.8","System Settings","FR-63","FR-63"),
    ("3.1","3.1.9","Tenants","FR-64","FR-71"),
    ("3.1","3.1.10","User Interface Permissions","FR-72","FR-74"),
    ("3.1","3.1.11","Users","FR-75","FR-80"),
    ("3.2","3.2.1","Address Pickers","FR-81","FR-81"),
    ("3.2","3.2.2","Member Basic Details","FR-82","FR-86"),
    ("3.2","3.2.3","Member Family","FR-87","FR-95"),
    ("3.2","3.2.4","Member Notes","FR-96","FR-99"),
    ("3.2","3.2.5","Members","FR-100","FR-108"),
    ("3.2","3.2.6","Preferences","FR-109","FR-114"),
    ("3.2","3.2.7","User Emergency Contacts","FR-115","FR-120"),
    ("3.3","3.3.1","Organisations","FR-121","FR-129"),
    ("3.4","3.4.1","Memberships","FR-130","FR-136"),
    ("3.4","3.4.2","Memberships Purchase","FR-137","FR-142"),
    ("3.5","3.5.1","Booking Catalog","FR-143","FR-148"),
    ("3.5","3.5.2","Booking Class","FR-149","FR-159"),
    ("3.5","3.5.3","Booking Pricing Chart Discount","FR-160","FR-165"),
    ("3.5","3.5.4","Booking Transfer Request","FR-166","FR-166"),
    ("3.5","3.5.5","Class Management","FR-167","FR-168"),
    ("3.5","3.5.6","Class Term","FR-169","FR-170"),
    ("3.5","3.5.7","Profile Class Booking","FR-171","FR-172"),
    ("3.5","3.5.8","Profile Course Booking","FR-173","FR-175"),
    ("3.6","3.6.1","Asset Audit","FR-176","FR-176"),
    ("3.6","3.6.2","Asset Categories","FR-177","FR-177"),
    ("3.6","3.6.3","Asset Checkout","FR-178","FR-179"),
    ("3.6","3.6.4","Asset Credentials","FR-180","FR-185"),
    ("3.6","3.6.5","Asset Leases","FR-186","FR-195"),
    ("3.6","3.6.6","Asset Licenses","FR-196","FR-209"),
    ("3.6","3.6.7","Asset Metadata","FR-210","FR-220"),
    ("3.6","3.6.8","Asset Ownership Transfers","FR-221","FR-227"),
    ("3.6","3.6.9","Asset Registers","FR-228","FR-240"),
    ("3.6","3.6.10","Asset Reports","FR-241","FR-242"),
    ("3.6","3.6.11","Asset Types","FR-243","FR-244"),
    ("3.6","3.6.12","Clubs","FR-245","FR-249"),
    ("3.6","3.6.13","Workflows","FR-250","FR-250"),
    ("3.7","3.7.1","Credentials","FR-251","FR-252"),
    ("3.8","3.8.1","Entity Extensions","FR-253","FR-270"),
    ("3.9","3.9.1","Balances","FR-271","FR-272"),
    ("3.9","3.9.2","Finance Grid View","FR-273","FR-278"),
    ("3.9","3.9.3","Installments","FR-279","FR-287"),
    ("3.9","3.9.4","Payment Account","FR-288","FR-290"),
    ("3.9","3.9.5","Payment Console","FR-291","FR-297"),
    ("3.9","3.9.6","Payments","FR-298","FR-326"),
    ("3.9","3.9.7","Products","FR-327","FR-327"),
    ("3.9","3.9.8","Subscriptions","FR-328","FR-335"),
    ("3.10","3.10.1","Classes","FR-336","FR-382"),
    ("3.10","3.10.2","Clubs","FR-383","FR-386"),
    ("3.10","3.10.3","Events","FR-387","FR-399"),
    ("3.10","3.10.4","General Settings","FR-400","FR-405"),
    ("3.10","3.10.5","Multi-Factor Authentication","FR-406","FR-420"),
    ("3.10","3.10.6","Two-Factor Authentication","FR-421","FR-435"),
    ("3.11","3.11.1","Events","FR-436","FR-458"),
    ("3.11","3.11.2","Results","FR-459","FR-478"),
    ("3.11","3.11.3","Sports Results","FR-479","FR-487"),
    ("3.11","3.11.4","Upload Result","FR-488","FR-502"),
]

# Parse SRS for FR texts
srs_path = r"d:/Workspace/personal/srs-generator/justgo-platform/docs/justgo-platform-software-requirements-specification.md"
with open(srs_path, 'r', encoding='utf-8') as f:
    srs_text = f.read()

fr_pattern = re.compile(r'\|\s*FR-(\d+)\.\s*\|\s*(.+?)\s*\|', re.MULTILINE)
fr_entries = {}
for m in fr_pattern.finditer(srs_text):
    fr_num = int(m.group(1))
    fr_text = m.group(2).strip()
    fr_entries[fr_num] = fr_text

print(f"Parsed {len(fr_entries)} FR entries")

# Build FR -> subsection mapping
fr_to_sub = {}
for (sec, subsec, subname, fr_start, fr_end) in subsections:
    s_num = int(fr_start.replace("FR-",""))
    e_num = int(fr_end.replace("FR-",""))
    for n in range(s_num, e_num+1):
        fr_to_sub[n] = (sec, subsec, subname)

def subsec_slug(subsec_num, subname):
    return f"{subsec_num}-{subname.replace(' ','-')}"

def sec_slug(sec_num, sec_name):
    return sec_num + "-" + sec_name.replace(' ','-').replace('/','-')

# Write FR files
files_written = 0
for fr_num in range(1, 503):
    if fr_num not in fr_entries:
        print(f"WARNING: FR-{fr_num:03d} not in parsed entries")
        continue
    if fr_num not in fr_to_sub:
        print(f"WARNING: FR-{fr_num:03d} not mapped to subsection")
        continue

    fr_text = fr_entries[fr_num]
    sec, subsec, subname = fr_to_sub[fr_num]
    sec_name = data_sections[sec]["name"]
    sec_s = sec_slug(sec, sec_name)
    sub_s = subsec_slug(subsec, subname)
    fr_id = f"FR-{fr_num:03d}"

    content = f"""---
id: {fr_id}
type: functional-requirement
section: "{subsec}"
subsection: "{subname}"
feature: "{sec_name}"
tags:
  - srs
  - functional-requirement
---

# {fr_id}

**Feature Area:** [[{sec_s}|{sec}. {sec_name}]]
**Subsection:** [[{sub_s}|{subsec}. {subname}]]

## Requirement

{fr_text}
"""
    with open(os.path.join(base, f"{fr_id}.md"), 'w', encoding='utf-8') as f:
        f.write(content)
    files_written += 1

print(f"Written {files_written} FR files")

# Write subsection files
for (sec, subsec, subname, fr_start, fr_end) in subsections:
    sec_name = data_sections[sec]["name"]
    sec_s = sec_slug(sec, sec_name)
    sub_s = subsec_slug(subsec, subname)
    s_num = int(fr_start.replace("FR-",""))
    e_num = int(fr_end.replace("FR-",""))
    fr_links = "\n".join([f"- [[FR-{n:03d}]]" for n in range(s_num, e_num+1)])
    content = f"""---
id: "{subsec}"
type: subsection
section: "{sec}"
feature: "{sec_name}"
tags:
  - srs
  - subsection
---

# {subsec}. {subname}

**Feature Area:** [[{sec_s}|{sec}. {sec_name}]]

Range: {fr_start} to {fr_end}

## Functional Requirements

{fr_links}
"""
    with open(os.path.join(base, f"{sub_s}.md"), 'w', encoding='utf-8') as f:
        f.write(content)

print(f"Written {len(subsections)} subsection files")

# Write section files
for sec_num, sec_data in data_sections.items():
    sec_name = sec_data["name"]
    sec_s = sec_slug(sec_num, sec_name)
    subs = [(s,ss,sn,fs,fe) for (s,ss,sn,fs,fe) in subsections if s == sec_num]
    sub_links = "\n".join([f"- [[{subsec_slug(ss,sn)}|{ss}. {sn}]] ({fs} to {fe})" for (s,ss,sn,fs,fe) in subs])
    content = f"""---
id: "{sec_num}"
type: section
feature: "{sec_name}"
fr_range: "{sec_data['fr_range']}"
tags:
  - srs
  - section
---

# {sec_num}. {sec_name}

**Source:** [[justgo-platform-software-requirements-specification|SRS Document]]

**Business Scope:** {sec_data['scope']}

**FR Range:** {sec_data['fr_range']}

## Subsections

{sub_links}
"""
    with open(os.path.join(base, f"{sec_s}.md"), 'w', encoding='utf-8') as f:
        f.write(content)

print(f"Written {len(data_sections)} section files")

# Write MOC
moc_lines = []
for sec_num, sec_data in data_sections.items():
    sec_name = sec_data["name"]
    sec_s = sec_slug(sec_num, sec_name)
    moc_lines.append(f"\n### [[{sec_s}|{sec_num}. {sec_name}]] — {sec_data['fr_range']}\n")
    subs = [(s,ss,sn,fs,fe) for (s,ss,sn,fs,fe) in subsections if s == sec_num]
    for (s,ss,sn,fs,fe) in subs:
        sub_s = subsec_slug(ss,sn)
        moc_lines.append(f"- [[{sub_s}|{ss}. {sn}]] ({fs}–{fe})\n")

moc_content = """---
title: JustGo Platform SRS — Map of Content
tags:
  - srs
  - moc
  - justgo
---

# JustGo Platform SRS — Map of Content

**Source:** [[justgo-platform-software-requirements-specification|Full SRS Document]]

502 functional requirements across 11 feature areas.

## Feature Areas

""" + "".join(moc_lines) + """
## Cross-Cutting Themes

- Access control and tenant isolation appear throughout protected workflows.
- Most feature areas support create, view, update, remove, search/filter, validation, status change, upload/download, or export.
- Data integrity emphasized through validation outcomes, traceability, authorization checks, and downstream workflow availability.
- Mobile services mirror core platform capabilities for classes, events, clubs, security, and operational workflows.
"""
with open(os.path.join(base, "MOC.md"), 'w', encoding='utf-8') as f:
    f.write(moc_content)

print("Written MOC.md")
total = len(os.listdir(base))
print(f"Total files in SRS/: {total}")
