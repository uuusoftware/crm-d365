import re
import os
import json
import pandas as pd
import requests
from datetime import datetime, timedelta
from pyspark.sql import SparkSession, Row
from pyspark.sql.types import StringType
from pyspark.sql.functions import col, when, lit, concat, expr, collect_list, concat_ws, udf, pandas_udf, PandasUDFType, current_timestamp, coalesce
from pyspark.sql.types import *
from functools import lru_cache

current_timestamp = datetime.now()
current_timestamp_str = datetime.now().strftime("%Y%m%d%H%M%S")

tenant_id = os.getenv("TENANT", "default_tenant_id")
client_id = os.getenv("CLIENT", "default_client_id")
client_secret = os.getenv("SECRET", "default_client_secret")
ORG_URL = os.getenv("RESOURCE", "https://cmdynamicsqa.api.crm3.dynamics.com")
ENTITY_SET = "appointments"
ENTITY_LOGICAL = "appointment"
mapping = {
    # "Appointment ID": {
    #     "type": "string",
    #     "target_attribute": "activityid" 
    # },
    "Subject": {
        "type": "string",
        "target_attribute": "subject"
    },
    "Appointment Type": {
        "type": "optionset",
        "target_attribute": "cm_appointmenttype" 
    },
    "Visit Type": {
        "type": "optionset",
        "target_attribute": "cm_visittype" 
    },
    "Visit Reason": {
        "type": "lookup",
        "target_attribute": "cm_visitreason",
        "target_entity_logical": "cm_visitreasonmaster", 
        "target_entity_set": "cm_visitreasonmasters",
        "source_key": "Visit Reason",
        "name_column": "cm_name",
        "method": "bind"
    },
    "Location": {
        "type": "string",
        "target_attribute": "location"
    },
    "Required Attendees": {
        "type": "partylist",
        "target_attribute": "requiredattendees"
    },
    "Optional Attendees": {
        "type": "partylist",
        "target_attribute": "optionalattendees"
    },
    "Organizer": {
        "type": "partylist",
        "target_attribute": "organizer"
    },
    "Regarding": {
        "type": "polymorphic",
        "target_attribute": "regardingobjectid",
        "source_key": "Regarding",
        "candidates": [
            { "entity_logical": "incident", "entity_set": "incidents", "by": "name", "column": "ticketnumber", "force_string": True }
            # { "entity_logical": "account", "entity_set": "accounts", "by": "objectid", "column": "accountnumber", "force_string": True },
            # { "entity_logical": "contact", "entity_set": "contacts", "by": "objectid", "column": "cm_contactid", "force_string": True },
            # { "entity_logical": "lead",    "entity_set": "leads",    "by": "objectid", "column": "cm_leadid", "force_string": True }
        ],
        "method": "bind"
    },
    "Description": {
        "type": "string",
        "target_attribute": "description"
    },
    "Owner": {
        "type": "owner",
        "target_attribute": "ownerid",
        "source_key": "Owner",
        "candidates": [
            { "entity_logical": "systemuser", "entity_set": "systemusers", "by": "email", "column": "internalemailaddress" },
            { "entity_logical": "team",       "entity_set": "teams",       "by": "name",  "column": "name" }
        ],
        "method": "bind"
    },
    "Status": {
        "type": "optionset",
        "target_attribute": "statecode"
    },
    "Status Reason": {
        "type": "optionset",
        "target_attribute": "statuscode"
    },
    "Priority": {
        "type": "optionset",
        "target_attribute": "prioritycode"
    },
    "Scheduled Start": {
        "type": "date",
        "target_attribute": "scheduledstart"
    },
    "Scheduled End": {
        "type": "date",
        "target_attribute": "scheduledend"
    },
    "Actual Start": {
        "type": "date",
        "target_attribute": "actualstart"
    },
    "Actual End": {
        "type": "date",
        "target_attribute": "actualend"
    },
    "Duration": {
        "type": "int",
        "target_attribute": "scheduleddurationminutes"   # appointment uses *minutes*
    },
    "Is All Day Event": {
        "type": "bool",
        "target_attribute": "isalldayevent"
    },
    "Created On": {
        "type": "date",
        "target_attribute": "overriddencreatedon"
    },
    # "Created By": {
    #     "type": "lookup",
    #     "target_attribute": "cm_createdbyoverride", 
    #     "target_entity_logical": "systemuser",
    #     "target_entity_set": "systemusers",
    #     "source_key": "Created By",
    #     "name_column": "internalemailaddress",
    #     "method": "ref"
    # },
    "Object ID": {
        "type": "string",
        "target_attribute": "cm_objectid"
    }
}
CSV_DATETIME_FORMATS = [
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%dT%H:%M:%S",
    "%m/%d/%Y %I:%M:%S %p",
    "%m/%d/%Y %H:%M",
    "%Y-%m-%d"
]


