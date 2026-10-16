"""Writes JSON as json-glib's pretty printer does (indent 1, `"key" : value`, doubles to 17 digits), so data the
oracle wrote can be merged without rewriting every number."""
import json


def _number(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, int):
        return str(v)
    s = "%.17g" % v
    if "." not in s and "e" not in s and "inf" not in s and "nan" not in s:
        s += ".0"
    return s


def dumps(value, depth=0):
    pad = " " * (depth + 1)
    end = " " * depth
    if isinstance(value, dict):
        if not value:
            return "{}"
        items = [f"{pad}{json.dumps(k, ensure_ascii=False)} : {dumps(v, depth + 1)}" for k, v in value.items()]
        return "{\n" + ",\n".join(items) + "\n" + end + "}"
    if isinstance(value, list):
        if not value:
            return "[]"
        return "[\n" + ",\n".join(pad + dumps(v, depth + 1) for v in value) + "\n" + end + "]"
    if isinstance(value, str):
        return json.dumps(value, ensure_ascii=False)
    if value is None:
        return "null"
    return _number(value)