# Example usage
print(f"Tenant ID: {tenant_id}")
print(f"Client ID: {client_id}")
print(f"Client Secret: {client_secret}")
print(f"ORG URL: {ORG_URL}")

# ==========================================================
# ---- HTTP/AUTH -------------------------------------------
# ==========================================================
session = requests.Session()
session.headers.update({
    "OData-MaxVersion": "4.0",
    "OData-Version": "4.0",
    "Accept": "application/json",
    "Content-Type": "application/json; charset=utf-8",
    "MSCRM.BypassBusinessLogicExecution": "CustomSync,CustomAsync",
    "MSCRM.SuppressCallbackRegistrationExpanderJob": "true",
})

TOKEN_URL = f"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token"
API_VER = "v9.2"
BASE = f"{ORG_URL}/api/data/{API_VER}"

def get_token():
    r = requests.post(TOKEN_URL, data={
        "client_id": client_id,
        "client_secret": client_secret,
        "grant_type": "client_credentials",
        "scope": f"{ORG_URL}/.default"
    })
    r.raise_for_status()
    return r.json()["access_token"]

def ensure_auth():
    if "Authorization" not in session.headers:
        session.headers["Authorization"] = f"Bearer {get_token()}"
        
def who_am_i():
    j = api("GET", "WhoAmI()").json()
    uid = j.get("UserId")
    details = api("GET", f"systemusers({uid})?$select=fullname,domainname,applicationid").json()
    return {"UserId": uid, **details}

def api(method, path_or_url, headers=None, **kw):
    """Merged headers, auto-refresh on 401, raises with JSON body when possible."""
    ensure_auth()
    url = path_or_url if path_or_url.startswith("http") else f"{BASE}/{path_or_url.lstrip('/')}"
    merged = session.headers.copy()
    if headers:
        merged.update(headers)
    r = session.request(method, url, headers=merged, **kw)
    if r.status_code == 401:
        session.headers["Authorization"] = f"Bearer {get_token()}"
        r = session.request(method, url, headers=merged, **kw)
    if r.status_code >= 400:
        try:
            raise RuntimeError(f"{method} {url} -> {r.status_code}: {r.json()}")
        except Exception:
            raise RuntimeError(f"{method} {url} -> {r.status_code}: {r.text}")
    return r


# ==========================================================
# ---- LOOKUP RESOLUTION HELPERS ---------------------------
# ==========================================================
def try_parse_guid(s):
    if not s: return None
    s = s.strip().strip("{}")
    return s if len(s) == 36 and s.count("-") == 4 else None

@lru_cache(maxsize=2048)
def fetch_guid_by_secondary_column(entity_set, secondary_column, value,
                                   guid_column=None, *, force_string=False):
    if not value:
        return None
    g = try_parse_guid(value)
    if g:
        return g

    quoted = str(value).replace("'", "''")

    # Try QUOTED first (works for nvarchar like ticketnumber)
    q = f"{entity_set}?$filter={secondary_column} eq '{quoted}'&$top=1"
    j = api("GET", q).json()
    rows = j.get("value") or []

    # If nothing found and not forcing string, optionally try unquoted numeric
    if not rows and (not force_string) and str(value).strip().replace('.', '', 1).isdigit():
        q2 = f"{entity_set}?$filter={secondary_column} eq {value}&$top=1"
        rows = api("GET", q2).json().get("value") or []

    if not rows:
        return None

    row = rows[0]
    if guid_column and guid_column in row:
        return row[guid_column]
    for k, v in row.items():
        if k.endswith("id") and try_parse_guid(str(v)):
            return v
    return None

def set_lookup_with_ref(parent_entity_set: str, parent_id: str, nav_prop: str, target_entity_set: str, target_id: str): 
    """ 
    Preferred, robust way to set a lookup: 
    PUT <parent>/<id>/<nav_prop>/$ref with {"@odata.id": "<.../target_set(guid)>"} 
    """ 
    ref_url = f"{parent_entity_set}({parent_id})/{nav_prop}/$ref" 
    payload = {"@odata.id": f"{BASE}/{target_entity_set}({target_id})"} 
    api("PUT", ref_url, data=json.dumps(payload))


# ==========================================================
# ---- Activity Party Builder ------------------------------
# ==========================================================
def _party_item_from_token(token: str, ptm: int) -> dict:
    """
    Returns a dict suitable for inlining under appointment_activity_parties:
    - resolved: typed partyid_*@odata.bind
    - unresolved: addressused
    Always includes @odata.type + participationtypemask.
    Resolution order: systemuser(email) -> contact(emailaddress1) -> account(name) -> lead(emailaddress1)
    """
    # systemuser
    guid = fetch_guid_by_secondary_column("systemusers", "internalemailaddress", token)
    if guid:
        return {
            "@odata.type": "Microsoft.Dynamics.CRM.activityparty",
            "participationtypemask": ptm,
            "partyid_systemuser@odata.bind": f"/systemusers({guid})"
        }
    # contact
    guid = fetch_guid_by_secondary_column("contacts", "emailaddress1", token)
    if guid:
        return {
            "@odata.type": "Microsoft.Dynamics.CRM.activityparty",
            "participationtypemask": ptm,
            "partyid_contact@odata.bind": f"/contacts({guid})"
        }
    # account
    guid = fetch_guid_by_secondary_column("accounts", "name", token)
    if guid:
        return {
            "@odata.type": "Microsoft.Dynamics.CRM.activityparty",
            "participationtypemask": ptm,
            "partyid_account@odata.bind": f"/accounts({guid})"
        }
    # lead
    guid = fetch_guid_by_secondary_column("leads", "emailaddress1", token)
    if guid:
        return {
            "@odata.type": "Microsoft.Dynamics.CRM.activityparty",
            "participationtypemask": ptm,
            "partyid_lead@odata.bind": f"/leads({guid})"
        }
    # unresolved -> addressused
    return {
        "@odata.type": "Microsoft.Dynamics.CRM.activityparty",
        "participationtypemask": ptm,
        "addressused": token
    }


# ==========================================================
# ---- METADATA HELPERS ------------------------------------
# ==========================================================
@lru_cache(maxsize=512)
def get_single_nav_prop(referencing_entity_logical: str, referencing_attribute_logical: str, referenced_entity_logical: str):
    """
    Return the single-valued navigation property name for a lookup attribute.
    Works for Many-to-One and One-to-Many (fallback).
    """
    # 1) Many-to-One (correct for lookups like appointment.cm_visitreason -> cm_visitreasonmaster)
    url_m1 = (
        f"EntityDefinitions(LogicalName='{referencing_entity_logical}')/ManyToOneRelationships"
        f"?$select=ReferencingAttribute,ReferencingEntityNavigationPropertyName,ReferencedEntity"
        f"&$filter=ReferencingAttribute eq '{referencing_attribute_logical}' and ReferencedEntity eq '{referenced_entity_logical}'"
    )
    data = api("GET", url_m1).json().get("value", [])
    if data:
        return data[0]["ReferencingEntityNavigationPropertyName"]

    # 2) Fallback: One-to-Many (some envs expose via this collection too)
    url_1n = (
        f"EntityDefinitions(LogicalName='{referencing_entity_logical}')/OneToManyRelationships"
        f"?$select=ReferencingAttribute,ReferencingEntityNavigationPropertyName,ReferencedEntity"
        f"&$filter=ReferencingAttribute eq '{referencing_attribute_logical}' and ReferencedEntity eq '{referenced_entity_logical}'"
    )
    data = api("GET", url_1n).json().get("value", [])
    if data:
        return data[0]["ReferencingEntityNavigationPropertyName"]

    raise RuntimeError(
        f"No M:1 or 1:N relationship found for "
        f"{referencing_entity_logical}.{referencing_attribute_logical} -> {referenced_entity_logical}"
    )

def resolve_polymorphic_target(value, strategies):
    if not value: return (None, None, None)
    v = str(value).strip()
    g = try_parse_guid(v)
    if g and strategies:
        s0 = strategies[0]
        return (s0["entity_set"], s0["entity_logical"], g)

    for s in strategies:
        col = s.get("column", "name")
        force_str = bool(s.get("force_string", False))  # <—
        guid = fetch_guid_by_secondary_column(s["entity_set"], col, v, None, force_string=force_str)
        if guid:
            return (s["entity_set"], s["entity_logical"], guid)
    return (None, None, None)

@lru_cache(maxsize=512)
def get_optionset_map(entity_logical: str, attribute_logical: str):
    """
    Works for: Picklist, MultiSelectPicklist, Status, State, Boolean.
    Returns (label_to_value, value_to_label).
    """
    # 1) Read base metadata to learn the concrete type
    base_url = (
        f"EntityDefinitions(LogicalName='{entity_logical}')/"
        f"Attributes(LogicalName='{attribute_logical}')"
        f"?$select=LogicalName,SchemaName"
    )
    base = api("GET", base_url).json()
    otype = base.get("@odata.type", "")

    label_to_value, value_to_label = {}, {}

    def add_options(options):
        if not options:
            return
        for o in options:
            val = o["Value"]
            labels = o["Label"]["LocalizedLabels"] or []
            lbl = (labels[0]["Label"] if labels else str(val)).strip()
            label_to_value[lbl.lower()] = val
            value_to_label[val] = lbl

    # 2) Branch by concrete type and fetch options properly
    if otype.endswith("BooleanAttributeMetadata"):
        url = (
            f"EntityDefinitions(LogicalName='{entity_logical}')/"
            f"Attributes(LogicalName='{attribute_logical}')/"
            f"Microsoft.Dynamics.CRM.BooleanAttributeMetadata?$select=TrueOption,FalseOption"
        )
        j = api("GET", url).json()
        t = j.get("TrueOption") or {}
        f = j.get("FalseOption") or {}
        if "Value" in t and t.get("Label", {}).get("LocalizedLabels"):
            lbl = t["Label"]["LocalizedLabels"][0]["Label"].strip()
            label_to_value[lbl.lower()] = t["Value"]
            value_to_label[t["Value"]] = lbl
        if "Value" in f and f.get("Label", {}).get("LocalizedLabels"):
            lbl = f["Label"]["LocalizedLabels"][0]["Label"].strip()
            label_to_value[lbl.lower()] = f["Value"]
            value_to_label[f["Value"]] = lbl

    elif (
        otype.endswith("PicklistAttributeMetadata")
        or otype.endswith("MultiSelectPicklistAttributeMetadata")
        or otype.endswith("StatusAttributeMetadata")
        or otype.endswith("StateAttributeMetadata")
        or otype.endswith("EnumAttributeMetadata")
    ):
        # Cast to the enum-like type, then expand OptionSet/GlobalOptionSet
        url = (
            f"EntityDefinitions(LogicalName='{entity_logical}')/"
            f"Attributes(LogicalName='{attribute_logical}')/"
            f"Microsoft.Dynamics.CRM.{otype.split('.')[-1]}"
            f"?$select=LogicalName"
            f"&$expand=OptionSet($select=Options),GlobalOptionSet($select=Options)"
        )
        j = api("GET", url).json()
        options = (j.get("OptionSet") or {}).get("Options") or (j.get("GlobalOptionSet") or {}).get("Options") or []
        add_options(options)

    else:
        # Not an optionset-type attribute
        raise RuntimeError(
            f"{entity_logical}.{attribute_logical} is not an enum-like attribute (type={otype})."
        )

    return label_to_value, value_to_label


# ==========================================================
# ---- VALUE CONVERTERS ------------------------------------
# ==========================================================
def to_bool(v):
    if v is None: return None
    s = str(v).strip().lower()
    if s in ("1","true","yes","y","t"): return True
    if s in ("0","false","no","n","f"): return False
    return None

def to_int(v):
    try:
        return int(float(v)) if v not in (None, "") else None
    except:
        return None

def to_float(v):
    try:
        return float(v) if v not in (None, "") else None
    except:
        return None

def to_iso(v):
    if v in (None, ""):
        return None
    if isinstance(v, (datetime, )):
        return v.isoformat()
    s = str(v).strip()
    for fmt in CSV_DATETIME_FORMATS:
        try:
            return datetime.strptime(s, fmt).isoformat()
        except:
            pass
    # last resort: return raw;
    return s


# ==========================================================
# ---- ROW -> PAYLOAD BUILDER ------------------------------
# ==========================================================
def _norm_col(s: str) -> str:
    # lowercase, trim, collapse whitespace, strip NBSP
    return re.sub(r"\s+", " ", (s or "").replace("\u00A0", " ").strip().lower())
    
def _normalize_all_day(payload: dict) -> dict:
    if not payload.get("isalldayevent"):
        return payload

    def _to_date_start_utc(iso_s: str) -> datetime:
        # Accepts 'YYYY-MM-DDTHH:MM:SS[.fff]Z' or 'YYYY-MM-DD ...'
        s = iso_s.replace(" ", "T")
        if not s.endswith("Z"):  # assume it's already UTC-ish; add Z if missing
            s += "Z"
        d = datetime.fromisoformat(s.replace("Z", "+00:00"))
        # snap to midnight UTC
        return datetime(d.year, d.month, d.day, 0, 0, 0, tzinfo=d.tzinfo)

    if payload.get("scheduledstart"):
        start_utc_midnight = _to_date_start_utc(payload["scheduledstart"])
        payload["scheduledstart"] = start_utc_midnight.isoformat().replace("+00:00", "Z")
        payload["scheduledend"] = (start_utc_midnight + timedelta(days=1)).isoformat().replace("+00:00", "Z")
        # Either set to 1440 or drop it to let DV compute
        payload["scheduleddurationminutes"] = 1440

    return payload

def build_payload_from_row(row_dict: dict):
    """
    Returns (payload_dict, deferred_lookups) where:
      - payload_dict: fields we can set directly on create (strings/ints/bools/dates/option values)
      - deferred_lookups: list of lookup instructions to set using $ref (safer), executed after create/identify record
    """
    src_index = {_norm_col(k): k for k in row_dict.keys()}
    payload, deferred = {}, []

    for src_col, spec in mapping.items():
        actual_key = src_index.get(_norm_col(src_col))
        if not actual_key:
            continue
        raw = row_dict.get(actual_key)

        if spec["type"] in ("string", "int", "float", "bool", "date"):
            tgt = spec["target_attribute"]
            if spec["type"] == "string":
                val = None if raw in (None, "") else str(raw)
            elif spec["type"] == "int":
                val = to_int(raw)
            elif spec["type"] == "float":
                val = to_float(raw)
            elif spec["type"] == "bool":
                val = to_bool(raw)
            elif spec["type"] == "date":
                val = to_iso(raw)
            if val is not None:
                payload[tgt] = val

        elif spec["type"] == "optionset":
            label_to_value, _ = get_optionset_map(ENTITY_LOGICAL, spec["target_attribute"])
            if raw not in (None, ""):
                key = str(raw).strip().lower()
                if key.isdigit() and int(key) in _.keys():
                    # value passed as numeric string
                    payload[spec["target_attribute"]] = int(key)
                else:
                    val = label_to_value.get(key)
                    if val is not None:
                        payload[spec["target_attribute"]] = val

        elif spec["type"] == "lookup":
            src_val = row_dict.get(spec.get("source_key") or src_col)
            if not src_val:
                continue
            target_id = fetch_guid_by_secondary_column(
                spec["target_entity_set"],
                spec["name_column"],
                str(src_val)
            )
            if not target_id:
                # skip if unresolved
                continue

            # Discover nav property for the lookup (attribute logical name MUST be used here)
            attr_logical = spec["target_attribute"]
            nav_prop = get_single_nav_prop(ENTITY_LOGICAL, attr_logical, spec["target_entity_logical"])

            if spec.get("method", "ref") == "ref":
                deferred.append({
                    "nav_prop": nav_prop,
                    "target_set": spec["target_entity_set"],
                    "target_id": target_id
                })
            else:
                payload[f"{nav_prop}@odata.bind"] = f"/{spec['target_entity_set']}({target_id})"

        elif spec["type"] == "partylist":
            src_val = row_dict.get(src_col)
            if not src_val:
                continue

            # map role to ptm (required=5, optional=6, organizer=7)
            role = spec["target_attribute"]
            ptm = 5 if role == "requiredattendees" else 6 if role == "optionalattendees" else 7

            # accumulate under a single nav prop for appointments
            ap_key = "appointment_activity_parties"
            if ap_key not in payload:
                payload[ap_key] = []

            for token in [t.strip() for t in str(src_val).split(",") if t.strip()]:
                payload[ap_key].append(_party_item_from_token(token, ptm))

        elif spec["type"] == "polymorphic":
            src_val = row_dict.get(spec.get("source_key") or src_col)
            if not src_val:
                continue

            cand = spec.get("candidates", [])
            type_hint_col = spec.get("type_hint_column")
            if type_hint_col and row_dict.get(type_hint_col):
                hint = str(row_dict[type_hint_col]).strip().lower()
                cand = [c for c in cand if c["entity_logical"].lower() == hint] or cand

            target_set, target_logical, target_id = resolve_polymorphic_target(src_val, cand)
            if not target_id:
                continue

            nav_prop = get_single_nav_prop(ENTITY_LOGICAL, spec["target_attribute"], target_logical)

            if spec.get("method","ref") == "ref":
                deferred.append({"nav_prop": nav_prop, "target_set": target_set, "target_id": target_id})
            else:
                payload[f"{nav_prop}@odata.bind"] = f"/{target_set}({target_id})"

        elif spec["type"] == "owner":
            src_val = row_dict.get(spec.get("source_key") or src_col)
            if not src_val:
                continue

            resolved = None
            for c in spec.get("candidates", []):
                guid = fetch_guid_by_secondary_column(c["entity_set"], c.get("column", "name"), str(src_val))
                if guid:
                    resolved = (c["entity_set"], guid)
                    break
            if not resolved:
                continue

            target_set, target_id = resolved
            if spec.get("method", "ref") == "ref":
                deferred.append({"nav_prop": "ownerid", "target_set": target_set, "target_id": target_id})
            else:
                payload["ownerid@odata.bind"] = f"/{target_set}({target_id})"

    return payload, deferred

# ==========================================================
# ---- CREATE / UPSERT / PATCH ------------------------------
# ==========================================================
def create_record(entity_set: str, payload: dict) -> str:
    r = api("POST", entity_set, data=json.dumps(payload))
    loc = r.headers.get("OData-EntityId") or r.headers.get("Location")
    if not loc:
        raise RuntimeError("Create succeeded but no OData-EntityId/Location header was returned.")
    return loc.split("(")[-1].rstrip(")")

def _patch_by_id(entity_set: str, record_id: str, payload: dict):
    api("PATCH", f"{entity_set}({record_id})", data=json.dumps(payload))

@lru_cache(maxsize=256)
def get_primary_id_attribute(entity_logical: str) -> str:
    j = api("GET", f"EntityDefinitions(LogicalName='{entity_logical}')?$select=PrimaryIdAttribute").json()
    return j["PrimaryIdAttribute"]

def _escape_literal(v: str) -> str:
    return str(v).replace("'", "''")

def _is_number_like(v) -> bool:
    try:
        float(str(v)); return True
    except:
        return False

def _build_eq_filter(attr: str, value, force_string: bool = False):
    if value is None:
        return None
    if not force_string:
        try:
            float(str(value))
            return f"{attr} eq {value}"
        except:
            pass
    # default: emit as string
    return f"{attr} eq '{_escape_literal(value)}'"

def upsert_by_unique_value(entity_set, entity_logical, unique_attr, unique_value, payload):
    primary_id = get_primary_id_attribute(entity_logical)
    f = _build_eq_filter(unique_attr, unique_value, force_string=True)  # unique is text
    j = api("GET", f"{entity_set}?$select={primary_id}&$filter={f}&$top=2").json()
    rows = j.get("value", [])

    if len(rows) == 1:
        rec_id = rows[0][primary_id]
        _patch_by_id(entity_set, rec_id, payload)
        return rec_id, False   # updated
    elif len(rows) == 0:
        rec_id = create_record(entity_set, payload)
        return rec_id, True    # created
    else:
        raise RuntimeError("Upsert ambiguity ...")


def set_deferred_lookups(entity_set: str, record_id: str, deferred: list):
    for lk in deferred:
        set_lookup_with_ref(entity_set, record_id, lk["nav_prop"], lk["target_set"], lk["target_id"])


# ==========================================================
# ---- Error Logging ---------------------------------------
# ==========================================================
def _coerce(v):
    """Turn Spark nulls/empties into clean Python values for our builder."""
    if v is None:
        return None
    s = str(v).strip()
    return None if s == "" else s

def _now_utc_stamp():
    return datetime.utcnow().strftime("%Y%m%d_%H%M%S")

def _log_success(sink, **kwargs):
    sink.append({
        "timestamp_utc": datetime.utcnow().isoformat(timespec="seconds") + "Z",
        **kwargs
    })

def _log_error(sink, **kwargs):
    # Keep error text reasonably sized for CSV
    msg = kwargs.get("error", "")
    kwargs["error"] = (msg if len(str(msg)) <= 4000 else str(msg)[:4000])
    sink.append({
        "timestamp_utc": datetime.utcnow().isoformat(timespec="seconds") + "Z",
        **kwargs
    })

def extract_timestamp(path):
    match = re.search(r'(\d{8,})', path)
    return int(match.group(1)) if match else 0


# test_row = {
#     "Appointment ID": "SRC-12345", # string
#     "Subject": "Follow-up Visit", # string
#     "Appointment Type": "Visit - Post-Collection", # optionset  
#     "Visit Type": "Curbside", # optionset
#     "Visit Reason": "Audit", # lookup to cm_visitreasonmaster
#     "Location": "Main Office",   # string
#     "Required Attendees": "Tyler.Vogel@lvs1.com, jonathan.miller@lvs1.com", # partylist
#     "Optional Attendees": "raju.penumetcha@lvs1.com, timb@example.com", # partylist
#     "Organizer": "Rojan.Thomas@lvs1.com", # partylist
#     "Regarding": "163771",  # Lookup to case
#     "Description": "Kickoff meeting with client", # string
#     "Owner": "jonathan.miller@lvs1.com",    # lookup to systemuser
#     "Status": "Completed", # optionset
#     "Status Reason": "Completed", # optionset
#     "Priority": "High", # optionset   
#     "Scheduled Start": "2018-08-29T15:30:00.000Z",  # date
#     "Scheduled End": "2018-08-30T23:00:00.000Z", # date
#     "Actual Start": "2018-08-29T15:30:00.000Z",
#     "Actual End": "2018-08-30T23:00:00.000Z",
#     "Duration": "1890", # int
#     "Is All Day Event": "false", # bool
#     "Created On": "2025-09-19 14:30:00",   # date  
#     "Created By": "Rojan.Thomas@lvs1.com",  # lookup to systemuser
#     "Object ID": "00163E532B501ED8AAF3BBA93453FB88" # string
# }
# payload, deferred = build_payload_from_row(test_row)
# print("=== Payload (directly settable fields) ===")
# print(json.dumps(payload, indent=2))

# print("\n=== Deferred lookups (to set with $ref after create) ===")
# print(json.dumps(deferred, indent=2))

# unique_attr = "cm_objectid"           # DV logical name for your column
# unique_val  = test_row["Object ID"]   # CSV column value

# rec_id, created = upsert_by_unique_value(
#     ENTITY_SET,
#     ENTITY_LOGICAL,
#     unique_attr,
#     unique_val,
#     payload
# ) 

ENTITY_SET      = "appointments"
ENTITY_LOGICAL  = "appointment"
DV_UNIQUE_ATTR  = "cm_objectid"     # Dataverse logical attr
SRC_UNIQUE_COL  = "Object ID"       # Source column name

DROP_STATE_STATUS_ON_CREATE = True

current_timestamp_str = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")

# List all CSV files
files = dbutils.fs.ls(
    "abfss://bronze@stcrmdevprodcm01.dfs.core.windows.net/incoming/mapping/visit"
)
csv_files = [f.path for f in files if f.path.endswith(".csv")]
csv_files_sorted = sorted(csv_files, key=extract_timestamp)

for file_path in csv_files_sorted:
    file_name = os.path.basename(file_path)

    print(f"\n=== Processing: {file_path} ===")

    df = spark.read.format("csv") \
        .option("header", "true") \
        .option("multiLine", "true") \
        .option("escape", "\"") \
        .load(file_path)

    # Drop irrelevant columns
    if "Appointment ID" in df.columns:
        df = df.drop("Appointment ID")
    if "Created By" in df.columns:
        df = df.drop("Created By")

    # ---- Base error frame: rows missing Object ID (standardize on cm_objectid1) ----
    if SRC_UNIQUE_COL in df.columns:
        error_df = df.filter((col(SRC_UNIQUE_COL).isNull()) | (col(SRC_UNIQUE_COL) == "")) \
            .select(col(SRC_UNIQUE_COL).alias("cm_objectid1"))
    else:
        # placeholder with correct schema
        error_df = spark.createDataFrame([], "cm_objectid1 string")

    error_df = error_df.withColumn(
        "Message", lit(f"{SRC_UNIQUE_COL} is blank from source")
    ).withColumn(
        "cm_objectid_Timestamp", concat(col("cm_objectid1"), lit("_"), lit(current_timestamp_str))
    ).withColumn(
        "Timestamp", lit(current_timestamp_str)
    )

    # ---- Work set: rows that DO have Object ID ----
    df_filtered = df.filter(~((col(SRC_UNIQUE_COL).isNull()) | (col(SRC_UNIQUE_COL) == "")))

    new_error_list = []   # collects Row(...) objects with message per-row
    success_count  = 0
    processed      = 0

    # Iterate each row (driver-side, like your coworker’s script)
    for row in df_filtered.toLocalIterator():
        processed += 1
        src = {k: _coerce(v) for k, v in row.asDict(recursive=True).items()}

        object_id = src.get(SRC_UNIQUE_COL)
        subject   = src.get("Subject")

        try:
            # 1) Build payload + deferred per your working test pattern
            payload, deferred = build_payload_from_row(src)
            payload = _normalize_all_day(payload) # Dynamics requires all-day appointments to have date-only values

            print(json.dumps(payload, indent=2))
            continue

            # Optional: avoid state/status on create
            # if DROP_STATE_STATUS_ON_CREATE:
            #     payload.pop("statecode", None)
            #     payload.pop("statuscode", None)

            # 2) Upsert by unique cm_objectid (no alt-keys)
            rec_id, created = upsert_by_unique_value(
                ENTITY_SET, ENTITY_LOGICAL, DV_UNIQUE_ATTR, object_id, payload
            )

            # 3) Apply deferred lookups via $ref (owner, visit reason, etc.)
            if deferred:
                try:
                    set_deferred_lookups(ENTITY_SET, rec_id, deferred)
                except Exception as e_ref:
                    # Partial failure: record exists; log the $ref issue but keep going
                    new_error_list.append(Row(
                        cm_objectid1=object_id or "",
                        Message=f"deferred_lookups: {str(e_ref)[:4000]}",
                        cm_objectid_Timestamp=f"{(object_id or 'noid')}_{current_timestamp_str}",
                        Timestamp=current_timestamp_str
                    ))

            success_count += 1

        except Exception as e:
            # Build or upsert error; log like phone script
            msg = str(e)
            if len(msg) > 4000:
                msg = msg[:4000]
            new_error_list.append(Row(
                cm_objectid1=object_id or "",  # <-- use cm_objectid1 consistently
                Message=msg,
                cm_objectid_Timestamp=f"{(object_id or 'noid')}_{current_timestamp_str}",
                Timestamp=current_timestamp_str
            ))

        if processed % 100 == 0:
            print(f"  processed {processed} rows... (ok={success_count}, err={len(new_error_list)})")

    # Add file metadata columns like the phone script
    df = df.withColumn("file_name", lit(file_name))

    # ---- Union row-level errors into error_df (schemas align now) ----
    if new_error_list:
        new_error_df = spark.createDataFrame(new_error_list)
        error_df = error_df.unionByName(new_error_df, allowMissingColumns=True)

    # ---- Build df_log: join column-to-column (no string literal) ----
    if df.count() > 0:
        if error_df.count() > 0:
            # Project the error columns explicitly and rename the key
            err_df_for_join = error_df.select(
                col("cm_objectid1").alias("err_cm_objectid1"),
                "Message",
                "cm_objectid_Timestamp",
                "Timestamp"
            )

            df_log = df.join(
                err_df_for_join,
                df[SRC_UNIQUE_COL] == col("err_cm_objectid1"),
                how="left"
            ).drop("err_cm_objectid1")
        else:
            # no errors: add placeholders so we can fill defaults
            df_log = df.withColumn("Message", lit(None).cast("string")) \
                    .withColumn("Timestamp", lit(None).cast("string"))

        # Default "Success" where Message is null and stamp a timestamp
        df_log = df_log.withColumn(
            "Message", coalesce(col("Message"), lit("Success"))
        ).withColumn(
            "Timestamp", when(col("Timestamp").isNull(), lit(current_timestamp_str)).otherwise(col("Timestamp"))
        )

        # Reorder: Object ID, Message, Timestamp first
        desired = [SRC_UNIQUE_COL, "Message", "Timestamp"]
        ordered_cols = [c for c in desired if c in df_log.columns] + [c for c in df_log.columns if c not in desired]
        df_log = df_log.select(*ordered_cols)
    else:
        df_log = spark.createDataFrame([], schema=f"`{SRC_UNIQUE_COL}` string, Message string, Timestamp string")

    display(df)
    display(error_df)
    display(df_log)

    print(f"  -> processed={processed}, success={success_count}, errors={len(new_error_list)}")


    ts = datetime.utcnow().strftime("%Y%m%d_%H%M%S")
    base = "abfss://bronze@stcrmdevprodcm01.dfs.core.windows.net/incoming/mapping/visit/logs"
    df_log.coalesce(1).write.mode("append").option("header", "true").csv(f"{base}/visits_log_{ts}")
    error_df.coalesce(1).write.mode("append").option("header", "true").csv(f"{base}/visits_errors_{ts}")
